using LogitechLuaRecorder.App.Infrastructure;

namespace LogitechLuaRecorder.App.Models;

public sealed class EditableMacroEvent : ObservableObject
{
    private int _sequenceNumber;
    private MacroEventKind _kind;
    private MacroEventAction _action;
    private string _keyIdentifier = string.Empty;
    private int? _keyScanCode;
    private MouseButtonKind _mouseButton = MouseButtonKind.Left;
    private int _x;
    private int _y;
    private int _wheelDelta;
    private int _delayBeforeMs;

    public int SequenceNumber
    {
        get => _sequenceNumber;
        set => SetProperty(ref _sequenceNumber, value);
    }

    public MacroEventKind Kind
    {
        get => _kind;
        set
        {
            if (SetProperty(ref _kind, value))
            {
                OnPropertyChanged(nameof(KindName));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string KindName
    {
        get => Kind.ToString();
        set
        {
            if (Enum.TryParse<MacroEventKind>(value, true, out var parsed))
            {
                Kind = parsed;
            }
        }
    }

    public MacroEventAction Action
    {
        get => _action;
        set
        {
            if (SetProperty(ref _action, value))
            {
                OnPropertyChanged(nameof(ActionName));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string ActionName
    {
        get => Action.ToString();
        set
        {
            if (Enum.TryParse<MacroEventAction>(value, true, out var parsed))
            {
                Action = parsed;
            }
        }
    }

    public string KeyIdentifier
    {
        get => _keyIdentifier;
        set
        {
            if (SetProperty(ref _keyIdentifier, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public int? KeyScanCode
    {
        get => _keyScanCode;
        set => SetProperty(ref _keyScanCode, value);
    }

    public MouseButtonKind MouseButton
    {
        get => _mouseButton;
        set
        {
            if (SetProperty(ref _mouseButton, value))
            {
                OnPropertyChanged(nameof(MouseButtonName));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string MouseButtonName
    {
        get => MouseButton.ToString();
        set
        {
            if (Enum.TryParse<MouseButtonKind>(value, true, out var parsed))
            {
                MouseButton = parsed;
            }
        }
    }

    public int X
    {
        get => _x;
        set
        {
            if (SetProperty(ref _x, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public int Y
    {
        get => _y;
        set
        {
            if (SetProperty(ref _y, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public int WheelDelta
    {
        get => _wheelDelta;
        set
        {
            if (SetProperty(ref _wheelDelta, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public int DelayBeforeMs
    {
        get => _delayBeforeMs;
        set
        {
            if (SetProperty(ref _delayBeforeMs, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string Summary =>
        Kind switch
        {
            MacroEventKind.Key => $"{Action} key {KeyIdentifier}",
            MacroEventKind.MouseButton => $"{Action} {MouseButton} @ ({X}, {Y})",
            MacroEventKind.MouseWheel => $"Wheel {WheelDelta} @ ({X}, {Y})",
            _ => "Unknown",
        };

    public MacroEventRecord ToRecord()
    {
        return Kind switch
        {
            MacroEventKind.Key => new KeyMacroEventRecord(DelayBeforeMs, KeyIdentifier.Trim(), Action == MacroEventAction.Down, KeyScanCode),
            MacroEventKind.MouseButton => new MouseButtonMacroEventRecord(DelayBeforeMs, MouseButton, Action == MacroEventAction.Down, X, Y),
            MacroEventKind.MouseWheel => new MouseWheelMacroEventRecord(DelayBeforeMs, WheelDelta, X, Y),
            _ => throw new InvalidOperationException($"Unsupported macro event kind '{Kind}'."),
        };
    }

    public static EditableMacroEvent FromRecord(MacroEventRecord record)
    {
        return record switch
        {
            KeyMacroEventRecord keyRecord => new EditableMacroEvent
            {
                Kind = MacroEventKind.Key,
                Action = keyRecord.IsKeyDown ? MacroEventAction.Down : MacroEventAction.Up,
                KeyIdentifier = keyRecord.KeyIdentifier,
                KeyScanCode = keyRecord.KeyScanCode,
                DelayBeforeMs = keyRecord.DelayBeforeMs,
            },
            MouseButtonMacroEventRecord mouseButtonRecord => new EditableMacroEvent
            {
                Kind = MacroEventKind.MouseButton,
                Action = mouseButtonRecord.IsButtonDown ? MacroEventAction.Down : MacroEventAction.Up,
                MouseButton = mouseButtonRecord.Button,
                X = mouseButtonRecord.X,
                Y = mouseButtonRecord.Y,
                DelayBeforeMs = mouseButtonRecord.DelayBeforeMs,
            },
            MouseWheelMacroEventRecord mouseWheelRecord => new EditableMacroEvent
            {
                Kind = MacroEventKind.MouseWheel,
                Action = MacroEventAction.Wheel,
                WheelDelta = mouseWheelRecord.Delta,
                X = mouseWheelRecord.X,
                Y = mouseWheelRecord.Y,
                DelayBeforeMs = mouseWheelRecord.DelayBeforeMs,
            },
            _ => throw new InvalidOperationException($"Unsupported macro event record '{record.GetType().Name}'."),
        };
    }

    public static EditableMacroEvent CreateKeyEvent(string keyIdentifier, bool isKeyDown, int delayBeforeMs, int? keyScanCode = null)
    {
        return new EditableMacroEvent
        {
            Kind = MacroEventKind.Key,
            Action = isKeyDown ? MacroEventAction.Down : MacroEventAction.Up,
            KeyIdentifier = keyIdentifier,
            KeyScanCode = keyScanCode,
            DelayBeforeMs = delayBeforeMs,
        };
    }

    public static EditableMacroEvent CreateMouseButtonEvent(MouseButtonKind button, bool isButtonDown, int x, int y, int delayBeforeMs)
    {
        return new EditableMacroEvent
        {
            Kind = MacroEventKind.MouseButton,
            Action = isButtonDown ? MacroEventAction.Down : MacroEventAction.Up,
            MouseButton = button,
            X = x,
            Y = y,
            DelayBeforeMs = delayBeforeMs,
        };
    }

    public static EditableMacroEvent CreateMouseWheelEvent(int delta, int x, int y, int delayBeforeMs)
    {
        return new EditableMacroEvent
        {
            Kind = MacroEventKind.MouseWheel,
            Action = MacroEventAction.Wheel,
            WheelDelta = delta,
            X = x,
            Y = y,
            DelayBeforeMs = delayBeforeMs,
        };
    }
}
