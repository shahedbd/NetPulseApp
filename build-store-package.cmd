@echo off
setlocal enabledelayedexpansion
REM ---------------------------------------------------------------------------
REM  Builds Desktop App Template packages.
REM
REM    build-store-package.cmd            both      (default)
REM    build-store-package.cmd store      Store upload only
REM    build-store-package.cmd sideload   installable package only
REM
REM  Two outputs, and they are NOT interchangeable:
REM
REM    *.msixupload    UNSIGNED - upload this to Partner Center. The Store signs
REM                    it. Windows cannot install it.
REM    *.msixbundle    SIGNED with the certificate named in
REM                    WinAppPakaging.wapproj (PackageCertificateThumbprint).
REM                    That thumbprint is whatever test certificate was
REM                    generated on THIS machine - it will not exist on a
REM                    fresh clone, and neither will the private key needed to
REM                    sign with it. Before building sideload/both here for
REM                    the first time on a new machine, create your own test
REM                    certificate (Visual Studio: right-click the wapproj >
REM                    Package and Publish > Create App Packages, or
REM                    New-SelfSignedCertificate) and update that thumbprint.
REM
REM  Building both takes two passes, because one package cannot be signed and
REM  unsigned at once. The default does both so neither is ever missing.
REM
REM  This is a template, not a shipped app: no PRO define, no variant blocks
REM  in AppConfig, and no tier system - so unlike the sibling repos this
REM  script pattern comes from, there is no AppDataFolderName / entitlement
REM  pre-flight to run. The checks below are the ones that still apply.
REM
REM  Platform is AnyCPU / bundle "neutral" - it must match AppxBundlePlatforms
REM  in the .wapproj.
REM ---------------------------------------------------------------------------

set MODE=%~1
if /i "%MODE%"=="" set MODE=both
if /i not "%MODE%"=="both" if /i not "%MODE%"=="store" if /i not "%MODE%"=="sideload" (
  echo Usage: %~nx0 [both^|store^|sideload]
  exit /b 1
)

set ROOT=%~dp0
set ROOT=%ROOT:~0,-1%
set APPPROJ=%ROOT%\DesktopAppTemplate
set WAPPROJ=%ROOT%\WinAppPakaging

if not exist "%WAPPROJ%\WinAppPakaging.wapproj" (
  echo ERROR: packaging project not found: %WAPPROJ%\WinAppPakaging.wapproj
  exit /b 1
)

REM ---------------------------------------------------------------------------
REM  Pre-flight.
REM ---------------------------------------------------------------------------
echo Pre-flight checks...

REM  Package.appxmanifest ships with a placeholder Store identity
REM  (YourCompany.DesktopAppTemplate / CN=YourPublisherId) - catch it before
REM  building a package meant for real submission or a real sideload test,
REM  since a placeholder identity cannot be uploaded to Partner Center and
REM  installing it will not update or replace a real app of the same name.
findstr /c:"YourCompany.DesktopAppTemplate" "%WAPPROJ%\Package.appxmanifest" >nul
if not errorlevel 1 (
  echo   WARNING: Package.appxmanifest still has the placeholder Identity
  echo            ^(YourCompany.DesktopAppTemplate / CN=YourPublisherId^). Fine
  echo            for a local sideload test of the template itself; replace
  echo            both before submitting to the Store or shipping a real app.
) else (
  echo   Store identity ...... customized, ok
)

REM  The manifest version is what the Store enforces; the assembly version is
REM  what the About dialog and file properties show. Divergence is legal but
REM  is almost always an oversight, so warn rather than block.
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([xml](Get-Content '%WAPPROJ%\Package.appxmanifest')).Package.Identity.Version"`) do set MANIFESTVER=%%v
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "(([xml](Get-Content '%APPPROJ%\DesktopAppTemplate.csproj')).Project.PropertyGroup.Version) -join ''"`) do set CSPROJVER=%%v

if "!CSPROJVER!"=="" (
  echo   NOTE: DesktopAppTemplate.csproj has no explicit ^<Version^> - skipping the version-match check.
) else if /i not "!MANIFESTVER!"=="!CSPROJVER!.0" if /i not "!MANIFESTVER!"=="!CSPROJVER!" (
  echo   WARNING: manifest version !MANIFESTVER! does not look like it matches csproj Version !CSPROJVER!
  echo            The Store uses the manifest value; the About dialog shows AppConfig.AppVersion instead.
) else (
  echo   Version !MANIFESTVER! matches the csproj ...... ok
)

echo.
echo   Packaging identity - check this is the app you meant to build:
for /f "tokens=*" %%v in ('findstr /c:"Name=" "%WAPPROJ%\Package.appxmanifest"') do echo     %%v
echo     Version="!MANIFESTVER!"
echo.

tasklist /fi "imagename eq DesktopAppTemplate.exe" 2>nul | findstr /i "DesktopAppTemplate.exe" >nul
if not errorlevel 1 (
  echo   WARNING: DesktopAppTemplate.exe is running - it may hold the build output
  echo            and fail the copy. If AppConfig.EnableSystemTray is on, closing
  echo            the window is not enough - it minimizes to the tray instead of
  echo            exiting; close it from the tray icon's Exit item.
  echo.
)

REM --- locate MSBuild (a .wapproj cannot be built by "dotnet build")
set VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
if not exist "%VSWHERE%" set VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -property installationPath`) do set VSPATH=%%i
set MSBUILD=%VSPATH%\MSBuild\Current\Bin\MSBuild.exe
if not exist "%MSBUILD%" ( echo ERROR: MSBuild not found - install the VS MSBuild component. & exit /b 1 )

REM --- clear obj once. A stale ResolvePackageAssets cache has shipped packages
REM     missing a dependency, which crashes on launch - this app pulls in
REM     System.Management, so it has one to lose.
echo Clearing intermediate output...
if exist "%APPPROJ%\obj"          rmdir /s /q "%APPPROJ%\obj"
if exist "%WAPPROJ%\obj"          rmdir /s /q "%WAPPROJ%\obj"
if exist "%WAPPROJ%\AppPackages"  rmdir /s /q "%WAPPROJ%\AppPackages"

echo Restoring...
"%MSBUILD%" "%WAPPROJ%\WinAppPakaging.wapproj" -t:Restore -p:Configuration=Release -p:Platform=AnyCPU -v:q -nologo || exit /b 1

if /i not "%MODE%"=="sideload" (
  echo.
  echo [1/2] Store upload package ^(unsigned^)...
  call :build StoreUpload false || exit /b 1
  REM The Store pass also drops an unsigned _Test bundle. It cannot be
  REM installed, and leaving it invites exactly that mistake; the signed one
  REM from the sideload pass replaces it.
  for /d %%d in ("%WAPPROJ%\AppPackages\*_Test") do rmdir /s /q "%%d"
)

if /i not "%MODE%"=="store" (
  echo.
  echo [2/2] Installable package ^(signed^)...
  call :build SideloadOnly true || exit /b 1
)

echo.
echo ===============================================================
for %%f in ("%WAPPROJ%\AppPackages\*.msixupload") do (
  echo  STORE  upload to Partner Center ^> Packages:
  echo     %%~ff
  echo     size: %%~zf bytes
)
for /r "%WAPPROJ%\AppPackages" %%f in (*.msixbundle) do (
  echo  LOCAL  signed, installable:
  echo     %%~ff
  echo     size: %%~zf bytes
  echo     install:   powershell -Command "Add-AppxPackage -Path '%%~ff'"
  echo     reinstall: powershell -Command "Remove-AppxPackage -Package (Get-AppxPackage *DesktopAppTemplate*).PackageFullName"
)
echo ===============================================================
endlocal
exit /b 0

:build
"%MSBUILD%" "%WAPPROJ%\WinAppPakaging.wapproj" ^
  -p:Configuration=Release ^
  -p:Platform=AnyCPU ^
  -p:UapAppxPackageBuildMode=%~1 ^
  -p:AppxBundle=Always ^
  -p:AppxBundlePlatforms=neutral ^
  -p:AppxPackageSigningEnabled=%~2 ^
  -v:m -nologo
exit /b %errorlevel%
