namespace OnoSoft.Shared.Theme;

/// <summary>
/// Single source of truth for the "おのソフト" (OnoSoft) look — dark UI with a blue accent.
/// Used both by WPF resource dictionaries (App.xaml) and by System.Drawing tray-icon
/// generation, so every app in the series looks like it belongs to the same family.
/// </summary>
public static class BrandColors
{
    public const string Background = "#1E1F26";
    public const string Panel = "#2A2B36";
    public const string Accent = "#5B8CFF";
    public const string Text = "#F0F1F5";
    public const string SubText = "#9A9CAC";
    public const string Hover = "#353748";
    public const string Border = "#3A3C4A";
}
