@echo off
REM ---------------------------------------------------------------------------
REM Clean grepWinUi, configuration Release (calls build.bat Release /t:Clean)
REM
REM Usage:  build_clean.bat [extra dotnet build / msbuild args]
REM   build_clean.bat
REM   build_clean.bat /v:minimal     extra args are passed on to dotnet build
REM Do not pass a configuration (Debug/Release, /p:Configuration=..., -c): rejected
REM Deletes the output in bin\Release\ and obj\Release\ of each project
REM ---------------------------------------------------------------------------
call "%~dp0build.bat" Release /t:Clean %*
exit /b %ERRORLEVEL%
