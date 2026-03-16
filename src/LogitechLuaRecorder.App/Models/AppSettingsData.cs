namespace LogitechLuaRecorder.App.Models;

public sealed class AppSettingsData
{
    public int ToggleHotkeyVirtualKey { get; set; } = 0x7B;
}

public sealed record HotkeyOption(int VirtualKey, string DisplayName);
