# クリップボード履歴マネージャー

Windows タスクトレイに常駐し、コピーしたテキスト・画像の履歴を記録して
いつでも呼び出せる C# / .NET 8 (WPF) 製のデスクトップアプリです。

「[おのソフト](../../README.md)」シリーズの第1弾で、共通基盤 [`OnoSoft.Shared`](../OnoSoft.Shared) を使っています。

## 主な機能

- クリップボードのテキスト・画像コピーを自動で履歴に記録(SQLiteに永続化、アプリ再起動後も保持)
- `Ctrl + Shift` **だけをタップしてすぐ離す**と、履歴ポップアップが「常に表示」状態で開く
  (他のウィンドウをクリックしても閉じない)。もう一度同じジェスチャーをすると閉じる。
  複数の項目を落ち着いて削除・整理したいときに使う
- **`Ctrl + Shift` を押したまま `V` を連打**すると、Alt+Tabのように候補が切り替わり、
  離した瞬間にその候補が貼り付けられる(今までどおり)
- ポップアップ内で検索(テキストの部分一致)
- ポップアップ右上の **「常に表示」ボタン**(📍→📌)でも同じことがマウスでできる
  (閉じるにはもう一度ボタンを押すか `Esc` キー)
- 履歴項目のピン留め(📍/📌) — ピン留めした項目は自動削除の対象外
- 履歴項目の削除、まとめてクリア(ピン留めは残る)
- 項目をダブルクリック / Enter で、直前にフォーカスしていたアプリへコピー&自動貼り付け
- タスクトレイアイコンの右クリックメニューから「履歴を表示」「履歴をクリア」「設定」「終了」
- 直近 200 件(非ピン留め)を超えると古い順に自動削除
- **設定画面**でテーマ(ダーク/ライト)・アクセントカラー・文字サイズを変更可能。
  トレイメニュー→「設定」、またはポップアップ右上の **⚙ボタン** からすぐ開ける
- 起動時に自動でアップデートを確認し、新しいバージョンがあればタスクトレイに通知

## インストール・アンインストール

[Releases](https://github.com/yusei-onodera-ac/OnoSoft/releases) から
`ClipboardHistoryManager-Setup-x.x.x.exe` をダウンロードして実行してください。

- 管理者権限は不要(ユーザーのローカルフォルダにインストールされます)
- Windowsの「設定 → アプリ → インストールされているアプリ」(または「アプリと機能」)から
  通常のアプリと同じようにアンインストールできます
- アンインストール時、履歴・設定データも削除するか確認されます
- 同じインストーラーを新しいバージョンで実行すると、上書き更新されます(アップデートも同じ手順)

ポータブル版(zipを展開してそのまま実行)も同じReleaseページに `-win-x64.zip` として置いてあります。

## 動作環境(開発時)

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

インストーラーをローカルでビルドしたい場合は [`installer/ClipboardHistoryManager.iss`](../../installer/ClipboardHistoryManager.iss)
を参照(Inno Setup 6が必要)。手順は [docs/RELEASE_PROCESS.md](../../docs/RELEASE_PROCESS.md) にまとめています。

## データの保存場所

- 履歴: `%APPDATA%\ClipboardHistoryManager\history.db` (SQLite)
- 設定: `%APPDATA%\OnoSoft\ClipboardHistoryManager\settings.json`

## プロジェクト構成

```
ClipboardHistoryManager/
├─ App.xaml / App.xaml.cs        起動処理・各サービスの配線・テーマ適用・更新チェック
├─ Models/
│  ├─ ClipboardEntry.cs          履歴1件分のデータモデル
│  └─ ClipboardManagerSettings.cs 永続設定(見た目設定を内包)
├─ Services/
│  ├─ ClipboardMonitor.cs        クリップボード監視・重複除去・履歴登録
│  └─ HistoryStore.cs            SQLite への永続化 (CRUD・ピン留め・自動トリム)
├─ ViewModels/ClipboardEntryViewModel.cs  ポップアップ表示用のバインディングラッパー
└─ Views/
   ├─ HistoryPopup.xaml(.cs)     履歴ポップアップ UI(候補切り替えモードも含む)
   └─ SettingsWindow.xaml(.cs)   設定画面(テーマ・アクセントカラー・文字サイズ・更新確認)
```

タスクトレイアイコン・グローバルホットキー受信・クリップボード変更通知の
ウィンドウ・Win32 API 宣言・テーマ適用・更新チェックは、シリーズ共通の
[`OnoSoft.Shared`](../OnoSoft.Shared) に切り出されています
(`TrayIconService`, `BackgroundMessageWindow`, `NativeMethods`, `LowLevelKeyboardHook`, `ThemeApplier`, `UpdateChecker`)。
`Ctrl+Shift`だけのタップ検知や候補選択中のキー監視には、キー入力を一切ブロックしない
低レベルキーボードフック(`LowLevelKeyboardHook`)を使っている(`RegisterHotKey`だけでは
「修飾キーだけ押して他のキーに触れず離す」ジェスチャーを検知できないため)。

## 既知の制限・今後の拡張余地

- ホットキーの組み合わせ自体(Ctrl+Shift+V)は固定(設定画面での変更は未実装)
- コピー元アプリへの自動貼り付けは `SendKeys` による `Ctrl+V` シミュレートのため、
  貼り付けを独自ショートカットに変更しているアプリでは動作しない場合があります
- Windows スタートアップへの自動起動はインストーラーのオプション(チェックボックス)で対応可能
- リッチテキスト/ファイルパスなど、テキスト・画像以外のクリップボード形式は非対応
