using System;
using System.Windows;
using ClipboardHistoryManager.Services;
using ClipboardHistoryManager.Views;
using OnoSoft.Shared.Native;
using OnoSoft.Shared.Tray;
using MessageBox = System.Windows.MessageBox;

namespace ClipboardHistoryManager;

public partial class App : System.Windows.Application
{
    private const int HotkeyId = 1;
    private const uint VK_V = 0x56;

    private BackgroundMessageWindow? _messageWindow;
    private HistoryStore? _store;
    private ClipboardMonitor? _clipboardMonitor;
    private TrayIconService? _trayIcon;
    private HistoryPopup? _popup;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _store = new HistoryStore();
        _messageWindow = new BackgroundMessageWindow("ClipboardHistoryManagerMessageWindow");
        _clipboardMonitor = new ClipboardMonitor(_store, _messageWindow);
        _popup = new HistoryPopup(_store);

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
            if (id == HotkeyId) OnHotkeyPressed();
        };

        var icon = IconFactory.CreateGlyphIcon("C");
        var menuItems = new[]
        {
            new TrayMenuItem("履歴を表示 (Ctrl+Shift+V)", () => OnHotkeyPressed()),
            TrayMenuItem.Separator,
            new TrayMenuItem("履歴をクリア", OnClearHistoryRequested),
            TrayMenuItem.Separator,
            new TrayMenuItem("終了", OnExitRequested),
        };
        _trayIcon = new TrayIconService("クリップボード履歴マネージャー (おのソフト)", icon, menuItems, onDoubleClick: OnHotkeyPressed);
    }

    private void OnHotkeyPressed()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        _popup?.ToggleVisibility(foreground);
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

    private void OnExitRequested() => Shutdown();

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _messageWindow?.Dispose();
        _popup?.Close();
        base.OnExit(e);
    }
}
