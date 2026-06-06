// MainWindow.Theme.cs
// Partial class — theme switching + ikony v tlačítkách (v0.5+)
// Umístění: OpenClawManager/ (vedle MainWindow.xaml.cs)

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Runtime.InteropServices;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager;

public partial class MainWindow
{
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int DwmDefaultColor = unchecked((int)0xFFFFFFFF);

    private sealed record ButtonVisualState(
        object? Content,
        Style? Style,
        Style? FocusVisualStyle,
        double Height,
        double Width,
        Thickness Margin,
        HorizontalAlignment HorizontalContentAlignment,
        Thickness Padding,
        Thickness BorderThickness,
        Brush Background,
        Brush BorderBrush);

    private sealed record ShellVisualState(
        Brush Background,
        Brush Foreground,
        Brush BorderBrush,
        Thickness BorderThickness,
        Style? Style);
    private sealed record ElementLayoutState(Visibility Visibility, Thickness Margin, string? Text);

    private readonly Dictionary<Button, ButtonVisualState> _buttonVisualStates = new();
    private readonly Dictionary<Control, ShellVisualState> _shellControlStates = new();
    private readonly Dictionary<FrameworkElement, ElementLayoutState> _layoutElementStates = new();
    private readonly Dictionary<TextBlock, Brush> _textBlockForegroundStates = new();
    private readonly Dictionary<MenuItem, object?> _menuItemIcons = new();
    private readonly Dictionary<MenuItem, object?> _menuItemHeaders = new();
    private static readonly Dictionary<string, Style> _themeStyleCache = new();
    private static readonly Dictionary<string, ImageSource> _themeIconSourceCache = new();
    private static readonly FontFamily LegacyIconFontFamily = new("Segoe UI Emoji");
    private static readonly FontFamily LegacyPlayIconFontFamily = new("Segoe UI");
    private static readonly FontFamily ModernDarkUiFontFamily = new("Inter, Segoe UI");
    private static readonly FontFamily ModernDarkCodeFontFamily = new("Cascadia Mono, Consolas");
    private double _mainStatusBarBaselineHeight = double.NaN;
    private Style? _appLogItemContainerStyleBaseline;
    /// <summary>
    /// Cached procedural scanline overlay used by the runtime-generated button feedback styles.
    /// It stays in C# because WPF XAML dictionaries cannot express this DrawingBrush pattern clearly.
    /// </summary>
    private static Brush? _pressedScanlineBrush;
    private AppTheme _activeTheme = AppTheme.Legacy;

    internal static bool IsFramelessTheme(AppTheme theme) =>
        theme != AppTheme.Legacy && theme != AppTheme.StandardDark;

    // DWM caption coloring is intentionally kept in code: WPF ResourceDictionaries cannot
    // set native Windows title-bar attributes for custom chrome windows.
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    // ── Inicializace theme (volat v konstruktoru po InitializeComponent) ──────
    private void InitTheme()
    {
        CaptureThemeBaseline();

        // Přihlásit se na event — přepnutí tématu za běhu (ze SettingsWindow)
        ThemeService.ThemeChanged += OnThemeChanged;

        // Aplikovat téma z nastavení
        var theme = OpenClawManager.App.GetService<ISettingsService>().Settings.Theme;
        ThemeService.Apply(theme);
    }

    // ── Callback při změně tématu (SettingsWindow → Save → ThemeService.Apply) ─
    private void OnThemeChanged(AppTheme theme)
    {
        // Musíme být na UI vlákně
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OnThemeChanged(theme));
            return;
        }

        ApplyThemeToUi(theme);

        if (theme == AppTheme.Legacy)
        {
            StopSplashVideo();
            SplashOverlay.Visibility = Visibility.Collapsed;

            if (Terminal.IsTuiRunning)
            {
                Terminal.HideSplashBorder();
                Terminal.ShowWebView();
            }
            else
            {
                Terminal.ShowSplashBorder();
            }
        }
        else
        {
            Terminal.HideSplashBorder();
            SplashMedia.Visibility = Visibility.Collapsed;
            SplashProgress.Visibility = Visibility.Collapsed;

            if (!Terminal.IsTuiRunning)
                SplashOverlay.Visibility = Visibility.Visible;
        }
    }

    // ── Aplikace tématu na UI ─────────────────────────────────────────────────
    private void ApplyThemeToUi(AppTheme theme)
    {
        _activeTheme = theme;
        RestoreThemeBaseline();
        ApplyThemeTitleBarMode(IsFramelessTheme(theme));

        // Fáze 3: Vrstvené pozadí — viditelné jen v ModernDark/ModernLight
        var showGlass = theme is AppTheme.ModernDark or AppTheme.ModernLight;
        BgWallpaper.Visibility = showGlass ? Visibility.Visible : Visibility.Collapsed;
        BgGlow.Visibility      = showGlass ? Visibility.Visible : Visibility.Collapsed;

        switch (theme)
        {
            case AppTheme.StandardLight:
                ApplyStandardUi();
                ApplyStandardToolLayout();
                ApplyStandardShell();
                break;
            case AppTheme.StandardDark:
                ApplyStandardUi();
                ApplyStandardDarkToolLayout();
                ApplyStandardDarkShell();
                break;
            case AppTheme.HighContrast:
                ApplyModernUi();
                ApplyModernToolLayout();
                ApplyModernPaletteShell();
                break;
            case AppTheme.ModernDark:
                ApplyModernVariantUi();
                ApplyModernToolLayout();
                ApplyModernVariantToolLayout();
                ApplyModernPaletteShell();
                ApplyModernDarkGlassShell();
                break;
            case AppTheme.ModernLight:
                ApplyModernVariantUi();
                ApplyModernToolLayout();
                ApplyModernVariantToolLayout();
                ApplyModernPaletteShell();
                break;
            case AppTheme.CrabCute:
                ApplyCrabCuteUi();
                break;
            case AppTheme.Legacy:
            default:
                ApplyLegacyUi();
                break;
        }
    }

    // ── Modern UI ─────────────────────────────────────────────────────────────
    private void ApplyStandardModernUi()
    {
        SetButtonIcon(BtnGatewayStart,    "start");
        SetButtonIcon(BtnGatewayStop,     "stop");
        SetButtonIcon(BtnGatewayRestart,  "restart");
        SetButtonIcon(BtnOpenPowerShell,  "powershell");
        SetButtonIcon(BtnOpenGatewayLog,  "gateway-log");
        SetButtonIcon(BtnCleaningTool,    "cleaning-tool");
        SetButtonIcon(BtnTokenManager,    "token-manager");
        SetButtonIcon(BtnDoctorFix,       "doctor-fix");
    }

    private void ApplyStandardUi()
    {
        ApplyStandardModernUi();
        ApplyStandardButtonFeedbackStyle(
            BtnStartTui,
            BtnGatewayStart,
            BtnGatewayStop,
            BtnGatewayRestart,
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            BtnCleaningTool,
            BtnTokenManager,
            BtnDoctorFix);

        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    private void ApplyModernUi()
    {
        // Ikony tlačítek — nahradit emoji za PNG ikony
        SetButtonIcon(BtnGatewayStart,    "start");
        SetButtonIcon(BtnGatewayStop,     "stop");
        SetButtonIcon(BtnGatewayRestart,  "restart");
        SetButtonIcon(BtnOpenPowerShell,  "powershell");
        SetButtonIcon(BtnOpenGatewayLog,  "gateway-log");
        SetButtonIcon(BtnCleaningTool,    "cleaning-tool");
        SetButtonIcon(BtnTokenManager,    "token-manager");
        SetButtonIcon(BtnDoctorFix,       "doctor-fix");
        ApplyModernButtonFeedbackStyle(
            BtnStartTui,
            BtnGatewayStart,
            BtnGatewayStop,
            BtnGatewayRestart,
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            BtnCleaningTool,
            BtnTokenManager,
            BtnDoctorFix);

        // TUI tlačítko — ponechat dynamický stav (UpdateStartTuiButton ho řídí)
        // Ikony Start/Stop se přepínají v UpdateStartTuiButton()
        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    private void ApplyModernVariantUi()
    {
        SetModernVariantButtonImage(BtnGatewayStart,   "start", 44, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnGatewayStop,    "stop", 44, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnGatewayRestart, "restart", 44, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnOpenPowerShell, "powershell", 56, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnOpenGatewayLog, "gateway-log", 56, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnCleaningTool,   "cleaning-tool", 56, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnTokenManager,   "token-manager", 56, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnDoctorFix,      "doctor-fix", 56, HorizontalAlignment.Center);

        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    // ── CrabCute UI ──────────────────────────────────────────────────────────
    private void ApplyCrabCuteUi()
    {
        ApplyCrabCuteShell();
        ApplyCrabCuteToolLayout();

        SetCrabCuteButtonImage(BtnGatewayStart,   "start", 44);
        SetCrabCuteButtonImage(BtnGatewayStop,    "stop", 44);
        SetCrabCuteButtonImage(BtnGatewayRestart, "restart", 44);
        SetCrabCuteButtonImage(BtnOpenPowerShell, "powershell", 44);
        SetCrabCuteButtonImage(BtnOpenGatewayLog, "gateway-log", 44);
        SetCrabCuteButtonImage(BtnCleaningTool,   "cleaning-tool", 44);
        SetCrabCuteButtonImage(BtnTokenManager,   "token-manager", 44);
        SetCrabCuteButtonImage(BtnDoctorFix,      "doctor-fix", 44);

        SetMenuIcon(MnuOpenPowerShell, "menu-powershell");
        SetMenuIcon(MnuOpenGatewayLog, "log");
        SetMenuIcon(MnuSettings, "menu-settings");
        SetMenuIcon(MnuMenuSettings, "menu-settings");

        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    private void ApplyCrabCuteShell()
    {
        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0xEE, 0xF1, 0xF5));
        var surface = ThemeService.GetBrush("Theme.Brush.Surface", Color.FromRgb(0xEE, 0xF1, 0xF5));
        var border = ThemeService.GetBrush("Theme.Brush.Border", Color.FromRgb(0xEE, 0xF1, 0xF5));
        var text = ThemeService.GetBrush("Theme.Brush.Text.Primary", Color.FromRgb(0x17, 0x20, 0x33));
        var secondary = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x5F, 0x6B, 0x7A));

        Background = background;
        Foreground = text;

        TitleBarHost.Background = surface;
        ApplyCaptionButtonVisuals(text, border, border);

        MainMenu.Background = surface;
        MainMenu.Foreground = text;
        MainStatusBar.Background = surface;
        MainStatusBar.Foreground = secondary;
        MainGridSplitter.Background = border;

        foreach (var group in new[] { GrpActions, GrpLatency, GrpAppLog })
        {
            group.Background = surface;
            group.Foreground = text;
            group.BorderBrush = border;
            group.BorderThickness = new Thickness(1);
        }

        AppLog.Background = surface;
        AppLog.Foreground = text;
        AppLog.BorderBrush = border;
    }

    private void ApplyModernPaletteShell()
    {
        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x19, 0x19, 0x19));
        var surface = ThemeService.GetBrush("Theme.Brush.Surface", Colors.White);
        var chrome = ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0x12, 0x12, 0x12));
        var text = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x9E, 0x9E, 0x9E));
        var primaryText = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        var secondary = text;

        Background = chrome;
        Foreground = text;

        var titleBar = ThemeService.GetBrush("Theme.Brush.TitleBar",       Color.FromRgb(0x20, 0x20, 0x20));
        var menuBg   = ThemeService.GetBrush("Theme.Brush.MenuBackground", Color.FromRgb(0x18, 0x18, 0x18));

        MainMenu.Background      = menuBg;
        MainMenu.Foreground      = text;
        TitleBarHost.Background  = titleBar;
        MainStatusBar.Background = titleBar;
        MainStatusBar.Foreground = secondary;
        MainStatusBar.Resources[typeof(Separator)] = CreateHiddenSeparatorStyle();
        MainGridSplitter.Background = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));

        GrpActions.Background = Brushes.Transparent;
        GrpActions.Foreground = text;
        GrpActions.BorderBrush = Brushes.Transparent;
        GrpActions.BorderThickness = new Thickness(0);
        GrpActions.Style = FindThemeStyle("Style.GroupBox.Hidden");

        foreach (var group in new[] { GrpLatency, GrpAppLog })
        {
            group.Background = surface;
            group.Foreground = text;
            group.BorderBrush = Brushes.Transparent;
            group.BorderThickness = new Thickness(0);
            group.Style = FindThemeStyle("Style.GroupBox.ModernPalette");
        }

        AppLog.Background = surface;
        AppLog.Foreground = text;
        AppLog.BorderBrush = Brushes.Transparent;
        AppLog.BorderThickness = new Thickness(0);
        RightPanel.Background = chrome;
        SplashOverlay.Background = menuBg;
        Terminal.SetShellBackground(chrome);

        var hoverBackground = ThemeService.GetBrush("Theme.Brush.Menu.Hover", Color.FromRgb(0x27, 0x27, 0x27));
        ApplyModernPaletteMenuVisuals(text, text, chrome, background, hoverBackground);
        ApplyCaptionButtonVisuals(text, hoverBackground, ThemeService.GetBrush("Theme.Brush.Pressed", Color.FromRgb(0x30, 0x30, 0x30)));
        ApplyModernPaletteButtonText();
        ApplyModernPaletteMainWindowText(primaryText, secondary);

        var captionText = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        ApplyWindowCaptionColor(GetBrushColor(titleBar, Color.FromRgb(0x20, 0x20, 0x20)), GetBrushColor(captionText, Colors.White));
    }

    private void ApplyStandardShell()
    {
        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0xF4, 0xF6, 0xFA));
        var surface = ThemeService.GetBrush("Theme.Brush.Surface", Colors.White);
        var chrome = ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0xF4, 0xF6, 0xFA));
        var border = ThemeService.GetBrush("Theme.Brush.Border", Color.FromRgb(0xE5, 0xE7, 0xEB));
        var primaryText = ThemeService.GetBrush("Theme.Brush.Text.Primary", Color.FromRgb(0x1A, 0x1A, 0x2E));
        var secondaryText = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x6B, 0x72, 0x80));
        var hover = ThemeService.GetBrush("Theme.Brush.Menu.Hover", Color.FromRgb(0xE5, 0xE7, 0xEB));
        var splashBackground = ThemeService.GetBrush("Brush.SplashModernBackground", Color.FromRgb(0x4C, 0x24, 0x7E));

        Background = background;
        Foreground = primaryText;

        TitleBarHost.Background = chrome;
        ApplyCaptionButtonVisuals(primaryText, hover, border);

        MainMenu.Background = chrome;
        MainMenu.Foreground = secondaryText;
        MainStatusBar.Background = chrome;
        MainStatusBar.Foreground = secondaryText;
        MainStatusBar.Resources[typeof(Separator)] = BuildStandardSeparatorStyle(border);
        MainGridSplitter.Background = border;

        foreach (var group in new[] { GrpActions, GrpTools, GrpLatency, GrpAppLog })
        {
            group.Style = FindThemeStyle("Style.GroupBox.Standard");
            group.Background = surface;
            group.Foreground = primaryText;
            group.BorderBrush = border;
            group.BorderThickness = new Thickness(1);
        }

        AppLog.Background = surface;
        AppLog.Foreground = primaryText;
        AppLog.BorderBrush = border;
        AppLog.BorderThickness = new Thickness(1);

        RightPanel.Background = splashBackground;
        SplashOverlay.Background = splashBackground;
        SplashProgress.Foreground = border;
        Terminal.SetShellBackground(splashBackground);

        ApplyModernPaletteMenuVisuals(primaryText, secondaryText, chrome, surface, hover);
        ApplyStandardMainWindowText(primaryText, secondaryText, primaryText);
        ApplyWindowCaptionColor(null, null);
    }

    private void ApplyStandardDarkShell()
    {
        // Všechny barvy z XAML tokenů — žádné hardcoded hodnoty
        var chrome    = ThemeService.GetBrush("Theme.Brush.Chrome",         Color.FromRgb(0x12, 0x12, 0x12));
        var bg        = ThemeService.GetBrush("Theme.Brush.Background",     Color.FromRgb(0x28, 0x28, 0x28));
        var active    = ThemeService.GetBrush("Theme.Brush.Active",         Color.FromRgb(0x38, 0x38, 0x38));
        var hover     = ThemeService.GetBrush("Theme.Brush.Hover",          Color.FromRgb(0x46, 0x46, 0x46));
        var pressed   = ThemeService.GetBrush("Theme.Brush.Pressed",        Color.FromRgb(0x53, 0x53, 0x53));
        var separator = ThemeService.GetBrush("Theme.Brush.Separator",      Color.FromRgb(0x1E, 0x1E, 0x1E));
        var primary   = ThemeService.GetBrush("Theme.Brush.Text.Primary",   Colors.White);
        var secondary = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x78, 0x78, 0x78));
        var menuHover = ThemeService.GetBrush("Theme.Brush.Menu.Hover",     Color.FromRgb(0x38, 0x38, 0x38));

        // ── Okno ─────────────────────────────────────────────────────────────
        Background = bg;
        Foreground = secondary;

        // ── Title/menu bar visuals ───────────────────────────────────────────
        TitleBarHost.Background = chrome;
        ApplyCaptionButtonVisuals(primary, hover, pressed);

        // ── Menu ─────────────────────────────────────────────────────────────
        MainMenu.Background = chrome;
        MainMenu.Foreground = secondary;
        ApplyModernPaletteMenuVisuals(primary, secondary, chrome, bg, menuHover);

        // ── Status bar ───────────────────────────────────────────────────────
        MainStatusBar.Background = chrome;
        MainStatusBar.Foreground = secondary;
        MainStatusBar.Resources[typeof(Separator)] = CreateHiddenSeparatorStyle();

        // ── GridSplitter — 2px, decentní ─────────────────────────────────────
        MainGridSplitter.Width = 2;
        MainGridSplitter.Background = separator;

        // ── GrpActions — průhledný, bez headeru ──────────────────────────────
        GrpActions.Style          = FindThemeStyle("Style.GroupBox.Hidden");
        GrpActions.Background     = Brushes.Transparent;
        GrpActions.BorderBrush    = Brushes.Transparent;
        GrpActions.BorderThickness = new Thickness(0);

        // ── GrpTools — průhledný, bez headeru (Nástroje splývají s bg) ───────
        GrpTools.Style          = FindThemeStyle("Style.GroupBox.Hidden");
        GrpTools.Background     = Brushes.Transparent;
        GrpTools.BorderBrush    = Brushes.Transparent;
        GrpTools.BorderThickness = new Thickness(0);

        // ── GrpLatency + GrpAppLog — flat Active (#383838), bez headeru ──────
        foreach (var grp in new[] { GrpLatency, GrpAppLog })
        {
            grp.Style          = FindThemeStyle("Style.GroupBox.Hidden");
            grp.Background     = active;
            grp.Foreground     = secondary;
            grp.BorderBrush    = Brushes.Transparent;
            grp.BorderThickness = new Thickness(0);
        }

        AppLog.Background      = active;
        AppLog.Foreground      = secondary;
        AppLog.BorderBrush     = Brushes.Transparent;
        AppLog.BorderThickness = new Thickness(0);

        // ── TUI okno — nejtmavší (#121212 = Chrome) ───────────────────────────
        RightPanel.Background    = chrome;
        SplashOverlay.Background = chrome;
        Terminal.SetShellBackground(chrome);
        SplashProgress.Foreground = hover;

        // ── Akce tlačítka (TUI + Gateway) — výrazná idle ────────────────────
        ApplyStandardActiveButtonStyle(active, hover, pressed, primary,
            BtnStartTui,
            BtnGatewayStart,
            BtnGatewayStop,
            BtnGatewayRestart);

        // ── Nástroje tlačítka — splývají s pozadím a zesílí až na hover ──────
        ApplyStandardToolButtonStyle(bg, active, primary,
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            BtnCleaningTool,
            BtnTokenManager,
            BtnDoctorFix);

        // ── Texty ─────────────────────────────────────────────────────────────
        ApplyStandardMainWindowText(secondary, secondary, primary);
        ApplyWindowCaptionColor(
            GetBrushColor(chrome, Color.FromRgb(0x12, 0x12, 0x12)),
            GetBrushColor(primary, Colors.White));
    }

    private void ApplyModernPaletteMenuVisuals(
        Brush foreground,
        Brush topLevelForeground,
        Brush background,
        Brush popupBackground,
        Brush hoverBackground)
    {
        MainMenu.Resources[typeof(MenuItem)] = FindThemeStyle("Style.MenuItem.Dark");
        MainMenu.Resources[typeof(Separator)] = CreateHiddenSeparatorStyle();
    }

    private void ApplyThemeTitleBarMode(bool useCustomTitleBar)
    {
        if (useCustomTitleBar)
        {
            MainMenu.VerticalAlignment = VerticalAlignment.Stretch;
            MainMenu.Padding = new Thickness(0);
            MainMenu.Margin = new Thickness(0);
            return;
        }

        TitleBarHost.ClearValue(Border.BackgroundProperty);
        MainMenu.ClearValue(FrameworkElement.VerticalAlignmentProperty);
        MainMenu.ClearValue(Control.PaddingProperty);
        MainMenu.ClearValue(FrameworkElement.MarginProperty);
    }

    private void ConfigureWindowChromeForStartup(AppTheme theme)
    {
        if (IsFramelessTheme(theme))
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 46,
                CornerRadius = theme == AppTheme.ModernDark ? new CornerRadius(8) : new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(6),
                UseAeroCaptionButtons = false
            });

            TitleBarHost.Height = 46;
            CaptionButtons.Visibility = Visibility.Visible;
            UpdateMaximizeGlyph();
            return;
        }

        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResize;
        WindowChrome.SetWindowChrome(this, null);
        TitleBarHost.ClearValue(FrameworkElement.HeightProperty);
        CaptionButtons.Visibility = Visibility.Collapsed;
    }

    private void ApplyCaptionButtonVisuals(Brush foreground, Brush hoverBackground, Brush pressedBackground)
    {
        var style = CreateCaptionButtonStyle(hoverBackground, pressedBackground);

        foreach (var button in new[] { BtnWindowMinimize, BtnWindowMaximize, BtnWindowClose })
        {
            button.Style = style;
            button.Background = Brushes.Transparent;
            button.Foreground = foreground;
            button.BorderBrush = Brushes.Transparent;
            button.BorderThickness = new Thickness(0);
        }
    }

    private void ApplyModernDarkCaptionButtonVisuals()
    {
        var captionStyle = FindThemeStyle("Theme.Style.CaptionButton");
        var closeStyle = FindThemeStyle("Theme.Style.CaptionButton.Close");

        BtnWindowMinimize.Style = captionStyle;
        BtnWindowMaximize.Style = captionStyle;
        BtnWindowClose.Style = closeStyle;

        foreach (var button in new[] { BtnWindowMinimize, BtnWindowMaximize, BtnWindowClose })
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.BorderThickness = new Thickness(0);
            button.Padding = new Thickness(0);
            button.FocusVisualStyle = null;
        }
    }

    private static Style CreateCaptionButtonStyle(Brush hoverBackground, Brush pressedBackground)
    {
        var cacheKey = $"caption|{BrushCacheKey(hoverBackground)}|{BrushCacheKey(pressedBackground)}";
        return GetCachedStyle(cacheKey, () =>
        {
        var root = new FrameworkElementFactory(typeof(Border));
        root.Name = "Root";
        root.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        root.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        root.SetValue(Border.BorderThicknessProperty, new Thickness(0));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        root.AppendChild(presenter);

        var template = new ControlTemplate(typeof(Button)) { VisualTree = root };

        var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBackground, "Root"));

        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(Border.BackgroundProperty, pressedBackground, "Root"));

        template.Triggers.Add(hover);
        template.Triggers.Add(pressed);

        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        return style;
        });
    }

    private static Style CreateHiddenSeparatorStyle()
    {
        return GetCachedStyle("separator|hidden", () =>
        {
        var style = new Style(typeof(Separator));
        style.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        return style;
        });
    }

    private static Style BuildStandardSeparatorStyle(Brush brush)
    {
        var cacheKey = $"separator|standard|{BrushCacheKey(brush)}";
        return GetCachedStyle(cacheKey, () =>
        {
        var style = new Style(typeof(Separator));
        style.Setters.Add(new Setter(Control.BackgroundProperty, brush));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, brush));
        style.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0));
        return style;
        });
    }

    private static Style CreateModernDarkMenuSeparatorStyle()
    {
        return GetCachedStyle("separator|modern-dark-menu|555958", () =>
        {
        var brush = new SolidColorBrush(Color.FromRgb(0x55, 0x59, 0x58));

        var root = new FrameworkElementFactory(typeof(Border));
        root.SetValue(FrameworkElement.HeightProperty, 1.0);
        root.SetValue(FrameworkElement.MarginProperty, new Thickness(12, 6, 12, 6));
        root.SetValue(Border.BackgroundProperty, brush);

        var style = new Style(typeof(Separator));
        style.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(typeof(Separator)) { VisualTree = root }));
        style.Setters.Add(new Setter(Control.BackgroundProperty, brush));
        style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 13.0));
        return style;
        });
    }

    private void ApplyModernPaletteButtonText()
    {
        var buttonText = ThemeService.GetBrush("Theme.Brush.ButtonText", Colors.Black);

        foreach (var label in new[]
        {
            BtnStartTuiLabel,
            BtnStartTuiSubLabel,
            BtnGatewayStartLabel,
            BtnGatewayStopLabel,
            BtnGatewayRestartLabel,
            BtnPowerShellLabel,
            BtnGatewayLogLabel,
            BtnCleaningToolLabel,
            BtnTokenManagerLabel,
            BtnDoctorFixLabel
        })
        {
            label.Foreground = buttonText;
        }
    }

    private void ApplyModernPaletteMainWindowText(Brush primary, Brush secondary)
    {
        foreach (var textBlock in new[]
        {
            TxtGatewayLabel,
            TxtSectionOpen,
            TxtSectionTools,
            TxtSectionMaintenance,
            TxtLatencyLast,
            TxtLatencyAvg,
            TxtLatencyMax,
            TxtLatencyCount
        })
        {
            textBlock.Foreground = primary;
        }

        foreach (var textBlock in new[]
        {
            LatencyLast,
            LatencyAvg,
            LatencyMax,
            LatencyCount,
            StatusGatewayText,
            StatusGatewayPid,
            StatusGatewayUptime,
            StatusRam,
            StatusVram,
            StatusCpu
        })
        {
            textBlock.Foreground = secondary;
        }

        foreach (var item in EnumerateVisualChildren(MainStatusBar).OfType<TextBlock>())
            item.Foreground = secondary;

        GrpActions.Foreground = primary;
        GrpLatency.Foreground = primary;
        GrpAppLog.Foreground = primary;
        AppLog.Foreground = secondary;
        MainStatusBar.Foreground = secondary;
    }

    private void ApplyStandardMainWindowText(Brush primaryText, Brush secondaryText, Brush buttonText)
    {
        foreach (var textBlock in new[]
        {
            TxtLatencyLast,
            TxtLatencyAvg,
            TxtLatencyMax,
            TxtLatencyCount,
            LatencyLast,
            LatencyAvg,
            LatencyMax,
            LatencyCount
        })
        {
            textBlock.Foreground = secondaryText;
        }

        foreach (var textBlock in new[]
        {
            TxtGatewayLabel,
            TxtSectionOpen,
            TxtSectionTools,
            TxtSectionMaintenance,
            StatusAppVersion
        })
        {
            textBlock.Foreground = primaryText;
        }

        foreach (var label in new[]
        {
            BtnStartTuiLabel,
            BtnStartTuiSubLabel,
            BtnGatewayStartLabel,
            BtnGatewayStopLabel,
            BtnGatewayRestartLabel,
            BtnPowerShellLabel,
            BtnGatewayLogLabel,
            BtnCleaningToolLabel,
            BtnTokenManagerLabel,
            BtnDoctorFixLabel
        })
        {
            label.Foreground = buttonText;
        }

        foreach (var item in EnumerateVisualChildren(MainStatusBar).OfType<TextBlock>())
            item.Foreground = secondaryText;

        GrpActions.Foreground = primaryText;
        GrpTools.Foreground = primaryText;
        GrpLatency.Foreground = primaryText;
        GrpAppLog.Foreground = primaryText;
        AppLog.Foreground = primaryText;
        MainStatusBar.Foreground = secondaryText;
    }

    private void AlignToolButtonsLeft()
    {
        foreach (var button in new[]
        {
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            BtnCleaningTool,
            BtnTokenManager,
            BtnDoctorFix
        })
        {
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            if (button.Content is StackPanel stack)
            {
                stack.HorizontalAlignment = HorizontalAlignment.Left;
                stack.VerticalAlignment = VerticalAlignment.Center;
            }
        }
    }

    private static IEnumerable<DependencyObject> EnumerateVisualChildren(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in EnumerateVisualChildren(child))
                yield return descendant;
        }
    }

    // ── StandardDark layout ───────────────────────────────────────────────────

    private void ApplyStandardDarkToolLayout()
    {
        // Základ: přesunout Nástroje tlačítka do GrpTools, skrýt nadpisy sekcí
        ApplyStandardToolLayout();

        // StandardDark-specifické marginy
        BtnStartTui.Margin       = new Thickness(6, 6, 6, 4);
        BtnGatewayStart.Margin   = new Thickness(6, 0, 3, 3);
        BtnGatewayStop.Margin    = new Thickness(3, 0, 6, 3);
        BtnGatewayRestart.Margin = new Thickness(6, 0, 6, 6);
        BtnOpenPowerShell.Margin = new Thickness(6, 0, 6, 2);
        BtnOpenGatewayLog.Margin = new Thickness(6, 0, 6, 6);
        BtnCleaningTool.Margin   = new Thickness(6, 0, 6, 2);
        BtnTokenManager.Margin   = new Thickness(6, 0, 6, 2);
        BtnDoctorFix.Margin      = new Thickness(6, 0, 6, 6);

        GrpLatency.Margin = new Thickness(0, 4, 0, 2);
        GrpAppLog.Margin  = new Thickness(0, 2, 0, 0);
    }

    // Akce tlačítka — výrazná idle, hover, pressed
    private static void ApplyStandardActiveButtonStyle(
        Brush idle, Brush hover, Brush pressed, Brush textBrush,
        params Button[] buttons)
    {
        var style = FindThemeStyle("Style.Button.StandardFlat");
        foreach (var btn in buttons)
        {
            btn.Style = style;
            btn.Background = idle;
            btn.Foreground = textBrush;
            btn.BorderBrush = Brushes.Transparent;
            btn.BorderThickness = new Thickness(0);
            btn.FocusVisualStyle = null;
        }
    }

    // Nástroje tlačítka — splývají s bg idle, hover = active
    private static void ApplyStandardToolButtonStyle(
        Brush idle, Brush hoverColor, Brush textBrush,
        params Button[] buttons)
    {
        var style = FindThemeStyle("Style.Button.StandardFlat");
        foreach (var btn in buttons)
        {
            btn.Style = style;
            btn.Background = idle;
            btn.Foreground = textBrush;
            btn.BorderBrush = Brushes.Transparent;
            btn.BorderThickness = new Thickness(0);
            btn.FocusVisualStyle = null;
        }
    }

    private void ApplyModernToolLayout()
    {
        TxtGatewayLabel.Visibility = Visibility.Collapsed;
        TxtSectionOpen.Text = L10n.Get("Str_Section_Tools");
        TxtSectionOpen.Visibility = ThemeService.IsModernPaletteTheme(OpenClawManager.App.GetService<ISettingsService>().Settings.Theme)
            ? Visibility.Collapsed
            : Visibility.Visible;
        TxtSectionTools.Visibility = Visibility.Collapsed;
        TxtSectionMaintenance.Visibility = Visibility.Collapsed;

        BtnStartTui.Margin = new Thickness(0, 0, 0, 8);
        BtnGatewayRestart.Margin = new Thickness(0, 0, 0, 8);
        BtnOpenPowerShell.Margin = new Thickness(0, 0, 0, 8);
        BtnOpenGatewayLog.Margin = new Thickness(0, 0, 0, 8);
        BtnCleaningTool.Margin = new Thickness(0, 0, 0, 8);
        BtnTokenManager.Margin = new Thickness(0, 0, 0, 8);
    }

    private void ApplyStandardToolLayout()
    {
        RestoreToolControlsToActions();

        GrpTools.Visibility = Visibility.Visible;
        TxtGatewayLabel.Visibility = Visibility.Collapsed;
        TxtSectionOpen.Visibility = Visibility.Collapsed;
        TxtSectionTools.Visibility = Visibility.Collapsed;
        TxtSectionMaintenance.Visibility = Visibility.Collapsed;

        foreach (var element in new UIElement[]
        {
            TxtSectionOpen,
            TxtSectionTools,
            TxtSectionMaintenance
        })
        {
            RemoveFromCurrentPanel(element);
        }

        var toolButtons = new[]
        {
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            BtnCleaningTool,
            BtnTokenManager,
            BtnDoctorFix
        };

        for (var i = 0; i < toolButtons.Length; i++)
        {
            var button = toolButtons[i];
            RemoveFromCurrentPanel(button);
            ToolsStack.Children.Add(button);
            button.Margin = new Thickness(0, 0, 0, i == toolButtons.Length - 1 ? 0 : 6);
            button.HorizontalContentAlignment = HorizontalAlignment.Left;

            if (button.Content is StackPanel stack)
            {
                stack.HorizontalAlignment = HorizontalAlignment.Left;
                stack.VerticalAlignment = VerticalAlignment.Center;
            }
        }
    }

    private void ApplyCrabCuteToolLayout()
    {
        TxtSectionOpen.Text = "Nástroje:";
        TxtSectionTools.Visibility = Visibility.Collapsed;
        TxtSectionMaintenance.Visibility = Visibility.Collapsed;

        BtnGatewayRestart.Margin = new Thickness(0, 0, 0, 4);
        BtnOpenPowerShell.Margin = new Thickness(0, 0, 0, 2);
        BtnOpenGatewayLog.Margin = new Thickness(0, 0, 0, 2);
        BtnCleaningTool.Margin = new Thickness(0, 0, 0, 2);
        BtnTokenManager.Margin = new Thickness(0, 0, 0, 2);
    }

    private void ApplyModernVariantToolLayout()
    {
        BtnOpenPowerShell.Margin = new Thickness(0, 0, 0, 8);
        BtnOpenGatewayLog.Margin = new Thickness(0, 0, 0, 8);
        BtnCleaningTool.Margin = new Thickness(0, 0, 0, 8);
        BtnTokenManager.Margin = new Thickness(0, 0, 0, 8);
        BtnDoctorFix.Margin = new Thickness(0, 0, 0, 0);
    }

    private void ReapplyCurrentThemeLayoutAfterLocalization()
    {
        if (OpenClawManager.App.GetService<ISettingsService>().Settings.Theme == AppTheme.StandardLight)
            ApplyStandardToolLayout();
        else if (OpenClawManager.App.GetService<ISettingsService>().Settings.Theme == AppTheme.StandardDark)
            ApplyStandardDarkToolLayout();
        else if (ThemeService.IsModernPaletteTheme(OpenClawManager.App.GetService<ISettingsService>().Settings.Theme) || OpenClawManager.App.GetService<ISettingsService>().Settings.Theme == AppTheme.HighContrast)
            ApplyModernToolLayout();
        else if (OpenClawManager.App.GetService<ISettingsService>().Settings.Theme == AppTheme.CrabCute)
            ApplyCrabCuteToolLayout();
    }

    // ── Legacy UI — obnovit emoji TextBlock ───────────────────────────────────
    private void ApplyLegacyUi()
    {
        TitleBarHost.Background = SystemColors.WindowBrush;
        MainMenu.Background = SystemColors.WindowBrush;
        MainMenu.Foreground = SystemColors.ControlTextBrush;
        MainStatusBar.Background = SystemColors.WindowBrush;
        MainStatusBar.Foreground = SystemColors.ControlTextBrush;

        BtnStartTuiSymbol.FontFamily = LegacyPlayIconFontFamily;
        BtnStartTuiSymbol.FontSize = 16;
        BtnStartTuiSymbol.FontWeight = FontWeights.Bold;
        BtnStartTuiSymbol.Margin = new Thickness(0, 0, 8, 0);
        BtnStartTuiSymbol.RenderTransformOrigin = new Point(0.5, 0.5);

        RestoreButtonLegacy(BtnGatewayStart,   "▲", "Green",  "Start");
        RestoreButtonLegacy(BtnGatewayStop,    "■", "Red",    "Stop");
        RestoreButtonLegacy(BtnGatewayRestart, "↻", "Orange", "Restart");
        RestoreButtonLegacy(BtnOpenPowerShell, "⚡", null,    BtnPowerShellLabel.Text);
        RestoreButtonLegacy(BtnOpenGatewayLog, "📄", null,   BtnGatewayLogLabel.Text);
        RestoreButtonLegacy(BtnCleaningTool,   "🧹", null,   BtnCleaningToolLabel.Text);
        RestoreButtonLegacy(BtnTokenManager,   "🔑", null,   BtnTokenManagerLabel.Text);
        RestoreButtonLegacy(BtnDoctorFix,      "🩺", null,   BtnDoctorFixLabel.Text);
        BtnDoctorFix.Background = ActionDangerBrush;

        // Start glyphy mají být klasický trojúhelník, ne emoji play ikona.
        if (BtnGatewayStart.Content is StackPanel startStack
            && startStack.Children.Count > 0
            && startStack.Children[0] is TextBlock startIcon)
        {
            startIcon.FontFamily = LegacyPlayIconFontFamily;
            startIcon.RenderTransformOrigin = new Point(0.5, 0.5);
            startIcon.RenderTransform = new RotateTransform(90);
        }

        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    private void RestoreToolControlsToActions()
    {
        var ordered = new UIElement[]
        {
            TxtSectionOpen,
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            TxtSectionTools,
            BtnCleaningTool,
            BtnTokenManager,
            TxtSectionMaintenance,
            BtnDoctorFix
        };

        foreach (var element in ordered)
            RemoveFromCurrentPanel(element);

        var insertIndex = ActionsStack.Children.IndexOf(BtnGatewayRestart) + 1;
        if (insertIndex <= 0) insertIndex = ActionsStack.Children.Count;

        foreach (var element in ordered)
            ActionsStack.Children.Insert(insertIndex++, element);

        GrpTools.Visibility = Visibility.Collapsed;
        ToolsStack.Children.Clear();
    }

    private static void RemoveFromCurrentPanel(UIElement element)
    {
        if (VisualTreeHelper.GetParent(element) is Panel parent)
            parent.Children.Remove(element);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Style FindThemeStyle(string key) =>
        (Style)Application.Current.FindResource(key);

    private static Style GetCachedStyle(string key, Func<Style> factory)
    {
        if (_themeStyleCache.TryGetValue(key, out var cached))
            return cached;

        var style = factory();
        _themeStyleCache[key] = style;
        return style;
    }

    private static string BrushCacheKey(Brush brush)
    {
        return brush switch
        {
            SolidColorBrush solid => $"solid:{solid.Color}:{solid.Opacity:F3}",
            _ => $"{brush.GetType().FullName}:{brush.GetHashCode()}:{brush.Opacity:F3}"
        };
    }

    /// <summary>
    /// Nastaví obsah tlačítka na PNG ikonu (Modern téma).
    /// Ikona je načtena z Resources/Icons/Modern/{name}.png jako embedded resource.
    /// Zachovává původní Label TextBlock pro lokalizaci — jen skryje emoji.
    /// </summary>
    private void SetButtonIcon(Button btn, string iconName)
    {
        try
        {
            var source = GetThemeIconSource(iconName);
            if (source == null) return;

            var img = new Image
            {
                Source = source,
                Width  = 28,
                Height = 28,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            // Najít existující Label TextBlock (druhý child v StackPanel)
            // a nahradit emoji TextBlock za Image
            if (btn.Content is StackPanel sp && sp.Children.Count >= 1)
            {
                // Nahradit první element (emoji TextBlock) za Image
                if (sp.Children[0] is TextBlock emoji)
                {
                    sp.Children.RemoveAt(0);
                    sp.Children.Insert(0, img);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Theme] Ikona {iconName} se nenačetla: {ex.Message}");
        }
    }

    private void SetCrabCuteButtonImage(Button btn, string iconName, double height)
    {
        var image = CreateThemeImage(iconName, height);
        if (image == null) return;

        btn.Content = image;
        btn.Style = CreateCrabCuteButtonFeedbackStyle(OpenClawManager.App.GetService<ISettingsService>().Settings.UseButtonScanlineEffect);
        btn.Height = height;
        btn.Margin = btn == BtnStartTui ? new Thickness(0, 0, 0, 8) : btn.Margin;
        btn.Padding = new Thickness(0);
        btn.BorderThickness = new Thickness(0);
        btn.Background = Brushes.Transparent;
        btn.BorderBrush = Brushes.Transparent;
        btn.FocusVisualStyle = null;
    }

    private void SetModernVariantButtonImage(Button btn, string iconName, double height, HorizontalAlignment alignment)
    {
        var image = CreateThemeImage(iconName, height);
        if (image == null) return;

        image.HorizontalAlignment = alignment;

        btn.Content = image;
        btn.Style = _activeTheme == AppTheme.ModernDark
            ? FindThemeStyle("Theme.Style.ActionImageButton")
            : BuildModernVariantImageButtonFeedbackStyle(
                OpenClawManager.App.GetService<ISettingsService>().Settings.UseButtonScanlineEffect,
                ThemeService.GetCurrentButtonInteraction().Equals("PressScanline", StringComparison.OrdinalIgnoreCase));
        btn.Height = height;
        btn.Padding = new Thickness(0);
        btn.BorderThickness = new Thickness(0);
        btn.Background = Brushes.Transparent;
        btn.BorderBrush = Brushes.Transparent;
        btn.HorizontalContentAlignment = alignment;
        btn.FocusVisualStyle = null;
    }

    private static void ApplyModernButtonFeedbackStyle(params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.Style = BuildModernButtonFeedbackStyle(OpenClawManager.App.GetService<ISettingsService>().Settings.UseButtonScanlineEffect);
            button.FocusVisualStyle = null;
        }
    }

    private static void ApplyStandardButtonFeedbackStyle(params Button[] buttons)
    {
        var style = BuildStandardButtonFeedbackStyle(OpenClawManager.App.GetService<ISettingsService>().Settings.UseButtonScanlineEffect);
        var idleBrush = ThemeService.GetBrush("Theme.Brush.Disabled", Color.FromRgb(0x28, 0x28, 0x28));
        var buttonTextBrush = ThemeService.GetBrush("Theme.Brush.ButtonText", Colors.White);

        foreach (var button in buttons)
        {
            button.Style = style;
            button.FocusVisualStyle = null;
            button.Background = idleBrush;
            button.Foreground = buttonTextBrush;
            button.BorderBrush = Brushes.Transparent;
            button.BorderThickness = new Thickness(0);
        }
    }

    private static Style BuildStandardButtonFeedbackStyle(bool useScanlineEffect)
    {
        var idleBrush = ThemeService.GetBrush("Theme.Brush.Disabled", Color.FromRgb(0x28, 0x28, 0x28));
        var hoverBrush = ThemeService.GetBrush("Theme.Brush.Hover", Color.FromRgb(0x46, 0x46, 0x46));
        var pressedBrush = ThemeService.GetBrush("Theme.Brush.Pressed", Color.FromRgb(0x53, 0x53, 0x53));
        var buttonTextBrush = ThemeService.GetBrush("Theme.Brush.ButtonText", Colors.White);
        var cacheKey = $"button|standard-feedback|{useScanlineEffect}|{BrushCacheKey(idleBrush)}|{BrushCacheKey(hoverBrush)}|{BrushCacheKey(pressedBrush)}|{BrushCacheKey(buttonTextBrush)}";
        return GetCachedStyle(cacheKey, () =>
        {
        var pressedOverlayBrush = CreatePressedScanlineBrush();

        var root = new FrameworkElementFactory(typeof(Border));
        root.Name = "Root";
        root.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        root.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        root.SetValue(Border.BorderThicknessProperty, new Thickness(0));
        root.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

        var contentGrid = new FrameworkElementFactory(typeof(Grid));
        contentGrid.Name = "ContentGrid";
        contentGrid.SetValue(
            FrameworkElement.HorizontalAlignmentProperty,
            new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        contentGrid.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        contentGrid.AppendChild(presenter);

        var pressedOverlay = new FrameworkElementFactory(typeof(Border));
        pressedOverlay.Name = "PressedOverlay";
        pressedOverlay.SetValue(Border.BackgroundProperty, pressedOverlayBrush);
        pressedOverlay.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        pressedOverlay.SetValue(UIElement.OpacityProperty, 0.0);
        pressedOverlay.SetValue(UIElement.IsHitTestVisibleProperty, false);
        contentGrid.AppendChild(pressedOverlay);

        root.AppendChild(contentGrid);

        var template = new ControlTemplate(typeof(Button)) { VisualTree = root };

        var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBrush, "Root"));

        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(Border.BackgroundProperty, pressedBrush, "Root"));
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, useScanlineEffect ? 0.62 : 0.0, "PressedOverlay"));

        template.Triggers.Add(hover);
        template.Triggers.Add(pressed);

        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, idleBrush));
        style.Setters.Add(new Setter(Control.ForegroundProperty, buttonTextBrush));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        style.Triggers.Add(new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false,
            Setters =
            {
                new Setter(Control.BackgroundProperty, idleBrush),
                new Setter(UIElement.OpacityProperty, 0.55)
            }
        });
        return style;
        });
    }

    private static Style BuildModernButtonFeedbackStyle(bool useScanlineEffect)
    {
        var hoverBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE7, 0xEE));
        var hoverBorderBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xC2, 0xCF));
        var pressedBrush = new SolidColorBrush(Color.FromRgb(0xD2, 0xD9, 0xE2));
        var pressedBorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0x9A, 0xAA));
        var cacheKey = $"button|modern-feedback|{useScanlineEffect}";
        return GetCachedStyle(cacheKey, () =>
        {
        var pressedOverlayBrush = CreatePressedScanlineBrush();

        var root = new FrameworkElementFactory(typeof(Border));
        root.Name = "Root";
        root.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        root.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        root.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
        root.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

        var contentGrid = new FrameworkElementFactory(typeof(Grid));
        contentGrid.Name = "ContentGrid";
        contentGrid.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentGrid.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentGrid.SetValue(UIElement.RenderTransformProperty, new TranslateTransform(0, 0));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.Name = "ContentHost";
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        contentGrid.AppendChild(presenter);

        var pressedOverlay = new FrameworkElementFactory(typeof(Border));
        pressedOverlay.Name = "PressedOverlay";
        pressedOverlay.SetValue(Border.BackgroundProperty, pressedOverlayBrush);
        pressedOverlay.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        pressedOverlay.SetValue(UIElement.OpacityProperty, 0.0);
        pressedOverlay.SetValue(UIElement.IsHitTestVisibleProperty, false);
        contentGrid.AppendChild(pressedOverlay);

        root.AppendChild(contentGrid);

        var template = new ControlTemplate(typeof(Button)) { VisualTree = root };
        var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBrush, "Root"));
        hover.Setters.Add(new Setter(Border.BorderBrushProperty, hoverBorderBrush, "Root"));
        hover.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1), "Root"));
        hover.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(2, 2), "ContentGrid"));

        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(Border.BackgroundProperty, pressedBrush, "Root"));
        pressed.Setters.Add(new Setter(Border.BorderBrushProperty, pressedBorderBrush, "Root"));
        pressed.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1), "Root"));
        pressed.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(3, 3), "ContentGrid"));
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, useScanlineEffect ? 0.62 : 0.0, "PressedOverlay"));

        template.Triggers.Add(hover);
        template.Triggers.Add(pressed);

        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        style.Triggers.Add(new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false,
            Setters = { new Setter(UIElement.OpacityProperty, 0.55) }
        });
        return style;
        });
    }

    private static void ApplyDarkButtonFeedbackStyle(Brush idleBrush, params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.Style = BuildDarkButtonFeedbackStyle(OpenClawManager.App.GetService<ISettingsService>().Settings.UseButtonScanlineEffect, idleBrush);
            button.FocusVisualStyle = null;
        }
    }

    private static Style BuildDarkButtonFeedbackStyle(bool useScanlineEffect, Brush idleBrush)
    {
        var hoverBrush = ThemeService.GetBrush("Theme.Brush.Hover", Color.FromRgb(0x38, 0x38, 0x38));
        var pressedBrush = ThemeService.GetBrush("Theme.Brush.Pressed", Color.FromRgb(0x30, 0x30, 0x30));
        var buttonTextBrush = ThemeService.GetBrush("Theme.Brush.ButtonText", Colors.White);
        var cacheKey = $"button|dark-feedback|{useScanlineEffect}|{BrushCacheKey(idleBrush)}|{BrushCacheKey(hoverBrush)}|{BrushCacheKey(pressedBrush)}|{BrushCacheKey(buttonTextBrush)}";
        return GetCachedStyle(cacheKey, () =>
        {
        var pressedOverlayBrush = CreatePressedScanlineBrush();

        var root = new FrameworkElementFactory(typeof(Border));
        root.Name = "Root";
        root.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        root.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        root.SetValue(Border.BorderThicknessProperty, new Thickness(0));
        root.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

        var contentGrid = new FrameworkElementFactory(typeof(Grid));
        contentGrid.Name = "ContentGrid";
        contentGrid.SetValue(
            FrameworkElement.HorizontalAlignmentProperty,
            new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        contentGrid.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentGrid.SetValue(UIElement.RenderTransformProperty, new TranslateTransform(0, 0));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.Name = "ContentHost";
        presenter.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        contentGrid.AppendChild(presenter);

        var pressedOverlay = new FrameworkElementFactory(typeof(Border));
        pressedOverlay.Name = "PressedOverlay";
        pressedOverlay.SetValue(Border.BackgroundProperty, pressedOverlayBrush);
        pressedOverlay.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        pressedOverlay.SetValue(UIElement.OpacityProperty, 0.0);
        pressedOverlay.SetValue(UIElement.IsHitTestVisibleProperty, false);
        contentGrid.AppendChild(pressedOverlay);

        root.AppendChild(contentGrid);

        var template = new ControlTemplate(typeof(Button)) { VisualTree = root };

        var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBrush, "Root"));
        hover.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(2, 2), "ContentGrid"));

        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(Border.BackgroundProperty, pressedBrush, "Root"));
        pressed.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(3, 3), "ContentGrid"));
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, useScanlineEffect ? 0.62 : 0.0, "PressedOverlay"));

        template.Triggers.Add(hover);
        template.Triggers.Add(pressed);

        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, idleBrush));
        style.Setters.Add(new Setter(Control.ForegroundProperty, buttonTextBrush));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        style.Triggers.Add(new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false,
            Setters =
            {
                new Setter(Control.BackgroundProperty, idleBrush),
                new Setter(UIElement.OpacityProperty, 0.55)
            }
        });
        return style;
        });
    }

    private static Color GetBrushColor(Brush brush, Color fallback)
    {
        return brush is SolidColorBrush solid ? solid.Color : fallback;
    }

    private static Style CreateCrabCuteButtonFeedbackStyle(bool useScanlineEffect)
    {
        var cacheKey = $"button|crabcute-feedback|{useScanlineEffect}";
        return GetCachedStyle(cacheKey, () =>
        {
        var pressedOverlayBrush = CreatePressedScanlineBrush();

        var root = new FrameworkElementFactory(typeof(Border));
        root.Name = "Root";
        root.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        root.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        root.SetValue(Border.BorderThicknessProperty, new Thickness(0));

        var contentGrid = new FrameworkElementFactory(typeof(Grid));
        contentGrid.Name = "ContentGrid";
        contentGrid.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentGrid.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentGrid.SetValue(UIElement.RenderTransformProperty, new TranslateTransform(0, 0));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentGrid.AppendChild(presenter);

        var pressedOverlay = new FrameworkElementFactory(typeof(Border));
        pressedOverlay.Name = "PressedOverlay";
        pressedOverlay.SetValue(Border.BackgroundProperty, pressedOverlayBrush);
        pressedOverlay.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        pressedOverlay.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 4, 0));
        pressedOverlay.SetValue(UIElement.OpacityProperty, 0.0);
        pressedOverlay.SetValue(UIElement.IsHitTestVisibleProperty, false);
        contentGrid.AppendChild(pressedOverlay);

        root.AppendChild(contentGrid);

        var template = new ControlTemplate(typeof(Button)) { VisualTree = root };

        var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(2, 2), "ContentGrid"));

        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(3, 3), "ContentGrid"));
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.82, "ContentGrid"));
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, useScanlineEffect ? 0.62 : 0.0, "PressedOverlay"));

        template.Triggers.Add(hover);
        template.Triggers.Add(pressed);

        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        style.Triggers.Add(new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false,
            Setters = { new Setter(UIElement.OpacityProperty, 0.55) }
        });
        return style;
        });
    }

    private static Style BuildModernVariantImageButtonFeedbackStyle(bool useScanlineEffect, bool lightInteraction)
    {
        var cacheKey = $"button|modern-variant-image|{useScanlineEffect}|{lightInteraction}";
        return GetCachedStyle(cacheKey, () =>
        {
        var hoverOverlayBrush = CreatePressedScanlineBrush();

        var root = new FrameworkElementFactory(typeof(Border));
        root.Name = "Root";
        root.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        root.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        root.SetValue(Border.BorderThicknessProperty, new Thickness(0));

        var contentGrid = new FrameworkElementFactory(typeof(Grid));
        contentGrid.Name = "ContentGrid";
        contentGrid.SetValue(
            FrameworkElement.HorizontalAlignmentProperty,
            new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        contentGrid.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentGrid.SetValue(UIElement.RenderTransformProperty, new TranslateTransform(0, 0));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentGrid.AppendChild(presenter);

        var hoverOverlay = new FrameworkElementFactory(typeof(Border));
        hoverOverlay.Name = "HoverOverlay";
        hoverOverlay.SetValue(Border.BackgroundProperty, hoverOverlayBrush);
        hoverOverlay.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        hoverOverlay.SetValue(UIElement.OpacityProperty, 0.0);
        hoverOverlay.SetValue(UIElement.IsHitTestVisibleProperty, false);
        contentGrid.AppendChild(hoverOverlay);

        root.AppendChild(contentGrid);

        var template = new ControlTemplate(typeof(Button)) { VisualTree = root };

        var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        if (lightInteraction)
        {
            hover.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(3, 3), "ContentGrid"));
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.82, "ContentGrid"));
        }
        else
        {
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, useScanlineEffect ? 0.62 : 0.0, "HoverOverlay"));
        }

        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        if (lightInteraction)
        {
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, useScanlineEffect ? 0.62 : 0.0, "HoverOverlay"));
        }
        else
        {
            pressed.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(3, 3), "ContentGrid"));
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.82, "ContentGrid"));
        }

        template.Triggers.Add(hover);
        template.Triggers.Add(pressed);

        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        style.Triggers.Add(new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false,
            Setters = { new Setter(UIElement.OpacityProperty, 0.55) }
        });
        return style;
        });
    }

    /// <summary>
    /// Builds the small pressed-state scanline overlay shared by procedural button templates.
    /// This remains code-based because the generated brush is easier to audit here than as XAML geometry.
    /// </summary>
    private static Brush CreatePressedScanlineBrush()
    {
        if (_pressedScanlineBrush != null) return _pressedScanlineBrush;

        var drawingGroup = new DrawingGroup();
        drawingGroup.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromArgb(0x32, 0x00, 0x00, 0x00)),
            null,
            new RectangleGeometry(new Rect(0, 0, 1, 1))));
        drawingGroup.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromArgb(0xC0, 0x00, 0x00, 0x00)),
            null,
            new RectangleGeometry(new Rect(0, 0, 1, 0.22))));

        var brush = new DrawingBrush(drawingGroup)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 1, 3),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None,
            Opacity = 1.0
        };

        if (brush.CanFreeze) brush.Freeze();
        _pressedScanlineBrush = brush;
        return brush;
    }

    private Image? CreateThemeImage(string iconName, double height)
    {
        try
        {
            var source = GetThemeIconSource(iconName);
            if (source == null) return null;

            var image = new Image
            {
                Source = source,
                Height = height,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }
        catch (Exception ex)
        {
            Log($"[Theme] Ikona {iconName} se nenačetla: {ex.Message}");
            return null;
        }
    }

    private ImageSource? GetThemeIconSource(string iconName)
    {
        var uri = ThemeService.GetIconUri(_activeTheme, iconName);
        if (uri == null) return null;

        var cacheKey = $"{_activeTheme}|{iconName}|{uri}";
        if (_themeIconSourceCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = uri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        if (bitmap.CanFreeze) bitmap.Freeze();

        _themeIconSourceCache[cacheKey] = bitmap;
        return bitmap;
    }

    private void SetMenuIcon(MenuItem item, string iconName)
    {
        item.Icon = CreateThemeImage(iconName, 18);
    }

    private void ApplyThemeSpecificTuiVisual(bool tuiRunning)
    {
        if (_activeTheme is AppTheme.ModernDark or AppTheme.ModernLight)
        {
            SetModernVariantButtonImage(BtnStartTui, tuiRunning ? "stop" : "tui", tuiRunning ? 44 : 88, HorizontalAlignment.Center);
            return;
        }

        if (_activeTheme != AppTheme.CrabCute) return;

        SetCrabCuteButtonImage(BtnStartTui, tuiRunning ? "stop" : "tui", tuiRunning ? 44 : 88);
    }

    /// <summary>
    /// Captures the original XAML-defined visual state before any theme-specific code mutates controls.
    /// RestoreThemeBaseline uses this snapshot so theme switching never compounds previous runtime edits.
    /// </summary>
    private void CaptureThemeBaseline()
    {
        if (_buttonVisualStates.Count > 0) return;

        foreach (var button in new[]
        {
            BtnStartTui,
            BtnGatewayStart,
            BtnGatewayStop,
            BtnGatewayRestart,
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            BtnCleaningTool,
            BtnTokenManager,
            BtnDoctorFix
        })
        {
            _buttonVisualStates[button] = new ButtonVisualState(
                button.Content,
                button.Style,
                button.FocusVisualStyle,
                button.Height,
                button.Width,
                button.Margin,
                button.HorizontalContentAlignment,
                button.Padding,
                button.BorderThickness,
                button.Background,
                button.BorderBrush);
        }

        foreach (var element in new FrameworkElement[]
        {
            TxtGatewayLabel,
            TxtSectionOpen,
            TxtSectionTools,
            TxtSectionMaintenance
        })
        {
            _layoutElementStates[element] = new ElementLayoutState(
                element.Visibility,
                element.Margin,
                element is TextBlock tb ? tb.Text : null);
        }

        foreach (var item in new[] { MnuOpenPowerShell, MnuOpenGatewayLog, MnuSettings, MnuMenuSettings })
        {
            _menuItemIcons[item] = item.Icon;
        }

        foreach (var item in new[] { MnuMenuOpen, MnuMenuSettings, MnuMenuHelp })
        {
            _menuItemHeaders[item] = item.Header;
        }

        foreach (var textBlock in new[]
        {
            BtnStartTuiLabel,
            BtnStartTuiSubLabel,
            BtnGatewayStartLabel,
            BtnGatewayStopLabel,
            BtnGatewayRestartLabel,
            BtnPowerShellLabel,
            BtnGatewayLogLabel,
            BtnCleaningToolLabel,
            BtnTokenManagerLabel,
            BtnDoctorFixLabel,
            StatusGatewayPrefix,
            StatusGatewayText,
            StatusGatewayPid,
            StatusGatewayUptime,
            StatusRam,
            StatusVram,
            StatusCpu,
            StatusAppVersion
        })
        {
            _textBlockForegroundStates[textBlock] = textBlock.Foreground;
        }

        foreach (var control in new Control[] { MainMenu, MainStatusBar, GrpActions, GrpTools, GrpLatency, GrpAppLog, AppLog })
        {
            _shellControlStates[control] = new ShellVisualState(
                control.Background,
                control.Foreground,
                control.BorderBrush,
                control.BorderThickness,
                control.Style);
        }

        _mainStatusBarBaselineHeight = MainStatusBar.Height;
        _appLogItemContainerStyleBaseline = AppLog.ItemContainerStyle;
    }

    /// <summary>
    /// Restores controls to the captured baseline before applying the next theme layer.
    /// This snapshot/restore pattern keeps legacy runtime theme code deterministic across repeated switches.
    /// </summary>
    private void RestoreThemeBaseline()
    {
        RestoreToolControlsToActions();
        ApplyWindowCaptionColor(null, null);
        ApplyThemeTitleBarMode(false);
        RestoreModernDarkTitleBarOverrides();
        ClearValue(Control.FontFamilyProperty);
        ClearValue(Control.FontSizeProperty);
        ClearValue(Control.ForegroundProperty);
        MainMenu.ClearValue(Control.FontFamilyProperty);
        MainMenu.ClearValue(Control.FontSizeProperty);
        MainMenu.ClearValue(Control.FontWeightProperty);
        MainStatusBar.ClearValue(Control.FontFamilyProperty);
        MainStatusBar.ClearValue(Control.FontSizeProperty);
        MainStatusBar.ClearValue(Control.FontWeightProperty);
        MainStatusBar.ClearValue(Control.PaddingProperty);
        if (!double.IsNaN(_mainStatusBarBaselineHeight))
            MainStatusBar.Height = _mainStatusBarBaselineHeight;
        Background = SystemColors.WindowBrush;
        Foreground = SystemColors.ControlTextBrush;
        MainGridSplitter.Background = Brushes.LightGray;
        RightPanel.Background = Brushes.Transparent;
        SplashOverlay.SetResourceReference(Border.BackgroundProperty, "Brush.SplashModernBackground");
        SplashImage.Opacity = 1.0;
        SplashImage.Stretch = Stretch.Uniform;
        SplashMedia.Opacity = 1.0;
        SplashMedia.Stretch = Stretch.Uniform;
        SplashProgress.ClearValue(Control.ForegroundProperty);
        Terminal.ResetShellBackground();

        foreach (var (button, state) in _buttonVisualStates)
        {
            button.Content = state.Content;
            button.Style = state.Style;
            button.FocusVisualStyle = state.FocusVisualStyle;
            button.Height = state.Height;
            button.Width = state.Width;
            button.Margin = state.Margin;
            button.HorizontalContentAlignment = state.HorizontalContentAlignment;
            button.Padding = state.Padding;
            button.BorderThickness = state.BorderThickness;
            button.Background = state.Background;
            button.BorderBrush = state.BorderBrush;
        }

        foreach (var (item, icon) in _menuItemIcons)
        {
            item.Icon = icon;
        }

        foreach (var (item, header) in _menuItemHeaders)
        {
            item.Header = header;
        }

        foreach (var (control, state) in _shellControlStates)
        {
            control.Background = state.Background;
            control.Foreground = state.Foreground;
            control.BorderBrush = state.BorderBrush;
            control.BorderThickness = state.BorderThickness;
            control.Style = state.Style;
            control.ClearValue(Control.BackgroundProperty);
            control.ClearValue(Control.ForegroundProperty);
            control.ClearValue(Control.BorderBrushProperty);
        }

        foreach (var (element, state) in _layoutElementStates)
        {
            element.Visibility = state.Visibility;
            element.Margin = state.Margin;
            if (element is TextBlock tb && state.Text != null)
                tb.Text = state.Text;
        }

        foreach (var (textBlock, foreground) in _textBlockForegroundStates)
        {
            textBlock.Foreground = foreground;
        }

        MainMenu.Resources.Remove(typeof(MenuItem));
        MainMenu.Resources.Remove(typeof(Separator));
        MainStatusBar.Resources.Remove(typeof(Separator));
        MainStatusBar.Resources.Remove(typeof(StatusBarItem));
        AppLog.ItemContainerStyle = _appLogItemContainerStyleBaseline;
        AppLog.Resources.Remove(typeof(ScrollBar));
        AppLog.Resources.Remove(typeof(Thumb));
        foreach (var separator in MnuMenuOpen.Items.OfType<Separator>())
            separator.ClearValue(FrameworkElement.StyleProperty);
    }

    private void RestoreModernDarkTitleBarOverrides()
    {
        TitleBarHost.Margin = new Thickness(0);
        TitleBarHost.CornerRadius = new CornerRadius(0);
        TitleBarHost.BorderThickness = new Thickness(0);
        TitleBarHost.ClearValue(Border.PaddingProperty);
        TitleBarHost.ClearValue(Border.BorderBrushProperty);

        CaptionButtons.Margin = new Thickness(0);
        foreach (var button in new[] { BtnWindowMinimize, BtnWindowMaximize, BtnWindowClose })
        {
            button.Margin = new Thickness(0);
            button.ClearValue(Control.FontFamilyProperty);
            button.ClearValue(Control.FontSizeProperty);
        }
    }

    /// <summary>
    /// Applies native DWM caption, border, and title text colors for themes that use custom window chrome.
    /// </summary>
    private void ApplyWindowCaptionColor(Color? captionColor, Color? textColor)
    {
        void ApplyNow()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            var caption = captionColor.HasValue ? ToColorRef(captionColor.Value) : DwmDefaultColor;
            var border = caption;
            var text = textColor.HasValue ? ToColorRef(textColor.Value) : DwmDefaultColor;

            _ = DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref caption, sizeof(int));
            _ = DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref border, sizeof(int));
            _ = DwmSetWindowAttribute(hwnd, DwmwaTextColor, ref text, sizeof(int));
        }

        if (new WindowInteropHelper(this).Handle == IntPtr.Zero)
            SourceInitialized += (_, _) => ApplyNow();
        else
            ApplyNow();
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    /// <summary>
    /// Obnoví původní emoji TextBlock v tlačítku (Legacy téma).
    /// </summary>
    private static void RestoreButtonLegacy(Button btn, string emoji, string? color, string label)
    {
        if (btn.Content is not StackPanel sp)
            return;

        TextBlock icon;
        if (sp.Children.Count == 0)
        {
            icon = CreateLegacyIconTextBlock();
            sp.Children.Add(icon);
        }
        else if (sp.Children[0] is TextBlock existingIcon)
        {
            icon = existingIcon;
        }
        else
        {
            sp.Children.RemoveAt(0);
            icon = CreateLegacyIconTextBlock();
            sp.Children.Insert(0, icon);
        }

        icon.Text = emoji;
        icon.FontFamily = LegacyIconFontFamily;
        icon.FontSize = 14;
        icon.Margin = new Thickness(0, 0, 8, 0);
        icon.VerticalAlignment = VerticalAlignment.Center;
        icon.FontWeight = color != null ? FontWeights.Bold : FontWeights.Normal;
        if (color != null)
            icon.Foreground = (Brush)new BrushConverter().ConvertFromString(color)!;
        else
            icon.ClearValue(TextBlock.ForegroundProperty);

        var labelBlock = sp.Children.OfType<TextBlock>().FirstOrDefault(tb => !ReferenceEquals(tb, icon));
        if (labelBlock != null && !string.IsNullOrWhiteSpace(label))
            labelBlock.Text = label;
    }

    private static TextBlock CreateLegacyIconTextBlock()
    {
        return new TextBlock
        {
            FontFamily = LegacyIconFontFamily,
            FontSize = 14,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    // Fáze 4 — ModernDark glass override: aplikuje průhledné glass brushe nad
    // ApplyModernPaletteShell tak, aby panely "pluly" nad BgWallpaper.
    private void ApplyModernDarkGlassShell()
    {
        var barBg = ThemeService.GetBrush("Theme.Brush.TopBarBg", Color.FromRgb(0x14, 0x14, 0x14));
        var panelBg = ThemeService.GetBrush("Theme.Brush.GlassPanelBg", Color.FromRgb(0x14, 0x17, 0x1C));
        var glassBorder = ThemeService.GetBrush("Theme.Brush.GlassPanelBorder", Color.FromRgb(0x23, 0x27, 0x2F));
        var chrome = ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0x0B, 0x0C, 0x10));

        BgWallpaper.Visibility = Visibility.Collapsed;
        BgGlow.Visibility = Visibility.Visible;
        BgGlow.Background = CreateModernDarkBackdropBrush();

        GrpActions.Style = FindThemeStyle("Theme.Style.GlassPanelGroupBox.NoHeader");
        GrpLatency.Style = FindThemeStyle("Theme.Style.GlassPanelGroupBox");
        GrpAppLog.Style = FindThemeStyle("Theme.Style.GlassPanelGroupBox");

        // Sidebar (Column 0) průhledné pozadí — "float" over BgWallpaper
        // Levý Grid sdílí background Window; nastavit přímo Background na Window nestačí,
        // ale GrpActions a status bar jsou hlavní plochy.
        GrpActions.Background = panelBg;
        GrpActions.BorderBrush = glassBorder;
        GrpActions.BorderThickness = new Thickness(1);
        GrpLatency.Background = panelBg;
        GrpLatency.BorderBrush = glassBorder;
        GrpLatency.BorderThickness = new Thickness(1);
        GrpAppLog.Background  = panelBg;
        GrpAppLog.BorderBrush = glassBorder;
        GrpAppLog.BorderThickness = new Thickness(1);
        AppLog.Style = FindThemeStyle("Theme.Style.AppLog");
        AppLog.ItemContainerStyle = FindThemeStyle("Theme.Style.AppLogItem");
        AppLog.Resources[typeof(ScrollBar)] = FindThemeStyle("Theme.Style.DarkScrollBar");
        AppLog.Resources[typeof(Thumb)] = FindThemeStyle("Theme.Style.DarkScrollThumb");

        // Title bar + status bar — tmavý glass pruh
        MainStatusBar.Background = barBg;
        ApplyModernDarkTitleBar(barBg, glassBorder);

        // Jemný glassborder pro GroupBoxy
        GrpLatency.BorderBrush    = glassBorder;
        GrpLatency.BorderThickness = new Thickness(1);
        GrpAppLog.BorderBrush     = glassBorder;
        GrpAppLog.BorderThickness  = new Thickness(1);
        MainGridSplitter.Background = glassBorder;
        RightPanel.Background = new SolidColorBrush(Color.FromArgb(0xD9, 0x05, 0x06, 0x0C));
        SplashOverlay.Background = CreateModernDarkBackdropBrush();
        SplashImage.Opacity = 0.34;
        SplashImage.Stretch = Stretch.UniformToFill;
        SplashMedia.Opacity = 0.66;
        SplashMedia.Stretch = Stretch.UniformToFill;
        Terminal.SetShellBackground(chrome);
        ApplyModernDarkTypography();
        ApplyModernDarkMainButtons();

        // Okno samotné — průhledné, aby BgWallpaper prosvítal
        Background = Brushes.Transparent;
    }

    private void ApplyModernDarkMainButtons()
    {
        SetModernVariantButtonImage(BtnGatewayStart, "start", 44, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnGatewayStop, "stop", 44, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnGatewayRestart, "restart", 44, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnOpenPowerShell, "powershell", 56, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnOpenGatewayLog, "gateway-log", 56, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnCleaningTool, "cleaning-tool", 56, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnTokenManager, "token-manager", 56, HorizontalAlignment.Center);
        SetModernVariantButtonImage(BtnDoctorFix, "doctor-fix", 56, HorizontalAlignment.Center);
        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    private void ApplyModernDarkTitleBar(Brush barBackground, Brush borderBrush)
    {
        TitleBarHost.Height = 46;
        TitleBarHost.Margin = new Thickness(0);
        TitleBarHost.CornerRadius = new CornerRadius(0);
        TitleBarHost.BorderThickness = new Thickness(1);
        TitleBarHost.BorderBrush = ThemeService.GetBrush("Theme.Brush.TopBarBorder", GetBrushColor(borderBrush, Color.FromRgb(0x23, 0x27, 0x2F)));
        TitleBarHost.Background = barBackground;
        TitleBarHost.Padding = new Thickness(16, 0, 16, 0);

        MainMenu.Background = Brushes.Transparent;
        MainMenu.Foreground = ThemeService.GetBrush("Theme.Brush.MenuText", Color.FromRgb(0x55, 0x59, 0x58));
        MainMenu.FontFamily = ModernDarkUiFontFamily;
        MainMenu.FontSize = 12;
        MainMenu.FontWeight = FontWeights.Bold;
        MainMenu.VerticalAlignment = VerticalAlignment.Center;
        MainMenu.Margin = new Thickness(0);
        MainMenu.Padding = new Thickness(0);
        MainMenu.Resources[typeof(MenuItem)] = FindThemeStyle("Theme.Style.MainMenuButton");
        MainMenu.Resources[typeof(Separator)] = CreateHiddenSeparatorStyle();
        ApplyModernDarkMenuSeparator();
        ApplyModernDarkTopMenuHeaders();

        foreach (var item in EnumerateVisualChildren(MainMenu).OfType<MenuItem>())
            item.Icon = null;

        CaptionButtons.Margin = new Thickness(0, 0, 4, 0);
        foreach (var button in new[] { BtnWindowMinimize, BtnWindowMaximize, BtnWindowClose })
        {
            button.Width = 44;
            button.Height = 30;
            button.Margin = new Thickness(0, 0, 6, 0);
            button.Padding = new Thickness(0);
            button.Foreground = new SolidColorBrush(Color.FromRgb(0xC2, 0xC6, 0xD4));
            button.FontFamily = ModernDarkUiFontFamily;
            button.FontSize = 13;
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.BorderThickness = new Thickness(0);
            button.FocusVisualStyle = null;
        }
        BtnWindowClose.Margin = new Thickness(0);
        ApplyModernDarkCaptionButtonVisuals();
    }

    private void ApplyModernDarkTypography()
    {
        var mainText = ThemeService.GetBrush("Theme.Brush.StatusBarText", Color.FromRgb(0x55, 0x59, 0x58));

        FontFamily = ModernDarkUiFontFamily;
        FontSize = 12;
        Foreground = mainText;
        MainMenu.FontFamily = ModernDarkUiFontFamily;
        MainMenu.FontSize = 12;
        MainMenu.FontWeight = FontWeights.Bold;
        MainMenu.Foreground = mainText;

        ApplyModernDarkStatusBarTypography(mainText);

        foreach (var textBlock in new[]
        {
            TxtGatewayLabel,
            TxtSectionOpen,
            TxtSectionTools,
            TxtSectionMaintenance,
            TxtLatencyLast,
            TxtLatencyAvg,
            TxtLatencyMax,
            TxtLatencyCount,
            LatencyLast,
            LatencyAvg,
            LatencyMax,
            LatencyCount,
            BtnStartTuiLabel,
            BtnStartTuiSubLabel,
            BtnGatewayStartLabel,
            BtnGatewayStopLabel,
            BtnGatewayRestartLabel,
            BtnPowerShellLabel,
            BtnGatewayLogLabel,
            BtnCleaningToolLabel,
            BtnTokenManagerLabel,
            BtnDoctorFixLabel
        })
        {
            textBlock.FontFamily = ModernDarkUiFontFamily;
            textBlock.FontSize = 12;
            textBlock.FontWeight = FontWeights.Normal;
            textBlock.Foreground = mainText;
        }

        GrpActions.FontFamily = ModernDarkUiFontFamily;
        GrpLatency.FontFamily = ModernDarkUiFontFamily;
        GrpAppLog.FontFamily = ModernDarkUiFontFamily;
        GrpAppLog.FontSize = 12;
        GrpAppLog.FontWeight = FontWeights.Normal;
        GrpAppLog.Foreground = mainText;
        AppLog.FontFamily = ModernDarkCodeFontFamily;
        AppLog.Foreground = mainText;
    }

    private static Style CreateModernDarkStatusBarItemStyle(Brush foreground)
    {
        return GetCachedStyle($"statusbaritem|modern-dark|{BrushCacheKey(foreground)}", () =>
        {
        var style = new Style(typeof(StatusBarItem));
        style.Setters.Add(new Setter(Control.FontFamilyProperty, ModernDarkUiFontFamily));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Normal));
        style.Setters.Add(new Setter(Control.ForegroundProperty, foreground));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        return style;
        });
    }

    private void ApplyModernDarkStatusBarTypography(Brush foreground)
    {
        MainStatusBar.FontFamily = ModernDarkUiFontFamily;
        MainStatusBar.FontSize = 12;
        MainStatusBar.FontWeight = FontWeights.Normal;
        MainStatusBar.Height = 36;
        MainStatusBar.Foreground = foreground;
        MainStatusBar.Padding = new Thickness(16, 0, 0, 0);
        MainStatusBar.Resources[typeof(StatusBarItem)] = CreateModernDarkStatusBarItemStyle(foreground);
        MainStatusBar.Resources[typeof(Separator)] = CreateHiddenSeparatorStyle();
        StatusAppVersion.Margin = new Thickness(0, 0, 16, 0);

        foreach (var textBlock in new[]
        {
            StatusGatewayPrefix,
            StatusGatewayText,
            StatusGatewayPid,
            StatusGatewayUptime,
            StatusRam,
            StatusVram,
            StatusCpu,
            StatusAppVersion
        })
        {
            textBlock.Style = FindThemeStyle("Theme.Style.StatusBarText");
            textBlock.FontFamily = ModernDarkUiFontFamily;
            textBlock.FontSize = 12;
            textBlock.FontWeight = FontWeights.Normal;
            textBlock.Foreground = foreground;
        }
    }

    private void ApplyModernDarkMenuSeparator()
    {
        foreach (var separator in MnuMenuOpen.Items.OfType<Separator>())
            separator.Style = CreateModernDarkMenuSeparatorStyle();
    }

    private void ApplyModernDarkTopMenuHeaders()
    {
        foreach (var item in new[] { MnuMenuOpen, MnuMenuSettings, MnuMenuHelp })
        {
            if (item.Header is string text)
                item.Header = text.Replace("_", string.Empty);
        }
    }

    private static Brush CreateModernDarkBackdropBrush()
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0xF7, 0x05, 0x06, 0x0C), 0.0),
                new GradientStop(Color.FromArgb(0xEA, 0x0F, 0x11, 0x15), 0.55),
                new GradientStop(Color.FromArgb(0xF5, 0x05, 0x06, 0x0C), 1.0)
            }
        };
    }
}
