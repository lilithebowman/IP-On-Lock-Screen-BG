@echo off
echo Building IP Lock Screen Background Service...
echo.

REM Clean previous builds
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"

REM Restore packages
echo Restoring NuGet packages...
dotnet restore IPLockScreenService.csproj
if %errorlevel% neq 0 (
    echo Failed to restore packages.
    pause
    exit /b 1
)

REM Build the service
echo Building service executable...
dotnet publish IPLockScreenService.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if %errorlevel% neq 0 (
    echo Failed to build service.
    pause
    exit /b 1
)

REM Build test runner
echo Building test runner...
dotnet build TestRunner.csproj -c Release
if %errorlevel% neq 0 (
    echo Warning: Failed to build test runner.
) else (
    echo Test runner built successfully.
    dotnet publish TestRunner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o bin\test
)

echo.
echo Build completed successfully!
echo.
echo Service executable: bin\Release\net8.0-windows\win-x64\publish\IPLockScreenService.exe
echo Test runner executable: bin\test\TestRunner.exe
echo.
echo To install the service, run 'install-service.bat' as administrator.
echo To test without installing, run 'test.bat' or the executable from bin\test\
echo.
pause
