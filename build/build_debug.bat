@echo off
REM ---------------------------------------------------------------------------
REM Build grepWinUi, configuration Debug (calls build.bat)
REM
REM Usage:  build_debug.bat [extra dotnet build / msbuild args]
REM   build_debug.bat                  incremental Debug build
REM   build_debug.bat /t:Rebuild       full rebuild
REM   build_debug.bat /t:Clean         delete build output
REM   build_debug.bat /v:normal        more detailed build log
REM Do not pass a configuration (Debug/Release, /p:Configuration=..., -c): rejected
REM Output: src\GrepWinUi.App\bin\Debug\net10.0-windows\GrepWinUi.App.exe
REM ---------------------------------------------------------------------------
call "%~dp0build.bat" Debug %*
exit /b %ERRORLEVEL%
