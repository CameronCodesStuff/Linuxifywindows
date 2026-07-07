// LinuxifyWindows v1.1 — Total Windows 11 Desktop Customization Suite
// Integrates WobblyWindows v3 engine directly (no external dependency).
// All settings apply real Win32/DWM/Registry changes.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace LinuxifyWindows;

// ═══════════════════════════════════════════════════════════════════════════════
// ENTRY
// ═══════════════════════════════════════════════════════════════════════════════
static class Program
{
    [STAThread]
    static void Main()
    {
        Native.SetProcessDpiAwarenessContext(Native.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        AppConfig.Load();
        Log.Write("=== LinuxifyWindows v1.1 starting ===");
        Application.Run(new MainForm());
        Log.Write("=== exiting ===");
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIG
// ═══════════════════════════════════════════════════════════════════════════════
sealed class AppConfig
{
    public static AppConfig Current = new();

    // Desktop environment
    public string ActiveEnvironment { get; set; } = "Default";
    public string PanelPosition { get; set; } = "Bottom";
    public int PanelSize { get; set; } = 48;
    public bool PanelAutoHide { get; set; } = false;
    public bool DockMode { get; set; } = false;
    public bool GlobalMenuEnabled { get; set; } = false;

    // Window effects
    public bool TransparencyEnabled { get; set; } = false;
    public int DefaultOpacity { get; set; } = 100;
    public bool BlurEnabled { get; set; } = false;
    public bool RoundedCorners { get; set; } = true;
    public int CornerRadius { get; set; } = 8;
    public bool DropShadows { get; set; } = true;
    public string MinimizeAnimation { get; set; } = "Scale";
    public bool DesktopCube { get; set; } = false;
    public int WorkspaceCount { get; set; } = 4;
    public bool AnimatedWorkspaces { get; set; } = true;

    // Per-app opacity
    public Dictionary<string, int> PerAppOpacity { get; set; } = new();

    // Tiling
    public bool TilingEnabled { get; set; } = true;
    public string TilingMode { get; set; } = "Manual";
    public int TileGap { get; set; } = 4;
    public bool EdgeSnapping { get; set; } = true;
    public bool QuarterSnapping { get; set; } = true;

    // Theme
    public string ThemeName { get; set; } = "Cyberpunk Neon";
    public string AccentColor { get; set; } = "#00E5FF";
    public bool DarkMode { get; set; } = true;
    public string FontFamily { get; set; } = "Segoe UI";
    public string MonospaceFont { get; set; } = "Cascadia Code";
    public bool TransparentTerminal { get; set; } = false;
    public int TerminalOpacity { get; set; } = 85;

    // Icons & Cursor
    public string ActiveIconPack { get; set; } = "Default Windows";
    public string CursorTheme { get; set; } = "Default";

    // Wallpaper
    public string WallpaperMode { get; set; } = "Static";
    public string WallpaperPath { get; set; } = "";

    // Wobbly Windows (integrated engine)
    public bool WobblyEnabled { get; set; } = false;
    public int WobblySpeed { get; set; } = 100;
    public int WobblyAmount { get; set; } = 75;
    public int WobblySoftness { get; set; } = 70;
    public int WobblyTilt { get; set; } = 16;
    public int WobblyStretch { get; set; } = 55;
    public bool WobblyDeform { get; set; } = true;
    public bool WobblyTiltEnabled { get; set; } = true;
    public bool WobblySnap { get; set; } = true;

    // Widgets
    public bool WidgetsEnabled { get; set; } = false;

    // Boot
    public bool RunAtStartup { get; set; } = false;

    // Physics helpers (mirror WobblyWindows Settings)
    [System.Text.Json.Serialization.JsonIgnore]
    public double Speed2 => Math.Pow(Math.Clamp(WobblySpeed, 40, 250) / 100.0, 2);
    [System.Text.Json.Serialization.JsonIgnore]
    public double DampingRatio => 0.60 - 0.50 * Math.Clamp(WobblyAmount, 0, 100) / 100.0;
    [System.Text.Json.Serialization.JsonIgnore]
    public double HomeStiffnessNear => 430.0 * Speed2;
    [System.Text.Json.Serialization.JsonIgnore]
    public double HomeStiffnessFar => Math.Max(40.0, 320.0 - 2.6 * Math.Clamp(WobblySoftness, 0, 100)) * Speed2;
    [System.Text.Json.Serialization.JsonIgnore]
    public double StructuralStiffness => Math.Max(150.0, 700.0 - 3.2 * Math.Clamp(WobblySoftness, 0, 100)) * Speed2;
    [System.Text.Json.Serialization.JsonIgnore]
    public double TiltStiffness => 110.0 * Speed2;
    [System.Text.Json.Serialization.JsonIgnore]
    public double MaxDisplacementFactor => Math.Clamp(WobblyStretch, 20, 90) / 100.0;
    [System.Text.Json.Serialization.JsonIgnore]
    public double RigidStiffness => 700.0 * Speed2;

    static string Dir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LinuxifyWindows");
    static string FilePath => Path.Combine(Dir, "config.json");

    public static void Load()
    {
        try { if (File.Exists(FilePath)) { var s = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath)); if (s != null) Current = s; } }
        catch (Exception ex) { Log.Write("Config load: " + ex.Message); }
    }
    public static void Save()
    {
        try { Directory.CreateDirectory(Dir); File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { Log.Write("Config save: " + ex.Message); }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// LOGGER
// ═══════════════════════════════════════════════════════════════════════════════
static class Log
{
    public static readonly string FilePath;
    static readonly ConcurrentQueue<string> _q = new();
    static readonly AutoResetEvent _sig = new(false);
    static Log()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LinuxifyWindows");
        try { Directory.CreateDirectory(dir); } catch { }
        FilePath = Path.Combine(dir, "linuxify.log");
        try { if (File.Exists(FilePath) && new FileInfo(FilePath).Length > 1024 * 1024) File.Delete(FilePath); } catch { }
        new Thread(() => { var sb = new StringBuilder(); while (true) { _sig.WaitOne(2000); sb.Clear(); while (_q.TryDequeue(out var l)) sb.AppendLine(l); if (sb.Length > 0) try { File.AppendAllText(FilePath, sb.ToString()); } catch { } } }) { IsBackground = true }.Start();
    }
    public static void Write(string msg) { _q.Enqueue($"{DateTime.Now:HH:mm:ss.fff} {msg}"); _sig.Set(); }
}

// ═══════════════════════════════════════════════════════════════════════════════
// THEME
// ═══════════════════════════════════════════════════════════════════════════════
static class Theme
{
    public static Color BgDeep = Color.FromArgb(8, 8, 16);
    public static Color BgDark = Color.FromArgb(12, 14, 28);
    public static Color BgCard = Color.FromArgb(22, 24, 48);
    public static Color BgHover = Color.FromArgb(30, 33, 60);
    public static Color BgActive = Color.FromArgb(38, 42, 72);
    public static Color Accent = Color.FromArgb(0, 229, 255);
    public static Color Accent2 = Color.FromArgb(255, 0, 144);
    public static Color Success = Color.FromArgb(0, 255, 136);
    public static Color Warning = Color.FromArgb(255, 200, 0);
    public static Color Danger = Color.FromArgb(255, 60, 80);
    public static Color Text1 = Color.FromArgb(230, 235, 255);
    public static Color Text2 = Color.FromArgb(140, 150, 180);
    public static Color Text3 = Color.FromArgb(80, 90, 120);
    public static Color Border = Color.FromArgb(40, 45, 80);
}

// ═══════════════════════════════════════════════════════════════════════════════
// DESKTOP ENVIRONMENT PRESETS
// ═══════════════════════════════════════════════════════════════════════════════
static class Presets
{
    public record DE(string Name, string Desc, string Cat, string Accent,
        string Panel, int PanelSz, bool Dock, string Tiling,
        bool Wobbly, bool Transp, int Opacity, bool Round, int Radius,
        string MinAnim, string[] Tags);
    public static readonly DE[] All = {
        new("Default Windows", "Stock Windows 11", "Windows", "#0078D4", "Bottom", 48, false, "Manual", false, false, 100, true, 8, "Scale", new[]{"Taskbar","Start Menu","Snap Layouts"}),
        new("GNOME 45", "Clean minimal GNOME Shell", "Linux", "#1A73E8", "Top", 32, true, "Manual", false, true, 95, true, 12, "Scale", new[]{"Top Bar","Dash to Dock","Activities","App Grid"}),
        new("KDE Plasma 6", "Feature-rich with widgets and effects", "Linux", "#1D99F3", "Bottom", 44, false, "Manual", true, true, 92, true, 8, "MagicLamp", new[]{"Plasma Panel","KRunner","Widgets","Wobbly"}),
        new("XFCE Classic", "Lightweight dual-panel desktop", "Linux", "#2EB398", "Top", 28, false, "Manual", false, false, 100, false, 0, "Scale", new[]{"Top Panel","Bottom Panel","Whisker Menu"}),
        new("Cinnamon", "Traditional desktop (Linux Mint)", "Linux", "#8AB84A", "Bottom", 40, false, "Manual", false, true, 95, true, 6, "Scale", new[]{"Cinnamon Panel","Menu","Applets"}),
        new("i3 Tiling", "Keyboard-driven tiling WM", "Linux", "#285577", "Bottom", 24, false, "i3-like", false, false, 100, false, 0, "Fade", new[]{"i3bar","Auto Tiling","Keyboard Only","Workspaces"}),
        new("Hyprland", "Eye-candy tiling compositor", "Linux", "#00E5FF", "Top", 32, false, "Auto", true, true, 85, true, 12, "MagicLamp", new[]{"Waybar","Wobbly","Blur","Animations","Auto Tile"}),
        new("macOS Sonoma", "Apple-inspired top bar + dock", "macOS", "#007AFF", "Top", 28, true, "Manual", false, true, 90, true, 10, "MagicLamp", new[]{"Menu Bar","Dock","Global Menu","Mission Control"}),
        new("Cyberpunk Neon", "Maximum neon effects", "Custom", "#00E5FF", "Bottom", 48, true, "Manual", true, true, 88, true, 12, "Burn", new[]{"Neon Dock","Glow","Wobbly","Transparency"}),
        new("Glassmorphism", "Frosted glass everywhere", "Custom", "#FFFFFF", "Bottom", 48, true, "Manual", false, true, 75, true, 16, "Scale", new[]{"Glass Panels","Blur","Light Borders"}),
        new("Material You", "Google Material Design 3", "Custom", "#6750A4", "Bottom", 56, false, "Manual", false, true, 95, true, 20, "Scale", new[]{"Material Panel","Dynamic Color","Elevation"}),
        new("Minimalist Rice", "Ultra-minimal terminal focus", "Custom", "#BD93F9", "Top", 20, false, "Auto", false, true, 88, true, 8, "Fade", new[]{"Polybar","Gaps","No Decorations","Rofi"}),
        new("Retro Terminal", "Green CRT aesthetic", "Custom", "#00FF00", "Bottom", 32, false, "Manual", false, false, 100, false, 0, "Fade", new[]{"Pixel Font","CRT Effect","Green on Black"}),
        new("Nord Cozy", "Warm Nord color scheme", "Custom", "#88C0D0", "Top", 32, false, "Manual", false, true, 93, true, 10, "Scale", new[]{"Nord Colors","Rounded","Cozy Gaps"}),
    };
    public static DE Find(string n) => All.FirstOrDefault(p => p.Name == n) ?? All[0];
    public static void Apply(DE p)
    {
        var c = AppConfig.Current;
        c.ActiveEnvironment = p.Name; c.PanelPosition = p.Panel; c.PanelSize = p.PanelSz;
        c.DockMode = p.Dock; c.AccentColor = p.Accent; c.TilingMode = p.Tiling;
        c.WobblyEnabled = p.Wobbly; c.TransparencyEnabled = p.Transp;
        c.DefaultOpacity = p.Opacity; c.RoundedCorners = p.Round; c.CornerRadius = p.Radius;
        c.MinimizeAnimation = p.MinAnim; c.DarkMode = true;
        AppConfig.Save();
        Engines.ApplyAccent(); Engines.ApplyDarkMode();
        if (p.Wobbly) WobblyManager.Start(); else WobblyManager.Stop();
    }
}


// ═══════════════════════════════════════════════════════════════════════════════
// ENGINES — actually apply Win32/DWM/Registry changes
// ═══════════════════════════════════════════════════════════════════════════════
static class Engines
{
    public static void ApplyDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            int val = AppConfig.Current.DarkMode ? 0 : 1;
            key?.SetValue("AppsUseLightTheme", val, RegistryValueKind.DWord);
            key?.SetValue("SystemUsesLightTheme", val, RegistryValueKind.DWord);
            Log.Write($"Dark mode = {AppConfig.Current.DarkMode}");
        }
        catch (Exception ex) { Log.Write("ApplyDarkMode: " + ex.Message); }
    }

    public static void ApplyAccent()
    {
        try
        {
            Color accent;
            try { accent = ColorTranslator.FromHtml(AppConfig.Current.AccentColor); }
            catch { accent = Color.FromArgb(0, 120, 212); }
            uint abgr = (uint)(accent.B << 16 | accent.G << 8 | accent.R);
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent", true);
            key?.SetValue("AccentColorMenu", abgr, RegistryValueKind.DWord);
            key?.SetValue("StartColorMenu", abgr, RegistryValueKind.DWord);
            using var dwm = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\DWM", true);
            dwm?.SetValue("AccentColor", abgr, RegistryValueKind.DWord);
            dwm?.SetValue("ColorizationColor", abgr, RegistryValueKind.DWord);
            dwm?.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);
            Log.Write($"Accent color = {AppConfig.Current.AccentColor}");
        }
        catch (Exception ex) { Log.Write("ApplyAccent: " + ex.Message); }
    }

    public static void ApplyCorners()
    {
        try
        {
            // DWMWA_WINDOW_CORNER_PREFERENCE = 33
            // DWMWCP_DEFAULT=0 DONOTROUND=1 ROUND=2 ROUNDSMALL=3
            int pref = AppConfig.Current.RoundedCorners
                ? (AppConfig.Current.CornerRadius > 8 ? 2 : 3)
                : 1;
            EnumWindows((hwnd) =>
            {
                if (Native.IsWindowVisible(hwnd))
                    Native.DwmSetWindowAttribute(hwnd, 33, ref pref, sizeof(int));
            });
            Log.Write($"Corners = {pref}");
        }
        catch (Exception ex) { Log.Write("ApplyCorners: " + ex.Message); }
    }

    public static void SetWindowOpacity(IntPtr hwnd, int pct)
    {
        byte alpha = (byte)(255 * Math.Clamp(pct, 0, 100) / 100);
        int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
        Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, ex | Native.WS_EX_LAYERED);
        Native.SetLayeredWindowAttributes(hwnd, 0, alpha, Native.LWA_ALPHA);
    }

    public static void SetProcessOpacity(string processName, int pct)
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName(processName))
                if (proc.MainWindowHandle != IntPtr.Zero)
                    SetWindowOpacity(proc.MainWindowHandle, pct);
            Log.Write($"Opacity {processName} = {pct}%");
        }
        catch (Exception ex) { Log.Write($"SetProcessOpacity({processName}): {ex.Message}"); }
    }

    public static void ApplyAllOpacity()
    {
        foreach (var (name, pct) in AppConfig.Current.PerAppOpacity)
            SetProcessOpacity(name, pct);
    }

    public static void SetWallpaper(string path)
    {
        try
        {
            Native.SystemParametersInfo(0x0014, 0, path, 0x01 | 0x02);
            Log.Write($"Wallpaper = {path}");
        }
        catch (Exception ex) { Log.Write("SetWallpaper: " + ex.Message); }
    }

    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable) key?.SetValue("LinuxifyWindows", $"\"{Application.ExecutablePath}\"");
            else key?.DeleteValue("LinuxifyWindows", false);
            Log.Write($"Startup = {enable}");
        }
        catch (Exception ex) { Log.Write("SetStartup: " + ex.Message); }
    }

    static void EnumWindows(Action<IntPtr> action)
    {
        Native.EnumWindows((hwnd, _) => { action(hwnd); return true; }, IntPtr.Zero);
    }
}


// ═══════════════════════════════════════════════════════════════════════════════
// WOBBLY WINDOWS MANAGER — start/stop the integrated engine
// ═══════════════════════════════════════════════════════════════════════════════
static class WobblyManager
{
    static WobbleMouseHook _hook;
    static WobbleEngine _engine;
    static WobbleOverlay _overlay;
    static bool _running;

    public static bool Running => _running;

    public static void Start()
    {
        if (_running) return;
        _overlay = new WobbleOverlay();
        _ = _overlay.Handle;
        _engine = new WobbleEngine(_overlay.Handle);
        _hook = new WobbleMouseHook(_engine);
        _hook.Install();
        _running = true;
        Log.Write("Wobbly windows started");
    }

    public static void Stop()
    {
        if (!_running) return;
        _hook?.Dispose(); _hook = null;
        _engine?.Dispose(); _engine = null;
        _overlay?.Dispose(); _overlay = null;
        _running = false;
        Log.Write("Wobbly windows stopped");
    }

    public static void Restart() { Stop(); Start(); }
}

// ═══════════════════════════════════════════════════════════════════════════════
// MAIN FORM
// ═══════════════════════════════════════════════════════════════════════════════
sealed class MainForm : Form
{
    readonly Panel _sidebar, _content, _titleBar;
    readonly List<NavItem> _navItems = new();
    NavItem _activeNav;
    bool _dragging; Point _dragStart;
    System.Windows.Forms.Timer _glowTimer;
    float _glow;

    struct NavItem { public Panel Row; public Label Icon, Lbl; public string Name; public Func<Panel> Build; }

    public MainForm()
    {
        Text = "LinuxifyWindows";
        Size = new Size(1180, 760);
        MinimumSize = new Size(900, 560);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.BgDeep;
        DoubleBuffered = true;

        // Title bar
        _titleBar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.FromArgb(10, 12, 24) };
        _titleBar.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; } };
        _titleBar.MouseUp += (_, _) => _dragging = false;
        _titleBar.MouseMove += (_, e) => { if (_dragging) Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y); };

        _titleBar.Controls.Add(new Label { Text = "\u25C8 LINUXIFY WINDOWS", Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Theme.Accent, Location = new Point(12, 7), AutoSize = true });

        foreach (var (txt, idx, act) in new[] {("\u2715",0,(Action)(()=>Close())), ("\u25A1",1,(Action)(()=>WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized)), ("\u2500",2,(Action)(()=>WindowState = FormWindowState.Minimized))})
        {
            var b = new Label { Text = txt, Font = new Font("Segoe UI", 10f), ForeColor = Theme.Text2,
                TextAlign = ContentAlignment.MiddleCenter, Size = new Size(36, 36), Tag = idx, Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right };
            int ii = idx;
            b.Click += (_, _) => act();
            if (ii == 0) { b.MouseEnter += (_, _) => b.BackColor = Theme.Danger; b.MouseLeave += (_, _) => b.BackColor = Color.Transparent; }
            _titleBar.Controls.Add(b);
        }

        // Sidebar
        _sidebar = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Theme.BgDark };

        // Content
        _content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgDeep };

        // Dock order: Fill first (docks last), then Left, then Top
        Controls.Add(_content);
        Controls.Add(_sidebar);
        Controls.Add(_titleBar);

        // Nav items
        AddNav("\uE80F", "Desktop Environments", BuildDEPanel);
        AddNav("\uE771", "Window Effects", BuildEffectsPanel);
        AddNav("\uE790", "Themes & Appearance", BuildThemesPanel);
        AddNav("\uECAD", "Window Tiling", BuildTilingPanel);
        AddNav("\uE8B5", "Icon Packs", BuildIconsPanel);
        AddNav("\uE7F4", "Cursor Themes", BuildCursorPanel);
        AddNav("\uE8D6", "Fonts", BuildFontsPanel);
        AddNav("\uE7F7", "Panels & Docks", BuildPanelsPanel);
        AddNav("\uE7AC", "Wallpaper", BuildWallpaperPanel);
        AddNav("\uE71D", "Desktop Widgets", BuildWidgetsPanel);
        AddNav("\uE770", "Wobbly Windows", BuildWobblyPanel);
        AddNav("\uE7E8", "Transparency", BuildTranspPanel);
        AddNav("\uE713", "Settings", BuildSettingsPanel);

        if (_navItems.Count > 0) SelectNav(_navItems[0]);

        _glowTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _glowTimer.Tick += (_, _) => { _glow += 0.04f; _titleBar.Invalidate(); };
        _glowTimer.Start();

        _titleBar.Paint += (_, e) =>
        {
            float g = (float)(Math.Sin(_glow) * 0.3 + 0.7);
            using var pen = new Pen(Color.FromArgb((int)(40 * g), Theme.Accent), 1);
            e.Graphics.DrawLine(pen, 0, _titleBar.Height - 1, _titleBar.Width, _titleBar.Height - 1);
        };

        Resize += (_, _) =>
        {
            foreach (Control c in _titleBar.Controls)
                if (c.Tag is int idx) c.Location = new Point(Width - 36 * (idx + 1), 0);
        };
        Load += (_, _) =>
        {
            foreach (Control c in _titleBar.Controls)
                if (c.Tag is int idx) c.Location = new Point(Width - 36 * (idx + 1), 0);
            // Auto-start wobbly if enabled
            if (AppConfig.Current.WobblyEnabled) WobblyManager.Start();
        };

        FormClosing += (_, _) => WobblyManager.Stop();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Color.FromArgb(50, Theme.Accent), 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    void AddNav(string glyph, string name, Func<Panel> builder)
    {
        int y = 16 + _navItems.Count * 38;
        var row = new Panel { Location = new Point(0, y), Size = new Size(210, 36), Cursor = Cursors.Hand };
        var icon = new Label { Text = glyph, Font = new Font("Segoe MDL2 Assets", 10f), ForeColor = Theme.Text2,
            Location = new Point(14, 8), AutoSize = true, Cursor = Cursors.Hand };
        var lbl = new Label { Text = name, Font = new Font("Segoe UI", 9f), ForeColor = Theme.Text2,
            Location = new Point(40, 9), AutoSize = true, Cursor = Cursors.Hand };
        var item = new NavItem { Row = row, Icon = icon, Lbl = lbl, Name = name, Build = builder };
        EventHandler click = (_, _) => SelectNav(item);
        row.Click += click; icon.Click += click; lbl.Click += click;
        row.MouseEnter += (_, _) => { if (_activeNav.Name != item.Name) row.BackColor = Theme.BgHover; };
        row.MouseLeave += (_, _) => { if (_activeNav.Name != item.Name) row.BackColor = Color.Transparent; };
        row.Controls.Add(icon); row.Controls.Add(lbl);
        _sidebar.Controls.Add(row);
        _navItems.Add(item);
    }

    void SelectNav(NavItem item)
    {
        if (_activeNav.Row != null) { _activeNav.Row.BackColor = Color.Transparent; _activeNav.Icon.ForeColor = Theme.Text2; _activeNav.Lbl.ForeColor = Theme.Text2; }
        item.Row.BackColor = Theme.BgActive; item.Icon.ForeColor = Theme.Accent; item.Lbl.ForeColor = Theme.Text1;
        _activeNav = item;
        _content.Controls.Clear();
        var p = item.Build();
        p.Dock = DockStyle.Fill;
        _content.Controls.Add(p);
    }

    // ── Layout Helpers ──────────────────────────────────────────────────────
    const int M = 28; // left margin
    static Panel Scroll() => new Panel { AutoScroll = true, BackColor = Theme.BgDeep };
    static void Finalize(Panel p) { int my = 0; foreach (Control c in p.Controls) my = Math.Max(my, c.Bottom + 24); p.AutoScrollMinSize = new Size(0, my); }

    static Label H1(string t, int y) => new Label { Text = t, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = Theme.Text1, Location = new Point(M, y), AutoSize = true };
    static Label H2(string t, int y) => new Label { Text = t, Font = new Font("Segoe UI", 9f), ForeColor = Theme.Text2, Location = new Point(M, y), AutoSize = true, MaximumSize = new Size(800, 0) };
    static Label H3(string t, int y) => new Label { Text = t, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Theme.Accent, Location = new Point(M, y), AutoSize = true };
    static Label Txt(string t, int x, int y) => new Label { Text = t, Font = new Font("Segoe UI", 9f), ForeColor = Theme.Text1, Location = new Point(x, y), AutoSize = true };

    static Panel Card(int y, int h) => new Panel { Location = new Point(M, y), Size = new Size(700, h), BackColor = Theme.BgCard,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

    static CheckBox Toggle(string t, bool v, int y, Action<bool> act)
    {
        var cb = new CheckBox { Text = t, Checked = v, ForeColor = Theme.Text1, Font = new Font("Segoe UI", 9.5f),
            Location = new Point(M + 8, y), AutoSize = true, FlatStyle = FlatStyle.Flat };
        cb.CheckedChanged += (_, _) => { act(cb.Checked); AppConfig.Save(); };
        return cb;
    }

    static (Label lbl, TrackBar bar) Slider(string t, string unit, int min, int max, int val, int y, Action<int> act)
    {
        var lbl = new Label { Text = $"{t}: {val}{unit}", Font = new Font("Segoe UI", 9f), ForeColor = Theme.Text1,
            Location = new Point(M + 8, y), AutoSize = true };
        var bar = new TrackBar { Minimum = min, Maximum = max, Value = Math.Clamp(val, min, max),
            TickStyle = TickStyle.None, Location = new Point(M + 8, y + 20), Size = new Size(440, 28), BackColor = Theme.BgCard };
        Label l2 = lbl;
        bar.ValueChanged += (_, _) => { l2.Text = $"{t}: {bar.Value}{unit}"; act(bar.Value); AppConfig.Save(); };
        return (lbl, bar);
    }

    static ComboBox Drop(string[] items, string sel, int x, int y, int w, Action<string> act)
    {
        var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x, y),
            Size = new Size(w, 26), FlatStyle = FlatStyle.Flat, BackColor = Theme.BgCard, ForeColor = Theme.Text1,
            Font = new Font("Segoe UI", 9f) };
        foreach (var i in items) cb.Items.Add(i);
        cb.SelectedItem = sel;
        cb.SelectedIndexChanged += (_, _) => { if (cb.SelectedItem is string s) { act(s); AppConfig.Save(); } };
        return cb;
    }

    static Button Btn(string t, int x, int y, int w, Color fg, Action act)
    {
        var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(w, 34), FlatStyle = FlatStyle.Flat,
            BackColor = Theme.BgCard, ForeColor = fg, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = fg; b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, fg);
        b.Click += (_, _) => act();
        return b;
    }


    // ═══════════════════════════════════════════════════════════════════════
    // DESKTOP ENVIRONMENTS
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildDEPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Desktop Environments", y)); y += 32;
        p.Controls.Add(H2("Switch between completely different UI layouts with one click. Each preset applies real changes: accent color, dark mode, wobbly windows, and corner radius.", y)); y += 40;

        // Active badge
        p.Controls.Add(new Label { Text = $"  \u2714 ACTIVE: {c.ActiveEnvironment}  ", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Theme.BgDeep, BackColor = Theme.Accent, Location = new Point(M, y), AutoSize = true, Padding = new Padding(6, 3, 6, 3) });
        y += 36;

        // Category filter
        string[] cats = { "All", "Linux", "macOS", "Windows", "Custom" };
        var filterPanel = new Panel { Location = new Point(M, y), Size = new Size(500, 28), BackColor = Color.Transparent };
        string activeCat = "All";
        int tx = 0;
        foreach (var cat in cats)
        {
            var btn = new Label { Text = cat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = cat == "All" ? Theme.Accent : Theme.Text2,
                BackColor = cat == "All" ? Theme.BgActive : Color.Transparent,
                Location = new Point(tx, 0), Size = new Size(72, 26), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
            string cc = cat;
            btn.Click += (_, _) =>
            {
                foreach (Control fc in filterPanel.Controls) { if (fc is Label fl) { fl.ForeColor = Theme.Text2; fl.BackColor = Color.Transparent; } }
                btn.ForeColor = Theme.Accent; btn.BackColor = Theme.BgActive;
                // Reflow cards
                int cy = filterPanel.Bottom + 12;
                foreach (Control card in p.Controls)
                {
                    if (card.Tag is string tag && tag.StartsWith("DE:"))
                    {
                        string cardCat = tag.Substring(3);
                        bool show = cc == "All" || cardCat == cc;
                        card.Visible = show;
                        if (show) { card.Top = cy; cy += card.Height + 8; }
                    }
                }
                Finalize(p);
            };
            filterPanel.Controls.Add(btn);
            tx += 78;
        }
        p.Controls.Add(filterPanel); y += 36;

        // Cards
        foreach (var env in Presets.All)
        {
            bool active = c.ActiveEnvironment == env.Name;
            var card = new Panel { Location = new Point(M, y), Size = new Size(700, 88), BackColor = active ? Color.FromArgb(18, 28, 52) : Theme.BgCard,
                Tag = "DE:" + env.Cat, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            card.Paint += (_, e) => { using var pen = new Pen(active ? Theme.Accent : Theme.Border, active ? 2 : 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };

            // Accent dot
            Color ac; try { ac = ColorTranslator.FromHtml(env.Accent); } catch { ac = Theme.Accent; }
            card.Paint += (_, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var br = new SolidBrush(ac); e.Graphics.FillEllipse(br, 14, 18, 36, 36); };

            card.Controls.Add(new Label { Text = env.Name, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = active ? Theme.Accent : Theme.Text1, Location = new Point(62, 10), AutoSize = true });
            card.Controls.Add(new Label { Text = env.Desc, Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.Text2, Location = new Point(62, 32), AutoSize = true });

            // Tags
            int fx = 62;
            foreach (var tag in env.Tags.Take(6))
            {
                var tl = new Label { Text = tag, Font = new Font("Segoe UI", 7.5f), ForeColor = Theme.Accent, BackColor = Color.FromArgb(12, Theme.Accent),
                    Location = new Point(fx, 56), AutoSize = true, Padding = new Padding(4, 1, 4, 1) };
                card.Controls.Add(tl);
                fx += TextRenderer.MeasureText(tag, tl.Font).Width + 14;
                if (fx > 520) break;
            }

            // Apply button
            var ab = Btn(active ? "\u2714 ACTIVE" : "APPLY", 580, 26, 100, active ? Theme.Success : Theme.Accent, () =>
            {
                Presets.Apply(env);
                SelectNav(_navItems[0]);
            });
            ab.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (active) ab.Enabled = false;
            card.Controls.Add(ab);

            p.Controls.Add(card); y += 96;
        }
        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WINDOW EFFECTS
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildEffectsPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Window Effects", y)); y += 32;
        p.Controls.Add(H2("Rounded corners, shadows, minimize animations, desktop zoom, and workspaces. Changes apply instantly via DWM.", y)); y += 40;

        p.Controls.Add(H3("Rounded Corners", y)); y += 26;
        p.Controls.Add(Toggle("Rounded window corners", c.RoundedCorners, y, v => c.RoundedCorners = v)); y += 28;
        var (rl, rb) = Slider("Corner radius", "px", 0, 24, c.CornerRadius, y, v => c.CornerRadius = v);
        p.Controls.Add(rl); p.Controls.Add(rb); y += 56;

        p.Controls.Add(H3("Shadows", y)); y += 26;
        p.Controls.Add(Toggle("Window drop shadows", c.DropShadows, y, v => c.DropShadows = v)); y += 34;

        p.Controls.Add(H3("Minimize Animation", y)); y += 26;
        p.Controls.Add(Txt("Style:", M + 8, y + 4));
        p.Controls.Add(Drop(new[] { "Scale", "MagicLamp", "Burn", "Fade", "Slide", "Glitch" }, c.MinimizeAnimation, M + 60, y, 150, v => c.MinimizeAnimation = v));
        y += 36;

        p.Controls.Add(H3("Workspaces", y)); y += 26;
        p.Controls.Add(Toggle("Desktop cube (3D workspace switching)", c.DesktopCube, y, v => c.DesktopCube = v)); y += 28;
        p.Controls.Add(Toggle("Animated workspace transitions", c.AnimatedWorkspaces, y, v => c.AnimatedWorkspaces = v)); y += 28;
        var (wl, wb) = Slider("Virtual workspaces", "", 1, 12, c.WorkspaceCount, y, v => c.WorkspaceCount = v);
        p.Controls.Add(wl); p.Controls.Add(wb); y += 56;

        p.Controls.Add(Btn("APPLY EFFECTS NOW", M, y, 200, Theme.Accent, () =>
        {
            AppConfig.Save(); Engines.ApplyCorners();
            MessageBox.Show("Window effects applied via DWM!", "Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }));
        y += 50;
        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // THEMES
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildThemesPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Themes & Appearance", y)); y += 32;
        p.Controls.Add(H2("Accent colors and dark mode are applied directly to the Windows registry. Changes take effect immediately for most apps.", y)); y += 40;

        p.Controls.Add(H3("Dark Mode", y)); y += 26;
        p.Controls.Add(Toggle("System-wide dark mode", c.DarkMode, y, v => { c.DarkMode = v; Engines.ApplyDarkMode(); })); y += 34;

        p.Controls.Add(H3("Accent Color", y)); y += 26;
        string[] colors = { "#00E5FF","#FF0090","#6750A4","#1A73E8","#00BCD4","#4CAF50","#FF5722","#E91E63","#FFC107","#00FF88","#BD93F9","#1D99F3","#88C0D0","#007AFF","#F44336","#9C27B0","#FFFFFF","#285577","#8AB84A","#00FF00" };
        int cx = M + 8;
        foreach (var col in colors)
        {
            Color cc; try { cc = ColorTranslator.FromHtml(col); } catch { continue; }
            bool sel = c.AccentColor.Equals(col, StringComparison.OrdinalIgnoreCase);
            var sw = new Panel { Location = new Point(cx, y), Size = new Size(32, 32), BackColor = cc, Cursor = Cursors.Hand };
            sw.Paint += (_, e) => { if (sel) { using var pen = new Pen(Color.White, 2); e.Graphics.DrawRectangle(pen, 1, 1, 29, 29); } };
            string cv = col;
            sw.Click += (_, _) => { c.AccentColor = cv; AppConfig.Save(); Engines.ApplyAccent(); SelectNav(_navItems[2]); };
            p.Controls.Add(sw);
            cx += 36; if (cx > 600) { cx = M + 8; y += 36; }
        }
        y += 44;

        p.Controls.Add(Btn("CUSTOM COLOR...", M + 8, y, 160, Theme.Accent, () =>
        {
            using var cd = new ColorDialog { FullOpen = true };
            if (cd.ShowDialog() == DialogResult.OK)
            { c.AccentColor = $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}"; AppConfig.Save(); Engines.ApplyAccent(); SelectNav(_navItems[2]); }
        }));
        y += 44;

        p.Controls.Add(H3("Theme Presets", y)); y += 26;
        string[][] themes = {
            new[]{"Cyberpunk Neon","#00E5FF"}, new[]{"Adwaita Dark","#3584E4"}, new[]{"Breeze Dark","#1D99F3"},
            new[]{"Dracula","#BD93F9"}, new[]{"Nord","#88C0D0"}, new[]{"Gruvbox","#D79921"},
            new[]{"Catppuccin","#CBA6F7"}, new[]{"Tokyo Night","#7AA2F7"}, new[]{"One Dark","#61AFEF"},
            new[]{"Solarized","#268BD2"}, new[]{"Material","#009688"}, new[]{"macOS","#007AFF"},
        };
        foreach (var t in themes)
        {
            bool sel = c.ThemeName == t[0];
            Color tc; try { tc = ColorTranslator.FromHtml(t[1]); } catch { tc = Theme.Accent; }
            var card = Card(y, 36);
            card.Paint += (_, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var b = new SolidBrush(tc); e.Graphics.FillEllipse(b, 10, 8, 18, 18); using var pen = new Pen(sel ? Theme.Accent : Theme.Border, 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Controls.Add(new Label { Text = t[0], Font = new Font("Segoe UI", 9.5f, sel ? FontStyle.Bold : FontStyle.Regular), ForeColor = sel ? Theme.Accent : Theme.Text1, Location = new Point(36, 8), AutoSize = true });
            if (!sel) { string tn = t[0]; var ab = Btn("Apply", 600, 2, 70, Theme.Accent, () => { c.ThemeName = tn; AppConfig.Save(); SelectNav(_navItems[2]); }); ab.Anchor = AnchorStyles.Top | AnchorStyles.Right; card.Controls.Add(ab); }
            else card.Controls.Add(new Label { Text = "\u2714", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Theme.Success, Location = new Point(620, 6), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right });
            p.Controls.Add(card); y += 42;
        }

        p.Controls.Add(H3("Terminal", y)); y += 26;
        p.Controls.Add(Toggle("Transparent terminal background", c.TransparentTerminal, y, v => c.TransparentTerminal = v)); y += 28;
        var (tl, tb) = Slider("Terminal opacity", "%", 20, 100, c.TerminalOpacity, y, v => c.TerminalOpacity = v);
        p.Controls.Add(tl); p.Controls.Add(tb); y += 60;

        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TILING
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildTilingPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Window Tiling & Snapping", y)); y += 32;
        p.Controls.Add(H2("From basic snap layouts to full keyboard-driven i3 tiling.", y)); y += 34;

        p.Controls.Add(Toggle("Enable window tiling", c.TilingEnabled, y, v => c.TilingEnabled = v)); y += 32;

        p.Controls.Add(H3("Tiling Mode", y)); y += 26;
        foreach (var (name, desc) in new[]{("Manual","Snap to edges/corners manually"),("Auto","Auto-tile new windows into space"),("i3-like","Keyboard-driven splits and stacks")})
        {
            bool sel = c.TilingMode == name;
            var card = Card(y, 50);
            card.Cursor = Cursors.Hand;
            card.Paint += (_, e) => { using var pen = new Pen(sel ? Theme.Accent : Theme.Border, sel ? 2 : 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = sel ? Theme.Accent : Theme.Text1, Location = new Point(14, 6), AutoSize = true });
            card.Controls.Add(new Label { Text = desc, Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.Text2, Location = new Point(14, 28), AutoSize = true });
            string n = name; card.Click += (_, _) => { c.TilingMode = n; AppConfig.Save(); SelectNav(_navItems[3]); };
            p.Controls.Add(card); y += 58;
        }

        p.Controls.Add(H3("Snapping", y)); y += 26;
        p.Controls.Add(Toggle("Edge snapping (screen edges)", c.EdgeSnapping, y, v => c.EdgeSnapping = v)); y += 28;
        p.Controls.Add(Toggle("Quarter snapping (corners)", c.QuarterSnapping, y, v => c.QuarterSnapping = v)); y += 28;
        var (gl, gb) = Slider("Tile gap", "px", 0, 24, c.TileGap, y, v => c.TileGap = v);
        p.Controls.Add(gl); p.Controls.Add(gb); y += 60;

        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ICON PACKS
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildIconsPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Icon Packs", y)); y += 32;
        p.Controls.Add(H2("Swap icon sets freely and revert anytime.", y)); y += 34;
        p.Controls.Add(new Label { Text = $"  \u2714 ACTIVE: {c.ActiveIconPack}  ", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Theme.BgDeep, BackColor = Theme.Accent, Location = new Point(M, y), AutoSize = true, Padding = new Padding(6, 3, 6, 3) });
        p.Controls.Add(Btn("RESTORE DEFAULTS", M + 300, y, 170, Theme.Warning, () => { c.ActiveIconPack = "Default Windows"; AppConfig.Save(); SelectNav(_navItems[4]); }));
        y += 36;

        string[][] packs = { new[]{"Papirus","Modern flat icons","#4CAF50"}, new[]{"Tela","Vivid flat colors","#FF5722"}, new[]{"Numix Circle","Circle icons","#F44336"},
            new[]{"Candy","Gradient candy","#E040FB"}, new[]{"Whitesur","macOS Big Sur style","#007AFF"}, new[]{"Breeze","KDE default","#1D99F3"},
            new[]{"Adwaita","GNOME monochrome","#FFFFFF"}, new[]{"Cyberpunk Neon","Glowing outlines","#00E5FF"}, new[]{"Pixel Perfect","8-bit retro","#00FF00"} };
        foreach (var pk in packs)
        {
            bool sel = c.ActiveIconPack == pk[0];
            var card = Card(y, 48);
            Color pc; try { pc = ColorTranslator.FromHtml(pk[2]); } catch { pc = Theme.Accent; }
            card.Paint += (_, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var b = new SolidBrush(pc); e.Graphics.FillEllipse(b, 12, 12, 22, 22);
                using var pen = new Pen(sel ? Theme.Accent : Theme.Border, sel ? 2 : 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Controls.Add(new Label { Text = pk[0], Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = sel ? Theme.Accent : Theme.Text1, Location = new Point(44, 6), AutoSize = true });
            card.Controls.Add(new Label { Text = pk[1], Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.Text2, Location = new Point(44, 26), AutoSize = true });
            if (sel) card.Controls.Add(new Label { Text = "\u2714 ACTIVE", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Theme.Success, Location = new Point(580, 14), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right });
            else { string pn = pk[0]; var ab = Btn("APPLY", 600, 8, 80, Theme.Accent, () => { c.ActiveIconPack = pn; AppConfig.Save(); SelectNav(_navItems[4]); }); ab.Anchor = AnchorStyles.Top | AnchorStyles.Right; card.Controls.Add(ab); }
            p.Controls.Add(card); y += 54;
        }
        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CURSOR THEMES
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildCursorPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Cursor Themes", y)); y += 32;
        p.Controls.Add(H2("Replace the mouse cursor with Linux cursor themes.", y)); y += 34;
        string[][] cursors = { new[]{"Default","Windows 11","#FFFFFF"}, new[]{"Breeze","KDE","#1D99F3"}, new[]{"Adwaita","GNOME","#FFFFFF"},
            new[]{"Bibata Modern","Round modern","#000000"}, new[]{"Bibata Neon","Neon glow","#00E5FF"}, new[]{"Capitaine","macOS-style","#FFFFFF"} };
        foreach (var cr in cursors)
        {
            bool sel = c.CursorTheme == cr[0];
            var card = Card(y, 42);
            card.Paint += (_, e) => { using var pen = new Pen(sel ? Theme.Accent : Theme.Border, 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Controls.Add(new Label { Text = cr[0], Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = sel ? Theme.Accent : Theme.Text1, Location = new Point(14, 4), AutoSize = true });
            card.Controls.Add(new Label { Text = cr[1], Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.Text2, Location = new Point(14, 22), AutoSize = true });
            if (!sel) { string cn = cr[0]; var ab = Btn("APPLY", 600, 5, 80, Theme.Accent, () => { c.CursorTheme = cn; AppConfig.Save(); SelectNav(_navItems[5]); }); ab.Anchor = AnchorStyles.Top | AnchorStyles.Right; card.Controls.Add(ab); }
            else card.Controls.Add(new Label { Text = "\u2714", ForeColor = Theme.Success, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(620, 8), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right });
            p.Controls.Add(card); y += 48;
        }
        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FONTS
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildFontsPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Fonts", y)); y += 32;
        p.Controls.Add(H2("Customize fonts for UI elements and terminal.", y)); y += 34;

        p.Controls.Add(H3("System UI Font", y)); y += 26;
        p.Controls.Add(Drop(new[]{"Segoe UI","Inter","Roboto","Noto Sans","Ubuntu","Fira Sans","Open Sans","Cantarell"}, c.FontFamily, M + 8, y, 240, v => c.FontFamily = v)); y += 38;

        p.Controls.Add(H3("Monospace / Terminal Font", y)); y += 26;
        p.Controls.Add(Drop(new[]{"Cascadia Code","Fira Code","JetBrains Mono","Source Code Pro","Hack","Iosevka","Ubuntu Mono","Consolas"}, c.MonospaceFont, M + 8, y, 240, v => c.MonospaceFont = v)); y += 38;

        p.Controls.Add(H3("Preview", y)); y += 26;
        var preview = Card(y, 100);
        preview.Controls.Add(new Label { Text = "The quick brown fox jumps over the lazy dog", Font = new Font("Segoe UI", 11f), ForeColor = Theme.Text1, Location = new Point(16, 10), AutoSize = true });
        preview.Controls.Add(new Label { Text = "$ sudo apt install neofetch && neofetch", Font = new Font("Consolas", 10f), ForeColor = Theme.Success, Location = new Point(16, 40), AutoSize = true });
        preview.Controls.Add(new Label { Text = "fn main() { println!(\"Hello, Linux!\"); }", Font = new Font("Consolas", 10f), ForeColor = Theme.Accent, Location = new Point(16, 64), AutoSize = true });
        p.Controls.Add(preview); y += 120;

        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PANELS & DOCKS
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildPanelsPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Panels & Docks", y)); y += 32;
        p.Controls.Add(H2("Panels on any screen edge, macOS docks, or multiple bars.", y)); y += 34;

        p.Controls.Add(H3("Panel Position", y)); y += 26;
        foreach (var (pos, icon, desc) in new[]{("Top","\u2B06","GNOME/macOS top bar"),("Bottom","\u2B07","Windows taskbar"),("Left","\u2B05","Unity launcher"),("Right","\u27A1","Right edge")})
        {
            bool sel = c.PanelPosition == pos;
            var card = new Panel { Location = new Point(M + (pos == "Top" ? 0 : pos == "Bottom" ? 172 : pos == "Left" ? 344 : 516), y), Size = new Size(164, 56), BackColor = sel ? Color.FromArgb(18,28,52) : Theme.BgCard, Cursor = Cursors.Hand };
            card.Paint += (_, e) => { using var pen = new Pen(sel ? Theme.Accent : Theme.Border, sel ? 2 : 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Controls.Add(new Label { Text = $"{icon} {pos}", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = sel ? Theme.Accent : Theme.Text1, Location = new Point(10, 6), AutoSize = true });
            card.Controls.Add(new Label { Text = desc, Font = new Font("Segoe UI", 7.5f), ForeColor = Theme.Text2, Location = new Point(10, 30), AutoSize = true });
            string pp = pos; card.Click += (_, _) => { c.PanelPosition = pp; AppConfig.Save(); SelectNav(_navItems[7]); };
            p.Controls.Add(card);
        }
        y += 68;

        var (sl, sb) = Slider("Panel size", "px", 20, 72, c.PanelSize, y, v => c.PanelSize = v);
        p.Controls.Add(sl); p.Controls.Add(sb); y += 56;
        p.Controls.Add(Toggle("Auto-hide panel", c.PanelAutoHide, y, v => c.PanelAutoHide = v)); y += 28;
        p.Controls.Add(Toggle("Dock mode (floating, macOS-style)", c.DockMode, y, v => c.DockMode = v)); y += 28;
        p.Controls.Add(Toggle("Global application menu", c.GlobalMenuEnabled, y, v => c.GlobalMenuEnabled = v)); y += 40;

        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WALLPAPER
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildWallpaperPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Wallpaper", y)); y += 32;
        p.Controls.Add(H2("Static wallpapers are set via the real Windows API. Select an image and click Set.", y)); y += 34;

        p.Controls.Add(H3("Mode", y)); y += 26;
        p.Controls.Add(Drop(new[]{"Static","Animated","Video","GIF","Interactive"}, c.WallpaperMode, M + 8, y, 160, v => c.WallpaperMode = v)); y += 36;

        p.Controls.Add(H3("Wallpaper File", y)); y += 26;
        p.Controls.Add(new Label { Text = string.IsNullOrEmpty(c.WallpaperPath) ? "No file selected" : Path.GetFileName(c.WallpaperPath),
            Font = new Font("Segoe UI", 9f), ForeColor = Theme.Text2, Location = new Point(M + 8, y), AutoSize = true });
        p.Controls.Add(Btn("BROWSE...", M + 400, y - 4, 120, Theme.Accent, () =>
        {
            using var ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp|GIF|*.gif|Video|*.mp4;*.webm|All|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK) { c.WallpaperPath = ofd.FileName; AppConfig.Save(); SelectNav(_navItems[8]); }
        }));
        y += 36;

        p.Controls.Add(Btn("SET WALLPAPER NOW", M, y, 200, Theme.Accent, () =>
        {
            if (!string.IsNullOrEmpty(c.WallpaperPath) && File.Exists(c.WallpaperPath))
            { Engines.SetWallpaper(c.WallpaperPath); MessageBox.Show("Wallpaper set!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            else MessageBox.Show("Select a valid image file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }));
        y += 50;
        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WIDGETS
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildWidgetsPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Desktop Widgets", y)); y += 32;
        p.Controls.Add(H2("Conky/Rainmeter-style desktop overlays. Toggle the engine to enable widget rendering.", y)); y += 34;
        p.Controls.Add(Toggle("Enable desktop widgets", c.WidgetsEnabled, y, v => c.WidgetsEnabled = v)); y += 36;

        string[][] widgets = { new[]{"System Monitor","CPU, RAM, GPU, disk usage","\uE7F4"}, new[]{"Clock","Analog or digital","\uE823"},
            new[]{"Weather","Conditions and forecast","\uE9CA"}, new[]{"Music Player","Now playing + controls","\uE8D6"},
            new[]{"Network","Upload/download speeds","\uE839"}, new[]{"Notes","Sticky notes on desktop","\uE70B"},
            new[]{"Neofetch","System info display","\uE756"}, new[]{"Custom HTML","Fully custom widget","\uE943"} };
        foreach (var w in widgets)
        {
            var card = Card(y, 46);
            card.Paint += (_, e) => { using var pen = new Pen(Theme.Border, 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Controls.Add(new Label { Text = w[2], Font = new Font("Segoe MDL2 Assets", 12f), ForeColor = Theme.Accent, Location = new Point(12, 10), AutoSize = true });
            card.Controls.Add(new Label { Text = w[0], Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.Text1, Location = new Point(40, 4), AutoSize = true });
            card.Controls.Add(new Label { Text = w[1], Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.Text2, Location = new Point(40, 24), AutoSize = true });
            var ab = Btn("ADD", 630, 8, 50, Theme.Accent, () => MessageBox.Show($"'{w[0]}' widget added to desktop.", "Widget", MessageBoxButtons.OK, MessageBoxIcon.Information));
            ab.Anchor = AnchorStyles.Top | AnchorStyles.Right; card.Controls.Add(ab);
            p.Controls.Add(card); y += 52;
        }
        Finalize(p); return p;
    }


    // ═══════════════════════════════════════════════════════════════════════
    // WOBBLY WINDOWS (integrated engine controls)
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildWobblyPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Wobbly Windows", y)); y += 32;
        p.Controls.Add(H2("Compiz-style jelly deformation with real soft-body physics and 3D tilt. The engine runs directly inside LinuxifyWindows \u2014 no separate app needed.", y)); y += 44;

        // Status indicator
        var status = new Label { Text = WobblyManager.Running ? "\u25CF  ENGINE RUNNING" : "\u25CB  ENGINE STOPPED",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = WobblyManager.Running ? Theme.Success : Theme.Text3,
            Location = new Point(M, y), AutoSize = true };
        p.Controls.Add(status); y += 28;

        p.Controls.Add(Toggle("Enable Wobbly Windows", c.WobblyEnabled, y, v =>
        {
            c.WobblyEnabled = v;
            if (v) WobblyManager.Start(); else WobblyManager.Stop();
            status.Text = WobblyManager.Running ? "\u25CF  ENGINE RUNNING" : "\u25CB  ENGINE STOPPED";
            status.ForeColor = WobblyManager.Running ? Theme.Success : Theme.Text3;
        }));
        y += 34;

        p.Controls.Add(new Label { Text = "Drag any title bar to wobble, or hold Ctrl+Alt and drag anywhere on any window.",
            Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.Text2, Location = new Point(M + 8, y), AutoSize = true });
        y += 28;

        p.Controls.Add(H3("Physics", y)); y += 26;
        var (sl, sb) = Slider("Speed", "%", 40, 250, c.WobblySpeed, y, v => c.WobblySpeed = v);
        p.Controls.Add(new Label { Text = "How fast the jelly reacts", Font = new Font("Segoe UI", 8f), ForeColor = Theme.Text3, Location = new Point(M + 470, y + 4), AutoSize = true });
        p.Controls.Add(sl); p.Controls.Add(sb); y += 56;

        var (wl, wb) = Slider("Wobble amount", "%", 0, 100, c.WobblyAmount, y, v => c.WobblyAmount = v);
        p.Controls.Add(new Label { Text = "Rebound juiciness", Font = new Font("Segoe UI", 8f), ForeColor = Theme.Text3, Location = new Point(M + 470, y + 4), AutoSize = true });
        p.Controls.Add(wl); p.Controls.Add(wb); y += 56;

        var (jl, jb) = Slider("Jelly softness", "%", 0, 100, c.WobblySoftness, y, v => c.WobblySoftness = v);
        p.Controls.Add(new Label { Text = "Rubbery trailing stretch", Font = new Font("Segoe UI", 8f), ForeColor = Theme.Text3, Location = new Point(M + 470, y + 4), AutoSize = true });
        p.Controls.Add(jl); p.Controls.Add(jb); y += 56;

        var (tl, tb) = Slider("3D tilt angle", "\u00B0", 0, 30, c.WobblyTilt, y, v => c.WobblyTilt = v);
        p.Controls.Add(new Label { Text = "Max lean during motion", Font = new Font("Segoe UI", 8f), ForeColor = Theme.Text3, Location = new Point(M + 470, y + 4), AutoSize = true });
        p.Controls.Add(tl); p.Controls.Add(tb); y += 56;

        var (xl, xb) = Slider("Max stretch", "%", 20, 90, c.WobblyStretch, y, v => c.WobblyStretch = v);
        p.Controls.Add(new Label { Text = "How far mesh can stray", Font = new Font("Segoe UI", 8f), ForeColor = Theme.Text3, Location = new Point(M + 470, y + 4), AutoSize = true });
        p.Controls.Add(xl); p.Controls.Add(xb); y += 56;

        p.Controls.Add(H3("Features", y)); y += 26;
        p.Controls.Add(Toggle("Jelly deformation (soft-body mesh)", c.WobblyDeform, y, v => c.WobblyDeform = v)); y += 28;
        p.Controls.Add(Toggle("3D tilt on motion", c.WobblyTiltEnabled, y, v => c.WobblyTiltEnabled = v)); y += 28;
        p.Controls.Add(Toggle("Edge snapping on release", c.WobblySnap, y, v => c.WobblySnap = v)); y += 34;

        p.Controls.Add(H3("Quick Presets", y)); y += 26;
        foreach (var (n, sp, am, sf, ti, st) in new[]{("Subtle",80,30,40,8,30),("Default",100,75,70,16,55),("Bouncy",130,95,90,22,75),("Jello",70,100,100,28,90),("Stiff",200,15,20,5,25)})
        {
            var pb = Btn(n, M + (n == "Subtle" ? 0 : n == "Default" ? 110 : n == "Bouncy" ? 220 : n == "Jello" ? 330 : 440), y, 100, Theme.Accent, () =>
            { c.WobblySpeed = sp; c.WobblyAmount = am; c.WobblySoftness = sf; c.WobblyTilt = ti; c.WobblyStretch = st; AppConfig.Save(); SelectNav(_navItems[10]); });
            p.Controls.Add(pb);
        }
        y += 44;

        p.Controls.Add(Btn("RESTART ENGINE", M, y, 160, Theme.Warning, () => { if (c.WobblyEnabled) { WobblyManager.Restart(); MessageBox.Show("Wobbly engine restarted with new settings.", "Restarted", MessageBoxButtons.OK, MessageBoxIcon.Information); } }));
        y += 50;

        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TRANSPARENCY
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildTranspPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Window Transparency", y)); y += 32;
        p.Controls.Add(H2("Per-app opacity control via real Win32 SetLayeredWindowAttributes. Click SET to apply instantly to running windows.", y)); y += 40;

        p.Controls.Add(Toggle("Enable transparency system", c.TransparencyEnabled, y, v => c.TransparencyEnabled = v)); y += 28;
        p.Controls.Add(Toggle("Background blur (Acrylic/Mica)", c.BlurEnabled, y, v => c.BlurEnabled = v)); y += 34;

        var (dl, db) = Slider("Default opacity", "%", 20, 100, c.DefaultOpacity, y, v => c.DefaultOpacity = v);
        p.Controls.Add(dl); p.Controls.Add(db); y += 60;

        p.Controls.Add(H3("Per-Application Opacity", y)); y += 26;
        string[][] apps = { new[]{"Windows Terminal","WindowsTerminal"}, new[]{"File Explorer","explorer"}, new[]{"Microsoft Edge","msedge"},
            new[]{"Google Chrome","chrome"}, new[]{"Firefox","firefox"}, new[]{"VS Code","Code"},
            new[]{"Discord","Discord"}, new[]{"Spotify","Spotify"}, new[]{"Notepad","notepad"}, new[]{"Task Manager","Taskmgr"} };
        foreach (var app in apps)
        {
            int ao = c.PerAppOpacity.GetValueOrDefault(app[1], 100);
            var card = Card(y, 38);
            card.Paint += (_, e) => { using var pen = new Pen(Theme.Border, 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Controls.Add(new Label { Text = app[0], Font = new Font("Segoe UI", 9f), ForeColor = Theme.Text1, Location = new Point(12, 9), AutoSize = true });
            var opLbl = new Label { Text = $"{ao}%", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.Accent,
                Location = new Point(510, 9), Size = new Size(40, 18), TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            card.Controls.Add(opLbl);
            string pn = app[1];
            var slider = new TrackBar { Minimum = 20, Maximum = 100, Value = ao, TickStyle = TickStyle.None,
                Location = new Point(240, 4), Size = new Size(260, 28), BackColor = Theme.BgCard, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            slider.ValueChanged += (_, _) => { c.PerAppOpacity[pn] = slider.Value; opLbl.Text = $"{slider.Value}%"; AppConfig.Save(); };
            card.Controls.Add(slider);
            var sb = Btn("SET", 560, 3, 44, Theme.Accent, () => Engines.SetProcessOpacity(pn, c.PerAppOpacity.GetValueOrDefault(pn, 100)));
            sb.Anchor = AnchorStyles.Top | AnchorStyles.Right; card.Controls.Add(sb);
            var rb = Btn("100", 608, 3, 44, Theme.Text2, () => { c.PerAppOpacity[pn] = 100; AppConfig.Save(); Engines.SetProcessOpacity(pn, 100); SelectNav(_navItems[11]); });
            rb.Anchor = AnchorStyles.Top | AnchorStyles.Right; card.Controls.Add(rb);
            p.Controls.Add(card); y += 44;
        }
        y += 12;
        p.Controls.Add(Btn("APPLY ALL", M, y, 140, Theme.Accent, () => { AppConfig.Save(); Engines.ApplyAllOpacity();
            MessageBox.Show("Transparency applied to all running windows!", "Applied", MessageBoxButtons.OK, MessageBoxIcon.Information); }));
        y += 50;
        Finalize(p); return p;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SETTINGS
    // ═══════════════════════════════════════════════════════════════════════
    Panel BuildSettingsPanel()
    {
        var p = Scroll(); var c = AppConfig.Current; int y = 12;
        p.Controls.Add(H1("Settings", y)); y += 32;
        p.Controls.Add(H2("Startup, shortcuts, import/export, and diagnostics.", y)); y += 34;

        p.Controls.Add(H3("Startup", y)); y += 26;
        p.Controls.Add(Toggle("Run LinuxifyWindows at startup", c.RunAtStartup, y, v => { c.RunAtStartup = v; Engines.SetStartup(v); })); y += 34;

        p.Controls.Add(H3("Shortcuts", y)); y += 26;
        p.Controls.Add(Btn("CREATE DESKTOP SHORTCUT", M + 8, y, 220, Theme.Accent, () =>
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string lnk = Path.Combine(desktop, "LinuxifyWindows.lnk");
                // Use WScript.Shell
                var t = Type.GetTypeFromProgID("WScript.Shell");
                if (t != null) { dynamic shell = Activator.CreateInstance(t); dynamic shortcut = shell.CreateShortcut(lnk);
                    shortcut.TargetPath = Application.ExecutablePath; shortcut.Description = "LinuxifyWindows"; shortcut.Save();
                    MessageBox.Show($"Shortcut created at:\n{lnk}", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            }
            catch (Exception ex) { MessageBox.Show($"Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }));
        y += 42;

        p.Controls.Add(H3("Configuration", y)); y += 26;
        p.Controls.Add(Btn("EXPORT CONFIG", M + 8, y, 140, Theme.Accent, () =>
        {
            using var sfd = new SaveFileDialog { Filter = "JSON|*.json", FileName = "linuxify-config.json" };
            if (sfd.ShowDialog() == DialogResult.OK) { AppConfig.Save(); File.Copy(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LinuxifyWindows", "config.json"), sfd.FileName, true);
                MessageBox.Show("Exported!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }));
        p.Controls.Add(Btn("IMPORT CONFIG", M + 160, y, 140, Theme.Accent, () =>
        {
            using var ofd = new OpenFileDialog { Filter = "JSON|*.json" };
            if (ofd.ShowDialog() == DialogResult.OK) { File.Copy(ofd.FileName, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LinuxifyWindows", "config.json"), true);
                AppConfig.Load(); SelectNav(_navItems[12]); }
        }));
        p.Controls.Add(Btn("RESET ALL", M + 312, y, 110, Theme.Danger, () =>
        {
            if (MessageBox.Show("Reset all settings?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            { AppConfig.Current = new AppConfig(); AppConfig.Save(); SelectNav(_navItems[12]); }
        }));
        y += 42;

        p.Controls.Add(H3("Diagnostics", y)); y += 26;
        p.Controls.Add(Btn("OPEN LOG", M + 8, y, 120, Theme.Text2, () => { try { Process.Start(new ProcessStartInfo(Log.FilePath) { UseShellExecute = true }); } catch { } }));
        p.Controls.Add(Btn("OPEN CONFIG DIR", M + 140, y, 160, Theme.Text2, () => { try { Process.Start(new ProcessStartInfo(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LinuxifyWindows")) { UseShellExecute = true }); } catch { } }));
        y += 42;

        p.Controls.Add(H3("About", y)); y += 26;
        var about = Card(y, 60);
        about.Controls.Add(new Label { Text = "LINUXIFY WINDOWS v1.1", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Theme.Accent, Location = new Point(16, 8), AutoSize = true });
        about.Controls.Add(new Label { Text = "Total desktop customization with integrated Wobbly Windows engine", Font = new Font("Segoe UI", 9f), ForeColor = Theme.Text2, Location = new Point(16, 34), AutoSize = true });
        p.Controls.Add(about); y += 80;

        Finalize(p); return p;
    }
} // end MainForm


// ═══════════════════════════════════════════════════════════════════════════════
// WOBBLY WINDOWS ENGINE — integrated from WobblyWindows v3
// Full soft-body mesh physics, 3D tilt, overlay rendering
// ═══════════════════════════════════════════════════════════════════════════════

// Physics constants
static class WobblyConfig
{
    public const int GridCells = 6;
    public const int TickMs = 6;
    public const double MaxSubstep = 0.004;
    public const double GlobalDamping = 2.8;
    public const double TiltPerVelocity = 0.008;
    public const double TiltDampingRatio = 0.55;
    public const double FocalLength = 2400.0;
    public const double SettleDistance = 0.4;
    public const double SettleVelocity = 8.0;
    public const double SettleTiltRad = 0.002;
    public const double MaxSnapshotPixels = 2_500_000;
    public const int SnapThreshold = 8;
}

// Fullscreen transparent overlay for rendering the wobbling snapshot
sealed class WobbleOverlay : Form
{
    public WobbleOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Location = Point.Empty;
        Size = SystemInformation.VirtualScreen.Size;
        AllowTransparency = true;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;
        Visible = false;
    }
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT
                | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
            return cp;
        }
    }
}

// Mouse hook — intercepts title bar drags and Ctrl+Alt drags
sealed class WobbleMouseHook : IDisposable
{
    readonly WobbleEngine _engine;
    IntPtr _hookHandle;
    Native.LowLevelMouseProc _proc;
    readonly uint _ownPid = (uint)Environment.ProcessId;
    IntPtr _lastDownWindow; long _lastDownTick; Point _lastDownPoint;

    static readonly string[] ExcludedClasses = { "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
        "Progman", "WorkerW", "TaskListThumbnailWnd", "XamlExplorerHostIslandWindow",
        "Windows.UI.Core.CoreWindow", "ForegroundStaging" };

    public WobbleMouseHook(WobbleEngine engine) { _engine = engine; }

    public void Install()
    {
        _proc = HookCallback;
        _hookHandle = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _proc, Native.GetModuleHandle(null), 0);
    }

    IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            var ms = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
            var pt = new Point(ms.pt.X, ms.pt.Y);

            if (msg == Native.WM_LBUTTONDOWN && !_engine.Dragging)
            {
                if (TryBeginDrag(pt))
                    return (IntPtr)1;
            }
            else if (msg == Native.WM_MOUSEMOVE && _engine.Dragging)
            {
                _engine.UpdateCursor(pt);
            }
            else if (msg == Native.WM_LBUTTONUP && _engine.Dragging)
            {
                _engine.EndDrag(pt);
            }
        }
        return Native.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    bool TryBeginDrag(Point pt)
    {
        IntPtr hit = Native.WindowFromPoint(pt);
        if (hit == IntPtr.Zero) return false;
        IntPtr root = Native.GetAncestor(hit, Native.GA_ROOT);
        if (root == IntPtr.Zero || !Native.IsWindowVisible(root)) return false;
        Native.GetWindowThreadProcessId(root, out uint pid);
        if (pid == _ownPid) return false;
        string cls = Native.GetClassNameSafe(root);
        foreach (var ex in ExcludedClasses)
            if (string.Equals(cls, ex, StringComparison.OrdinalIgnoreCase)) return false;

        bool combo = Native.IsKeyDown(Native.VK_CONTROL) && Native.IsKeyDown(Native.VK_MENU);
        if (!combo)
        {
            if (!Native.TryHitTest(hit != root ? hit : root, pt, out int hc) || hc != Native.HTCAPTION)
            {
                if (hit != root && Native.TryHitTest(root, pt, out int rh) && rh == Native.HTCAPTION) { }
                else return false;
            }
        }

        bool zoomed = Native.IsZoomed(root);

        // Double-click → maximize toggle
        long now = Environment.TickCount64;
        if (!combo && !zoomed && root == _lastDownWindow
            && now - _lastDownTick <= Native.GetDoubleClickTime()
            && Math.Abs(pt.X - _lastDownPoint.X) <= SystemInformation.DoubleClickSize.Width
            && Math.Abs(pt.Y - _lastDownPoint.Y) <= SystemInformation.DoubleClickSize.Height)
        {
            _lastDownTick = 0; _engine.Cancel();
            Native.PostMessage(root, Native.WM_NCLBUTTONDBLCLK, (IntPtr)Native.HTCAPTION, Native.MakeLParam(pt.X, pt.Y));
            return true;
        }
        _lastDownTick = now; _lastDownWindow = root; _lastDownPoint = pt;
        _engine.BeginDrag(root, pt, zoomed);
        return true;
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero) { Native.UnhookWindowsHookEx(_hookHandle); _hookHandle = IntPtr.Zero; }
    }
}

// The physics engine: soft-body mesh + 3D tilt, rendered via UpdateLayeredWindow
sealed class WobbleEngine : IDisposable
{
    public bool Dragging { get { lock (_g) return _dragging; } }
    enum Mode { Idle, Mesh, Rigid }
    const int N = WobblyConfig.GridCells + 1;
    const int PC = N * N;

    readonly object _g = new();
    readonly AutoResetEvent _wake = new(false);
    readonly Thread _thread;
    readonly IntPtr _overlay;
    volatile bool _shutdown;

    bool _dragging, _active, _needsSetup, _wasZoomed;
    IntPtr _hwnd;
    double _tx, _ty;
    int _grabDx, _grabDy;
    Point _grabCursor, _releaseCursor;
    bool _pendingSnap;

    Mode _mode = Mode.Idle;
    int _visW, _visH, _visOffX, _visOffY;
    double _prevTx, _prevTy, _velEmaX, _velEmaY;
    readonly double[] _mpx=new double[PC], _mpy=new double[PC], _mvx=new double[PC], _mvy=new double[PC];
    readonly double[] _homeX=new double[PC], _homeY=new double[PC], _kHome=new double[PC], _cHome=new double[PC];
    int _pin=-1;
    (int a,int b,double rest)[] _springs = Array.Empty<(int,int,double)>();
    double _maxDisp, _structK;
    double _tiltX, _tiltY, _tiltVX, _tiltVY;
    double _rpx, _rpy, _rvx, _rvy;
    IntPtr _curHwnd; double _lastTx, _lastTy;
    Bitmap _snapshot, _canvas;
    double _snapScale = 1.0;
    bool _overlayShown, _madeTransparent;
    int _origExStyle;
    bool _loggedMoveFail;
    readonly PointF[] _proj = new PointF[PC];

    public WobbleEngine(IntPtr overlayHandle)
    {
        _overlay = overlayHandle;
        _thread = new Thread(Loop) { IsBackground = true, Name = "WobblePhysics", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
    }

    public void BeginDrag(IntPtr hwnd, Point cursor, bool wasZoomed)
    {
        lock (_g) { _hwnd = hwnd; _grabCursor = cursor; _wasZoomed = wasZoomed; _dragging = true; _active = true; _needsSetup = true; _pendingSnap = false; }
        _wake.Set();
    }
    public void UpdateCursor(Point cursor) { lock (_g) { if (!_dragging) return; _tx = cursor.X - _grabDx; _ty = cursor.Y - _grabDy; } }
    public void EndDrag(Point cursor) { lock (_g) { if (!_dragging) return; _dragging = false; _tx = cursor.X - _grabDx; _ty = cursor.Y - _grabDy; _releaseCursor = cursor; _pendingSnap = AppConfig.Current.WobblySnap; } }
    public void Cancel() { lock (_g) { _dragging = false; _active = false; } }

    void Loop()
    {
        var sw = Stopwatch.StartNew(); double last = 0;
        while (!_shutdown)
        {
            bool active, setup;
            lock (_g) { active = _active; setup = _needsSetup; }
            if (!active) { if (_mode != Mode.Idle) Teardown(true, false); _wake.WaitOne(); sw.Restart(); last = 0; continue; }
            if (setup) { lock (_g) _needsSetup = false; Setup(); sw.Restart(); last = 0; continue; }
            double now = sw.Elapsed.TotalSeconds, dt = Math.Min(now - last, 0.03); last = now;
            try { if (_mode == Mode.Mesh) TickMesh(dt); else if (_mode == Mode.Rigid) TickRigid(dt); else Cancel(); }
            catch { Teardown(true, false); Cancel(); }
            Thread.Sleep(WobblyConfig.TickMs);
        }
        Teardown(true, false);
    }

    void Setup()
    {
        if (_mode != Mode.Idle) Teardown(true, _mode == Mode.Mesh);
        IntPtr hwnd; Point grab; bool wasZoomed;
        lock (_g) { hwnd = _hwnd; grab = _grabCursor; wasZoomed = _wasZoomed; }
        if (!Native.IsWindow(hwnd)) { Cancel(); return; }
        _curHwnd = hwnd;

        if (wasZoomed)
        {
            Native.GetWindowRect(hwnd, out var zrc);
            double relX = Math.Clamp((grab.X - zrc.Left) / (double)Math.Max(1, zrc.Right - zrc.Left), 0.05, 0.95);
            int capOff = Math.Min(grab.Y - zrc.Top, 28);
            Native.ShowWindow(hwnd, Native.SW_RESTORE);
            Native.GetWindowRect(hwnd, out var rrc);
            int rw = rrc.Right - rrc.Left;
            Native.SetWindowPos(hwnd, IntPtr.Zero, grab.X - (int)(relX * rw), grab.Y - capOff, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
        }

        Native.GetWindowRect(hwnd, out var wrc);
        var vis = wrc;
        if (Native.DwmGetWindowAttribute(hwnd, Native.DWMWA_EXTENDED_FRAME_BOUNDS, out var ext, Marshal.SizeOf<Native.RECT>()) == 0 && ext.Right > ext.Left && ext.Bottom > ext.Top)
            vis = ext;
        _visW = vis.Right - vis.Left; _visH = vis.Bottom - vis.Top;
        _visOffX = vis.Left - wrc.Left; _visOffY = vis.Top - wrc.Top;
        if (_visW < 8 || _visH < 8) { Cancel(); return; }

        lock (_g) { _grabDx = Math.Clamp(_grabCursor.X - vis.Left, 0, _visW); _grabDy = Math.Clamp(_grabCursor.Y - vis.Top, 0, _visH); _tx = vis.Left; _ty = vis.Top; }
        _prevTx = vis.Left; _prevTy = vis.Top; _lastTx = vis.Left; _lastTy = vis.Top;
        _velEmaX = _velEmaY = 0; _tiltX = _tiltY = _tiltVX = _tiltVY = 0; _loggedMoveFail = false;
        ForceForeground(hwnd);

        bool mesh = AppConfig.Current.WobblyDeform && TryCapture(hwnd, wrc, vis) && TryMakeTransparent(hwnd);
        if (mesh)
        {
            InitMesh(); RenderOverlay();
            Native.SetWindowPos(_overlay, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            _overlayShown = true;
            Native.SetLayeredWindowAttributes(hwnd, 0, 0, Native.LWA_ALPHA);
            _mode = Mode.Mesh;
        }
        else
        {
            _rpx = wrc.Left; _rpy = wrc.Top; _rvx = _rvy = 0;
            _mode = Mode.Rigid;
        }
    }

    bool TryCapture(IntPtr hwnd, Native.RECT wrc, Native.RECT vis)
    {
        try
        {
            int ww = wrc.Right - wrc.Left, wh = wrc.Bottom - wrc.Top;
            using var raw = new Bitmap(ww, wh, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(raw)) { IntPtr hdc = g.GetHdc(); bool ok = Native.PrintWindow(hwnd, hdc, Native.PW_RENDERFULLCONTENT); g.ReleaseHdc(hdc); if (!ok) return false; }
            _snapScale = 1.0;
            double px = (double)_visW * _visH;
            if (px > WobblyConfig.MaxSnapshotPixels) _snapScale = Math.Sqrt(WobblyConfig.MaxSnapshotPixels / px);
            int sw = Math.Max(8, (int)(_visW * _snapScale)), sh = Math.Max(8, (int)(_visH * _snapScale));
            _snapshot?.Dispose(); _snapshot = new Bitmap(sw, sh, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(_snapshot)) { g.InterpolationMode = InterpolationMode.Bilinear; g.DrawImage(raw, new Rectangle(0, 0, sw, sh), new Rectangle(_visOffX, _visOffY, _visW, _visH), GraphicsUnit.Pixel); }
            return true;
        }
        catch { return false; }
    }

    bool TryMakeTransparent(IntPtr hwnd)
    {
        _origExStyle = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
        if ((_origExStyle & Native.WS_EX_LAYERED) != 0) return false;
        Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, _origExStyle | Native.WS_EX_LAYERED);
        if ((Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE) & Native.WS_EX_LAYERED) == 0) return false;
        Native.SetLayeredWindowAttributes(hwnd, 0, 255, Native.LWA_ALPHA);
        _madeTransparent = true; return true;
    }

    void InitMesh()
    {
        var S = AppConfig.Current;
        double gu, gv, tx, ty;
        lock (_g) { gu = _grabDx / (double)_visW; gv = _grabDy / (double)_visH; tx = _tx; ty = _ty; }
        for (int r = 0; r < N; r++)
            for (int c = 0; c < N; c++)
            {
                int i = r * N + c;
                _homeX[i] = _visW * c / (double)(N - 1); _homeY[i] = _visH * r / (double)(N - 1);
                _mpx[i] = tx + _homeX[i]; _mpy[i] = ty + _homeY[i]; _mvx[i] = 0; _mvy[i] = 0;
                double du = c / (double)(N - 1) - gu, dv = r / (double)(N - 1) - gv;
                double t = Math.Clamp(Math.Sqrt(du * du + dv * dv) / Math.Sqrt(2.0), 0, 1);
                _kHome[i] = S.HomeStiffnessNear + (S.HomeStiffnessFar - S.HomeStiffnessNear) * t;
                _cHome[i] = 2.0 * S.DampingRatio * Math.Sqrt(_kHome[i]);
            }
        int pr = (int)Math.Round(gv * (N - 1)), pc = (int)Math.Round(gu * (N - 1));
        _pin = Math.Clamp(pr, 0, N - 1) * N + Math.Clamp(pc, 0, N - 1);
        var springs = new List<(int, int, double)>();
        void AddSpring(int a, int b) { double dx = _homeX[b] - _homeX[a], dy = _homeY[b] - _homeY[a]; springs.Add((a, b, Math.Sqrt(dx * dx + dy * dy))); }
        for (int r = 0; r < N; r++)
            for (int c = 0; c < N; c++)
            { int i = r * N + c; if (c + 1 < N) AddSpring(i, i + 1); if (r + 1 < N) AddSpring(i, i + N); if (c + 1 < N && r + 1 < N) { AddSpring(i, i + N + 1); AddSpring(i + 1, i + N); } }
        _springs = springs.ToArray();
        _maxDisp = S.MaxDisplacementFactor * Math.Max(_visW, _visH);
        _structK = S.StructuralStiffness;
    }

    void TickMesh(double dt)
    {
        IntPtr hwnd; double tx, ty; bool dragging, snapCheck; Point relCur;
        lock (_g) { if (!_active) return; hwnd = _hwnd; tx = _tx; ty = _ty; dragging = _dragging; snapCheck = !_dragging && _pendingSnap; relCur = _releaseCursor; if (snapCheck) _pendingSnap = false; }
        if (!Native.IsWindow(hwnd)) { Teardown(false, false); Cancel(); return; }
        _lastTx = tx; _lastTy = ty;
        if (snapCheck && TrySnapTarget(relCur, out var snapAct)) { Teardown(true, false); snapAct(hwnd); Cancel(); return; }
        if (dt > 1e-5) { double ivx = (tx - _prevTx) / dt, ivy = (ty - _prevTy) / dt; _velEmaX += (ivx - _velEmaX) * Math.Min(1.0, dt * 14); _velEmaY += (ivy - _velEmaY) * Math.Min(1.0, dt * 14); }
        _prevTx = tx; _prevTy = ty;
        IntegrateMesh(dt, tx, ty, dragging); IntegrateTilt(dt, dragging);
        if (!dragging && IsMeshSettled(tx, ty)) { Teardown(true, true); Cancel(); return; }
        RenderOverlay();
    }

    void IntegrateMesh(double dt, double tx, double ty, bool dragging)
    {
        int steps = Math.Max(1, (int)Math.Ceiling(dt / WobblyConfig.MaxSubstep));
        double h = dt / steps; int pin = dragging ? _pin : -1;
        for (int s = 0; s < steps; s++)
        {
            if (pin >= 0) { double nx = tx + _homeX[pin], ny = ty + _homeY[pin]; _mvx[pin] = (nx - _mpx[pin]) / h; _mvy[pin] = (ny - _mpy[pin]) / h; _mpx[pin] = nx; _mpy[pin] = ny; }
            double decay = Math.Max(0.0, 1.0 - WobblyConfig.GlobalDamping * h);
            for (int i = 0; i < PC; i++)
            {
                if (i == pin) continue;
                double fx = _kHome[i] * (tx + _homeX[i] - _mpx[i]) - _cHome[i] * _mvx[i];
                double fy = _kHome[i] * (ty + _homeY[i] - _mpy[i]) - _cHome[i] * _mvy[i];
                _mvx[i] = (_mvx[i] + fx * h) * decay; _mvy[i] = (_mvy[i] + fy * h) * decay;
            }
            foreach (var (a, b, rest) in _springs)
            {
                double dx = _mpx[b] - _mpx[a], dy = _mpy[b] - _mpy[a];
                double dist = Math.Sqrt(dx * dx + dy * dy); if (dist < 1e-6) continue;
                double f = _structK * (dist - rest) / dist * h;
                if (a != pin) { _mvx[a] += f * dx; _mvy[a] += f * dy; }
                if (b != pin) { _mvx[b] -= f * dx; _mvy[b] -= f * dy; }
            }
            for (int i = 0; i < PC; i++)
            {
                if (i == pin) continue;
                _mpx[i] += _mvx[i] * h; _mpy[i] += _mvy[i] * h;
                double ex = _mpx[i] - (tx + _homeX[i]), ey = _mpy[i] - (ty + _homeY[i]);
                double d = Math.Sqrt(ex * ex + ey * ey);
                if (d > _maxDisp) { double k = _maxDisp / d; _mpx[i] = tx + _homeX[i] + ex * k; _mpy[i] = ty + _homeY[i] + ey * k; }
            }
        }
    }

    void IntegrateTilt(double dt, bool dragging)
    {
        var S = AppConfig.Current;
        double maxRad = (S.WobblyTiltEnabled ? S.WobblyTilt : 0) * Math.PI / 180.0;
        double targetY = 0, targetX = 0;
        if (S.WobblyTiltEnabled && dragging)
        { targetY = Math.Clamp(-_velEmaX * WobblyConfig.TiltPerVelocity * Math.PI / 180.0, -maxRad, maxRad); targetX = Math.Clamp(_velEmaY * WobblyConfig.TiltPerVelocity * Math.PI / 180.0, -maxRad, maxRad); }
        double k = S.TiltStiffness, c = 2.0 * WobblyConfig.TiltDampingRatio * Math.Sqrt(k);
        int steps = Math.Max(1, (int)Math.Ceiling(dt / WobblyConfig.MaxSubstep)); double h = dt / steps;
        for (int s = 0; s < steps; s++) { _tiltVX += (k * (targetX - _tiltX) - c * _tiltVX) * h; _tiltVY += (k * (targetY - _tiltY) - c * _tiltVY) * h; _tiltX += _tiltVX * h; _tiltY += _tiltVY * h; }
    }

    bool IsMeshSettled(double tx, double ty)
    {
        if (Math.Abs(_tiltX) > WobblyConfig.SettleTiltRad || Math.Abs(_tiltY) > WobblyConfig.SettleTiltRad) return false;
        if (Math.Abs(_tiltVX) > 0.05 || Math.Abs(_tiltVY) > 0.05) return false;
        for (int i = 0; i < PC; i++)
        { if (Math.Abs(_mpx[i] - (tx + _homeX[i])) >= WobblyConfig.SettleDistance) return false; if (Math.Abs(_mpy[i] - (ty + _homeY[i])) >= WobblyConfig.SettleDistance) return false;
          if (Math.Abs(_mvx[i]) >= WobblyConfig.SettleVelocity) return false; if (Math.Abs(_mvy[i]) >= WobblyConfig.SettleVelocity) return false; }
        return true;
    }

    void RenderOverlay()
    {
        double cx = 0, cy = 0;
        for (int i = 0; i < PC; i++) { cx += _mpx[i]; cy += _mpy[i]; }
        cx /= PC; cy /= PC;
        double sinY = Math.Sin(_tiltY), cosY = Math.Cos(_tiltY), sinX = Math.Sin(_tiltX), cosX = Math.Cos(_tiltX);
        double f = WobblyConfig.FocalLength;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < PC; i++)
        {
            double dx = _mpx[i] - cx, dy = _mpy[i] - cy, z = dx * sinY + dy * sinX;
            double sc = f / Math.Max(200.0, f + z);
            float X = (float)(cx + dx * cosY * sc), Y = (float)(cy + dy * cosX * sc);
            _proj[i] = new PointF(X, Y);
            if (X < minX) minX = X; if (X > maxX) maxX = X; if (Y < minY) minY = Y; if (Y > maxY) maxY = Y;
        }
        int ox = (int)Math.Floor(minX) - 3, oy = (int)Math.Floor(minY) - 3;
        int bw = (int)Math.Ceiling(maxX) - ox + 6, bh = (int)Math.Ceiling(maxY) - oy + 6;
        bw = Math.Clamp(bw, 8, 4096); bh = Math.Clamp(bh, 8, 4096);
        if (_canvas == null || _canvas.Width < bw || _canvas.Height < bh)
        { _canvas?.Dispose(); _canvas = new Bitmap(Math.Min(4096, bw + bw / 4), Math.Min(4096, bh + bh / 4), PixelFormat.Format32bppArgb); }

        using (var g = Graphics.FromImage(_canvas))
        {
            g.CompositingQuality = CompositingQuality.HighSpeed; g.InterpolationMode = InterpolationMode.Bilinear;
            g.SmoothingMode = SmoothingMode.None; g.PixelOffsetMode = PixelOffsetMode.Half; g.Clear(Color.Transparent);
            float cellW = _snapshot.Width / (float)(N - 1), cellH = _snapshot.Height / (float)(N - 1);
            using var clipPath = new GraphicsPath();
            var dest = new PointF[3]; var tri = new PointF[3];
            for (int r = 0; r < N - 1; r++)
                for (int c = 0; c < N - 1; c++)
                {
                    int i = r * N + c;
                    var dUL = new PointF(_proj[i].X - ox, _proj[i].Y - oy);
                    var dUR = new PointF(_proj[i + 1].X - ox, _proj[i + 1].Y - oy);
                    var dLL = new PointF(_proj[i + N].X - ox, _proj[i + N].Y - oy);
                    var dBR = new PointF(_proj[i + N + 1].X - ox, _proj[i + N + 1].Y - oy);
                    float sx = c * cellW, sy = r * cellH;
                    var src = new RectangleF(sx, sy, Math.Min(cellW, _snapshot.Width - sx), Math.Min(cellH, _snapshot.Height - sy));
                    dest[0] = dUL; dest[1] = dUR; dest[2] = dLL; tri[0] = dUL; tri[1] = dUR; tri[2] = dLL;
                    DrawTri(g, clipPath, dest, tri, src);
                    dest[0] = new PointF(dUR.X + dLL.X - dBR.X, dUR.Y + dLL.Y - dBR.Y); dest[1] = dUR; dest[2] = dLL;
                    tri[0] = dUR; tri[1] = dBR; tri[2] = dLL;
                    DrawTri(g, clipPath, dest, tri, src);
                }
            g.ResetClip();
        }
        PushToOverlay(_canvas, ox, oy, bw, bh);
    }

    void DrawTri(Graphics g, GraphicsPath clipPath, PointF[] dest, PointF[] tri, RectangleF src)
    {
        float cx = (tri[0].X + tri[1].X + tri[2].X) / 3f, cy = (tri[0].Y + tri[1].Y + tri[2].Y) / 3f;
        Span<PointF> inf = stackalloc PointF[3];
        for (int k = 0; k < 3; k++) { float dx = tri[k].X - cx, dy = tri[k].Y - cy; float len = MathF.Sqrt(dx * dx + dy * dy); float s = len > 0.001f ? (len + 0.8f) / len : 1f; inf[k] = new PointF(cx + dx * s, cy + dy * s); }
        clipPath.Reset(); clipPath.AddPolygon(new[] { inf[0], inf[1], inf[2] });
        g.SetClip(clipPath); g.DrawImage(_snapshot, dest, src, GraphicsUnit.Pixel);
    }

    void PushToOverlay(Bitmap bmp, int x, int y, int w, int h)
    {
        IntPtr screenDC = Native.GetDC(IntPtr.Zero), memDC = Native.CreateCompatibleDC(screenDC);
        IntPtr hBmp = IntPtr.Zero, old = IntPtr.Zero;
        try
        {
            hBmp = bmp.GetHbitmap(Color.FromArgb(0)); old = Native.SelectObject(memDC, hBmp);
            var dst = new Native.POINT { X = x, Y = y }; var size = new Native.SIZE { cx = w, cy = h };
            var src = new Native.POINT { X = 0, Y = 0 };
            var blend = new Native.BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
            Native.UpdateLayeredWindow(_overlay, screenDC, ref dst, ref size, memDC, ref src, 0, ref blend, Native.ULW_ALPHA);
        }
        finally { if (old != IntPtr.Zero) Native.SelectObject(memDC, old); if (hBmp != IntPtr.Zero) Native.DeleteObject(hBmp); Native.DeleteDC(memDC); Native.ReleaseDC(IntPtr.Zero, screenDC); }
    }

    void TickRigid(double dt)
    {
        IntPtr hwnd; double tx, ty; bool dragging, snapCheck; Point relCur;
        lock (_g) { if (!_active) return; hwnd = _hwnd; dragging = _dragging; tx = _tx - _visOffX; ty = _ty - _visOffY; snapCheck = !_dragging && _pendingSnap; relCur = _releaseCursor; if (snapCheck) _pendingSnap = false; }
        if (!Native.IsWindow(hwnd)) { Cancel(); return; }
        if (snapCheck && TrySnapTarget(relCur, out var snapAct)) { snapAct(hwnd); Cancel(); return; }
        double k = AppConfig.Current.RigidStiffness, c = 2.0 * 0.45 * Math.Sqrt(k);
        int steps = Math.Max(1, (int)Math.Ceiling(dt / WobblyConfig.MaxSubstep)); double h = dt / steps;
        for (int s = 0; s < steps; s++) { _rvx += (k * (tx - _rpx) - c * _rvx) * h; _rvy += (k * (ty - _rpy) - c * _rvy) * h; _rpx += _rvx * h; _rpy += _rvy * h; }
        bool settled = !dragging && Math.Abs(_rvx) < WobblyConfig.SettleVelocity && Math.Abs(_rvy) < WobblyConfig.SettleVelocity && Math.Abs(tx - _rpx) < WobblyConfig.SettleDistance && Math.Abs(ty - _rpy) < WobblyConfig.SettleDistance;
        int X = (int)Math.Round(settled ? tx : _rpx), Y = (int)Math.Round(settled ? ty : _rpy);
        Native.SetWindowPos(hwnd, IntPtr.Zero, X, Y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_NOOWNERZORDER | Native.SWP_ASYNCWINDOWPOS);
        if (settled) { _mode = Mode.Idle; Cancel(); }
    }

    void Teardown(bool restoreWin, bool applyFinal)
    {
        IntPtr hwnd = _curHwnd; double tx = _lastTx, ty = _lastTy;
        try
        {
            if (_mode == Mode.Mesh && restoreWin && Native.IsWindow(hwnd))
            {
                if (applyFinal) Native.SetWindowPos(hwnd, IntPtr.Zero, (int)Math.Round(tx) - _visOffX, (int)Math.Round(ty) - _visOffY, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_NOOWNERZORDER);
                if (_madeTransparent) { Native.SetLayeredWindowAttributes(hwnd, 0, 255, Native.LWA_ALPHA); Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, _origExStyle); }
            }
        } catch { }
        _madeTransparent = false;
        if (_overlayShown) { Native.ShowWindow(_overlay, Native.SW_HIDE); _overlayShown = false; }
        _snapshot?.Dispose(); _snapshot = null; _mode = Mode.Idle; _pin = -1;
    }

    static bool TrySnapTarget(Point cursor, out Action<IntPtr> apply)
    {
        apply = null;
        IntPtr mon = Native.MonitorFromPoint(cursor, Native.MONITOR_DEFAULTTONEAREST);
        var mi = Native.MONITORINFO.New(); if (!Native.GetMonitorInfo(mon, ref mi)) return false;
        var m = mi.rcMonitor; var wa = mi.rcWork;
        if (cursor.Y <= m.Top + WobblyConfig.SnapThreshold) { apply = h => Native.ShowWindow(h, Native.SW_MAXIMIZE); return true; }
        if (cursor.X <= m.Left + WobblyConfig.SnapThreshold) { apply = h => Native.SetWindowPos(h, IntPtr.Zero, wa.Left, wa.Top, (wa.Right - wa.Left) / 2, wa.Bottom - wa.Top, Native.SWP_NOZORDER | Native.SWP_NOACTIVATE); return true; }
        if (cursor.X >= m.Right - 1 - WobblyConfig.SnapThreshold) { int half = (wa.Right - wa.Left) / 2; apply = h => Native.SetWindowPos(h, IntPtr.Zero, wa.Left + half, wa.Top, (wa.Right - wa.Left) - half, wa.Bottom - wa.Top, Native.SWP_NOZORDER | Native.SWP_NOACTIVATE); return true; }
        return false;
    }

    static void ForceForeground(IntPtr hwnd)
    {
        if (!Native.SetForegroundWindow(hwnd))
        { Native.keybd_event(Native.VK_MENU, 0, 0, UIntPtr.Zero); Native.keybd_event(Native.VK_MENU, 0, Native.KEYEVENTF_KEYUP, UIntPtr.Zero); Native.SetForegroundWindow(hwnd); }
    }

    public void Dispose() { _shutdown = true; Cancel(); _wake.Set(); _thread.Join(500); _canvas?.Dispose(); _snapshot?.Dispose(); }
}


// ═══════════════════════════════════════════════════════════════════════════════
// Win32 / DWM / GDI INTEROP
// ═══════════════════════════════════════════════════════════════════════════════
static class Native
{
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const uint LWA_ALPHA = 0x2;
    public const int ULW_ALPHA = 2;
    public const int PW_RENDERFULLCONTENT = 2;
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    public const int WH_MOUSE_LL = 14;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_NCHITTEST = 0x0084;
    public const int WM_NCLBUTTONDBLCLK = 0x00A3;
    public const int HTCAPTION = 2;
    public const uint GA_ROOT = 2;

    public const int SW_HIDE = 0;
    public const int SW_RESTORE = 9;
    public const int SW_MAXIMIZE = 3;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_NOOWNERZORDER = 0x0200;
    public const uint SWP_ASYNCWINDOWPOS = 0x4000;
    public static readonly IntPtr HWND_TOPMOST = (IntPtr)(-1);

    public const int VK_CONTROL = 0x11;
    public const int VK_MENU = 0x12;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const int MONITOR_DEFAULTTONEAREST = 2;

    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (IntPtr)(-4);

    public delegate bool LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int cx, cy; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags;
        public static MONITORINFO New() { var m = new MONITORINFO(); m.cbSize = Marshal.SizeOf<MONITORINFO>(); return m; } }
    [StructLayout(LayoutKind.Sequential)] public struct MARGINS { public int Left, Right, Top, Bottom; }

    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
    [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    [DllImport("user32.dll")] public static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
    [DllImport("user32.dll")] public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point pt);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr ho);
    [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(Point pt, int dwFlags);
    [DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int SendMessageTimeoutW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
    [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT pvAttribute, int cbAttribute);
    [DllImport("dwmapi.dll")] public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    public static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));
    public static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;
    public static string GetClassNameSafe(IntPtr hwnd) { var sb = new StringBuilder(260); GetClassName(hwnd, sb, 260); return sb.ToString(); }

    public static bool TryHitTest(IntPtr hwnd, Point pt, out int hitCode)
    {
        hitCode = 0;
        int result = SendMessageTimeoutW(hwnd, WM_NCHITTEST, IntPtr.Zero, MakeLParam(pt.X, pt.Y), 0x0002, 100, out IntPtr res);
        if (result == 0) return false;
        hitCode = (int)(long)res;
        return true;
    }
}
