using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LangKeep.Application.Services;
using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;
using Microsoft.Extensions.Logging;

namespace LangKeep.UI.Wpf.ViewModels;

/// <summary>
/// ViewModel for the settings window that manages language preferences.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IPreferenceRepository _repository;
    private readonly IKeyboardLayoutProvider _layoutProvider;
    private readonly IActiveWindowProvider _activeWindowProvider;
    private readonly RuleEvaluationService _ruleService;
    private readonly ILogger<SettingsViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(
        IPreferenceRepository repository,
        IKeyboardLayoutProvider layoutProvider,
        IActiveWindowProvider activeWindowProvider,
        RuleEvaluationService ruleService,
        ILogger<SettingsViewModel> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _layoutProvider = layoutProvider ?? throw new ArgumentNullException(nameof(layoutProvider));
        _activeWindowProvider = activeWindowProvider ?? throw new ArgumentNullException(nameof(activeWindowProvider));
        _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the collection of application-language mappings.
    /// </summary>
    public ObservableCollection<PreferenceItem> Preferences { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the view model is loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoaded;

    /// <summary>
    /// Gets or sets the status message displayed in the UI.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Gets or sets the selected preference item.
    /// </summary>
    [ObservableProperty]
    private PreferenceItem? _selectedItem;

    /// <summary>
    /// Loads preferences from the repository.
    /// </summary>
    [RelayCommand]
    public async Task LoadPreferencesAsync()
    {
        try
        {
            IsLoaded = false;
            Preferences.Clear();

            var preferences = await _repository.GetAllAsync();
            foreach (var pref in preferences.OrderBy(p => p.Application.ProcessName))
            {
                Preferences.Add(new PreferenceItem
                {
                    ProcessName = pref.Application.ProcessName,
                    LanguageTag = pref.Layout.LanguageTag,
                    DisplayName = pref.Layout.DisplayName,
                    IsEnabled = pref.IsEnabled,
                });
            }

            // Sync in-memory rules with loaded preferences
            foreach (var pref in preferences)
            {
                _ruleService.AddOrUpdate(new MatchingRule(
                    pref.Application.ProcessName,
                    pref.Layout.LanguageTag,
                    pref.IsEnabled));
            }

            StatusMessage = $"Loaded {Preferences.Count} preference(s).";
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load preferences.");
            StatusMessage = "Failed to load preferences.";
        }
    }

    /// <summary>
    /// Adds or updates a preference for the currently active window.
    /// </summary>
    [RelayCommand]
    public async Task AddCurrentAsync()
    {
        try
        {
            var active = _activeWindowProvider.GetActiveWindow();
            if (active is null)
            {
                StatusMessage = "Could not detect active window.";
                return;
            }

            var existing = Preferences.FirstOrDefault(p =>
                string.Equals(p.ProcessName, active.Application.ProcessName, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.LanguageTag = active.CurrentLayout.LanguageTag;
                existing.DisplayName = active.CurrentLayout.DisplayName;
            }
            else
            {
                Preferences.Add(new PreferenceItem
                {
                    ProcessName = active.Application.ProcessName,
                    LanguageTag = active.CurrentLayout.LanguageTag,
                    DisplayName = active.CurrentLayout.DisplayName,
                    IsEnabled = true,
                });
            }

            // Persist
            var preference = new LanguagePreference(
                active.Application,
                active.CurrentLayout);
            await _repository.SaveAsync(preference);

            // Update in-memory rule
            _ruleService.AddOrUpdate(new MatchingRule(
                active.Application.ProcessName,
                active.CurrentLayout.LanguageTag));

            StatusMessage = $"Added: {active.Application.ProcessName} → {active.CurrentLayout.LanguageTag}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add current window preference.");
            StatusMessage = "Failed to add preference.";
        }
    }

    /// <summary>
    /// Deletes the selected preference.
    /// </summary>
    [RelayCommand]
    public async Task DeleteSelectedAsync()
    {
        if (SelectedItem is null)
        {
            StatusMessage = "No item selected.";
            return;
        }

        try
        {
            var identity = new LangKeep.Core.Models.ApplicationIdentity(SelectedItem.ProcessName);
            await _repository.DeleteAsync(identity);
            _ruleService.Remove(SelectedItem.ProcessName);
            Preferences.Remove(SelectedItem);

            StatusMessage = $"Deleted: {SelectedItem.ProcessName}";
            SelectedItem = null;
        }
        catch (Exception ex)
        { 
            _logger.LogError(ex, "Failed to delete preference.");
            StatusMessage = "Failed to delete preference.";
        }
    }

    /// <summary> 
    /// Toggles the enabled state of a preference.
    /// </summary>
    [RelayCommand] 
    public async Task ToggleEnabledAsync(PreferenceItem item)
    {
        try
        {
            var identity = new LangKeep.Core.Models.ApplicationIdentity(item.ProcessName);
            var layout = new KeyboardLayout(item.LanguageTag);
            var preference = new LanguagePreference(identity, layout, item.IsEnabled);
            await _repository.SaveAsync(preference);

            // Update in-memory rule
            var rule = _ruleService.Rules.FirstOrDefault(r =>
                string.Equals(r.ProcessName, item.ProcessName, StringComparison.OrdinalIgnoreCase));

            if (rule is not null)
            {
                rule.IsEnabled = item.IsEnabled;
            }

            StatusMessage = item.IsEnabled
                ? $"Enabled: {item.ProcessName}"
                : $"Disabled: {item.ProcessName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle preference.");
            StatusMessage = "Failed to update preference.";
        }
    }

    /// <summary>
    /// Exports preferences to clipboard as JSON.
    /// </summary>
    [RelayCommand]
    public async Task ExportAsync()
    {
        try
        {
            string json = await _repository.ExportAsync();
            System.Windows.Clipboard.SetText(json);
            StatusMessage = "Configuration exported to clipboard.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export configuration.");
            StatusMessage = "Failed to export configuration.";
        }
    }

    /// <summary>
    /// Imports preferences from clipboard JSON.
    /// </summary>
    [RelayCommand]
    public async Task ImportAsync()
    {
        try
        {
            string json = System.Windows.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(json))
            {
                StatusMessage = "Clipboard is empty.";
                return;
            }

            await _repository.ImportAsync(json);
            await LoadPreferencesAsync();
            StatusMessage = "Configuration imported successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import configuration.");
            StatusMessage = "Failed to import configuration.";
        }
    }
}

/// <summary>
/// Represents a single preference item in the settings UI.
/// </summary>
public sealed partial class PreferenceItem : ObservableObject
{
    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _languageTag = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;
}
