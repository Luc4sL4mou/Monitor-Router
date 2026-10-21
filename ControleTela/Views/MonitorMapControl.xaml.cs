using ControleTela.ViewModels;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ControleTela.Views;

/// <summary>
/// Desenha os monitores na posição real, como em Configurações > Sistema > Tela.
/// Monitors e SelectedMonitor são "DependencyProperties": propriedades especiais do WPF que
/// aceitam binding vindo do XAML (ex.: SelectedMonitor="{Binding SelectedMonitor}").
/// </summary>
public partial class MonitorMapControl : UserControl
{
    private const double MapPadding = 24; // margem interna do mapa
    private const double Gap = 4;      // espaço entre monitores vizinhos

    public static readonly DependencyProperty MonitorsProperty = DependencyProperty.Register(
        nameof(Monitors), typeof(IEnumerable<MonitorViewModel>), typeof(MonitorMapControl),
        new PropertyMetadata(null, OnMonitorsChanged));

    public static readonly DependencyProperty SelectedMonitorProperty = DependencyProperty.Register(
        nameof(SelectedMonitor), typeof(MonitorViewModel), typeof(MonitorMapControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((MonitorMapControl)d).UpdateVisuals()));

    public MonitorMapControl()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Rebuild();
    }

    public IEnumerable<MonitorViewModel>? Monitors
    {
        get => (IEnumerable<MonitorViewModel>?)GetValue(MonitorsProperty);
        set => SetValue(MonitorsProperty, value);
    }

    public MonitorViewModel? SelectedMonitor
    {
        get => (MonitorViewModel?)GetValue(SelectedMonitorProperty);
        set => SetValue(SelectedMonitorProperty, value);
    }

    // Quando a lista muda (monitor conectado/desconectado), redesenhamos.
    private static void OnMonitorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var map = (MonitorMapControl)d;
        if (e.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= map.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += map.OnCollectionChanged;
        map.Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        MapCanvas.Children.Clear();
        var monitors = Monitors?.ToList();
        if (monitors is null || monitors.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0) return;

        // 1) Caixa que envolve todos os monitores (em pixels reais do Windows).
        int minX = monitors.Min(m => m.Info.X), minY = monitors.Min(m => m.Info.Y);
        int maxX = monitors.Max(m => m.Info.X + m.Info.Width), maxY = monitors.Max(m => m.Info.Y + m.Info.Height);
        double totalW = Math.Max(1, maxX - minX), totalH = Math.Max(1, maxY - minY);

        // 2) Escala para caber no controle, e deslocamento para centralizar.
        double scale = Math.Min((ActualWidth - 2 * MapPadding) / totalW, (ActualHeight - 2 * MapPadding) / totalH);
        double offsetX = (ActualWidth - totalW * scale) / 2;
        double offsetY = (ActualHeight - totalH * scale) / 2;

        // 3) Um retângulo arredondado por monitor, com o número no meio.
        foreach (var vm in monitors)
        {
            double w = vm.Info.Width * scale - Gap, h = vm.Info.Height * scale - Gap;
            var number = new TextBlock
            {
                Text = vm.Number.ToString(),
                FontSize = Math.Clamp(h * 0.3, 12, 36),
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var box = new Border
            {
                Width = Math.Max(8, w),
                Height = Math.Max(8, h),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Child = number,
                Tag = vm,
                ToolTip = $"{vm.DisplayName}\n{vm.Info.Width} × {vm.Info.Height}"
            };
            box.MouseLeftButtonUp += (_, _) => SetCurrentValue(SelectedMonitorProperty, vm);
            box.MouseEnter += (_, _) => ApplyVisual(box);
            box.MouseLeave += (_, _) => ApplyVisual(box);

            Canvas.SetLeft(box, offsetX + (vm.Info.X - minX) * scale + Gap / 2);
            Canvas.SetTop(box, offsetY + (vm.Info.Y - minY) * scale + Gap / 2);
            MapCanvas.Children.Add(box);
            ApplyVisual(box);
        }
    }

    private void UpdateVisuals()
    {
        foreach (var box in MapCanvas.Children.OfType<Border>()) ApplyVisual(box);
    }

    // SetResourceReference = "use a cor do tema"; se o tema mudar, a cor acompanha sozinha.
    private void ApplyVisual(Border box)
    {
        bool selected = box.Tag == SelectedMonitor;
        var number = (TextBlock)box.Child;
        if (selected)
        {
            var accent = GetWindowsAccentBrush();
            box.Background = accent;
            box.BorderBrush = accent;
            number.Foreground = Brushes.White;
        }
        else
        {
            box.SetResourceReference(Border.BackgroundProperty,
                box.IsMouseOver ? "ControlFillColorSecondaryBrush" : "ControlFillColorDefaultBrush");
            box.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
            number.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        }
    }

    /// <summary>
    /// Cor de destaque exata do Windows (a mesma do mapa em Configurações > Tela). Fica no registro
    /// em HKCU\Software\Microsoft\Windows\DWM\AccentColor, no formato 0xAABBGGRR (ordem invertida).
    /// </summary>
    internal static Brush GetWindowsAccentBrush()
    {
        if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "AccentColor", null) is int abgr)
        {
            var brush = new SolidColorBrush(Color.FromRgb((byte)abgr, (byte)(abgr >> 8), (byte)(abgr >> 16)));
            brush.Freeze();
            return brush;
        }
        return (Brush)Application.Current.FindResource("AccentFillColorDefaultBrush");
    }
}
