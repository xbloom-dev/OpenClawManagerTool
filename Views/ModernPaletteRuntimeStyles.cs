using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

internal static class ModernPaletteRuntimeStyles
{
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const string ModernDarkSecondaryShellAppliedKey = "OpenClaw.ModernDark.SecondaryShellApplied";
    /// <summary>
    /// Cached procedural scanline overlay kept in C# because it is a generated DrawingBrush,
    /// not a simple theme token.
    /// </summary>
    private static Brush? _pressedScanlineBrush;

    private static Style FindThemeStyle(string key) =>
        (Style)Application.Current.FindResource(key);

    // DWM caption coloring is native Windows chrome work, so it intentionally remains in code.
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void ApplyIfModernPalette(Window window)
    {
        window.Icon = null;

        var theme = ThemeService.CurrentTheme;
        if (theme != AppTheme.StandardLight && !ThemeService.IsModernPaletteTheme(theme)) return;

        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x19, 0x19, 0x19));
        window.Background = ThemeService.GetBrush("Theme.Brush.WindowBackground", GetBrushColor(background, Color.FromRgb(0x19, 0x19, 0x19)));
        window.Foreground = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        window.Resources[typeof(Button)] = theme == AppTheme.ModernDark
            ? FindThemeStyle("Theme.Style.Button")
            : FindThemeStyle("Style.Button.StandardFlat");
        if (theme == AppTheme.ModernDark)
            ApplyModernDarkSecondaryShell(window);
        else
            ApplyCaption(window);
        window.Loaded += (_, _) => ApplyLoadedVisuals(window, theme);
    }

    private static void ApplyModernDarkSecondaryShell(Window window)
    {
        if (window.Resources.Contains(ModernDarkSecondaryShellAppliedKey) || window.IsLoaded) return;
        if (window.Content is not UIElement content) return;

        var useFullTransparency = OpenClawManager.App.GetService<ISettingsService>().Settings.UseFullSecondaryWindowTransparency;
        window.Resources[ModernDarkSecondaryShellAppliedKey] = true;
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = useFullTransparency;
        window.Background = useFullTransparency
            ? Brushes.Transparent
            : GetResourceBrush("Theme.Brush.SecondaryWindowBg", new SolidColorBrush(Color.FromRgb(0x05, 0x06, 0x0C)));
        if (window.ResizeMode == ResizeMode.CanResize)
            window.ResizeMode = ResizeMode.CanResizeWithGrip;

        window.Content = null;
        var shell = new Border
        {
            Background = GetResourceBrush("Theme.Brush.SecondaryWindowBg",
                new SolidColorBrush(Color.FromRgb(0x05, 0x06, 0x0C))),
            BorderBrush = GetResourceBrush("Theme.Brush.SecondaryWindowBorder",
                new SolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF))),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Child = content,
            SnapsToDevicePixels = true,
            ClipToBounds = true
        };

        shell.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState != MouseButtonState.Pressed || IsInsideInteractiveElement(e.OriginalSource as DependencyObject))
                return;

            try
            {
                window.DragMove();
            }
            catch (InvalidOperationException)
            {
                // DragMove can throw if Windows has already ended the drag gesture.
            }
        };

        window.Content = shell;
    }

    private static void ApplyLoadedVisuals(Window window, AppTheme theme)
    {
        var isModernDark = theme == AppTheme.ModernDark;
        var primary = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        var secondary = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x78, 0x78, 0x78));
        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x19, 0x19, 0x19));
        var surface = ThemeService.GetBrush("Theme.Brush.Surface", Color.FromRgb(0x27, 0x27, 0x27));
        var border = ThemeService.GetBrush("Theme.Brush.Border", Color.FromRgb(0x4E, 0x4E, 0x4E));
        var glassSection = isModernDark
            ? new SolidColorBrush(Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF))
            : ThemeService.GetBrush("Theme.Brush.Glass.SectionBg", Color.FromArgb(0x0B, 0xFF, 0xFF, 0xFF));
        var glassBorder = isModernDark
            ? new SolidColorBrush(Color.FromArgb(0x4A, 0xFF, 0xFF, 0xFF))
            : ThemeService.GetBrush("Theme.Brush.Glass.GlassBorder", Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF));
        var inputBackground = isModernDark
            ? new SolidColorBrush(Color.FromArgb(0xDA, 0x04, 0x04, 0x08))
            : surface;
        var logBackground = isModernDark
            ? new SolidColorBrush(Color.FromArgb(0xEA, 0x04, 0x04, 0x08))
            : surface;

        foreach (var element in EnumerateVisualChildren(window))
        {
            switch (element)
            {
                case Button button when isModernDark:
                    ApplyModernDarkButtonVisual(button);
                    break;
                case TextBlock textBlock when isModernDark && IsInside<Button>(textBlock):
                    break;
                case TextBlock textBlock:
                    textBlock.Foreground = IsSecondaryText(textBlock) ? secondary : primary;
                    break;
                case Label label:
                    label.Foreground = primary;
                    break;
                case GroupBox groupBox when isModernDark:
                    groupBox.Background = glassSection;
                    groupBox.Foreground = primary;
                    groupBox.BorderBrush = glassBorder;
                    groupBox.BorderThickness = new Thickness(1);
                    groupBox.Style = FindThemeStyle("Theme.Style.GroupBox");
                    break;
                case GroupBox groupBox:
                    groupBox.Background = surface;
                    groupBox.Foreground = primary;
                    groupBox.BorderBrush = border;
                    groupBox.Style = FindThemeStyle("Style.GroupBox.ModernPalette");
                    break;
                case CheckBox checkBox:
                    checkBox.Foreground = primary;
                    break;
                case RadioButton radioButton:
                    radioButton.Foreground = primary;
                    break;
                case TextBox textBox when isModernDark:
                    textBox.Background = IsLargeTextBox(textBox) ? logBackground : inputBackground;
                    textBox.Foreground = primary;
                    textBox.BorderBrush = glassBorder;
                    textBox.CaretBrush = primary;
                    break;
                case TextBox textBox:
                    textBox.Background = surface;
                    textBox.Foreground = primary;
                    textBox.BorderBrush = border;
                    textBox.CaretBrush = primary;
                    break;
                case PasswordBox passwordBox:
                    passwordBox.Background = isModernDark ? inputBackground : surface;
                    passwordBox.Foreground = primary;
                    passwordBox.BorderBrush = isModernDark ? glassBorder : border;
                    passwordBox.CaretBrush = primary;
                    break;
                case ComboBox comboBox:
                    comboBox.Background = isModernDark ? inputBackground : surface;
                    comboBox.Foreground = primary;
                    comboBox.BorderBrush = isModernDark ? glassBorder : border;
                    break;
                case DataGrid dataGrid:
                    dataGrid.Background = isModernDark ? logBackground : surface;
                    dataGrid.Foreground = primary;
                    dataGrid.BorderBrush = isModernDark ? glassBorder : border;
                    dataGrid.HorizontalGridLinesBrush = isModernDark ? glassBorder : border;
                    dataGrid.VerticalGridLinesBrush = isModernDark ? glassBorder : border;
                    break;
                case Border { Name: "ShortcutFrame" } shortcutFrame:
                    shortcutFrame.Background = isModernDark ? glassSection : surface;
                    shortcutFrame.BorderBrush = isModernDark ? glassBorder : border;
                    break;
                case Border borderElement when borderElement.BorderThickness != new Thickness(0):
                    borderElement.BorderBrush = isModernDark ? glassBorder : border;
                    if (borderElement.CornerRadius == new CornerRadius(0))
                        borderElement.CornerRadius = new CornerRadius(isModernDark ? 14 : 6);
                    break;
                case StatusBar statusBar:
                    statusBar.Background = isModernDark
                        ? ThemeService.GetBrush("Theme.Brush.Glass.BarBg", Color.FromRgb(0x14, 0x14, 0x14))
                        : ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0x12, 0x12, 0x12));
                    statusBar.Foreground = secondary;
                    break;
            }
        }
    }

    private static void ApplyModernDarkButtonVisual(Button button)
    {
        var text = ExtractButtonText(button.Content);
        var role = GetButtonRole(button, text);

        button.BorderThickness = new Thickness(1);
        button.Padding = new Thickness(12, 7, 12, 7);
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;

        switch (role)
        {
            case ButtonRole.Primary:
                button.Background = CreateAccentBrush();
                button.BorderBrush = new SolidColorBrush(Color.FromArgb(0x66, 0xD6, 0xE7, 0xFF));
                button.Foreground = Brushes.White;
                break;
            case ButtonRole.Danger:
                button.Background = new SolidColorBrush(Color.FromArgb(0x18, 0xEF, 0x44, 0x44));
                button.BorderBrush = new SolidColorBrush(Color.FromArgb(0x7A, 0xEF, 0x44, 0x44));
                button.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x86));
                break;
            case ButtonRole.Warning:
                button.Background = new SolidColorBrush(Color.FromArgb(0x16, 0xFB, 0xBF, 0x24));
                button.BorderBrush = new SolidColorBrush(Color.FromArgb(0x72, 0xFB, 0xBF, 0x24));
                button.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0xD7, 0x7A));
                break;
            default:
                button.Background = new SolidColorBrush(Color.FromArgb(0x2F, 0xFF, 0xFF, 0xFF));
                button.BorderBrush = new SolidColorBrush(Color.FromArgb(0x52, 0xFF, 0xFF, 0xFF));
                button.Foreground = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
                break;
        }
    }

    private static Brush CreateAccentBrush()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x5A, 0xA1, 0xFF), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1D, 0x4E, 0xD8), 1.0));
        return brush;
    }

    private static string ExtractButtonText(object? content)
    {
        return content switch
        {
            null => string.Empty,
            string text => text,
            TextBlock textBlock => textBlock.Text,
            AccessText accessText => accessText.Text,
            _ => content.ToString() ?? string.Empty
        };
    }

    private static ButtonRole GetButtonRole(Button button, string text)
    {
        var key = $"{button.Name} {text}".ToLowerInvariant();
        if (ContainsAny(key, "redact", "remove", "delete", "smaz", "stop", "zastavit"))
            return ButtonRole.Danger;
        if (ContainsAny(key, "restore", "backup", "zálohovat", "zalohovat", "reset"))
            return ButtonRole.Warning;
        if (ContainsAny(key, "verify", "ověřit", "overit", "save", "uložit", "ulozit", "run", "spustit", "yes", "ano", "ok", "live"))
            return ButtonRole.Primary;
        return ButtonRole.Neutral;
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(value.Contains);
    }

    private static bool IsLargeTextBox(TextBox textBox)
    {
        return textBox.AcceptsReturn || textBox.MinLines > 1 || textBox.Height >= 80;
    }

    private enum ButtonRole
    {
        Neutral,
        Primary,
        Danger,
        Warning
    }

    private static bool IsSecondaryText(TextBlock textBlock)
    {
        if (IsInside<StatusBar>(textBlock)) return true;
        if (textBlock.FontWeight.ToOpenTypeWeight() >= FontWeights.SemiBold.ToOpenTypeWeight()) return false;
        if (textBlock.FontSize <= 11.5) return true;
        return IsMutedBrush(textBlock.Foreground);
    }

    private static bool IsMutedBrush(Brush brush)
    {
        if (brush is not SolidColorBrush solid) return false;

        var color = solid.Color;
        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        var min = Math.Min(color.R, Math.Min(color.G, color.B));

        return max - min <= 10 && max is >= 80 and <= 190;
    }

    private static bool IsInside<T>(DependencyObject element)
        where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(element);
        while (current != null)
        {
            if (current is T) return true;
            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static bool IsInsideInteractiveElement(DependencyObject? element)
    {
        var current = element;
        while (current != null)
        {
            if (current is ButtonBase or TextBoxBase or PasswordBox or ComboBox or ListBox or DataGrid or ScrollBar or RadioButton or CheckBox)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static Brush GetResourceBrush(string key, Brush fallback)
    {
        return Application.Current.TryFindResource(key) as Brush ?? fallback;
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

    private static void ApplyCaption(Window window)
    {
        void ApplyNow()
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var caption = ToColorRef(GetBrushColor(
                ThemeService.GetBrush("Theme.Brush.TitleBar", Color.FromRgb(0x20, 0x20, 0x20)),
                Color.FromRgb(0x20, 0x20, 0x20)));
            var border = caption;
            var text = ToColorRef(GetBrushColor(
                ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White),
                Colors.White));

            _ = DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref caption, sizeof(int));
            _ = DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref border, sizeof(int));
            _ = DwmSetWindowAttribute(hwnd, DwmwaTextColor, ref text, sizeof(int));
        }

        if (new WindowInteropHelper(window).Handle == IntPtr.Zero)
            window.SourceInitialized += (_, _) => ApplyNow();
        else
            ApplyNow();
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    private static Color GetBrushColor(Brush brush, Color fallback)
    {
        return brush is SolidColorBrush solid ? solid.Color : fallback;
    }

}
