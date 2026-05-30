using System.Windows;
using LangKeep.UI.Wpf.ViewModels;

namespace LangKeep.UI.Wpf.Windows;

/// <summary>
/// Settings window that displays and manages language preferences.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The settings view model.</param>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        InitializeComponent();

        // Load preferences when the window is shown
        Loaded += async (_, _) => await _viewModel.LoadPreferencesAsync();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
