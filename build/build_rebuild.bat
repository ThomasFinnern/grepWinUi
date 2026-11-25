@echo off
REM ---------------------------------------------------------------------------
REM Full rebuild of grepWinUi, configuration Release (calls build.bat Release /t:Rebuild)
REM
REM Usage:  build_rebuild.bat [extra dotnet build / msbuild args]
REM   build_rebuild.bat
REM   build_rebuild.bat /v:minimal     extra args are passed on to dotnet build
REM Do not pass a configuration (Debug/Release, /p:Configuration=..., -c): rejected
REM Output: src\GrepWinUi.App\bin\Release\net10.0-windows\GrepWinUi.App.exe
REM ---------------------------------------------------------------------------
call "%~dp0build.bat" Release /t:Rebuild %*
exit /b %ERRORLEVEL%
