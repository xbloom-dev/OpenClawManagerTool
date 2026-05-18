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
        Thickness Margin,
        HorizontalAlignment HorizontalContentAlignment,
        Thickness Padding,
        Thickness BorderThickness,
        Brush Background,
        Brush BorderBrush);

    private sealed record ShellVisualState(Brush Background, Brush Foreground);
    private sealed record ElementLayoutState(Visibility Visibility, Thickness Margin, string? Text);

    private readonly Dictionary<Button, ButtonVisualState> _buttonVisualStates = new();
    private readonly Dictionary<Control, ShellVisualState> _shellControlStates = new();
    private readonly Dictionary<FrameworkElement, ElementLayoutState> _layoutElementStates = new();
    private readonly Dictionary<TextBlock, Brush> _textBlockForegroundStates = new();
    private readonly Dictionary<MenuItem, object?> _menuItemIcons = new();
    private AppTheme _activeTheme = AppTheme.Legacy;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    // ── Inicializace theme (volat v konstruktoru po InitializeComponent) ──────
    private void InitTheme()
    {
        CaptureThemeBaseline();

        // Přihlásit se na event — přepnutí tématu za běhu (ze SettingsWindow)
        ThemeService.ThemeChanged += OnThemeChanged;

        // Aplikovat téma z nastavení
        var theme = SettingsService.Current.Theme;
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

        switch (theme)
        {
            case AppTheme.Modern:
                ApplyStandardModernUi();
                break;
            case AppTheme.HighContrast:
                ApplyModernUi();
                ApplyModernToolLayout();
                break;
            case AppTheme.Dark:
            case AppTheme.ModernLight:
                ApplyDarkUi();
                ApplyModernToolLayout();
                ApplyDarkToolLayout();
                ApplyResourceThemeShell();
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

    private void ApplyDarkUi()
    {
        var primaryIdle = ThemeService.GetBrush("Theme.Brush.Disabled", Color.FromRgb(0x27, 0x27, 0x27));
        var secondaryIdle = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x19, 0x19, 0x19));

        SetButtonIcon(BtnGatewayStart,    "start");
        SetButtonIcon(BtnGatewayStop,     "stop");
        SetButtonIcon(BtnGatewayRestart,  "restart");
        SetButtonIcon(BtnOpenPowerShell,  "powershell");
        SetButtonIcon(BtnOpenGatewayLog,  "gateway-log");
        SetButtonIcon(BtnCleaningTool,    "cleaning-tool");
        SetButtonIcon(BtnTokenManager,    "token-manager");
        SetButtonIcon(BtnDoctorFix,       "doctor-fix");

        ApplyDarkButtonFeedbackStyle(primaryIdle,
            BtnStartTui,
            BtnGatewayStart,
            BtnGatewayStop,
            BtnGatewayRestart);

        ApplyDarkButtonFeedbackStyle(secondaryIdle,
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            BtnCleaningTool,
            BtnTokenManager,
            BtnDoctorFix);

        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    // ── CrabCute UI ──────────────────────────────────────────────────────────
    private void ApplyCrabCuteUi()
    {
        ApplyCrabCuteShell();
        ApplyModernToolLayout();

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

    private void ApplyResourceThemeShell()
    {
        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x19, 0x19, 0x19));
        var surface = ThemeService.GetBrush("Theme.Brush.Surface", Color.FromRgb(0x19, 0x19, 0x19));
        var chrome = ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0x12, 0x12, 0x12));
        var text = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x4E, 0x4E, 0x4E));
        var secondary = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x4E, 0x4E, 0x4E));

        Background = background;
        Foreground = text;

        MainMenu.Background = background;
        MainMenu.Foreground = text;
        MainStatusBar.Background = chrome;
        MainStatusBar.Foreground = secondary;
        MainStatusBar.Resources[typeof(Separator)] = CreateHiddenSeparatorStyle();
        MainGridSplitter.Background = background;

        GrpActions.Background = surface;
        GrpActions.Foreground = text;
        GrpActions.BorderBrush = Brushes.Transparent;
        GrpActions.BorderThickness = new Thickness(0);

        foreach (var group in new[] { GrpLatency, GrpAppLog })
        {
            group.Background = surface;
            group.Foreground = text;
            group.BorderBrush = text;
            group.BorderThickness = new Thickness(1);
            group.Style = CreateDarkFramedGroupBoxStyle();
        }

        AppLog.Background = surface;
        AppLog.Foreground = text;
        AppLog.BorderBrush = Brushes.Transparent;
        AppLog.BorderThickness = new Thickness(0);
        Terminal.Background = background;
        ApplyDarkMenuVisuals();
        ApplyDarkButtonText();
        ApplyDarkMainWindowText();
        AlignToolButtonsLeft();

        var captionText = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        ApplyWindowCaptionColor(GetBrushColor(chrome, Color.FromRgb(0x12, 0x12, 0x12)), GetBrushColor(captionText, Colors.White));
    }

    private void ApplyDarkMenuVisuals()
    {
        MainMenu.Resources[typeof(MenuItem)] = CreateDarkMenuItemStyle();
        MainMenu.Resources[typeof(Separator)] = CreateHiddenSeparatorStyle();
    }

    private static Style CreateHiddenSeparatorStyle()
    {
        var style = new Style(typeof(Separator));
        style.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        return style;
    }

    private static Style CreateDarkMenuItemStyle()
    {
        var foreground = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        var topLevelForeground = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x4E, 0x4E, 0x4E));
        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x19, 0x19, 0x19));
        var popupBackground = ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0x12, 0x12, 0x12));
        var hoverBackground = ThemeService.GetBrush("Theme.Brush.Disabled", Color.FromRgb(0x27, 0x27, 0x27));

        var style = new Style(typeof(MenuItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, foreground));
        style.Setters.Add(new Setter(Control.BackgroundProperty, background));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 4, 8, 4)));
        style.Setters.Add(new Setter(Control.TemplateProperty, CreateDarkMenuItemTemplate(topLevelForeground, popupBackground, hoverBackground)));
        return style;
    }

    private static Style CreateDarkFramedGroupBoxStyle()
    {
        var textBrush = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x4E, 0x4E, 0x4E));
        var borderBrush = ThemeService.GetBrush("Theme.Brush.Border", Color.FromRgb(0x4E, 0x4E, 0x4E));
        var backgroundBrush = ThemeService.GetBrush("Theme.Brush.Surface", Color.FromRgb(0x19, 0x19, 0x19));

        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "Border";
        border.SetValue(Border.BackgroundProperty, backgroundBrush);
        border.SetValue(Border.BorderBrushProperty, borderBrush);
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var dock = new FrameworkElementFactory(typeof(DockPanel));
        dock.SetValue(DockPanel.LastChildFillProperty, true);

        var header = new FrameworkElementFactory(typeof(ContentPresenter));
        header.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        header.SetValue(TextElement.ForegroundProperty, textBrush);
        header.SetValue(TextElement.FontWeightProperty, FontWeights.SemiBold);
        header.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 6));
        header.SetValue(DockPanel.DockProperty, Dock.Top);
        dock.AppendChild(header);

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        dock.AppendChild(content);

        border.AppendChild(dock);

        var style = new Style(typeof(GroupBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, backgroundBrush));
        style.Setters.Add(new Setter(Control.ForegroundProperty, textBrush));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, borderBrush));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(typeof(GroupBox)) { VisualTree = border }));
        return style;
    }

    private static ControlTemplate CreateDarkMenuItemTemplate(Brush secondaryBrush, Brush popupBackground, Brush hoverBackground)
    {
        var root = new FrameworkElementFactory(typeof(Border));
        root.Name = "Root";
        root.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        root.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        root.SetValue(Border.BorderThicknessProperty, new Thickness(0));

        var dock = new FrameworkElementFactory(typeof(DockPanel));
        dock.Name = "Dock";
        dock.SetValue(FrameworkElement.MinWidthProperty, 160.0);
        dock.SetValue(FrameworkElement.MarginProperty, new Thickness(0));

        var arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.Name = "Arrow";
        arrow.SetValue(TextBlock.TextProperty, ">");
        arrow.SetValue(TextBlock.ForegroundProperty, secondaryBrush);
        arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(16, 0, 0, 0));
        arrow.SetValue(DockPanel.DockProperty, Dock.Right);
        arrow.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        dock.AppendChild(arrow);

        var gesture = new FrameworkElementFactory(typeof(TextBlock));
        gesture.Name = "Gesture";
        gesture.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(MenuItem.InputGestureTextProperty));
        gesture.SetValue(TextBlock.ForegroundProperty, secondaryBrush);
        gesture.SetValue(FrameworkElement.MarginProperty, new Thickness(24, 0, 0, 0));
        gesture.SetValue(DockPanel.DockProperty, Dock.Right);
        dock.AppendChild(gesture);

        var header = new FrameworkElementFactory(typeof(ContentPresenter));
        header.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        header.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        header.SetValue(TextElement.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        header.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 4, 8, 4));
        dock.AppendChild(header);

        root.AppendChild(dock);

        var popup = new FrameworkElementFactory(typeof(Popup));
        popup.Name = "PART_Popup";
        popup.SetValue(Popup.AllowsTransparencyProperty, true);
        popup.SetValue(Popup.FocusableProperty, false);
        popup.SetValue(Popup.IsOpenProperty, new TemplateBindingExtension(MenuItem.IsSubmenuOpenProperty));
        popup.SetValue(Popup.PlacementProperty, PlacementMode.Right);

        var popupBorder = new FrameworkElementFactory(typeof(Border));
        popupBorder.SetValue(Border.BackgroundProperty, popupBackground);
        popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0));

        var items = new FrameworkElementFactory(typeof(ItemsPresenter));
        popupBorder.AppendChild(items);
        popup.AppendChild(popupBorder);

        var panel = new FrameworkElementFactory(typeof(Grid));
        panel.AppendChild(root);
        panel.AppendChild(popup);

        var template = new ControlTemplate(typeof(MenuItem)) { VisualTree = panel };

        var hover = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, hoverBackground, "Root"));

        var submenu = new Trigger { Property = MenuItem.RoleProperty, Value = MenuItemRole.SubmenuHeader };
        submenu.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "Arrow"));
        submenu.Setters.Add(new Setter(Border.BackgroundProperty, hoverBackground, "Root"));

        var open = new Trigger { Property = MenuItem.IsSubmenuOpenProperty, Value = true };
        open.Setters.Add(new Setter(Border.BackgroundProperty, hoverBackground, "Root"));

        var topLevel = new Trigger { Property = MenuItem.RoleProperty, Value = MenuItemRole.TopLevelHeader };
        topLevel.Setters.Add(new Setter(Control.ForegroundProperty, secondaryBrush));
        topLevel.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed, "Arrow"));
        topLevel.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 0.0, "Dock"));
        topLevel.Setters.Add(new Setter(Popup.PlacementProperty, PlacementMode.Bottom, "PART_Popup"));

        template.Triggers.Add(hover);
        template.Triggers.Add(submenu);
        template.Triggers.Add(open);
        template.Triggers.Add(topLevel);
        return template;
    }

    private void ApplyDarkButtonText()
    {
        var buttonText = ThemeService.GetBrush("Theme.Brush.ButtonText", Color.FromRgb(0xD0, 0xD0, 0xD0));

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

    private void ApplyDarkMainWindowText()
    {
        var text = new SolidColorBrush(Color.FromRgb(0x27, 0x27, 0x27));

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
            StatusGatewayText,
            StatusGatewayPid,
            StatusGatewayUptime,
            StatusRam,
            StatusVram,
            StatusCpu
        })
        {
            textBlock.Foreground = text;
        }

        foreach (var item in EnumerateVisualChildren(MainStatusBar).OfType<TextBlock>())
            item.Foreground = text;

        GrpActions.Foreground = text;
        GrpLatency.Foreground = text;
        GrpAppLog.Foreground = text;
        AppLog.Foreground = text;
        MainStatusBar.Foreground = text;
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

    private void ApplyModernToolLayout()
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

    private void ApplyDarkToolLayout()
    {
        BtnOpenPowerShell.Margin = new Thickness(0, 0, 0, 6);
        BtnOpenGatewayLog.Margin = new Thickness(0, 0, 0, 6);
        BtnCleaningTool.Margin = new Thickness(0, 0, 0, 6);
        BtnTokenManager.Margin = new Thickness(0, 0, 0, 6);
        BtnDoctorFix.Margin = new Thickness(0, 0, 0, 0);
    }

    private void ReapplyCurrentThemeLayoutAfterLocalization()
    {
        if (SettingsService.Current.Theme != AppTheme.Legacy)
            ApplyModernToolLayout();
    }

    // ── Legacy UI — obnovit emoji TextBlock ───────────────────────────────────
    private void ApplyLegacyUi()
    {
        RestoreButtonLegacy(BtnGatewayStart,   "▶", "Green",  "Start");
        RestoreButtonLegacy(BtnGatewayStop,    "■", "Red",    "Stop");
        RestoreButtonLegacy(BtnGatewayRestart, "↻", "Orange", "Restart");
        RestoreButtonLegacy(BtnOpenPowerShell, "⚡", null,    BtnPowerShellLabel.Text);
        RestoreButtonLegacy(BtnOpenGatewayLog, "📄", null,   BtnGatewayLogLabel.Text);
        RestoreButtonLegacy(BtnCleaningTool,   "🧹", null,   BtnCleaningToolLabel.Text);
        RestoreButtonLegacy(BtnTokenManager,   "🔑", null,   BtnTokenManagerLabel.Text);
        RestoreButtonLegacy(BtnDoctorFix,      "🩺", null,   BtnDoctorFixLabel.Text);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Nastaví obsah tlačítka na PNG ikonu (Modern téma).
    /// Ikona je načtena z Resources/Icons/Modern/{name}.png jako embedded resource.
    /// Zachovává původní Label TextBlock pro lokalizaci — jen skryje emoji.
    /// </summary>
    private void SetButtonIcon(Button btn, string iconName)
    {
        var uri = ThemeService.GetIconUri(_activeTheme, iconName);
        if (uri == null) return;

        try
        {
            var img = new Image
            {
                Source = new BitmapImage(uri),
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
        btn.Style = CreateCrabCuteButtonFeedbackStyle(SettingsService.Current.UseButtonScanlineEffect);
        btn.Height = height;
        btn.Margin = btn == BtnStartTui ? new Thickness(0, 0, 0, 8) : btn.Margin;
        btn.Padding = new Thickness(0);
        btn.BorderThickness = new Thickness(0);
        btn.Background = Brushes.Transparent;
        btn.BorderBrush = Brushes.Transparent;
        btn.FocusVisualStyle = null;
    }

    private static void ApplyModernButtonFeedbackStyle(params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.Style = CreateModernButtonFeedbackStyle(SettingsService.Current.UseButtonScanlineEffect);
            button.FocusVisualStyle = null;
        }
    }

    private static Style CreateModernButtonFeedbackStyle(bool useScanlineEffect)
    {
        var hoverBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE7, 0xEE));
        var hoverBorderBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xC2, 0xCF));
        var pressedBrush = new SolidColorBrush(Color.FromRgb(0xD2, 0xD9, 0xE2));
        var pressedBorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0x9A, 0xAA));
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
    }

    private static void ApplyDarkButtonFeedbackStyle(Brush idleBrush, params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.Style = CreateDarkButtonFeedbackStyle(SettingsService.Current.UseButtonScanlineEffect, idleBrush);
            button.FocusVisualStyle = null;
        }
    }

    private static Style CreateDarkButtonFeedbackStyle(bool useScanlineEffect, Brush idleBrush)
    {
        var hoverBrush = ThemeService.GetBrush("Theme.Brush.Hover", Color.FromRgb(0x38, 0x38, 0x38));
        var pressedBrush = ThemeService.GetBrush("Theme.Brush.Pressed", Color.FromRgb(0x30, 0x30, 0x30));
        var buttonTextBrush = ThemeService.GetBrush("Theme.Brush.ButtonText", Colors.White);
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
    }

    private static Color GetBrushColor(Brush brush, Color fallback)
    {
        return brush is SolidColorBrush solid ? solid.Color : fallback;
    }

    private static Style CreateCrabCuteButtonFeedbackStyle(bool useScanlineEffect)
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
    }

    private static Brush CreatePressedScanlineBrush()
    {
        var drawingGroup = new DrawingGroup();
        drawingGroup.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromArgb(0x32, 0x00, 0x00, 0x00)),
            null,
            new RectangleGeometry(new Rect(0, 0, 1, 1))));
        drawingGroup.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromArgb(0xC0, 0x00, 0x00, 0x00)),
            null,
            new RectangleGeometry(new Rect(0, 0, 1, 0.22))));

        return new DrawingBrush(drawingGroup)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 1, 3),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None,
            Opacity = 1.0
        };
    }

    private Image? CreateThemeImage(string iconName, double height)
    {
        var uri = ThemeService.GetIconUri(_activeTheme, iconName);
        if (uri == null) return null;

        try
        {
            var image = new Image
            {
                Source = new BitmapImage(uri),
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

    private void SetMenuIcon(MenuItem item, string iconName)
    {
        item.Icon = CreateThemeImage(iconName, 18);
    }

    private void ApplyThemeSpecificTuiVisual(bool tuiRunning)
    {
        if (_activeTheme != AppTheme.CrabCute) return;

        SetCrabCuteButtonImage(BtnStartTui, tuiRunning ? "stop" : "tui", tuiRunning ? 44 : 88);
    }

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
                button.Margin,
                button.HorizontalContentAlignment,
                button.Padding,
                button.BorderThickness,
                button.Background,
                button.BorderBrush);
        }

        foreach (var element in new FrameworkElement[]
        {
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
            BtnDoctorFixLabel
        })
        {
            _textBlockForegroundStates[textBlock] = textBlock.Foreground;
        }

        foreach (var control in new Control[] { MainMenu, MainStatusBar, GrpActions, GrpLatency, GrpAppLog, AppLog })
        {
            _shellControlStates[control] = new ShellVisualState(control.Background, control.Foreground);
        }
    }

    private void RestoreThemeBaseline()
    {
        ApplyWindowCaptionColor(null, null);
        Background = SystemColors.WindowBrush;
        Foreground = SystemColors.ControlTextBrush;
        MainGridSplitter.Background = Brushes.LightGray;

        foreach (var (button, state) in _buttonVisualStates)
        {
            button.Content = state.Content;
            button.Style = state.Style;
            button.FocusVisualStyle = state.FocusVisualStyle;
            button.Height = state.Height;
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

        foreach (var (control, state) in _shellControlStates)
        {
            control.Background = state.Background;
            control.Foreground = state.Foreground;
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
    }

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
        if (btn.Content is StackPanel sp && sp.Children.Count >= 1)
        {
            // Pokud je první child Image (z Modern), nahradit zpět TextBlock
            if (sp.Children[0] is Image)
            {
                sp.Children.RemoveAt(0);
                var tb = new TextBlock
                {
                    Text              = emoji,
                    FontFamily        = new FontFamily("Segoe UI Emoji"),
                    FontSize          = 14,
                    Margin            = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                if (color != null)
                {
                    tb.Foreground = (Brush)new BrushConverter().ConvertFromString(color)!;
                    tb.FontWeight = FontWeights.Bold;
                }
                sp.Children.Insert(0, tb);
            }
        }
    }
}
