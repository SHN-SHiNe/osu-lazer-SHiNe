@echo off
echo ========================================
echo Building osu! Mania Player Mode
echo ========================================
echo.

cd /d "%~dp0"

echo Step 1: Building in Release mode...
dotnet build osu.Desktop -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo BUILD FAILED!
    echo ========================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Step 2: Running the game...
echo.
echo Look for [ManiaMode] logs to track the process
echo ========================================
echo.

dotnet run --project osu.Desktop -c Release

pause

