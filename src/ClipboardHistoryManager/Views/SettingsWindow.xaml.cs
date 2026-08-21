using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using OnoSoft.Shared.Theme;
using OnoSoft.Shared.Updates;
using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using RadioButton = System.Windows.Controls.RadioButton;

namespace ClipboardHistoryManager.Views;

public partial class SettingsWindow : Window
{
    private const string GitHubRepoUrl = "https://github.com/yusei-onodera-ac/OnoSoft";
    private const string GitHubOwner = "yusei-onodera-ac";
    private const string GitHubRepo = "OnoSoft";

    private readonly AppearanceSettings _original;
    private readonly AppearanceSettings _working;
    private bool _isLoaded;

    /// <summary>設定が保存されたときに呼ばれる。呼び出し側(App)でファイルへの永続化を行う。</summary>
    public event Action<AppearanceSettings>? Saved;

    public SettingsWindow(AppearanceSettings current)
    {
        InitializeComponent();

        _original = new AppearanceSettings
        {
            Mode = current.Mode,
            AccentColor = current.AccentColor,
            FontSize = current.FontSize
        };
        _working = current;

        BuildSwatches();
        LoadFromSettings();

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"現在のバージョン: {version?.ToString(3) ?? "-"}";

        _isLoaded = true;
    }

    private void BuildSwatches()
    {
        foreach (var (name, hex) in ThemeApplier.AccentPresets)
        {
            var button = new Button
            {
                Style = (Style)FindResource("SwatchButtonStyle"),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                ToolTip = name,
                Tag = hex
            };
            button.Click += (_, _) =>
            {
                CustomColorBox.Text = hex;
                ApplyAccent(hex);
            };
            SwatchPanel.Children.Add(button);
        }
    }

    private void LoadFromSettings()
    {
        (_working.Mode == ThemeMode.Dark ? DarkThemeRadio : LightThemeRadio).IsChecked = true;
        CustomColorBox.Text = _working.AccentColor;

        var radio = _working.FontSize switch
        {
            <= 12 => FontSmallRadio,
            <= 13 => FontMediumRadio,
            <= 16 => FontLargeRadio,
            _ => FontXLargeRadio
        };
        radio.IsChecked = true;
    }

    private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        _working.Mode = DarkThemeRadio.IsChecked == true ? ThemeMode.Dark : ThemeMode.Light;
        LivePreview();
    }

    private void FontSizeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        if (sender is RadioButton { Tag: string tagValue } && double.TryParse(tagValue, out var size))
        {
            _working.FontSize = size;
            LivePreview();
        }
    }

    private void CustomColorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded) return;
        ApplyAccent(CustomColorBox.Text);
    }

    private void ApplyAccent(string hex)
    {
        if (!IsValidHexColor(hex)) return;
        _working.AccentColor = hex;
        LivePreview();
    }

    private static bool IsValidHexColor(string hex)
    {
        try
        {
            _ = ColorConverter.ConvertFromString(hex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>編集中の内容をその場でアプリ全体に反映し、見た目を確認できるようにする。</summary>
    private void LivePreview() => ThemeApplier.Apply(System.Windows.Application.Current, _working);

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Text = "確認中...";

        var checker = new UpdateChecker(GitHubOwner, GitHubRepo, "ClipboardHistoryManager-SettingsCheck");
        var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        var update = await checker.CheckAsync(currentVersion);

        if (update is not null)
        {
            UpdateStatusText.Text = $"新しいバージョン {update.LatestVersion} があります。";
            var openResult = MessageBox.Show(
                $"新しいバージョン {update.LatestVersion} が公開されています。\nダウンロードページを開きますか?",
                "アップデートの確認", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (openResult == MessageBoxResult.Yes)
                OpenUrl(update.ReleaseUrl);
        }
        else
        {
            UpdateStatusText.Text = "最新バージョンです。";
        }

        CheckUpdateButton.IsEnabled = true;
    }

    private void OpenGitHubButton_Click(object sender, RoutedEventArgs e) => OpenUrl(GitHubRepoUrl);

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // ブラウザ起動に失敗しても致命的ではないので無視する
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Saved?.Invoke(_working);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // ライブプレビューで変えた分を元に戻す
        ThemeApplier.Apply(System.Windows.Application.Current, _original);
        Close();
    }
}
