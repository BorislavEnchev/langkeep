using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;
using Microsoft.Extensions.Logging;

namespace LangKeep.Application.Services;

/// <summary>
/// Manages the lifecycle of language preferences: retrieval, creation, update,
/// deletion, import, and export.
/// </summary>
public sealed class PreferenceManagementService
{
    private readonly IPreferenceRepository _repository;
    private readonly ILogger<PreferenceManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferenceManagementService"/> class.
    /// </summary>
    /// <param name="repository">The preference repository.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="repository"/> or <paramref name="logger"/> is null.
    /// </exception>
    public PreferenceManagementService(
        IPreferenceRepository repository,
        ILogger<PreferenceManagementService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all stored preferences.
    /// </summary>
    public async Task<IReadOnlyList<LanguagePreference>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await _repository.GetAllAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all preferences.");
            return Array.Empty<LanguagePreference>();
        }
    }

    /// <summary>
    /// Gets the preference for a specific application.
    /// </summary>
    public async Task<LanguagePreference?> GetForApplicationAsync(
        ApplicationIdentity application, CancellationToken ct = default)
    {
        try
        {
            return await _repository.GetForApplicationAsync(application, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve preference for {ProcessName}.", application.ProcessName);
            return null;
        }
    }

    /// <summary>
    /// Creates or updates a language preference.
    /// </summary>
    public async Task SetPreferenceAsync(
        ApplicationIdentity application,
        KeyboardLayout layout,
        bool isEnabled = true,
        CancellationToken ct = default)
    {
        var preference = new LanguagePreference(application, layout, isEnabled);
        try
        {
            await _repository.SaveAsync(preference, ct);
            _logger.LogInformation(
                "Preference saved: {ProcessName} → {LanguageTag} ({Enabled})",
                application.ProcessName, layout.LanguageTag, isEnabled ? "enabled" : "disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save preference for {ProcessName}.", application.ProcessName);
        }
    }

    /// <summary>
    /// Deletes the preference for a specific application.
    /// </summary>
    public async Task DeletePreferenceAsync(ApplicationIdentity application, CancellationToken ct = default)
    {
        try
        {
            await _repository.DeleteAsync(application, ct);
            _logger.LogInformation("Preference deleted for {ProcessName}.", application.ProcessName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete preference for {ProcessName}.", application.ProcessName);
        }
    }

    /// <summary>
    /// Exports all preferences to a JSON string.
    /// </summary>
    public async Task<string> ExportAsync(CancellationToken ct = default)
    {
        try
        {
            return await _repository.ExportAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export preferences.");
            return "{}";
        }
    }

    /// <summary>
    /// Imports preferences from a JSON string.
    /// </summary>
    public async Task ImportAsync(string json, CancellationToken ct = default)
    {
        try
        {
            await _repository.ImportAsync(json, ct);
            _logger.LogInformation("Preferences imported successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import preferences.");
        }
    }
}
