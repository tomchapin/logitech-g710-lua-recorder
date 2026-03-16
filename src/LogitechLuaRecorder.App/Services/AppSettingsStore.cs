using System.IO;
using System.Text.Json;
using LogitechLuaRecorder.App.Models;

namespace LogitechLuaRecorder.App.Services;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    public AppSettingsStore()
    {
        var appDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LogitechLuaRecorder");
        _settingsPath = Path.Combine(appDirectory, "settings.json");
    }

    public AppSettingsData Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettingsData();
            }

            var text = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettingsData>(text, JsonOptions) ?? new AppSettingsData();
            settings.ToggleHotkeyVirtualKey = RecordingHotkeyCatalog.NormalizeOrDefault(settings.ToggleHotkeyVirtualKey);
            return settings;
        }
        catch
        {
            return new AppSettingsData();
        }
    }

    public void Save(AppSettingsData settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.ToggleHotkeyVirtualKey = RecordingHotkeyCatalog.NormalizeOrDefault(settings.ToggleHotkeyVirtualKey);
        var directory = Path.GetDirectoryName(_settingsPath) ?? throw new InvalidOperationException("Invalid settings path.");
        Directory.CreateDirectory(directory);
        var text = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, text);
    }
}
