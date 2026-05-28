using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
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
    // Windows 11 22H2+ system backdrop (38 = DWMWA_SYSTEMBACKDROP_TYPE)
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmsbtTransientWindow = 3; // Acrylic
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

        if (OpenClawManager.App.GetService<ISettingsService>().Settings.Theme != AppTheme.Modern &&
            !ThemeService.IsModernPaletteTheme(OpenClawManager.App.GetService<ISettingsService>().Settings.Theme)) return;

        // True glass themes (Dark / ModernLight) have GlassPanel tokens; Standard (Modern) does not.
        bool isGlass = Application.Current.TryFindResource("Theme.Brush.GlassPanel.Background") is Brush;

        if (isGlass)
        {
            // Near-transparent background lets DWM Acrylic backdrop show through WPF rendering.
            window.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            EnableModernBackdrop(window);
        }
        else
        {
            var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x0F, 0x11, 0x15));
            window.Background = ThemeService.GetBrush("Theme.Brush.WindowBackground",
                GetBrushColor(background, Color.FromRgb(0x0F, 0x11, 0x15)));
        }

        window.Foreground = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        window.Resources[typeof(Button)] = FindThemeStyle("Style.Button.StandardFlat");
        ApplyCaption(window);
        window.Loaded += (_, _) => ApplyLoadedVisuals(window, isGlass);
    }

    /// <summary>
    /// Requests Windows 11 Acrylic system backdrop for a secondary window.
    /// Silently no-ops on Windows 10 and older (DWM attribute not supported).
    /// </summary>
    private static void EnableModernBackdrop(Window window)
    {
        void Apply()
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int acrylic = DwmsbtTransientWindow;
                DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref acrylic, sizeof(int));
            }
            catch
            {
                // Windows 10 / pre-22H2: attribute not supported — fall back gracefully
                // The near-transparent background still gives a subtle glass feel over whatever is behind.
            }
        }

        if (new WindowInteropHelper(window).Handle == IntPtr.Zero)
            window.SourceInitialized += (_, _) => Apply();
        else
            Apply();
    }

    private static void ApplyLoadedVisuals(Window window, bool isGlass = false)
    {
        var primary    = ThemeService.GetBrush("Theme.Brush.Text.Primary",    Colors.White);
        var secondary  = ThemeService.GetBrush("Theme.Brush.Text.Secondary",  Color.FromRgb(0x9A, 0xA1, 0xAC));
        var background = ThemeService.GetBrush("Theme.Brush.Background",      Color.FromRgb(0x0F, 0x11, 0x15));
        var surface    = ThemeService.GetBrush("Theme.Brush.Surface",         Color.FromRgb(0x14, 0x17, 0x1C));
        var border     = ThemeService.GetBrush("Theme.Brush.Border",          Color.FromRgb(0x23, 0x27, 0x2F));

        // Glass themes use semi-transparent glass panel tokens for GroupBoxes.
        var glassPanelBg  = isGlass ? ThemeService.GetBrush("Theme.Brush.GlassPanel.Background",
                                          Color.FromArgb(0xB8, 0x08, 0x0A, 0x0E)) : surface;
        var glassBorder   = isGlass ? ThemeService.GetBrush("Theme.Brush.GlassBorder",
                                          Color.FromArgb(0x29, 0xFF, 0xFF, 0xFF)) : border;
        var groupBoxStyle = isGlass ? FindThemeStyle("Style.GroupBox.GlassPanel")
                                    : FindThemeStyle("Style.GroupBox.ModernPalette");

        foreach (var element in EnumerateVisualChildren(window))
        {
            switch (element)
            {
                case TextBlock textBlock:
                    textBlock.Foreground = IsSecondaryText(textBlock) ? secondary : primary;
                    break;
                case Label label:
                    label.Foreground = primary;
                    break;
                case GroupBox groupBox:
                    groupBox.Background  = glassPanelBg;
                    groupBox.Foreground  = primary;
                    groupBox.BorderBrush = glassBorder;
                    groupBox.Style       = groupBoxStyle;
                    break;
                case CheckBox checkBox:
                    checkBox.Foreground = primary;
                    break;
                case RadioButton radioButton:
                    radioButton.Foreground = primary;
                    break;
                case TextBox textBox:
                    textBox.Background = surface;
                    textBox.Foreground = primary;
                    textBox.BorderBrush = border;
                    textBox.CaretBrush = primary;
                    break;
                case PasswordBox passwordBox:
                    passwordBox.Background = surface;
                    passwordBox.Foreground = primary;
                    passwordBox.BorderBrush = border;
                    passwordBox.CaretBrush = primary;
                    break;
                case ComboBox comboBox:
                    comboBox.Background = surface;
                    comboBox.Foreground = primary;
                    comboBox.BorderBrush = border;
                    break;
                case DataGrid dataGrid:
                    dataGrid.Background = surface;
                    dataGrid.Foreground = primary;
                    dataGrid.BorderBrush = border;
                    dataGrid.HorizontalGridLinesBrush = border;
                    dataGrid.VerticalGridLinesBrush = border;
                    break;
                case Border { Name: "ShortcutFrame" } shortcutFrame:
                    shortcutFrame.Background = surface;
                    shortcutFrame.BorderBrush = border;
                    break;
                case Border borderElement when borderElement.BorderThickness != new Thickness(0):
                    borderElement.BorderBrush = border;
                    if (borderElement.CornerRadius == new CornerRadius(0))
                        borderElement.CornerRadius = new CornerRadius(6);
                    break;
                case StatusBar statusBar:
                    statusBar.Background = ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0x0B, 0x0C, 0x10));
                    statusBar.Foreground = secondary;
                    break;
            }
        }
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
                ThemeService.GetBrush("Theme.Brush.TitleBar", Color.FromRgb(0x14, 0x17, 0x1C)),
                Color.FromRgb(0x14, 0x17, 0x1C)));
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
