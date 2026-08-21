using System;
using System.Windows;
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
    private const int VK_V = 0x56;
    private const string GitHubOwner = "yusei-onodera-ac";
    private const string GitHubRepo = "OnoSoft";

    private BackgroundMessageWindow? _messageWindow;
    private LowLevelKeyboardHook? _keyboardHook;
    private HistoryStore? _store;
    private ClipboardMonitor? _clipboardMonitor;
    private TrayIconService? _trayIcon;
    private HistoryPopup? _popup;
    private SettingsWindow? _settingsWindow;

    private JsonSettingsStore<ClipboardManagerSettings>? _settingsStore;
    private ClipboardManagerSettings _settings = new();

    // Ctrl+Shift の使い分け:
    //   ・Ctrl+Shift だけをタップしてすぐ離す     → 常に表示のオン/オフを切り替える
    //   ・Ctrl+Shift を押したまま V を(連打)する   → 候補選択モード。離した瞬間に確定貼り付け(従来どおり)
    //
    // RegisterHotKey は「Ctrl+Shift+V」のような特定の組み合わせでしか通知が来ず、
    // 「Ctrl+Shiftだけ押して離した(Vには触れていない)」を検知できない。
    // そのため低レベルキーボードフックで全キーの押下/解放を監視し、
    // Ctrl+Shiftを両方押している間に他のキーが押されたかどうかで判定する。
    private bool _ctrlDown;
    private bool _shiftDown;
    private bool _otherKeyPressedDuringHold;
    private bool _isCycling;

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
        _popup.SettingsRequested += OnSettingsRequested;
        // 初回表示の描画コスト(数百ms)を画面外で先に払っておく。ここで払わないと、
        // 最初のジェスチャー操作中にUIスレッドがブロックされてキー入力を取りこぼす。
        _popup.WarmUp();

        _messageWindow.StartClipboardListener();

        _keyboardHook = new LowLevelKeyboardHook();
        _keyboardHook.KeyDown += OnGlobalKeyDown;
        _keyboardHook.KeyUp += OnGlobalKeyUp;
        _keyboardHook.Start();

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

    private static bool IsControlKey(int vk) => vk is 0x11 or 0xA2 or 0xA3; // VK_CONTROL / VK_LCONTROL / VK_RCONTROL
    private static bool IsShiftKey(int vk) => vk is 0x10 or 0xA0 or 0xA1;   // VK_SHIFT / VK_LSHIFT / VK_RSHIFT

    private void OnGlobalKeyDown(int vk)
    {
        if (IsControlKey(vk)) { _ctrlDown = true; return; }
        if (IsShiftKey(vk)) { _shiftDown = true; return; }

        if (!_ctrlDown || !_shiftDown) return; // Ctrl+Shiftを両方押している間の他キーだけを見る

        _otherKeyPressedDuringHold = true;

        if (vk == VK_V)
        {
            if (!_isCycling)
            {
                _isCycling = true;
                var foreground = NativeMethods.GetForegroundWindow();
                _popup?.BeginCycle(foreground);
            }
            else
            {
                _popup?.AdvanceCycle();
            }
        }
    }

    private void OnGlobalKeyUp(int vk)
    {
        var wasCtrl = IsControlKey(vk);
        var wasShift = IsShiftKey(vk);
        if (!wasCtrl && !wasShift) return;

        var wasBothDown = _ctrlDown && _shiftDown;
        if (wasCtrl) _ctrlDown = false;
        if (wasShift) _shiftDown = false;

        // Ctrl+Shiftを両方押していた状態から、どちらか一方でも離れた瞬間だけ処理する
        if (!wasBothDown || (_ctrlDown && _shiftDown)) return;

        if (_isCycling)
        {
            _isCycling = false;
            _popup?.CommitCycle();
        }
        else if (!_otherKeyPressedDuringHold)
        {
            // Ctrl+Shiftだけをタップして離した(他のキーには一切触れていない) → 常に表示を切り替え
            TogglePinnedOpen();
        }

        _otherKeyPressedDuringHold = false;
    }

    private void TogglePinnedOpen()
    {
        if (_popup is null) return;

        if (_popup.IsPinnedOpen)
        {
            _popup.HideAndUnpin();
        }
        else
        {
            var foreground = NativeMethods.GetForegroundWindow();
            _popup.ShowKeepingOpen(foreground);
        }
    }

    /// <summary>タスクトレイからの「履歴を表示」。従来どおりの検索・クリックで選ぶ表示/非表示トグル。</summary>
    private void OnShowHistoryRequested()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        _popup?.ToggleVisibility(foreground);
    }

    private void OnCycleCancelled() => _isCycling = false;

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
        _keyboardHook?.Dispose();
        _trayIcon?.Dispose();
        _messageWindow?.Dispose();
        _popup?.Close();
        _settingsWindow?.Close();
        base.OnExit(e);
    }
}
