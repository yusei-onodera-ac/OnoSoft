using System;
using System.IO;
using System.Text.Json;

namespace OnoSoft.Shared.Settings;

/// <summary>
/// Loads/saves a POCO as JSON under %AppData%\OnoSoft\&lt;appName&gt;\settings.json.
/// Every OnoSoft app can define its own settings class (window position, hotkey,
/// user preferences, ...) and get persistence for free.
/// </summary>
public class JsonSettingsStore<T> where T : new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonSettingsStore(string appName, string fileName = "settings.json")
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OnoSoft", appName);
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, fileName);
    }

    public T Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new T();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings file — fall back to defaults rather than crash.
            return new T();
        }
    }

    public void Save(T settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
