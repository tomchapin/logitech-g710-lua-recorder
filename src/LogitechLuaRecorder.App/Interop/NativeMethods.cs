using System.Runtime.InteropServices;

namespace LogitechLuaRecorder.App.Interop;

internal static class NativeMethods
{
    internal const int WM_INPUT = 0x00FF;
    internal const uint RID_INPUT = 0x10000003;
    internal const uint RIM_TYPEMOUSE = 0;
    internal const uint RIM_TYPEKEYBOARD = 1;
    internal const uint RIDEV_INPUTSINK = 0x00000100;

    internal const ushort RI_KEY_BREAK = 0x0001;
    internal const ushort RI_KEY_E0 = 0x0002;
    internal const ushort RI_KEY_E1 = 0x0004;

    internal const ushort RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
    internal const ushort RI_MOUSE_LEFT_BUTTON_UP = 0x0002;
    internal const ushort RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;
    internal const ushort RI_MOUSE_RIGHT_BUTTON_UP = 0x0008;
    internal const ushort RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010;
    internal const ushort RI_MOUSE_MIDDLE_BUTTON_UP = 0x0020;
    internal const ushort RI_MOUSE_BUTTON_4_DOWN = 0x0040;
    internal const ushort RI_MOUSE_BUTTON_4_UP = 0x0080;
    internal const ushort RI_MOUSE_BUTTON_5_DOWN = 0x0100;
    internal const ushort RI_MOUSE_BUTTON_5_UP = 0x0200;
    internal const ushort RI_MOUSE_WHEEL = 0x0400;

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterRawInputDevices(
        [In] RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [DllImport("User32.dll", SetLastError = true)]
    internal static extern uint GetRawInputData(
        IntPtr hRawInput,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize,
        uint cbSizeHeader);

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    internal static (int X, int Y) GetCursorPosition()
    {
        return GetCursorPos(out var point) ? (point.X, point.Y) : (0, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct RAWINPUTDATA
    {
        [FieldOffset(0)]
        public RAWMOUSE Mouse;

        [FieldOffset(0)]
        public RAWKEYBOARD Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUT
    {
        public RAWINPUTHEADER Header;
        public RAWINPUTDATA Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct RAWMOUSE
    {
        [FieldOffset(0)]
        public ushort usFlags;

        [FieldOffset(4)]
        public uint ulButtons;

        [FieldOffset(4)]
        public ushort usButtonFlags;

        [FieldOffset(6)]
        public ushort usButtonData;

        [FieldOffset(8)]
        public uint ulRawButtons;

        [FieldOffset(12)]
        public int lLastX;

        [FieldOffset(16)]
        public int lLastY;

        [FieldOffset(20)]
        public uint ulExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
