namespace OnoSoft.Shared.Theme;

public enum ThemeMode
{
    Dark,
    Light
}

/// <summary>
/// ユーザーがカスタマイズできる見た目の設定。JsonSettingsStore でアプリごとに永続化する想定。
/// </summary>
public class AppearanceSettings
{
    public ThemeMode Mode { get; set; } = ThemeMode.Dark;

    /// <summary>アクセントカラー(#RRGGBB)。ボタンやハイライトに使う。</summary>
    public string AccentColor { get; set; } = BrandColors.Accent;

    /// <summary>一覧・本文テキストの基準フォントサイズ(px)。</summary>
    public double FontSize { get; set; } = 13;
}
