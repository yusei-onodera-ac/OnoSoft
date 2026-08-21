# リリース手順

このリポジトリは複数アプリをまとめて管理するモノレポ構成です
(`src/<アプリ名>/`)。1つのGitHubリポジトリの中で、アプリごとに
タグを打ち分けてリリースします。

## タグの付け方

```
<アプリのフォルダー名>-v<バージョン>
```

例: `ClipboardHistoryManager-v1.0.0`

`.github/workflows/release.yml` はこのタグ形式を見て「どのアプリを」
「どのバージョンとして」ビルドするかを自動判定します。

## 手順

1. `src/<アプリ名>/<アプリ名>.csproj` の `<Version>` を更新する
2. 変更をコミットする
3. タグを打ってプッシュする

   ```powershell
   git tag ClipboardHistoryManager-v1.0.1
   git push origin ClipboardHistoryManager-v1.0.1
   ```

4. GitHub Actions が自動で以下を行う:
   - 対象アプリを `win-x64` 向けに自己完結(self-contained)・単一exe (`PublishSingleFile`) でビルド
   - 生成物を zip にまとめる(ポータブル版)
   - `installer/<アプリ名>.iss` が存在すれば、Inno Setup でインストーラー
     (`<アプリ名>-Setup-<バージョン>.exe`)もビルドする
   - タグ名でGitHub Releaseを作成し、zip(と、あればインストーラー)を添付する
5. 完了後、GitHub Releaseページのリンクを配布サイト (`site/index.html`) に追記する

## インストーラー(アンインストール・アップデートを簡単にする)

`installer/<アプリ名>.iss` は Inno Setup のスクリプト。用意しておくと:

- 管理者権限不要でユーザーのローカルフォルダにインストールされる
- Windowsの「アプリと機能」に正式に登録され、普通にアンインストールできる
- アンインストール時に履歴・設定データを削除するか確認するダイアログが出る
- 同じ `AppId` のまま新しいバージョンのインストーラーを実行すると上書き更新される
  (=アップデートも「インストーラーを実行するだけ」で完了する)
- Windows起動時の自動起動やデスクトップアイコン作成をインストール時のチェックボックスで選べる

新しいアプリにインストーラーを追加する場合は、`installer/ClipboardHistoryManager.iss` を
コピーして `AppId`(新しいGUIDを発行する)・アプリ名・exeファイル名を書き換える。

## 配布上の注意点

- **コード署名をしていないため、初回起動時にWindows SmartScreenの警告が出ます。**
  無料の証明書は基本的にないため、当面は「詳細情報 → 実行」で回避してもらう前提になります。
  配布サイトやREADMEに一言案内を入れておくと問い合わせが減ります。
  本格的にシリーズ展開して信頼性を高めたくなったら、EV/OVコード署名証明書の導入を検討してください。
- 自己完結ビルド(`--self-contained true`)にしているため、利用者側に .NET ランタイムの
  インストールは不要です。その分、配布物のサイズは大きくなります(数十MB程度)。
  サイズを抑えたい場合はフレームワーク依存ビルドに切り替え、代わりに .NET Desktop Runtime の
  インストールを利用者に案内する運用もあります。
- `Microsoft.Data.Sqlite` のようなネイティブ依存を持つアプリは、
  `PublishSingleFile` と `SelfContained` を組み合わせても `e_sqlite3.dll` 等が
  実行ファイルと同じフォルダーに別ファイルとして出力されることがあります。
  リリース時は `dotnet publish` の出力フォルダーごと zip にすることで解決しています
  (ワークフローもそのようにしています)。

## ローカルでの手動ビルド確認

```powershell
$env:DOTNET_ROOT = "C:\dotnet"; $env:PATH = "C:\dotnet;$env:PATH"
dotnet publish src\ClipboardHistoryManager\ClipboardHistoryManager.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish\ClipboardHistoryManager
```

## インストーラーのローカルビルド確認

```powershell
# 1. 自己完結ビルドを publish/ に出力(上の手順を先に実行)
# 2. Inno Setup (winget install JRSoftware.InnoSetup) でコンパイル
cd installer
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" ClipboardHistoryManager.iss `
  /DMyAppVersion=1.1.0 /DSourceDir="..\publish\ClipboardHistoryManager"
```

セキュリティポリシーの厳しいPC(Application Control等)では、ビルドした
インストーラー自体の実行がブロックされることがある。その場合はCI
(GitHub Actions)か、ポリシーのない別PCで動作確認する。

## 新しいアプリをシリーズに追加する

`templates/NewAppTemplate/README.md` を参照。
