using GUI.Models;
using GUI.Services;
using SharpVectors.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GUI.Views;

/// <summary>
/// Interaction logic for StratagemPickerWindow.xaml
/// </summary>
public partial class StratagemPickerWindow : Window
{
    private readonly StratagemService _service;
    public Stratagem? SelectedStratagem { get; private set; }

    // Icon size within picker grid
    private const int IconSize = 56;  // Inner icon
    private const int ButtonSize = 80;  // Outer button (fixed square)

    private const string PlaceholderText = "Search by name or tag...";
    private bool _isPlaceholder = true;

    public StratagemPickerWindow(StratagemService service)
    {
        InitializeComponent();
        _service = service;
        PopulateCategories();
        SetPlaceholder();
    }

    private void SetPlaceholder()
    {
        _isPlaceholder = true;
        SearchBox.Text = PlaceholderText;
        SearchBox.Foreground = (System.Windows.Media.Brush)FindResource("TextSecBrush");
    }

    private void ClearPlaceholder()
    {
        if (!_isPlaceholder) return;
        _isPlaceholder = false;
        SearchBox.Text = string.Empty;
        SearchBox.Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush");
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        => ClearPlaceholder();

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchBox.Text))
            SetPlaceholder();
    }

    // ─── Initial population ────────────────────────────────────────────────

    private void PopulateCategories()
    {
        PopulatePanel(OffensivePanel, _service.Offensive);
        PopulatePanel(SupplyPanel, _service.Supply);
        PopulatePanel(DefensivePanel, _service.Defensive);
    }

    private void PopulatePanel(WrapPanel panel, IEnumerable<Stratagem> stratagems)
    {
        panel.Children.Clear();
        foreach (var s in stratagems)
            panel.Children.Add(CreateIconButton(s));
    }

    // ─── Icon button factory ───────────────────────────────────────────────

    private Button CreateIconButton(Stratagem stratagem)
    {

        // Icon image or fallback text
        UIElement iconElement;
        try
        {
            iconElement = new SvgViewbox
            {
                Source = new Uri(stratagem.IconPath, UriKind.Absolute),
                Width = IconSize,
                Height = IconSize,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        catch
        {
            iconElement = new TextBlock
            {
                Text = "?",
                FontSize = 24,
                Foreground = (Brush)FindResource("TextSecBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = IconSize,
                Height = IconSize
            };
        }

        // Name label below icon
        var label = new TextBlock
        {
            Text = stratagem.Name,
            Style = (Style)FindResource("StratagemLabel"),
            Width = ButtonSize,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };

        // Stack icon + label, centered in fixed-width column
        var stack = new StackPanel
        {
            Width = ButtonSize,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(iconElement);
        stack.Children.Add(label);

        // Wrap in button
        var btn = new Button
        {
            Content = stack,
            Style = (Style)FindResource("StratagemIconButton"),
            Width = ButtonSize,
            Height = ButtonSize + 30,  // icon + label height
            ToolTip = $"{stratagem.Name}\n{string.Join(" → ", stratagem.Inputs)}"
        };

        btn.Click += (_, _) =>
        {
            SelectedStratagem = stratagem;
            DialogResult = true;
            Close();
        };

        return btn;
    }

    // ─── Search ────────────────────────────────────────────────────────────

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Ignore changes while placeholder is active
        if (_isPlaceholder) return;

        var query = SearchBox.Text.Trim();

        if (string.IsNullOrEmpty(query))
        {
            // Show categories, hide search results
            OffensiveSection.Visibility = Visibility.Visible;
            SupplySection.Visibility = Visibility.Visible;
            DefensiveSection.Visibility = Visibility.Visible;
            SearchSection.Visibility = Visibility.Collapsed;
            return;
        }

        // Hide categories, show search results
        OffensiveSection.Visibility = Visibility.Collapsed;
        SupplySection.Visibility = Visibility.Collapsed;
        DefensiveSection.Visibility = Visibility.Collapsed;
        SearchSection.Visibility = Visibility.Visible;

        var results = _service.Search(query);
        PopulatePanel(SearchPanel, results);
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        SetPlaceholder();
        SearchBox.Focus();

        // Restore category view
        OffensiveSection.Visibility = Visibility.Visible;
        SupplySection.Visibility = Visibility.Visible;
        DefensiveSection.Visibility = Visibility.Visible;
        SearchSection.Visibility = Visibility.Collapsed;
    }
}
