@echo off
echo Testing Idle Detection Integration
echo ==================================
echo.

echo 1. Building the application...
cmake --build . --target TimeTrackerApp
if %errorlevel% neq 0 (
    echo Build failed!
    exit /b 1
)

echo.
echo 2. Running unit tests for idle components...
.\bin\Debug\TimeTrackerTests.exe --gtest_filter="IdleDetectorTest.*:IdleAnnotationDialogTest.*:ApiServiceTest.ShouldBeConstructible:ApiServiceTest.ShouldHaveUploadIdleTimeMethod"

echo.
echo 3. Build completed successfully!
echo 4. The TimeTracker application is ready for testing.
echo.
echo To test idle detection:
echo - Run the application: .\bin\Debug\TimeTrackerApp.exe
echo - Wait 5 minutes without any mouse/keyboard activity
echo - Move the mouse to trigger the idle annotation dialog
echo - Check activity_log.txt for IDLE_ANNOTATED entries
echo.
echo Note: The application runs in the system tray.
echo Right-click the tray icon to access options.
pause
