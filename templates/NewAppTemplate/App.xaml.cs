using System;
using System.Windows;
using OnoSoft.Shared.Native;
using OnoSoft.Shared.Settings;
using OnoSoft.Shared.Tray;
using OnoSoft.Shared.Updates;
using MessageBox = System.Windows.MessageBox;

namespace NewApp;

// ============================================================================
// おのソフト 新規アプリ テンプレート
//
// このファイルをコピーしたら、以下を書き換えてください:
//   1. AppDisplayName / TrayGlyph / HotkeyVirtualKey をこのアプリ用に変更
//   2. OnHotkeyPressed() の中身を、実際のポップアップ/ウィンドウ表示に差し替え
//   3. 必要であれば AppSettings にこのアプリ固有の設定項目を追加
//   4. GitHub リポジトリ名が決まったら UpdateChecker のリポジトリ名を設定
// ============================================================================

public partial class App : System.Windows.Application
{
    // --- ここをアプリごとに変更 ---
    private const string AppDisplayName = "New App (おのソフト)";
    private const string TrayGlyph = "N"; // タスクトレイアイコンに表示する1文字
    private const uint HotkeyVirtualKey = 0x4E; // 'N' キー。他アプリと衝突しない組み合わせを選ぶこと
    private const string GitHubRepoName = ""; // 例: "new-app" (空のままなら更新チェックはスキップ)
    // -------------------------------

    private const int HotkeyId = 1;

    private BackgroundMessageWindow? _messageWindow;
    private TrayIconService? _trayIcon;
    private JsonSettingsStore<AppSettings>? _settingsStore;
    private AppSettings _settings = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsStore = new JsonSettingsStore<AppSettings>(appName: "NewApp");
        _settings = _settingsStore.Load();

        _messageWindow = new BackgroundMessageWindow("NewAppMessageWindow");
        _messageWindow.StartClipboardListener(); // クリップボード監視が不要なら削除してよい
        if (!_messageWindow.RegisterHotkey(HotkeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, HotkeyVirtualKey))
        {
            MessageBox.Show(
                "ホットキーが他のアプリと衝突しています。タスクトレイから操作してください。",
                AppDisplayName, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        _messageWindow.HotkeyPressed += id => { if (id == HotkeyId) OnHotkeyPressed(); };

        var icon = IconFactory.CreateGlyphIcon(TrayGlyph);
        var menuItems = new[]
        {
            new TrayMenuItem("開く", OnHotkeyPressed),
            TrayMenuItem.Separator,
            new TrayMenuItem("終了", () => Shutdown()),
        };
        _trayIcon = new TrayIconService(AppDisplayName, icon, menuItems, onDoubleClick: OnHotkeyPressed);

        if (!string.IsNullOrEmpty(GitHubRepoName))
            _ = CheckForUpdatesAsync();
    }

    private void OnHotkeyPressed()
    {
        // TODO: ここで実際のポップアップ/メインウィンドウを表示する
        MessageBox.Show("ここにアプリ本体のUIを実装します。", AppDisplayName);
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        var checker = new UpdateChecker(owner: "onodera888", repo: GitHubRepoName, userAgent: AppDisplayName);
        var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        var update = await checker.CheckAsync(currentVersion);
        if (update is not null)
        {
            _trayIcon?.ShowBalloon(
                AppDisplayName,
                $"新しいバージョン {update.LatestVersion} が公開されています。クリックして確認してください。");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _settingsStore?.Save(_settings);
        _trayIcon?.Dispose();
        _messageWindow?.Dispose();
        base.OnExit(e);
    }
}

/// <summary>このアプリ固有の永続設定。必要な項目を追加していく。</summary>
public class AppSettings
{
    public bool StartWithWindows { get; set; }
}
