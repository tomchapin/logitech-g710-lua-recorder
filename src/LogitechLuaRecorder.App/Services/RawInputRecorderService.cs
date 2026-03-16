using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using LogitechLuaRecorder.App.Interop;
using LogitechLuaRecorder.App.Models;

namespace LogitechLuaRecorder.App.Services;

public sealed class RawInputRecorderService : IDisposable
{
    private const int GracePeriodMilliseconds = 400;
    private const long HotkeySuppressionMilliseconds = 600;

    private HwndSource? _source;
    private bool _isRecording;
    private bool _toggleHotkeyPressed;
    private int _toggleHotkeyVirtualKey = 0x7B;
    private Stopwatch? _stopwatch;
    private long _lastRecordedAtMilliseconds;
    private long _ignoreUntilMilliseconds;
    private long _suppressHotkeyUntilTick;

    public event EventHandler<EditableMacroEvent>? EventCaptured;
    public event EventHandler? ToggleRecordingRequested;

    public bool IsRecording => _isRecording;

    public int ToggleHotkeyVirtualKey => _toggleHotkeyVirtualKey;

    public string ToggleHotkeyDisplay => RecordingHotkeyCatalog.GetDisplayName(_toggleHotkeyVirtualKey);

    public void Attach(HwndSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        _source.AddHook(WndProc);
        RegisterRawInput(source.Handle);
    }

    public void StartRecording()
    {
        _stopwatch = Stopwatch.StartNew();
        _lastRecordedAtMilliseconds = 0;
        _ignoreUntilMilliseconds = GracePeriodMilliseconds;
        _isRecording = true;
    }

    public void StopRecording()
    {
        _isRecording = false;
        _stopwatch?.Stop();
    }

    public void SetToggleHotkey(int virtualKey)
    {
        _toggleHotkeyVirtualKey = RecordingHotkeyCatalog.NormalizeOrDefault(virtualKey);
        _toggleHotkeyPressed = false;
        _suppressHotkeyUntilTick = 0;
    }

    public void Dispose()
    {
        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_INPUT)
        {
            var capturedEvent = TryReadInputEvent(lParam);
            if (capturedEvent is not null)
            {
                EventCaptured?.Invoke(this, capturedEvent);
            }
        }

        return IntPtr.Zero;
    }

    private EditableMacroEvent? TryReadInputEvent(IntPtr rawInputHandle)
    {
        uint dwSize = 0;
        var headerSize = (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>();
        var result = NativeMethods.GetRawInputData(rawInputHandle, NativeMethods.RID_INPUT, IntPtr.Zero, ref dwSize, headerSize);
        if (result != 0 || dwSize == 0)
        {
            return null;
        }

        var buffer = Marshal.AllocHGlobal((int)dwSize);
        try
        {
            result = NativeMethods.GetRawInputData(rawInputHandle, NativeMethods.RID_INPUT, buffer, ref dwSize, headerSize);
            if (result == uint.MaxValue)
            {
                return null;
            }

            var rawInput = Marshal.PtrToStructure<NativeMethods.RAWINPUT>(buffer);
            return rawInput.Header.dwType switch
            {
                NativeMethods.RIM_TYPEKEYBOARD => ProcessKeyboard(rawInput.Data.Keyboard),
                NativeMethods.RIM_TYPEMOUSE => ProcessMouse(rawInput.Data.Mouse),
                _ => null,
            };
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private EditableMacroEvent? ProcessKeyboard(NativeMethods.RAWKEYBOARD keyboard)
    {
        var isKeyDown = (keyboard.Flags & NativeMethods.RI_KEY_BREAK) == 0;
        UpdateToggleHotkeyState(keyboard.VKey, isKeyDown);

        if (keyboard.VKey == _toggleHotkeyVirtualKey)
        {
            return null;
        }

        if (!_isRecording || _stopwatch is null)
        {
            return null;
        }

        var keyIdentifier = KeyTranslationService.ToLuaKey(keyboard.VKey);
        if (keyIdentifier is null || ShouldSuppressKeyForHotkey(keyboard.VKey))
        {
            return null;
        }

        return EditableMacroEvent.CreateKeyEvent(keyIdentifier, isKeyDown, GetDelayBeforeMs(), GetLogitechScanCode(keyboard));
    }

    private EditableMacroEvent? ProcessMouse(NativeMethods.RAWMOUSE mouse)
    {
        if (!_isRecording || _stopwatch is null)
        {
            return null;
        }

        var (x, y) = NativeMethods.GetCursorPosition();
        var delayBeforeMs = GetDelayBeforeMs();

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_LEFT_BUTTON_DOWN) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.Left, true, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_LEFT_BUTTON_UP) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.Left, false, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_RIGHT_BUTTON_DOWN) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.Right, true, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_RIGHT_BUTTON_UP) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.Right, false, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_MIDDLE_BUTTON_DOWN) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.Middle, true, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_MIDDLE_BUTTON_UP) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.Middle, false, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_BUTTON_4_DOWN) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.XButton1, true, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_BUTTON_4_UP) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.XButton1, false, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_BUTTON_5_DOWN) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.XButton2, true, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_BUTTON_5_UP) != 0)
        {
            return EditableMacroEvent.CreateMouseButtonEvent(MouseButtonKind.XButton2, false, x, y, delayBeforeMs);
        }

        if ((mouse.usButtonFlags & NativeMethods.RI_MOUSE_WHEEL) != 0)
        {
            var wheelDelta = unchecked((short)mouse.usButtonData);
            return EditableMacroEvent.CreateMouseWheelEvent(wheelDelta, x, y, delayBeforeMs);
        }

        return null;
    }

    private int GetDelayBeforeMs()
    {
        var elapsedMilliseconds = _stopwatch?.ElapsedMilliseconds ?? 0;
        if (elapsedMilliseconds < _ignoreUntilMilliseconds)
        {
            return 0;
        }

        var delay = (int)Math.Max(0, elapsedMilliseconds - Math.Max(_lastRecordedAtMilliseconds, _ignoreUntilMilliseconds));
        _lastRecordedAtMilliseconds = elapsedMilliseconds;
        return delay;
    }

    private void UpdateToggleHotkeyState(int virtualKey, bool isKeyDown)
    {
        if (virtualKey != _toggleHotkeyVirtualKey)
        {
            return;
        }

        if (isKeyDown)
        {
            if (_toggleHotkeyPressed)
            {
                return;
            }

            _toggleHotkeyPressed = true;
            _suppressHotkeyUntilTick = Environment.TickCount64 + HotkeySuppressionMilliseconds;
            ToggleRecordingRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        _toggleHotkeyPressed = false;
    }

    private bool ShouldSuppressKeyForHotkey(int virtualKey)
    {
        if (virtualKey == _toggleHotkeyVirtualKey)
        {
            return true;
        }

        return Environment.TickCount64 <= _suppressHotkeyUntilTick && virtualKey == _toggleHotkeyVirtualKey;
    }

    private static int? GetLogitechScanCode(NativeMethods.RAWKEYBOARD keyboard)
    {
        if (keyboard.MakeCode == 0)
        {
            return null;
        }

        var scanCode = keyboard.MakeCode;
        if ((keyboard.Flags & (NativeMethods.RI_KEY_E0 | NativeMethods.RI_KEY_E1)) != 0)
        {
            scanCode |= 0x100;
        }

        return scanCode;
    }

    private static void RegisterRawInput(IntPtr hwnd)
    {
        var devices = new[]
        {
            new NativeMethods.RAWINPUTDEVICE
            {
                usUsagePage = 0x01,
                usUsage = 0x02,
                dwFlags = NativeMethods.RIDEV_INPUTSINK,
                hwndTarget = hwnd,
            },
            new NativeMethods.RAWINPUTDEVICE
            {
                usUsagePage = 0x01,
                usUsage = 0x06,
                dwFlags = NativeMethods.RIDEV_INPUTSINK,
                hwndTarget = hwnd,
            },
        };

        if (!NativeMethods.RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>()))
        {
            throw new InvalidOperationException("Failed to register for raw input.");
        }
    }
}
