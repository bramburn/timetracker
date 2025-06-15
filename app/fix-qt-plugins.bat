@echo off
REM =============================================================================
REM Qt Platform Plugin Fix Script (Batch Version)
REM =============================================================================
REM Simple batch script to fix Qt platform plugin issues

echo Qt Platform Plugin Fix Script
echo ===============================

set QT_PATH=C:\Qt\6.9.0\msvc2022_64
set BUILD_CONFIG=Debug
set APP_PATH=%~dp0build\bin\%BUILD_CONFIG%

echo.
echo Checking Qt installation...
if not exist "%QT_PATH%" (
    echo ERROR: Qt installation not found at %QT_PATH%
    pause
    exit /b 1
)

echo.
echo Checking application build...
if not exist "%APP_PATH%\TimeTrackerApp.exe" (
    echo ERROR: Application not found at %APP_PATH%\TimeTrackerApp.exe
    echo Please build the application first using: cmake --build build --config %BUILD_CONFIG%
    pause
    exit /b 1
)

echo.
echo Creating platforms directory...
if not exist "%APP_PATH%\platforms" (
    mkdir "%APP_PATH%\platforms"
    echo Created platforms directory
) else (
    echo Platforms directory already exists
)

echo.
echo Using windeployqt for proper Qt deployment...
if exist "%APP_PATH%\TimeTrackerApp.exe" (
    "%QT_PATH%\bin\windeployqt.exe" --debug --compiler-runtime --force "%APP_PATH%\TimeTrackerApp.exe"
    if %errorlevel% equ 0 (
        echo Successfully deployed Qt libraries and plugins
    ) else (
        echo WARNING: windeployqt failed, falling back to manual copy
        goto MANUAL_COPY
    )
) else (
    echo WARNING: TimeTrackerApp.exe not found, using manual copy
    goto MANUAL_COPY
)
goto SKIP_MANUAL

:MANUAL_COPY
echo.
echo Copying Qt platform plugins manually...
copy "%QT_PATH%\plugins\platforms\qwindows.dll" "%APP_PATH%\platforms\" >nul 2>&1
copy "%QT_PATH%\plugins\platforms\qwindowsd.dll" "%APP_PATH%\platforms\" >nul 2>&1
copy "%QT_PATH%\plugins\platforms\qminimal.dll" "%APP_PATH%\platforms\" >nul 2>&1
copy "%QT_PATH%\plugins\platforms\qoffscreen.dll" "%APP_PATH%\platforms\" >nul 2>&1
echo Copied platform plugins (both debug and release versions)

:SKIP_MANUAL

echo.
echo Clearing potentially conflicting environment variables...
set QT_QPA_PLATFORM_PLUGIN_PATH=
set QT_PLUGIN_PATH=
set QT_QPA_PLATFORM=
set QT_AUTO_SCREEN_SCALE_FACTOR=
set QT_SCALE_FACTOR=
set QT_DEBUG_PLUGINS=
set QT_LOGGING_RULES=

echo.
echo Testing application with clean environment...
cd /d "%APP_PATH%"

echo Starting TimeTrackerApp.exe...
echo If you see a Qt platform plugin error, try running as Administrator.
echo.

REM Try to start the application
TimeTrackerApp.exe

if %errorlevel% neq 0 (
    echo.
    echo Application failed to start. Trying with debug output...
    set QT_DEBUG_PLUGINS=1
    set QT_LOGGING_RULES=qt.qpa.plugin.debug=true
    TimeTrackerApp.exe
)

echo.
echo Fix script completed!
echo.
echo If the application still fails to start:
echo 1. Right-click this batch file and "Run as Administrator"
echo 2. Rebuild: cmake --build build --config %BUILD_CONFIG%
echo 3. Run: .\fix-qt-platform-plugins.ps1 -Clean
echo 4. Check Qt installation at: C:\Qt\6.9.0\msvc2022_64\
echo.
pause
