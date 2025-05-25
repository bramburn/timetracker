@echo off
echo Installing TimeTracker Service to run as current user...

REM Get current user information
for /f "tokens=2 delims=\" %%i in ('whoami') do set USERNAME=%%i
for /f "tokens=1 delims=\" %%i in ('whoami') do set DOMAIN=%%i
set FULLUSER=%DOMAIN%\%USERNAME%

echo Current user: %FULLUSER%
echo.

REM Stop the service if it's running
echo Stopping service if running...
sc.exe stop "TimeTracker.DesktopApp" >nul 2>&1

REM Wait a moment for the service to stop
timeout /t 3 /nobreak >nul

REM Delete the service if it exists and wait for deletion to complete
echo Removing existing service...
sc.exe delete "TimeTracker.DesktopApp" >nul 2>&1

REM Wait for Windows to complete the deletion process
echo Waiting for service deletion to complete...
:wait_for_deletion
sc.exe query "TimeTracker.DesktopApp" >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Service still exists, waiting...
    timeout /t 2 /nobreak >nul
    goto wait_for_deletion
)

echo Service deletion completed.
echo.

REM Create the service with basic configuration
echo Creating service...
sc.exe create "TimeTracker.DesktopApp" binPath= "\"C:\Program Files\TimeTracker\DesktopApp\TimeTracker.DesktopApp.exe\"" DisplayName= "Internal Employee Activity Monitor" start= auto

if %ERRORLEVEL% neq 0 (
    echo Failed to create service
    echo Error code: %ERRORLEVEL%
    pause
    exit /b 1
)

echo Service created successfully!

REM Configure the service to run as current user
echo Configuring service to run as current user: %FULLUSER%
sc.exe config "TimeTracker.DesktopApp" obj= "%FULLUSER%" password= ""

if %ERRORLEVEL% neq 0 (
    echo Warning: Failed to configure service user account (Error: %ERRORLEVEL%)
    echo This is normal if your account has a password.
    echo You'll need to configure the service account manually.
)

REM Set service description
sc.exe description "TimeTracker.DesktopApp" "Monitors employee application usage and activity for productivity insights."

REM Configure service recovery options
sc.exe failure "TimeTracker.DesktopApp" reset= 86400 actions= restart/5000/restart/10000/restart/30000

echo.
echo Service installation completed!
echo.
echo IMPORTANT: You need to configure the service account manually:
echo 1. Press Windows + R, type "services.msc" and press Enter
echo 2. Find "Internal Employee Activity Monitor" in the list
echo 3. Right-click it and select "Properties"
echo 4. Go to the "Log On" tab
echo 5. Select "This account" and enter: %FULLUSER%
echo 6. Enter your Windows password
echo 7. Click "OK"
echo 8. Right-click the service and select "Start"
echo.
echo After doing this, the service should be able to monitor your activity!
echo.

pause
