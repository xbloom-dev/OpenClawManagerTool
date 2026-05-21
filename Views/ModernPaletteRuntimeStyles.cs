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
    private static readonly Dictionary<string, Style> StyleCache = new();
    private static Brush? _pressedScanlineBrush;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void ApplyIfModernPalette(Window window)
    {
        window.Icon = null;

        if (SettingsService.Current.Theme != AppTheme.Modern &&
            !ThemeService.IsModernPaletteTheme(SettingsService.Current.Theme)) return;

        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x19, 0x19, 0x19));
        window.Background = ThemeService.GetBrush("Theme.Brush.WindowBackground", GetBrushColor(background, Color.FromRgb(0x19, 0x19, 0x19)));
        window.Foreground = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        window.Resources[typeof(Button)] = GetModernPaletteButtonStyle(SettingsService.Current.UseButtonScanlineEffect);
        ApplyCaption(window);
        window.Loaded += (_, _) => ApplyLoadedVisuals(window);
    }

    private static void ApplyLoadedVisuals(Window window)
    {
        var primary = ThemeService.GetBrush("Theme.Brush.Text.Primary", Colors.White);
        var secondary = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x4E, 0x4E, 0x4E));
        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0x19, 0x19, 0x19));
        var surface = ThemeService.GetBrush("Theme.Brush.Surface", Color.FromRgb(0x27, 0x27, 0x27));
        var border = ThemeService.GetBrush("Theme.Brush.Border", Color.FromRgb(0x4E, 0x4E, 0x4E));

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
                    groupBox.Background = surface;
                    groupBox.Foreground = primary;
                    groupBox.BorderBrush = border;
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
                    break;
                case StatusBar statusBar:
                    statusBar.Background = ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0x12, 0x12, 0x12));
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

    private static Style CreateModernPaletteButtonStyle(bool useScanlineEffect)
    {
        var noShift = ThemeService.GetCurrentButtonInteraction()
            .Equals("PressScanlineNoShift", StringComparison.OrdinalIgnoreCase);
        var idleBrush = ThemeService.GetBrush("Theme.Brush.Disabled", Color.FromRgb(0x27, 0x27, 0x27));
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
        if (!noShift)
            hover.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(2, 2), "ContentGrid"));

        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(Border.BackgroundProperty, pressedBrush, "Root"));
        if (!noShift)
            pressed.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(3, 3), "ContentGrid"));
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
    }

    private static Style GetModernPaletteButtonStyle(bool useScanlineEffect)
    {
        var key = $"{SettingsService.Current.Theme}|{ThemeService.GetCurrentButtonInteraction()}|{useScanlineEffect}";
        if (StyleCache.TryGetValue(key, out var cached))
            return cached;

        var style = CreateModernPaletteButtonStyle(useScanlineEffect);
        StyleCache[key] = style;
        return style;
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
                ThemeService.GetBrush("Theme.Brush.Chrome", Color.FromRgb(0x12, 0x12, 0x12)),
                Color.FromRgb(0x12, 0x12, 0x12)));
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
