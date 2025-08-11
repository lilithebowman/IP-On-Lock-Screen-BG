@echo off
echo Installing IP Lock Screen Background Service...

REM Build the project
dotnet publish IPLockScreenService.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

REM Stop and remove existing service if it exists
sc stop "IPLockScreenService" 2>nul
sc delete "IPLockScreenService" 2>nul

REM Install the service
sc create "IPLockScreenService" binPath="%~dp0bin\Release\net6.0-windows\win-x64\publish\IPLockScreenService.exe" start=auto DisplayName="IP Lock Screen Background Service" depend=Tcpip

REM Set service description
sc description "IPLockScreenService" "Updates Windows lock screen background with current IP configuration information"

REM Start the service
sc start "IPLockScreenService"

echo Service installation completed.
echo You can check the service status with: sc query IPLockScreenService
pause
