# クリップボード履歴マネージャー

Windows タスクトレイに常駐し、コピーしたテキスト・画像の履歴を記録して
いつでも呼び出せる C# / .NET 8 (WPF) 製のデスクトップアプリです。

「[おのソフト](../../README.md)」シリーズの第1弾で、共通基盤 [`OnoSoft.Shared`](../OnoSoft.Shared) を使っています。

## 主な機能

- クリップボードのテキスト・画像コピーを自動で履歴に記録(SQLiteに永続化、アプリ再起動後も保持)
- `Ctrl + Shift + V` でカーソル付近に履歴ポップアップを表示/非表示
- ポップアップ内で検索(テキストの部分一致)
- 履歴項目のピン留め(📍/📌) — ピン留めした項目は自動削除の対象外
- 履歴項目の削除、まとめてクリア(ピン留めは残る)
- 項目をダブルクリック / Enter で、直前にフォーカスしていたアプリへコピー&自動貼り付け
- タスクトレイアイコンの右クリックメニューから「履歴を表示」「履歴をクリア」「終了」
- 直近 200 件(非ピン留め)を超えると古い順に自動削除

## 動作環境

- Windows
- .NET 8 SDK(ビルド時のみ必要。実行だけなら .NET 8 Desktop Runtime で可)

このマシンでは管理者権限が使えなかったため、.NET 8 SDK は `C:\dotnet` に
ユーザースコープでインストールしています(通常の `winget`/公式インストーラーで
入れた場合は不要な対応です)。

## ビルド・実行方法

付属の `run.ps1` を使うと、SDK のパス設定を気にせず操作できます。

```powershell
# ビルドのみ
.\run.ps1 build

# ビルドしてそのまま起動(バックグラウンドのタスクトレイ常駐)
.\run.ps1
```

通常の PowerShell で `dotnet` コマンドを直接使いたい場合は、事前に以下を実行してください。

```powershell
$env:DOTNET_ROOT = "C:\dotnet"
$env:PATH = "C:\dotnet;$env:PATH"
```

その後は通常どおり `dotnet build` / `dotnet run` が使えます。

## データの保存場所

`%APPDATA%\ClipboardHistoryManager\history.db` (SQLite)

## プロジェクト構成

```
ClipboardHistoryManager/
├─ App.xaml / App.xaml.cs        起動処理・各サービスの配線
├─ Models/ClipboardEntry.cs      履歴1件分のデータモデル
├─ Services/
│  ├─ ClipboardMonitor.cs        クリップボード監視・重複除去・履歴登録
│  └─ HistoryStore.cs            SQLite への永続化 (CRUD・ピン留め・自動トリム)
├─ ViewModels/ClipboardEntryViewModel.cs  ポップアップ表示用のバインディングラッパー
└─ Views/HistoryPopup.xaml(.cs)  履歴ポップアップ UI
```

タスクトレイアイコン・グローバルホットキー受信・クリップボード変更通知の
ウィンドウ・Win32 API 宣言は、シリーズ共通の [`OnoSoft.Shared`](../OnoSoft.Shared) に
切り出されています(`TrayIconService`, `BackgroundMessageWindow`, `NativeMethods`)。

## 既知の制限・今後の拡張余地

- ホットキーは `Ctrl+Shift+V` 固定(設定画面での変更は未実装)
- コピー元アプリへの自動貼り付けは `SendKeys` による `Ctrl+V` シミュレートのため、
  貼り付けを独自ショートカットに変更しているアプリでは動作しない場合があります
- Windows スタートアップへの自動登録は未実装(必要であれば
  `shell:startup` フォルダーへのショートカット追加、またはタスクスケジューラでの対応が可能です)
- リッチテキスト/ファイルパスなど、テキスト・画像以外のクリップボード形式は非対応
