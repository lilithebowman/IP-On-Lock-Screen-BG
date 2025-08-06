@echo off
echo Uninstalling IP Lock Screen Background Service...

REM Stop the service
sc stop "IPLockScreenService"

REM Delete the service
sc delete "IPLockScreenService"

echo Service uninstallation completed.
pause
