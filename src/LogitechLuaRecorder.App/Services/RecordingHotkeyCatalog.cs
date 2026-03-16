using LogitechLuaRecorder.App.Models;

namespace LogitechLuaRecorder.App.Services;

public static class RecordingHotkeyCatalog
{
    private static readonly IReadOnlyList<HotkeyOption> SupportedHotkeys = BuildSupportedHotkeys();

    public static IReadOnlyList<HotkeyOption> GetSupportedHotkeys()
    {
        return SupportedHotkeys;
    }

    public static string GetDisplayName(int virtualKey)
    {
        return SupportedHotkeys.FirstOrDefault(x => x.VirtualKey == virtualKey)?.DisplayName ?? "F12";
    }

    public static int NormalizeOrDefault(int virtualKey)
    {
        return SupportedHotkeys.Any(x => x.VirtualKey == virtualKey) ? virtualKey : 0x7B;
    }

    private static IReadOnlyList<HotkeyOption> BuildSupportedHotkeys()
    {
        var result = new List<HotkeyOption>();
        for (var virtualKey = 0x70; virtualKey <= 0x7B; virtualKey++)
        {
            result.Add(new HotkeyOption(virtualKey, $"F{virtualKey - 0x6F}"));
        }

        return result;
    }
}
