@echo off
setlocal enabledelayedexpansion
REM ---------------------------------------------------------------------------
REM  Deletes bin\ and obj\ for BOTH projects in this solution.
REM
REM  The previous version only ever looked for "bin" and "obj" in the *current
REM  working directory*. Launched from anywhere other than this folder it found
REM  nothing, printed "Done." and exited successfully - so it looked like it had
REM  worked. Even launched correctly it only cleaned the packaging project and
REM  left the app project's output, which is the larger of the two.
REM
REM  Visual Studio holds locks on these folders while a build or the debugger is
REM  running, so a delete can legitimately fail. That is reported here rather
REM  than swallowed.
REM ---------------------------------------------------------------------------

REM  %~dp0 is this script's own folder, so the paths below are correct no matter
REM  where it was launched from.
pushd "%~dp0"

set ROOT=%~dp0..
set FAILED=0

call :clean "%~dp0"            "WinAppPakaging"
call :clean "%ROOT%\BatteryZx" "BatteryZx"

echo.
if %FAILED% NEQ 0 (
  echo Some folders could not be deleted.
  echo Close Visual Studio ^(or stop the debugger^) and run this again.
) else (
  echo Done - both projects cleaned.
)

popd
echo.
pause
exit /b %FAILED%

REM ---------------------------------------------------------------------------
:clean
set TARGET=%~1
set LABEL=%~2

echo [%LABEL%]

for %%d in (bin obj) do (
  if exist "%TARGET%\%%d" (
    REM  Size is read before the delete - afterwards there is nothing to measure.
    REM  No pipe in the PowerShell: a "^|" does not survive being nested inside
    REM  this outer for loop, which silently made every size report 0 MB.
    for /f "usebackq delims=" %%s in (`powershell -NoProfile -Command "'{0:N0}' -f ((Measure-Object -InputObject (Get-ChildItem -LiteralPath '%TARGET%\%%d' -Recurse -File -ErrorAction SilentlyContinue) -Property Length -Sum).Sum / 1MB)"`) do set SIZE=%%s
    rmdir /s /q "%TARGET%\%%d" 2>nul
    if exist "%TARGET%\%%d" (
      echo   %%d ...... FAILED - in use
      set FAILED=1
    ) else (
      echo   %%d ...... deleted, !SIZE! MB reclaimed
    )
  ) else (
    echo   %%d ...... already clean
  )
)
exit /b 0
