@echo off
echo Testing IP Background Generator...
echo.

REM Build and run only the test runner
dotnet run --project TestRunner.csproj

echo.
echo Test completed.
pause
