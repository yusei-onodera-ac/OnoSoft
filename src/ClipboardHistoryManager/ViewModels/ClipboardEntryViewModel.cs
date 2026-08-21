using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using ClipboardHistoryManager.Models;

namespace ClipboardHistoryManager.ViewModels;

public class ClipboardEntryViewModel : INotifyPropertyChanged
{
    public ClipboardEntry Model { get; }

    public long Id => Model.Id;
    public bool IsImage => Model.Type == ClipboardEntryType.Image;
    public bool IsText => Model.Type == ClipboardEntryType.Text;
    public string Preview => Model.Preview;
    public string TimeLabel => Model.CreatedAt.ToString("MM/dd HH:mm");

    private bool _isPinned;
    public bool IsPinned
    {
        get => _isPinned;
        set { _isPinned = value; OnPropertyChanged(nameof(IsPinned)); OnPropertyChanged(nameof(PinLabel)); }
    }

    public string PinLabel => IsPinned ? "📌" : "📍";

    private BitmapImage? _thumbnail;
    public BitmapImage? Thumbnail
    {
        get
        {
            if (_thumbnail is null && Model.ImageData is { Length: > 0 })
            {
                var image = new BitmapImage();
                using var stream = new MemoryStream(Model.ImageData);
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.DecodePixelWidth = 260;
                image.EndInit();
                image.Freeze();
                _thumbnail = image;
            }
            return _thumbnail;
        }
    }

    public ClipboardEntryViewModel(ClipboardEntry model)
    {
        Model = model;
        _isPinned = model.IsPinned;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
