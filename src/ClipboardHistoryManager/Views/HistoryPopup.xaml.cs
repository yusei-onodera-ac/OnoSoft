using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ClipboardHistoryManager.Models;
using ClipboardHistoryManager.Services;
using ClipboardHistoryManager.ViewModels;
using OnoSoft.Shared.Native;
using Clipboard = System.Windows.Clipboard;

namespace ClipboardHistoryManager.Views;

public partial class HistoryPopup : Window
{
    private readonly HistoryStore _store;
    private readonly ClipboardMonitor _clipboardMonitor;
    private readonly ObservableCollection<ClipboardEntryViewModel> _allItems = new();
    private IntPtr _targetWindow;
    private bool _isCycling;
    private bool _keepOpen;

    /// <summary>Raised when the user presses Escape while an Alt+Tab style cycle is in progress (no paste).</summary>
    public event Action? CycleCancelled;

    /// <summary>Raised when the user clicks the header's settings (gear) button.</summary>
    public event Action? SettingsRequested;

    public HistoryPopup(HistoryStore store, ClipboardMonitor clipboardMonitor)
    {
        _store = store;
        _clipboardMonitor = clipboardMonitor;
        InitializeComponent();
    }

    /// <summary>
    /// Shows the popup near the cursor and remembers which window should receive the
    /// pasted content once the user picks an entry.
    /// </summary>
    public void PrepareAndShow(IntPtr previousForegroundWindow)
    {
        _isCycling = false;
        _keepOpen = false;
        KeepOpenButton.Content = "📍";
        KeepOpenButton.ToolTip = "常に表示(他をクリックしても閉じない)";
        _targetWindow = previousForegroundWindow;
        ReloadEntries();
        PositionNearCursor();

        Show();
        Activate();
        SearchBox.Text = string.Empty;
        SearchBox.Focus();
        Keyboard.Focus(SearchBox);
    }

    public void ToggleVisibility(IntPtr previousForegroundWindow)
    {
        if (IsVisible)
            Hide();
        else
            PrepareAndShow(previousForegroundWindow);
    }

    /// <summary>現在「常に表示」中で、実際に表示されているか。</summary>
    public bool IsPinnedOpen => _keepOpen && IsVisible;

    /// <summary>
    /// 通常の閲覧モードで開き、最初から「常に表示」を有効にする。
    /// Ctrl+Shift だけをタップして離すジェスチャーから呼ばれる。
    /// </summary>
    public void ShowKeepingOpen(IntPtr previousForegroundWindow)
    {
        PrepareAndShow(previousForegroundWindow);
        _keepOpen = true;
        KeepOpenButton.Content = "📌";
        KeepOpenButton.ToolTip = "常に表示中(もう一度押すと解除)";
    }

    /// <summary>「常に表示」を解除して閉じる。同じジェスチャーをもう一度行ったときに呼ばれる。</summary>
    public void HideAndUnpin()
    {
        _keepOpen = false;
        KeepOpenButton.Content = "📍";
        KeepOpenButton.ToolTip = "常に表示(他をクリックしても閉じない)";
        Hide();
    }

    /// <summary>
    /// Alt+Tab スタイルの候補切り替えを開始する。ポップアップを表示し、最新の項目をハイライトする。
    /// 呼び出し側(App)は物理的な Ctrl/Shift キーが離されたタイミングで <see cref="CommitCycle"/> を呼ぶ。
    /// </summary>
    public void BeginCycle(IntPtr previousForegroundWindow)
    {
        _isCycling = true;
        _targetWindow = previousForegroundWindow;
        SearchBox.Text = string.Empty;
        ReloadEntries();
        PositionNearCursor();

        // Activate() (フォーカス移動)はしない: サイクル中はキー入力の取りこぼしを
        // 減らすため最小限の処理に留める。確定時に SetForegroundWindow で元のアプリへ戻す。
        Show();

        if (HistoryList.Items.Count > 0)
            HistoryList.SelectedIndex = 0;
    }

    /// <summary>
    /// 初回表示時のWPF初期描画コスト(数百msかかることがある)を、画面外での
    /// 一度きりのShow/Hideで先に払っておく。これをしないと、初回のAlt+Tab方式の
    /// 候補切り替えでUIスレッドがブロックされ、その間のキー入力を取りこぼす。
    /// </summary>
    public void WarmUp()
    {
        var originalLeft = Left;
        var originalTop = Top;
        Left = -5000;
        Top = -5000;
        Show();
        Hide();
        Left = originalLeft;
        Top = originalTop;
    }

    /// <summary>候補切り替え中に、ハイライトを次の項目へ進める(末尾まで来たら先頭に戻る)。</summary>
    public void AdvanceCycle()
    {
        if (!_isCycling || HistoryList.Items.Count == 0) return;

        var next = (HistoryList.SelectedIndex + 1) % HistoryList.Items.Count;
        HistoryList.SelectedIndex = next;
        HistoryList.ScrollIntoView(HistoryList.SelectedItem);
    }

    /// <summary>修飾キーが離されたときに呼ばれる。ハイライト中の項目を確定し、貼り付ける。</summary>
    public void CommitCycle()
    {
        if (!_isCycling) return;
        _isCycling = false;

        if (HistoryList.SelectedItem is ClipboardEntryViewModel vm)
            _ = CopyAndPasteAsync(vm);
        else
            Hide();
    }

    private void ReloadEntries()
    {
        _allItems.Clear();
        foreach (var entry in _store.GetAll())
            _allItems.Add(new ClipboardEntryViewModel(entry));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = SearchBox?.Text?.Trim() ?? string.Empty;
        HistoryList.ItemsSource = string.IsNullOrEmpty(query)
            ? _allItems
            : _allItems.Where(i => i.IsText && i.Preview.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void PositionNearCursor()
    {
        NativeMethods.GetCursorPos(out var cursor);
        var workArea = SystemParameters.WorkArea;

        double left = cursor.X;
        double top = cursor.Y;

        if (left + Width > workArea.Right) left = workArea.Right - Width - 8;
        if (top + Height > workArea.Bottom) top = workArea.Bottom - Height - 8;
        if (left < workArea.Left) left = workArea.Left + 8;
        if (top < workArea.Top) top = workArea.Top + 8;

        Left = left;
        Top = top;
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplyFilter();

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (_isCycling)
        {
            // Alt+Tab方式の候補切り替え中は、常に表示ボタンの状態にかかわらず確実に閉じる
            _isCycling = false;
            Hide();
            return;
        }

        if (_keepOpen) return; // 「常に表示」中はフォーカスが外れても閉じない

        Hide();
    }

    private void KeepOpenButton_Click(object sender, RoutedEventArgs e)
    {
        _keepOpen = !_keepOpen;
        KeepOpenButton.Content = _keepOpen ? "📌" : "📍";
        KeepOpenButton.ToolTip = _keepOpen
            ? "常に表示中(もう一度押すと解除)"
            : "常に表示(他をクリックしても閉じない)";
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            var wasCycling = _isCycling;
            _isCycling = false;
            Hide();
            if (wasCycling) CycleCancelled?.Invoke();
        }
        else if (e.Key == Key.Enter && HistoryList.SelectedItem is ClipboardEntryViewModel vm)
        {
            _isCycling = false;
            _ = CopyAndPasteAsync(vm);
        }
    }

    private void HistoryList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (HistoryList.SelectedItem is ClipboardEntryViewModel vm)
            _ = CopyAndPasteAsync(vm);
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not FrameworkElement { DataContext: ClipboardEntryViewModel vm }) return;

        _store.TogglePin(vm.Id);
        vm.IsPinned = !vm.IsPinned;
        ReloadEntries();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not FrameworkElement { DataContext: ClipboardEntryViewModel vm }) return;

        _store.Delete(vm.Id);
        ReloadEntries();
    }

    private async Task CopyAndPasteAsync(ClipboardEntryViewModel vm)
    {
        Hide();

        try
        {
            // これから行う Clipboard.Set* は自分自身の書き込みなので、
            // それをまた新しい履歴として記録してしまわないよう1回だけ無視させる。
            _clipboardMonitor.SuppressNextChange();

            if (vm.IsImage && vm.Model.ImageData is { Length: > 0 } bytes)
            {
                var image = new BitmapImage();
                using (var stream = new MemoryStream(bytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                }
                Clipboard.SetImage(image);
            }
            else if (vm.IsText)
            {
                Clipboard.SetText(vm.Model.TextContent ?? string.Empty);
            }
        }
        catch (Exception)
        {
            // Clipboard write can transiently fail if another process holds the lock.
            return;
        }

        if (_targetWindow != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(_targetWindow);
            await Task.Delay(120);
            System.Windows.Forms.SendKeys.SendWait("^v");
        }
    }
}
