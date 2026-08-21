using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace OnoSoft.Shared.Theme;

/// <summary>
/// AppearanceSettings を実際の WPF ブラシ/フォントサイズ リソースに変換して
/// Application.Resources に適用する。AppTheme.xaml 側は DynamicResource で
/// 参照しているため、これを呼ぶだけで開いている画面にも即座に反映される。
/// </summary>
public static class ThemeApplier
{
    // ライトテーマの配色。ダーク側は BrandColors (シリーズ標準色) をそのまま使う。
    private const string LightBackground = "#F5F6FA";
    private const string LightPanel = "#FFFFFF";
    private const string LightText = "#1E1F26";
    private const string LightSubText = "#6B6D7A";
    private const string LightHover = "#EDEEF3";
    private const string LightBorder = "#DADCE3";

    public static void Apply(System.Windows.Application app, AppearanceSettings settings)
    {
        var isDark = settings.Mode == ThemeMode.Dark;
        var resources = app.Resources;

        resources["BgBrush"] = Brush(isDark ? BrandColors.Background : LightBackground);
        resources["PanelBrush"] = Brush(isDark ? BrandColors.Panel : LightPanel);
        resources["AccentBrush"] = Brush(settings.AccentColor);
        resources["TextBrush"] = Brush(isDark ? BrandColors.Text : LightText);
        resources["SubTextBrush"] = Brush(isDark ? BrandColors.SubText : LightSubText);
        resources["HoverBrush"] = Brush(isDark ? BrandColors.Hover : LightHover);
        resources["BorderBrush"] = Brush(isDark ? BrandColors.Border : LightBorder);
        resources["PopupFontSize"] = settings.FontSize;
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    /// <summary>設定画面のプリセットスウォッチに使えるアクセントカラー候補。</summary>
    public static readonly (string Name, string Hex)[] AccentPresets =
    {
        ("ブルー", "#5B8CFF"),
        ("パープル", "#9B6BFF"),
        ("グリーン", "#3DC48C"),
        ("オレンジ", "#FF9F5B"),
        ("ピンク", "#FF6B9B"),
        ("レッド", "#FF5B5B"),
    };
}
