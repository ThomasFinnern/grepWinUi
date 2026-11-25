@echo off
REM ---------------------------------------------------------------------------
REM Clean grepWinUi, configuration Debug (calls build.bat Debug /t:Clean)
REM
REM Usage:  build_debug_clean.bat [extra dotnet build / msbuild args]
REM   build_debug_clean.bat
REM   build_debug_clean.bat /v:minimal     extra args are passed on to dotnet build
REM Do not pass a configuration (Debug/Release, /p:Configuration=..., -c): rejected
REM Deletes the output in bin\Debug\ and obj\Debug\ of each project
REM ---------------------------------------------------------------------------
call "%~dp0build.bat" Debug /t:Clean %*
exit /b %ERRORLEVEL%
