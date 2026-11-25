@echo off
REM ---------------------------------------------------------------------------
REM Build (detailed log) of grepWinUi, configuration Release (calls build.bat Release /v:normal)
REM
REM Usage:  build_detailed.bat [extra dotnet build / msbuild args]
REM   build_detailed.bat
REM   build_detailed.bat /v:minimal     extra args are passed on to dotnet build
REM Do not pass a configuration (Debug/Release, /p:Configuration=..., -c): rejected
REM Output: src\GrepWinUi.App\bin\Release\net10.0-windows\GrepWinUi.App.exe
REM ---------------------------------------------------------------------------
call "%~dp0build.bat" Release /v:normal %*
exit /b %ERRORLEVEL%
