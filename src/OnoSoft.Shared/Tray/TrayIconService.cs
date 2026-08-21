using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OnoSoft.Shared.Tray;

/// <summary>One tray context-menu row. A null <see cref="Label"/> renders as a separator.</summary>
public sealed class TrayMenuItem
{
    public string? Label { get; }
    public Action? OnClick { get; }

    public TrayMenuItem(string label, Action onClick)
    {
        Label = label;
        OnClick = onClick;
    }

    private TrayMenuItem() { }

    public static readonly TrayMenuItem Separator = new();
}

/// <summary>
/// Owns the Windows notification-area icon and its context menu. Shared shell used
/// by every OnoSoft tray-resident app — only the icon, tooltip text, and menu items
/// differ between apps.
/// </summary>
public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIconService(string tooltipText, Icon icon, IEnumerable<TrayMenuItem> menuItems, Action? onDoubleClick = null)
    {
        var menu = new ContextMenuStrip();
        foreach (var item in menuItems)
        {
            if (item.Label is null)
                menu.Items.Add(new ToolStripSeparator());
            else
                menu.Items.Add(item.Label, null, (_, _) => item.OnClick?.Invoke());
        }

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = tooltipText.Length > 63 ? tooltipText[..63] : tooltipText, // NotifyIcon.Text limit
            ContextMenuStrip = menu
        };

        if (onDoubleClick is not null)
            _notifyIcon.DoubleClick += (_, _) => onDoubleClick();
    }

    public void ShowBalloon(string title, string text, int timeoutMs = 3000)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(timeoutMs);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
