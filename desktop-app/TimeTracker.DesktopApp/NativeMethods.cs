using System.Runtime.InteropServices;
using System.Text;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Contains P/Invoke declarations for Windows API functions used for window monitoring and input detection.
/// This class provides the native Windows API access needed for activity monitoring.
/// </summary>
internal static class NativeMethods
{
    // Window-related API functions
    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    internal static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // Hook-related API functions for input monitoring
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);

    // SetWinEventHook API for optimized window monitoring
    [DllImport("user32.dll")]
    internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax,
        IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
        uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    // WinEvent delegate for window change notifications
    internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    // WinEvent constants
    internal const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    // Raw Input API for optimized input monitoring
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand,
        IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    // Raw Input constants
    internal const int RIDEV_INPUTSINK = 0x00000100;
    internal const int RIDEV_REMOVE = 0x00000001;
    internal const int WM_INPUT = 0x00FF;
    internal const int RID_INPUT = 0x10000003;

    // GetLastInputInfo API for fallback input detection
    [DllImport("user32.dll")]
    internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    // Hook types
    internal const int WH_KEYBOARD_LL = 13;
    internal const int WH_MOUSE_LL = 14;

    // Hook procedure delegate
    internal delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    // Keyboard hook constants
    internal const int WM_KEYDOWN = 0x0100;
    internal const int WM_KEYUP = 0x0101;
    internal const int WM_SYSKEYDOWN = 0x0104;
    internal const int WM_SYSKEYUP = 0x0105;

    // Mouse hook constants
    internal const int WM_LBUTTONDOWN = 0x0201;
    internal const int WM_LBUTTONUP = 0x0202;
    internal const int WM_MOUSEMOVE = 0x0200;
    internal const int WM_MOUSEWHEEL = 0x020A;
    internal const int WM_RBUTTONDOWN = 0x0204;
    internal const int WM_RBUTTONUP = 0x0205;

    // Hook codes
    internal const int HC_ACTION = 0;

    /// <summary>
    /// Structure for low-level keyboard input
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        internal uint vkCode;
        internal uint scanCode;
        internal uint flags;
        internal uint time;
        internal IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Structure for low-level mouse input
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        internal POINT pt;
        internal uint mouseData;
        internal uint flags;
        internal uint time;
        internal IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Point structure for mouse coordinates
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        internal int x;
        internal int y;
    }

    /// <summary>
    /// Structure for Raw Input device registration
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTDEVICE
    {
        internal ushort UsagePage;
        internal ushort Usage;
        internal uint Flags;
        internal IntPtr Target;
    }

    /// <summary>
    /// Structure for GetLastInputInfo API
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LASTINPUTINFO
    {
        internal uint cbSize;
        internal uint dwTime;
    }

    /// <summary>
    /// Gets the process name from a process ID
    /// </summary>
    /// <param name="processId">The process ID</param>
    /// <returns>The process name or empty string if not found</returns>
    internal static string GetProcessName(uint processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the window title from a window handle
    /// </summary>
    /// <param name="hWnd">The window handle</param>
    /// <returns>The window title or empty string if not found</returns>
    internal static string GetWindowTitle(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return string.Empty;

        try
        {
            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return string.Empty;

            var builder = new StringBuilder(length + 1);
            GetWindowText(hWnd, builder, builder.Capacity);
            return builder.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the process ID associated with a window handle
    /// </summary>
    /// <param name="hWnd">The window handle</param>
    /// <returns>The process ID or 0 if not found</returns>
    internal static uint GetWindowProcessId(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return 0;

        try
        {
            GetWindowThreadProcessId(hWnd, out uint processId);
            return processId;
        }
        catch
        {
            return 0;
        }
    }
}
