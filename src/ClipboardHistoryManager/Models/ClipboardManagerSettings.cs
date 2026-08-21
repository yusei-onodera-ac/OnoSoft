using OnoSoft.Shared.Theme;

namespace ClipboardHistoryManager.Models;

/// <summary>このアプリの永続設定。JsonSettingsStore で %AppData%\OnoSoft\ClipboardHistoryManager\settings.json に保存する。</summary>
public class ClipboardManagerSettings
{
    public AppearanceSettings Appearance { get; set; } = new();
}
