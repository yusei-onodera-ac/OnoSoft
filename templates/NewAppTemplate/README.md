# 新規アプリの作り方(おのソフト シリーズ共通テンプレート)

このフォルダーは、シリーズの新しいアプリを始めるための最小構成です。
`ClipboardHistoryManager` と同じく `OnoSoft.Shared` を土台にしているので、
タスクトレイ常駐・グローバルホットキー・共通デザイン(ダークテーマ)・
設定の永続化・GitHub Releases 経由の更新チェックが最初から使えます。

## 手順

1. このフォルダーを `src/<新しいアプリ名>` にコピーする

   ```powershell
   Copy-Item -Recurse templates\NewAppTemplate src\MyNewApp
   ```

2. コピー先のファイルを次のように書き換える:
   - `NewApp.csproj` → `MyNewApp.csproj` にリネーム、`AssemblyName`/`RootNamespace`/`Product` を変更
   - `App.xaml` の `x:Class="NewApp.App"` を `MyNewApp.App` に変更
   - `App.xaml.cs` の `namespace NewApp;` を `namespace MyNewApp;` に変更
   - `App.xaml.cs` 冒頭の `AppDisplayName` / `TrayGlyph` / `HotkeyVirtualKey` / `GitHubRepoName` を設定

3. ソリューションに追加する

   ```powershell
   dotnet sln OnoSoft.sln add src\MyNewApp\MyNewApp.csproj
   ```

4. `OnHotkeyPressed()` の中身を、そのアプリ本来の機能(ポップアップ表示など)に差し替える。
   `ClipboardHistoryManager` の `Views/HistoryPopup.xaml(.cs)` が実装の参考になる
   (ダークテーマの角丸ポップアップ、検索ボックス、リスト表示のパターン)。

5. `docs/BRANDING.md` の命名規則に沿ってアプリ名・アイコングリフ・配色アクセントを決める。

6. `.github/workflows/release.yml` はリポジトリ単位で動く前提。1アプリ = 1リポジトリに
   分割して公開する場合は、このモノレポから該当アプリのフォルダーを抜き出し、
   `OnoSoft.Shared` を NuGet パッケージ化するか、サブモジュール/ファイルコピーで
   共有する運用に切り替える(詳細は `docs/RELEASE_PROCESS.md` 参照)。

## 使えるもの (OnoSoft.Shared)

| クラス | 用途 |
|---|---|
| `OnoSoft.Shared.Native.BackgroundMessageWindow` | クリップボード変更通知・グローバルホットキー受信用の非表示ウィンドウ |
| `OnoSoft.Shared.Native.NativeMethods` | Win32 P/Invoke (カーソル位置取得、フォアグラウンドウィンドウ操作など) |
| `OnoSoft.Shared.Tray.TrayIconService` | タスクトレイアイコン+右クリックメニュー |
| `OnoSoft.Shared.Tray.IconFactory` | 1文字グリフから角丸ブランドアイコンを生成 |
| `OnoSoft.Shared.Theme.AppTheme.xaml` | 共通カラーパレット・ボタンスタイル・ポップアップ枠スタイル |
| `OnoSoft.Shared.Settings.JsonSettingsStore<T>` | `%AppData%\OnoSoft\<アプリ名>\settings.json` への設定永続化 |
| `OnoSoft.Shared.Updates.UpdateChecker` | GitHub Releases の最新版チェック |
