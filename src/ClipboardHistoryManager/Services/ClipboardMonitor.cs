using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ClipboardHistoryManager.Models;
using OnoSoft.Shared.Native;
using Clipboard = System.Windows.Clipboard;

namespace ClipboardHistoryManager.Services;

/// <summary>
/// Watches the system clipboard and records new content into the history store.
/// </summary>
public class ClipboardMonitor
{
    private readonly HistoryStore _store;
    private readonly BackgroundMessageWindow _window;

    /// <summary>Raised on the UI thread whenever a new entry has been persisted.</summary>
    public event Action? HistoryChanged;

    public ClipboardMonitor(HistoryStore store, BackgroundMessageWindow window)
    {
        _store = store;
        _window = window;
        _window.ClipboardChanged += OnClipboardChanged;
    }

    private void OnClipboardChanged()
    {
        try
        {
            CaptureCurrentClipboard();
        }
        catch (Exception)
        {
            // Clipboard can be transiently locked by other apps (e.g. during screenshot
            // tools) — a missed capture is not worth crashing the tray app over.
        }
    }

    private void CaptureCurrentClipboard()
    {
        if (!Clipboard.ContainsText() && !Clipboard.ContainsImage())
            return;

        ClipboardEntry entry;

        if (Clipboard.ContainsImage())
        {
            var image = Clipboard.GetImage();
            if (image is null) return;

            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);

            entry = new ClipboardEntry
            {
                Type = ClipboardEntryType.Image,
                ImageData = stream.ToArray(),
                CreatedAt = DateTime.Now
            };
        }
        else
        {
            var text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text)) return;

            entry = new ClipboardEntry
            {
                Type = ClipboardEntryType.Text,
                TextContent = text,
                CreatedAt = DateTime.Now
            };
        }

        if (IsDuplicateOfMostRecent(entry)) return;

        _store.AddEntry(entry);
        HistoryChanged?.Invoke();
    }

    private bool IsDuplicateOfMostRecent(ClipboardEntry entry)
    {
        var mostRecent = _store.GetMostRecentPreview();
        if (mostRecent is null) return false;

        if (entry.Type == ClipboardEntryType.Text)
            return mostRecent == "T:" + entry.TextContent;

        var bytes = entry.ImageData ?? Array.Empty<byte>();
        var signature = "I:" + bytes.Length + ":" + Convert.ToBase64String(bytes[..Math.Min(64, bytes.Length)]);
        return mostRecent == signature;
    }
}
