using System;

namespace ClipboardHistoryManager.Models;

public enum ClipboardEntryType
{
    Text,
    Image
}

public class ClipboardEntry
{
    public long Id { get; set; }
    public ClipboardEntryType Type { get; set; }
    public string? TextContent { get; set; }
    public byte[]? ImageData { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPinned { get; set; }

    /// <summary>Short single-line preview used for the list and for duplicate comparison.</summary>
    public string Preview
    {
        get
        {
            if (Type == ClipboardEntryType.Image)
                return "[画像]";

            var text = TextContent ?? string.Empty;
            var singleLine = text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
            return singleLine.Length > 120 ? singleLine[..120] + "…" : singleLine;
        }
    }
}
