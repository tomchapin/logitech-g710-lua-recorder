namespace LogitechLuaRecorder.App.Services;

public static class KeyTranslationService
{
    private static readonly Dictionary<int, string> VKeyToLuaKey = new()
    {
        [0x08] = "backspace",
        [0x09] = "tab",
        [0x0D] = "enter",
        [0x10] = "shift",
        [0x11] = "ctrl",
        [0x12] = "alt",
        [0x13] = "pause",
        [0x14] = "capslock",
        [0x1B] = "escape",
        [0x20] = "spacebar",
        [0x21] = "pageup",
        [0x22] = "pagedown",
        [0x23] = "end",
        [0x24] = "home",
        [0x25] = "left",
        [0x26] = "up",
        [0x27] = "right",
        [0x28] = "down",
        [0x2C] = "printscreen",
        [0x2D] = "insert",
        [0x2E] = "delete",
        [0x5B] = "lwin",
        [0x5C] = "rwin",
        [0x5D] = "apps",
        [0x60] = "num0",
        [0x61] = "num1",
        [0x62] = "num2",
        [0x63] = "num3",
        [0x64] = "num4",
        [0x65] = "num5",
        [0x66] = "num6",
        [0x67] = "num7",
        [0x68] = "num8",
        [0x69] = "num9",
        [0x6A] = "num*",
        [0x6B] = "num+",
        [0x6D] = "num-",
        [0x6E] = "num.",
        [0x6F] = "num/",
        [0x90] = "numlock",
        [0x91] = "scrolllock",
        [0xA0] = "lshift",
        [0xA1] = "rshift",
        [0xA2] = "lctrl",
        [0xA3] = "rctrl",
        [0xA4] = "lalt",
        [0xA5] = "ralt",
        [0xBA] = ";",
        [0xBB] = "=",
        [0xBC] = ",",
        [0xBD] = "-",
        [0xBE] = ".",
        [0xBF] = "/",
        [0xC0] = "`",
        [0xDB] = "[",
        [0xDC] = "\\",
        [0xDD] = "]",
        [0xDE] = "'",
    };

    public static string? ToLuaKey(int virtualKey)
    {
        if (virtualKey is >= 0x41 and <= 0x5A)
        {
            return ((char)virtualKey).ToString().ToLowerInvariant();
        }

        if (virtualKey is >= 0x30 and <= 0x39)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey is >= 0x70 and <= 0x87)
        {
            return $"f{virtualKey - 0x6F}";
        }

        return VKeyToLuaKey.TryGetValue(virtualKey, out var value) ? value : null;
    }
}
