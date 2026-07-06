@echo off
setlocal enabledelayedexpansion
title LinuxifyWindows Installer
color 0B

echo.
echo   +--------------------------------------------------------------+
echo   ^|                                                              ^|
echo   ^|   _     ___ _   _ _   ___  _____ _____   __                 ^|
echo   ^|  ^| ^|   ^|_ _^| \ ^| ^| ^| ^| \ \/ /_ _^|  ___^| \ \  __             ^|
echo   ^|  ^| ^|    ^| ^|^|  \^| ^| ^| ^|  \  / ^| ^|^| ^|__    \ \/ /             ^|
echo   ^|  ^| ^|___ ^| ^|^| ^|\  ^| ^|_^| ^| /  \ ^| ^|^|  __^|    ^|  ^|              ^|
echo   ^|  ^|_____^|___^|_^| \_^|\___/ /_/\_\___^|_^|       ^|_^|              ^|
echo   ^|                                                              ^|
echo   ^|        Total Desktop Customization Suite v1.0.0              ^|
echo   ^|        Make Windows 11 look and feel like Linux              ^|
echo   ^|                                                              ^|
echo   +--------------------------------------------------------------+
echo.

:: -- Check admin --
net session >nul 2>nul
if not "%errorlevel%"=="0" (
    echo   [!] Some features require admin. Right-click and Run as Administrator
    echo       for full functionality. Core features still work without admin.
    echo.
)

:: -- Locate source files --
set "SOURCE=%~dp0"
if not exist "%SOURCE%src\Program.cs" (
    echo   [ERROR] Cannot find src\Program.cs
    echo   Run install.bat from inside the extracted LinuxifyWindows folder.
    pause
    exit /b 1
)
if not exist "%SOURCE%LinuxifyWindows.csproj" (
    echo   [ERROR] Cannot find LinuxifyWindows.csproj
    echo   Run install.bat from inside the extracted LinuxifyWindows folder.
    pause
    exit /b 1
)

:: -- Detect .NET 8 SDK --
echo   [1/6] Checking .NET 8 SDK...
set "DOTNET_OK=0"
where dotnet >nul 2>nul
if "%errorlevel%"=="0" (
    dotnet --version >nul 2>nul
    if "%errorlevel%"=="0" (
        set "DOTNET_OK=1"
    )
)
if "!DOTNET_OK!"=="1" (
    echo         [OK] dotnet found
) else (
    echo         [!!] dotnet SDK not found
    echo.
    echo   LinuxifyWindows requires the .NET 8 SDK to build.
    echo   Download: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
    echo.
    choice /C YN /M "   Continue anyway (Y/N)? "
    if errorlevel 2 (
        echo   Cancelled.
        pause
        exit /b 1
    )
)
echo.

:: -- Set install path --
set "INSTALL_DIR=%LOCALAPPDATA%\LinuxifyWindows"
echo   [2/6] Install directory:
echo         %INSTALL_DIR%
echo.
choice /C YN /M "   Install here (Y/N)? "
if errorlevel 2 (
    set /p "INSTALL_DIR=   Enter path: "
)
echo.

:: -- Create directories --
echo   [3/6] Creating directories...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%\src" mkdir "%INSTALL_DIR%\src"
if not exist "%INSTALL_DIR%\assets" mkdir "%INSTALL_DIR%\assets"
if not exist "%INSTALL_DIR%\themes" mkdir "%INSTALL_DIR%\themes"
if not exist "%INSTALL_DIR%\icons" mkdir "%INSTALL_DIR%\icons"
echo         [OK] Done
echo.

:: -- Copy files --
echo   [4/6] Copying files...
copy /Y "%SOURCE%src\Program.cs" "%INSTALL_DIR%\src\Program.cs" >nul 2>nul
copy /Y "%SOURCE%LinuxifyWindows.csproj" "%INSTALL_DIR%\LinuxifyWindows.csproj" >nul 2>nul
copy /Y "%SOURCE%README.md" "%INSTALL_DIR%\README.md" >nul 2>nul
if exist "%SOURCE%assets\*" xcopy /E /Y /Q "%SOURCE%assets" "%INSTALL_DIR%\assets\" >nul 2>nul
if exist "%SOURCE%themes\*" xcopy /E /Y /Q "%SOURCE%themes" "%INSTALL_DIR%\themes\" >nul 2>nul
if exist "%SOURCE%icons\*" xcopy /E /Y /Q "%SOURCE%icons" "%INSTALL_DIR%\icons\" >nul 2>nul
echo         [OK] Done
echo.

:: -- Build --
echo   [5/6] Building LinuxifyWindows...
echo         This may take a minute...
echo.

set "BUILD_OK=0"
dotnet publish "%INSTALL_DIR%\LinuxifyWindows.csproj" -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%INSTALL_DIR%\bin"
if "%errorlevel%"=="0" (
    echo.
    echo         [OK] Build succeeded
    set "BUILD_OK=1"
) else (
    echo.
    echo         [!!] Self-contained failed, trying framework-dependent...
    echo.
    dotnet publish "%INSTALL_DIR%\LinuxifyWindows.csproj" -c Release -o "%INSTALL_DIR%\bin"
    if "!errorlevel!"=="0" (
        echo.
        echo         [OK] Framework-dependent build succeeded
        set "BUILD_OK=1"
    ) else (
        echo.
        echo         [FAIL] Build failed. Install .NET 8 SDK and retry.
    )
)
echo.

:: -- Shortcuts --
echo   [6/6] Creating shortcuts...

set "EXE_PATH=%INSTALL_DIR%\bin\LinuxifyWindows.exe"
set "DESKTOP=%USERPROFILE%\Desktop"
set "STARTMENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"

powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%DESKTOP%\LinuxifyWindows.lnk'); $s.TargetPath = '%EXE_PATH%'; $s.WorkingDirectory = '%INSTALL_DIR%\bin'; $s.Description = 'LinuxifyWindows'; $s.Save()" >nul 2>nul
echo         [OK] Desktop shortcut

powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%STARTMENU%\LinuxifyWindows.lnk'); $s.TargetPath = '%EXE_PATH%'; $s.WorkingDirectory = '%INSTALL_DIR%\bin'; $s.Description = 'LinuxifyWindows'; $s.Save()" >nul 2>nul
echo         [OK] Start Menu shortcut

:: -- Uninstaller --
echo @echo off> "%INSTALL_DIR%\uninstall.bat"
echo title Uninstall LinuxifyWindows>> "%INSTALL_DIR%\uninstall.bat"
echo echo.>> "%INSTALL_DIR%\uninstall.bat"
echo echo Uninstalling LinuxifyWindows...>> "%INSTALL_DIR%\uninstall.bat"
echo choice /C YN /M "Are you sure (Y/N)? ">> "%INSTALL_DIR%\uninstall.bat"
echo if errorlevel 2 exit /b 0>> "%INSTALL_DIR%\uninstall.bat"
echo del /Q "%DESKTOP%\LinuxifyWindows.lnk" 2^>nul>> "%INSTALL_DIR%\uninstall.bat"
echo del /Q "%STARTMENU%\LinuxifyWindows.lnk" 2^>nul>> "%INSTALL_DIR%\uninstall.bat"
echo reg delete "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "LinuxifyWindows" /f 2^>nul>> "%INSTALL_DIR%\uninstall.bat"
echo echo Uninstalled. Delete %%LOCALAPPDATA%%\LinuxifyWindows to remove config.>> "%INSTALL_DIR%\uninstall.bat"
echo pause>> "%INSTALL_DIR%\uninstall.bat"
echo         [OK] Uninstaller created

echo.
echo   +--------------------------------------------------------------+
echo   ^|                                                              ^|
echo   ^|   INSTALLATION COMPLETE                                      ^|
echo   ^|                                                              ^|
echo   ^|   Location: %INSTALL_DIR%
echo   ^|                                                              ^|
echo   ^|   - Desktop shortcut created                                 ^|
echo   ^|   - Start Menu entry created                                 ^|
echo   ^|   - Uninstaller at install dir\uninstall.bat                 ^|
echo   ^|                                                              ^|
echo   ^|   Quick start:                                               ^|
echo   ^|     1. Double-click LinuxifyWindows on your desktop          ^|
echo   ^|     2. Pick a Desktop Environment preset                     ^|
echo   ^|     3. Customize everything                                  ^|
echo   ^|                                                              ^|
echo   +--------------------------------------------------------------+
echo.

if "!BUILD_OK!"=="1" (
    choice /C YN /M "   Launch LinuxifyWindows now (Y/N)? "
    if not errorlevel 2 (
        echo   Launching...
        start "" "%EXE_PATH%"
    )
) else (
    echo   Build was not successful. Install .NET 8 SDK and re-run install.bat
)

echo.
pause
