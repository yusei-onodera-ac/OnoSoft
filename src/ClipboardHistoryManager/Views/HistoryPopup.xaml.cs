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
    private readonly ObservableCollection<ClipboardEntryViewModel> _allItems = new();
    private IntPtr _targetWindow;

    public HistoryPopup(HistoryStore store)
    {
        _store = store;
        InitializeComponent();
    }

    /// <summary>
    /// Shows the popup near the cursor and remembers which window should receive the
    /// pasted content once the user picks an entry.
    /// </summary>
    public void PrepareAndShow(IntPtr previousForegroundWindow)
    {
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

    private void Window_Deactivated(object sender, EventArgs e) => Hide();

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Hide();
        else if (e.Key == Key.Enter && HistoryList.SelectedItem is ClipboardEntryViewModel vm)
            _ = CopyAndPasteAsync(vm);
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
