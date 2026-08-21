using System;
using System.Windows;
using System.Windows.Threading;
using ClipboardHistoryManager.Models;
using ClipboardHistoryManager.Services;
using ClipboardHistoryManager.Views;
using OnoSoft.Shared.Native;
using OnoSoft.Shared.Settings;
using OnoSoft.Shared.Theme;
using OnoSoft.Shared.Tray;
using OnoSoft.Shared.Updates;
using MessageBox = System.Windows.MessageBox;

namespace ClipboardHistoryManager;

public partial class App : System.Windows.Application
{
    private const int HotkeyId = 1;
    private const uint VK_V = 0x56;
    private const string GitHubOwner = "yusei-onodera-ac";
    private const string GitHubRepo = "OnoSoft";

    private BackgroundMessageWindow? _messageWindow;
    private HistoryStore? _store;
    private ClipboardMonitor? _clipboardMonitor;
    private TrayIconService? _trayIcon;
    private HistoryPopup? _popup;
    private SettingsWindow? _settingsWindow;

    private JsonSettingsStore<ClipboardManagerSettings>? _settingsStore;
    private ClipboardManagerSettings _settings = new();

    // Ctrl+Shift+V を押しっぱなしにして V を連打すると、Alt+Tab のように候補を送り、
    // 修飾キーを離した瞬間に確定して貼り付ける。
    //
    // 実装メモ: RegisterHotKey の WM_HOTKEY は、Ctrl+Shift を押したまま V を連打しても
    // 物理的なキー押下のたびにきちんと再発火する(Windowsのメッセージキューに溜まるので
    // 処理が多少遅れても取りこぼさない)。そのためVキーの「次へ進める」判定は WM_HOTKEY の
    // 再発火をそのまま使う。一方 WM_HOTKEY には対応するキーアップ通知が無いため、
    // Ctrl/Shiftが離されたか(=確定するタイミング)は GetAsyncKeyState の軽いポーリングで見る。
    private bool _isCycling;
    private DispatcherTimer? _modifierWatchTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsStore = new JsonSettingsStore<ClipboardManagerSettings>("ClipboardHistoryManager");
        _settings = _settingsStore.Load();
        ThemeApplier.Apply(this, _settings.Appearance);

        _store = new HistoryStore();
        _messageWindow = new BackgroundMessageWindow("ClipboardHistoryManagerMessageWindow");
        _clipboardMonitor = new ClipboardMonitor(_store, _messageWindow);
        _popup = new HistoryPopup(_store, _clipboardMonitor);
        _popup.CycleCancelled += OnCycleCancelled;
        // 初回表示の描画コスト(数百ms)を画面外で先に払っておく。ここで払わないと、
        // 最初のサイクル操作中にUIスレッドがブロックされてキー入力を取りこぼす。
        _popup.WarmUp();

        _messageWindow.StartClipboardListener();
        if (!_messageWindow.RegisterHotkey(HotkeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, VK_V))
        {
            MessageBox.Show(
                "ホットキー Ctrl+Shift+V は他のアプリで既に使用されています。\n" +
                "タスクトレイアイコンから履歴を開いてください。",
                "クリップボード履歴マネージャー",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        _messageWindow.HotkeyPressed += id =>
        {
            if (id == HotkeyId) OnHotkeyRepeat();
        };

        var icon = IconFactory.CreateGlyphIcon("C");
        var menuItems = new[]
        {
            new TrayMenuItem("履歴を表示 (Ctrl+Shift+V)", OnShowHistoryRequested),
            TrayMenuItem.Separator,
            new TrayMenuItem("履歴をクリア", OnClearHistoryRequested),
            TrayMenuItem.Separator,
            new TrayMenuItem("設定", OnSettingsRequested),
            TrayMenuItem.Separator,
            new TrayMenuItem("終了", OnExitRequested),
        };
        _trayIcon = new TrayIconService("クリップボード履歴マネージャー (おのソフト)", icon, menuItems, onDoubleClick: OnShowHistoryRequested);

        _ = CheckForUpdatesOnStartupAsync();
    }

    /// <summary>
    /// グローバルホットキーが発火するたびに呼ばれる。1回目はサイクル開始(ポップアップ表示+
    /// 最新項目をハイライト)、Ctrl+Shiftを押したままの2回目以降のV押下はハイライトを次の候補へ進める。
    /// </summary>
    private void OnHotkeyRepeat()
    {
        if (!_isCycling)
        {
            _isCycling = true;
            var foreground = NativeMethods.GetForegroundWindow();
            _popup?.BeginCycle(foreground);
            StartModifierWatch();
        }
        else
        {
            _popup?.AdvanceCycle();
        }
    }

    /// <summary>タスクトレイからの「履歴を表示」。従来どおりの検索・クリックで選ぶ表示/非表示トグル。</summary>
    private void OnShowHistoryRequested()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        _popup?.ToggleVisibility(foreground);
    }

    /// <summary>Ctrl または Shift が離されたかを軽くポーリングし、離れたらサイクルを確定する。</summary>
    private void StartModifierWatch()
    {
        _modifierWatchTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _modifierWatchTimer.Tick -= ModifierWatchTimer_Tick;
        _modifierWatchTimer.Tick += ModifierWatchTimer_Tick;
        _modifierWatchTimer.Start();
    }

    private void StopModifierWatch() => _modifierWatchTimer?.Stop();

    private void ModifierWatchTimer_Tick(object? sender, EventArgs e)
    {
        if (NativeMethods.IsKeyDown(NativeMethods.VK_CONTROL) && NativeMethods.IsKeyDown(NativeMethods.VK_SHIFT))
            return; // まだ押されたまま

        StopModifierWatch();
        _isCycling = false;
        _popup?.CommitCycle();
    }

    private void OnCycleCancelled()
    {
        _isCycling = false;
        StopModifierWatch();
    }

    private void OnClearHistoryRequested()
    {
        var result = MessageBox.Show(
            "ピン留めされていない履歴をすべて削除します。よろしいですか？",
            "履歴のクリア",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            _store?.ClearUnpinned();
    }

    private void OnSettingsRequested()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings.Appearance);
        _settingsWindow.Saved += appearance =>
        {
            _settings.Appearance = appearance;
            _settingsStore?.Save(_settings);
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private async System.Threading.Tasks.Task CheckForUpdatesOnStartupAsync()
    {
        // 起動直後の負荷を避けるため少し待ってから確認する
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));

        var checker = new UpdateChecker(GitHubOwner, GitHubRepo, "ClipboardHistoryManager");
        var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        var update = await checker.CheckAsync(currentVersion);

        if (update is not null)
        {
            _trayIcon?.ShowBalloon(
                "クリップボード履歴マネージャー (おのソフト)",
                $"新しいバージョン {update.LatestVersion} が利用できます。トレイメニューの「設定」からダウンロードできます。");
        }
    }

    private void OnExitRequested() => Shutdown();

    protected override void OnExit(ExitEventArgs e)
    {
        StopModifierWatch();
        _trayIcon?.Dispose();
        _messageWindow?.Dispose();
        _popup?.Close();
        _settingsWindow?.Close();
        base.OnExit(e);
    }
}
