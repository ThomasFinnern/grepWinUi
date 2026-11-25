@echo off
REM ---------------------------------------------------------------------------
REM Full rebuild of grepWinUi, configuration Debug (calls build.bat Debug /t:Rebuild)
REM
REM Usage:  build_debug_rebuild.bat [extra dotnet build / msbuild args]
REM   build_debug_rebuild.bat
REM   build_debug_rebuild.bat /v:minimal     extra args are passed on to dotnet build
REM Do not pass a configuration (Debug/Release, /p:Configuration=..., -c): rejected
REM Output: src\GrepWinUi.App\bin\Debug\net10.0-windows\GrepWinUi.App.exe
REM ---------------------------------------------------------------------------
call "%~dp0build.bat" Debug /t:Rebuild %*
exit /b %ERRORLEVEL%
