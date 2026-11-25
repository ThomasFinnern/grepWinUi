@echo off
REM ---------------------------------------------------------------------------
REM Build (detailed log) of grepWinUi, configuration Debug (calls build.bat Debug /v:normal)
REM
REM Usage:  build_debug_detailed.bat [extra dotnet build / msbuild args]
REM   build_debug_detailed.bat
REM   build_debug_detailed.bat /v:minimal     extra args are passed on to dotnet build
REM Do not pass a configuration (Debug/Release, /p:Configuration=..., -c): rejected
REM Output: src\GrepWinUi.App\bin\Debug\net10.0-windows\GrepWinUi.App.exe
REM ---------------------------------------------------------------------------
call "%~dp0build.bat" Debug /v:normal %*
exit /b %ERRORLEVEL%
