@echo off
setlocal

rem Run from a Visual Studio Developer Command Prompt so cl.exe is available.
set "SCRIPT_DIR=%~dp0"
set "OUTPUT_DIR=%SCRIPT_DIR%build\windows-x64"
set "SOURCE_FILE=%SCRIPT_DIR%energy_conversion.c"

where cl.exe >nul 2>nul
if errorlevel 1 (
    echo Error: cl.exe was not found. Run this script from a Visual Studio Developer Command Prompt.
    exit /b 1
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if errorlevel 1 (
    echo Error: could not create output directory "%OUTPUT_DIR%".
    exit /b 1
)

cl.exe /nologo /LD /O2 "%SOURCE_FILE%" /link /OUT:"%OUTPUT_DIR%\pda_energy.dll"
if errorlevel 1 exit /b %errorlevel%

echo Built "%OUTPUT_DIR%\pda_energy.dll"
endlocal
