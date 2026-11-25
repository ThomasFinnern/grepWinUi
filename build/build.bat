@echo off
REM ---------------------------------------------------------------------------
REM Build grepWinUi without Visual Studio (free .NET SDK, "dotnet build")
REM
REM Usage:  build.bat [Debug|Release] [extra dotnet build / msbuild args]
REM   build.bat                        Debug build (default configuration)
REM   build.bat Release                Release build
REM   build.bat Release /t:Rebuild     full rebuild
REM   build.bat /t:Clean               Debug, delete build output
REM   build.bat Debug /v:normal        more detailed build log
REM Configuration is set only by the first argument, not by /p:Configuration=... or -c
REM Builds the whole solution GrepWinUi.slnx (Core, App, Tests)
REM Output: src\GrepWinUi.App\bin\<Configuration>\net10.0-windows\GrepWinUi.App.exe
REM ---------------------------------------------------------------------------
setlocal

for %%i in ("%~dp0..") do set "ROOT=%%~fi"
set "SLN=%ROOT%\GrepWinUi.slnx"

REM Keep the extra args as one string (%%1..%%9 would split /p:Name=Value at "=")
set "ARGS=%*"
set "CONFIG=Debug"
if /i "%~1"=="Debug"   (set "CONFIG=Debug"   & call set "ARGS=%%ARGS:*%~1=%%")
if /i "%~1"=="Release" (set "CONFIG=Release" & call set "ARGS=%%ARGS:*%~1=%%")

REM First argument must be a configuration or a switch
if not "%~1"=="" if /i not "%~1"=="Debug" if /i not "%~1"=="Release" (
    set "FIRST=%~1"
    call :checkSwitch || exit /b 2
)

REM Configuration must match the chosen one (and the wrapper's name)
setlocal EnableDelayedExpansion
if defined ARGS if not "!ARGS:configuration=!"=="!ARGS!" (
    echo ERROR: Do not pass /p:Configuration=..., use the first argument or build_debug.bat / build_release.bat
    exit /b 2
)
REM A second configuration name (e.g. "build_debug.bat Release") or a -c switch
if defined ARGS for %%a in (!ARGS!) do (
    if /i "%%~a"=="Debug"   set "BADCFG=%%~a"
    if /i "%%~a"=="Release" set "BADCFG=%%~a"
    if /i "%%~a"=="-c"      set "BADCFG=%%~a"
)
if defined BADCFG (
    echo ERROR: Configuration "!BADCFG!" given twice or after a switch. Configuration here: %CONFIG%
    exit /b 2
)
endlocal

REM --- Find the .NET SDK (dotnet.exe on PATH) ---
set "DOTNET="
for /f "delims=" %%i in ('where dotnet 2^>nul') do (
    if not defined DOTNET set "DOTNET=%%i"
)
if not defined DOTNET (
    echo ERROR: dotnet not found. Install the free .NET SDK 10 or newer: https://dotnet.microsoft.com/download
    exit /b 1
)

echo dotnet:        %DOTNET%
echo Solution:      %SLN%
echo Configuration: %CONFIG%
echo.

"%DOTNET%" build "%SLN%" -nologo -c %CONFIG% %ARGS%
set "RC=%ERRORLEVEL%"

echo.
if "%RC%"=="0" (
    echo BUILD OK: %ROOT%\src\GrepWinUi.App\bin\%CONFIG%\net10.0-windows\
) else (
    echo BUILD FAILED ^(exit code %RC%^)
    echo If a file is "used by another process": close the running grepWinUi window first.
)

endlocal & exit /b %RC%

:checkSwitch
if "%FIRST:~0,1%"=="/" exit /b 0
if "%FIRST:~0,1%"=="-" exit /b 0
echo ERROR: Unknown configuration "%FIRST%". Use Debug or Release.
echo Usage: build.bat [Debug^|Release] [extra dotnet build / msbuild args]
exit /b 1
