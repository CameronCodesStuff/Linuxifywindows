# LinuxifyWindows v1.0.0

**Total Desktop Customization Suite — Make Windows 11 look and feel like Linux**

A comprehensive Windows application that brings the full power of Linux desktop customization to Windows 11. Switch between desktop environments, apply window effects, swap icon packs, enable wobbly windows, and customize every pixel of your desktop.

---

## Installation

### Quick Install
1. Double-click **`install.bat`**
2. Follow the on-screen prompts
3. Launch from the desktop shortcut

### Manual Build
```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o build
```

### Requirements
- **Windows 10/11** (64-bit)
- **.NET 8 Desktop Runtime** — [Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- For building from source: **.NET 8 SDK**

---

## Features

### Desktop Environments (One-Click Presets)
Switch between completely different UI layouts instantly:

| Environment | Style | Key Features |
|---|---|---|
| **GNOME 45** | Clean minimal | Top bar, Dash to Dock, Activities overview |
| **KDE Plasma 6** | Feature-rich | Panels, widgets, KWin effects, wobbly windows |
| **XFCE Classic** | Lightweight | Dual panels, Whisker menu |
| **Cinnamon** | Traditional | Linux Mint style, applets, desklets |
| **i3 Tiling** | Keyboard-driven | Auto-tiling, workspaces, no decorations |
| **Sway/Wayland** | Modern tiling | Waybar, blur, transparency |
| **Hyprland** | Eye-candy tiling | Animations, blur, rounded, auto-tile |
| **macOS Sonoma** | Apple-inspired | Menu bar, dock, global menu, Mission Control |
| **Cyberpunk Neon** | Max effects | Neon dock, glow, wobbly, animated wallpaper |
| **Glassmorphism** | Frosted glass | Glass panels, blur, soft shadows |
| **Material You** | Google MD3 | Dynamic color, large radius, elevation |
| **Minimalist Rice** | Ultra-minimal | Polybar, gaps, terminal focus |
| + 6 more presets | | |

### Window Effects
- **Rounded window corners** with adjustable radius
- **Drop shadows** and elevated active windows
- **Minimize animations**: Scale, MagicLamp, Burn, Fade, Slide, Glitch
- **Desktop cube** (3D workspace switching)
- **3D workspace transitions**
- **Desktop zoom** (Super + scroll)
- **Picture-in-Picture** mode
- **Virtual workspaces** (1–12)

### Wobbly Windows (Compiz-Style)
Integrated from WobblyWindows v3 — real soft-body physics:
- Jelly mesh deformation on drag
- 3D perspective tilt on motion
- Adjustable speed, wobble amount, softness, tilt, stretch
- Quick presets: Subtle, Default, Bouncy, Jello, Stiff
- Drag any title bar or Ctrl+Alt+drag anywhere

### Themes & Appearance
- **20+ accent color presets** + custom color picker
- **15 theme presets**: Cyberpunk Neon, Adwaita, Breeze, Dracula, Nord, Gruvbox, Catppuccin, Tokyo Night, One Dark, Solarized, Material, Glassmorphism, Retro Terminal, macOS, Windows Fluent
- **System dark/light mode** toggle
- **Window border** width and color
- **Secondary accent** color
- **Transparent terminal** with adjustable opacity and blur

### Window Transparency
- **System-wide transparency** with default opacity slider
- **Background blur** (Acrylic/Mica effect)
- **Per-application opacity** for 12+ common apps:
  - Windows Terminal, File Explorer, Edge, Chrome, Firefox, VS Code, Discord, Spotify, Steam, Notepad, Task Manager, Settings
- Real-time opacity application via Win32 API

### Window Tiling & Snapping
- **Manual**: Snap to edges and corners (Windows-style)
- **Auto**: New windows auto-tile into available space (Hyprland-style)
- **i3-like**: Full keyboard-driven tiling with splits, tabbed, stacked
- Edge, quarter, and smart snapping
- Adjustable tile gap (0–24px)

### Icon Packs
Swap complete icon sets and revert anytime:
- Papirus, Tela, Numix Circle, Candy, Whitesur (macOS), Reversal, Kora, Breeze, Adwaita, Cyberpunk Neon, Pixel Perfect
- Import custom icon packs from folders
- One-click restore to Windows defaults

### Cursor Themes
- Breeze, Adwaita, DMZ-Black, Bibata Modern, Bibata Neon, Capitaine (macOS), Oreo Spark, Vimix, Pixel Retro

### Fonts
- **System UI font**: Segoe UI, Inter, Roboto, SF Pro, Noto Sans, Ubuntu, Fira Sans, Open Sans, Cantarell, DejaVu Sans
- **Title bar font**: Semibold/Bold variants
- **Terminal/Monospace font**: Cascadia Code, Fira Code, JetBrains Mono, Source Code Pro, Hack, Iosevka, Ubuntu Mono, DejaVu Sans Mono, Consolas, Fantasque Sans Mono
- Live font preview panel

### Panels & Docks
- Panel on any edge: Top, Bottom, Left, Right
- Adjustable panel size (20–72px)
- Auto-hide, dock mode, global application menu
- Independent panels per monitor
- **8 panel style presets**: macOS Dock, GNOME Top Bar, KDE Plasma Panel, Unity Launcher, i3bar, Waybar, Polybar, Plank

### Wallpaper
- **5 modes**: Static, Animated, Video (MP4/WebM), GIF, Interactive
- File browser for wallpaper selection
- Independent wallpaper per monitor

### Desktop Widgets
16 widget types (Conky/Rainmeter-style):
- System Monitor, Clock, Calendar, Weather, Music Player, Network Monitor, Disk Usage, CPU Graph, RAM Monitor, Battery, Notes, RSS Feed, App Launcher, Neofetch, Terminal Output, Custom HTML
- Drag-to-position, per-widget opacity
- Add/remove with one click

### Boot & Login Screen
- Custom **boot animation** (Plymouth-style): Spinner, Matrix Rain, Neon Pulse, Linux Tux, Custom
- Custom **splash screen** during startup
- Custom **lock screen** overlay
- **6 login screen styles**: GDM/GNOME, SDDM/KDE, LightDM GTK, macOS Ventura, Cyberpunk Terminal, Minimal

### Keyboard Shortcuts
- Full i3/Sway-style keybindings
- Default bindings for terminal, app launcher, workspaces, tiling
- One-click load i3 defaults (27 bindings)
- Editable and extensible

### Settings & System
- Run at startup / start minimized
- Pin to Start Menu / create desktop shortcut / add to PATH
- Export/import configuration (JSON)
- Reset all to defaults
- Diagnostic log viewer
- Full uninstaller included

---

## Architecture

- **Single-file C# WinForms** application (~2000 lines)
- **Win32 API** for real transparency, DWM effects, accent colors
- **Registry integration** for dark mode, accent colors, startup
- **JSON configuration** persisted to `%LOCALAPPDATA%\LinuxifyWindows\config.json`
- **WobblyWindows integration** — syncs physics settings with WobblyWindows v3
- Custom borderless UI with animated glow effects
- Sidebar navigation with 15 setting panels

---

## File Structure

```
LinuxifyWindows/
├── install.bat              # One-click installer
├── build.bat                # Manual build script
├── LinuxifyWindows.csproj   # .NET 8 project file
├── README.md                # This file
├── src/
│   └── Program.cs           # Main application (all-in-one)
├── assets/                  # Icons and resources
├── themes/                  # Theme definitions
└── icons/                   # Icon pack storage
```

---

## Uninstall

Run `uninstall.bat` in the install directory, or manually:
1. Delete the install folder
2. Remove `%LOCALAPPDATA%\LinuxifyWindows` for config data
3. Remove Start Menu / Desktop shortcuts
4. Remove startup entry from Registry if set

---

## Integration with WobblyWindows

LinuxifyWindows includes a full Wobbly Windows settings panel that syncs with the standalone WobblyWindows v3 application. Install both side-by-side for the complete Compiz experience.

---

**Built with C# / .NET 8 / WinForms / Win32 API**
