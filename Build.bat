@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1"
set BUILD_EXIT_CODE=%ERRORLEVEL%
echo.
if not "%BUILD_EXIT_CODE%"=="0" (
  echo Build failed.
) else (
  echo Build completed.
)
pause
exit /b %BUILD_EXIT_CODE%
