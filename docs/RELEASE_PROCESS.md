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
   - 生成物を zip にまとめる
   - タグ名でGitHub Releaseを作成し、zipを添付する
5. 完了後、GitHub Releaseページのリンクを配布サイト (`site/index.html`) に追記する

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

## 新しいアプリをシリーズに追加する

`templates/NewAppTemplate/README.md` を参照。
