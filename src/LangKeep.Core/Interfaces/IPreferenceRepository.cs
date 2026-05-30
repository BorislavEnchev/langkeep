using LangKeep.Core.Models;

namespace LangKeep.Core.Interfaces;

/// <summary>
/// Provides read/write access to persisted language preferences.
/// </summary>
public interface IPreferenceRepository
{
    /// <summary>
    /// Gets all stored language preferences.
    /// </summary>
    Task<IReadOnlyList<LanguagePreference>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the preference for a specific application, if any.
    /// </summary>
    Task<LanguagePreference?> GetForApplicationAsync(
        ApplicationIdentity application,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates a language preference.
    /// </summary>
    Task SaveAsync(LanguagePreference preference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a language preference.
    /// </summary>
    Task DeleteAsync(ApplicationIdentity application, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all preferences to a portable format (JSON string).
    /// </summary>
    Task<string> ExportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports preferences from a portable format (JSON string).
    /// </summary>
    Task ImportAsync(string json, CancellationToken cancellationToken = default);
}
