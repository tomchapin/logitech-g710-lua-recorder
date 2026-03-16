using System.Text.Json.Serialization;
using System.Windows;

namespace LogitechLuaRecorder.App.Models;

public sealed class MacroDocumentData
{
    public string Name { get; set; } = "New Macro";

    public int TriggerGKey { get; set; } = 1;

    public ScreenBoundsData CaptureBounds { get; set; } = ScreenBoundsData.FromCurrentSystem();

    public List<MacroEventRecord> Events { get; set; } = [];
}

public sealed class ScreenBoundsData
{
    public int Left { get; set; }

    public int Top { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public static ScreenBoundsData FromCurrentSystem()
    {
        return new ScreenBoundsData
        {
            Left = (int)SystemParameters.VirtualScreenLeft,
            Top = (int)SystemParameters.VirtualScreenTop,
            Width = Math.Max(1, (int)SystemParameters.VirtualScreenWidth),
            Height = Math.Max(1, (int)SystemParameters.VirtualScreenHeight),
        };
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(KeyMacroEventRecord), "key")]
[JsonDerivedType(typeof(MouseButtonMacroEventRecord), "mouse-button")]
[JsonDerivedType(typeof(MouseWheelMacroEventRecord), "mouse-wheel")]
public abstract record MacroEventRecord(int DelayBeforeMs);

public sealed record KeyMacroEventRecord(int DelayBeforeMs, string KeyIdentifier, bool IsKeyDown, int? KeyScanCode = null) : MacroEventRecord(DelayBeforeMs);

public sealed record MouseButtonMacroEventRecord(int DelayBeforeMs, MouseButtonKind Button, bool IsButtonDown, int X, int Y) : MacroEventRecord(DelayBeforeMs);

public sealed record MouseWheelMacroEventRecord(int DelayBeforeMs, int Delta, int X, int Y) : MacroEventRecord(DelayBeforeMs);

public enum MacroEventKind
{
    Key,
    MouseButton,
    MouseWheel,
}

public enum MacroEventAction
{
    Down,
    Up,
    Wheel,
}

public enum MouseButtonKind
{
    Left = 1,
    Right = 2,
    Middle = 3,
    XButton1 = 4,
    XButton2 = 5,
}
