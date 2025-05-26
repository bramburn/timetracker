using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace WindowHookTest
{
    class Program
    {
        // Windows API declarations
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax,
            IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
            uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string? lpModuleName);

        // Delegates
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Constants
        const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        const int WH_KEYBOARD_LL = 13;
        const int WH_MOUSE_LL = 14;
        const int HC_ACTION = 0;
        const int WM_KEYDOWN = 0x0100;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_MOUSEMOVE = 0x0200;

        // Hook handles
        static IntPtr _winEventHook = IntPtr.Zero;
        static IntPtr _keyboardHook = IntPtr.Zero;
        static IntPtr _mouseHook = IntPtr.Zero;

        // Delegates to prevent garbage collection
        static WinEventDelegate _winEventProc = WinEventCallback;
        static LowLevelProc _keyboardProc = KeyboardHookCallback;
        static LowLevelProc _mouseProc = MouseHookCallback;

        static void Main(string[] args)
        {
            Console.WriteLine("Windows API Hook Test");
            Console.WriteLine("====================");
            Console.WriteLine("This will monitor window changes and input events.");
            Console.WriteLine("Press Ctrl+C to exit.");
            Console.WriteLine();

            try
            {
                // Install window event hook
                _winEventHook = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND,
                    EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    _winEventProc,
                    0,
                    0,
                    WINEVENT_OUTOFCONTEXT);

                if (_winEventHook == IntPtr.Zero)
                {
                    Console.WriteLine("ERROR: Failed to install window event hook");
                    return;
                }
                Console.WriteLine("✓ Window event hook installed successfully");

                // Install keyboard hook
                _keyboardHook = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    _keyboardProc,
                    GetModuleHandle(null),
                    0);

                if (_keyboardHook == IntPtr.Zero)
                {
                    Console.WriteLine("ERROR: Failed to install keyboard hook");
                    return;
                }
                Console.WriteLine("✓ Keyboard hook installed successfully");

                // Install mouse hook
                _mouseHook = SetWindowsHookEx(
                    WH_MOUSE_LL,
                    _mouseProc,
                    GetModuleHandle(null),
                    0);

                if (_mouseHook == IntPtr.Zero)
                {
                    Console.WriteLine("ERROR: Failed to install mouse hook");
                    return;
                }
                Console.WriteLine("✓ Mouse hook installed successfully");
                Console.WriteLine();

                // Show current window
                ShowCurrentWindow();
                Console.WriteLine();

                // Set up Ctrl+C handler
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("\nShutting down...");
                    Cleanup();
                    Environment.Exit(0);
                };

                // Message loop
                Console.WriteLine("Monitoring started. Switch between windows or use keyboard/mouse to see events...");
                Application.Run(); // This keeps the application running and processes Windows messages
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        static void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                if (eventType == EVENT_SYSTEM_FOREGROUND && hwnd != IntPtr.Zero)
                {
                    string windowTitle = GetWindowTitle(hwnd);
                    string processName = GetProcessName(hwnd);
                    
                    Console.WriteLine($"[WINDOW] {DateTime.Now:HH:mm:ss.fff} - Window Changed");
                    Console.WriteLine($"         Title: {windowTitle}");
                    Console.WriteLine($"         Process: {processName}");
                    Console.WriteLine($"         Handle: 0x{hwnd:X}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in window callback: {ex.Message}");
            }
        }

        static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= HC_ACTION && wParam == (IntPtr)WM_KEYDOWN)
                {
                    Console.WriteLine($"[KEYBOARD] {DateTime.Now:HH:mm:ss.fff} - Key pressed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in keyboard callback: {ex.Message}");
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= HC_ACTION)
                {
                    string eventType = wParam.ToInt32() switch
                    {
                        WM_LBUTTONDOWN => "Left Click",
                        WM_RBUTTONDOWN => "Right Click",
                        WM_MOUSEMOVE => "Move",
                        _ => $"Event {wParam}"
                    };

                    if (wParam.ToInt32() != WM_MOUSEMOVE) // Don't spam with mouse moves
                    {
                        Console.WriteLine($"[MOUSE] {DateTime.Now:HH:mm:ss.fff} - {eventType}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in mouse callback: {ex.Message}");
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        static string GetWindowTitle(IntPtr hwnd)
        {
            try
            {
                var sb = new StringBuilder(256);
                GetWindowText(hwnd, sb, sb.Capacity);
                return sb.ToString();
            }
            catch
            {
                return "[Unable to get title]";
            }
        }

        static string GetProcessName(IntPtr hwnd)
        {
            try
            {
                GetWindowThreadProcessId(hwnd, out uint processId);
                using var process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return "[Unable to get process]";
            }
        }

        static void ShowCurrentWindow()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    Console.WriteLine("Current active window:");
                    Console.WriteLine($"  Title: {GetWindowTitle(hwnd)}");
                    Console.WriteLine($"  Process: {GetProcessName(hwnd)}");
                    Console.WriteLine($"  Handle: 0x{hwnd:X}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR getting current window: {ex.Message}");
            }
        }

        static void Cleanup()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
                Console.WriteLine("Window event hook removed");
            }

            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
                Console.WriteLine("Keyboard hook removed");
            }

            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
                Console.WriteLine("Mouse hook removed");
            }
        }
    }
}
