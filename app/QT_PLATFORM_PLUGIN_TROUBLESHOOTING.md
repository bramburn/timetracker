# Qt Platform Plugin Troubleshooting Guide

This guide helps resolve the "This application failed to start because no Qt platform plugin could be initialized" error.

## Quick Fix

### Option 1: Run the Fix Script (Recommended)
```powershell
# PowerShell (recommended)
.\fix-qt-platform-plugins.ps1

# Or with cleaning environment variables
.\fix-qt-platform-plugins.ps1 -Clean

# Batch file alternative
.\fix-qt-plugins.bat
```

### Option 2: Manual Fix
1. Navigate to your build output directory: `app/build/bin/Debug/`
2. Create a `platforms` folder if it doesn't exist
3. Copy `qwindows.dll` from `C:/Qt/6.9.0/msvc2022_64/plugins/platforms/` to the `platforms` folder
4. Run the application

## Common Causes and Solutions

### 1. Missing Platform Plugins
**Symptoms:** Application fails to start with platform plugin error
**Solution:** Ensure `qwindows.dll` is in the `platforms` subdirectory next to your executable

### 2. Conflicting Environment Variables
**Symptoms:** Application works in some environments but not others
**Solution:** Clear these environment variables:
- `QT_QPA_PLATFORM_PLUGIN_PATH`
- `QT_PLUGIN_PATH`
- `QT_QPA_PLATFORM`

### 3. Multiple Qt Installations
**Symptoms:** Inconsistent behavior across different machines
**Solution:** 
- Remove old Qt installations from PATH
- Ensure only one Qt version is in your system PATH
- Use the fix script to verify your installation

### 4. Administrator Privileges Required
**Symptoms:** Application works for some users but not others
**Solution:** Run the application as Administrator (required for Windows hooks)

### 5. Incorrect Build Configuration
**Symptoms:** Debug/Release mismatch errors
**Solution:** Ensure you're running the correct build configuration:
```bash
# For Debug build
cmake --build build --config Debug

# For Release build  
cmake --build build --config Release
```

## Debugging Steps

### 1. Enable Qt Debug Output
```powershell
$env:QT_DEBUG_PLUGINS=1
$env:QT_LOGGING_RULES="qt.qpa.plugin.debug=true"
.\TimeTrackerApp.exe
```

### 2. Check File Structure
Your application directory should look like this:
```
build/bin/Debug/
├── TimeTrackerApp.exe
├── Qt6Cored.dll
├── Qt6Guid.dll
├── Qt6Widgetsd.dll
└── platforms/
    ├── qwindows.dll
    ├── qminimal.dll
    └── qoffscreen.dll
```

### 3. Verify Qt Installation
Ensure Qt is properly installed at: `C:/Qt/6.9.0/msvc2022_64/`

### 4. Check CMake Configuration
Verify your CMakePresets.json has the correct Qt path:
```json
{
    "cacheVariables": {
        "CMAKE_PREFIX_PATH": "C:/Qt/6.9.0/msvc2022_64"
    }
}
```

## Advanced Troubleshooting

### Dependency Walker Analysis
Use Dependency Walker (depends.exe) to check for missing DLLs:
1. Download Dependency Walker
2. Open your TimeTrackerApp.exe
3. Look for missing Qt DLLs in red

### Process Monitor
Use Process Monitor to see file access attempts:
1. Download Process Monitor from Microsoft Sysinternals
2. Filter by process name: TimeTrackerApp.exe
3. Look for failed file access attempts to Qt plugins

### Event Viewer
Check Windows Event Viewer for application errors:
1. Open Event Viewer
2. Navigate to Windows Logs > Application
3. Look for errors related to TimeTrackerApp.exe

## Prevention

### 1. Automated Deployment
The CMakeLists.txt now includes improved plugin deployment:
- Copies all necessary platform plugins
- Uses CMAKE_PREFIX_PATH for reliable paths
- Includes error handling and comments

### 2. Environment Isolation
- Avoid setting global Qt environment variables
- Use virtual environments or containers when possible
- Document your Qt installation path

### 3. Testing
- Test on clean machines without Qt development tools
- Verify both Debug and Release builds
- Test with different user privilege levels

## Getting Help

If you continue to experience issues:

1. Run the diagnostic script: `.\fix-qt-platform-plugins.ps1 -Verbose`
2. Check the CMake build output for errors
3. Verify your Qt installation is complete
4. Ensure you have the correct Visual Studio runtime installed

## Related Files

- `fix-qt-platform-plugins.ps1` - Comprehensive diagnostic and fix script
- `fix-qt-plugins.bat` - Simple batch file fix
- `CMakeLists.txt` - Build configuration with plugin deployment
- `main.cpp` - Application entry point with error handling
