@echo off
REM ---------------------------------------------------------------------------
REM Build grepWinUi, configuration Release (calls build.bat)
REM
REM Usage:  build_release.bat [extra dotnet build / msbuild args]
REM   build_release.bat                incremental Release build
REM   build_release.bat /t:Rebuild     full rebuild
REM   build_release.bat /t:Clean       delete build output
REM   build_release.bat /v:normal      more detailed build log
REM Do not pass a configuration (Debug/Release, /p:Configuration=..., -c): rejected
REM Output: src\GrepWinUi.App\bin\Release\net10.0-windows\GrepWinUi.App.exe
REM ---------------------------------------------------------------------------
call "%~dp0build.bat" Release %*
exit /b %ERRORLEVEL%
