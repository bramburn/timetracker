# Windows API Hook Test

A simple C# console application to test Windows API hooks for window monitoring and input detection.

## What it does

This application demonstrates the same Windows API hooks used in your TimeTracker application:

- **SetWinEventHook**: Monitors window changes (EVENT_SYSTEM_FOREGROUND)
- **SetWindowsHookEx**: Monitors keyboard and mouse input (WH_KEYBOARD_LL, WH_MOUSE_LL)

## How to run

1. Open PowerShell in the `WindowHookTest` directory
2. Run the application:
   ```powershell
   dotnet run
   ```

## What you'll see

The application will:
1. Install the hooks and show success messages
2. Display the current active window
3. Monitor and print:
   - Window changes (when you switch between applications)
   - Keyboard key presses
   - Mouse clicks (left/right clicks, but not mouse moves to avoid spam)

## Sample output

```
Windows API Hook Test
====================
This will monitor window changes and input events.
Press Ctrl+C to exit.

✓ Window event hook installed successfully
✓ Keyboard hook installed successfully
✓ Mouse hook installed successfully

Current active window:
  Title: Windows PowerShell
  Process: WindowsTerminal
  Handle: 0x12345678

Monitoring started. Switch between windows or use keyboard/mouse to see events...

[WINDOW] 14:30:15.123 - Window Changed
         Title: Visual Studio Code
         Process: Code
         Handle: 0x87654321

[KEYBOARD] 14:30:16.456 - Key pressed
[MOUSE] 14:30:17.789 - Left Click
```

## Exit

Press `Ctrl+C` to cleanly exit and remove all hooks.

## Requirements

- .NET 8.0
- Windows (uses Windows-specific APIs)
- Must be run with appropriate permissions (some hooks may require elevated privileges)
