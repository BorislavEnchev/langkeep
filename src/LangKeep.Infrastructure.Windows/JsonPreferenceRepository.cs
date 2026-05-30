using System.Text.Json;
using System.Text.Json.Serialization;
using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;
using Microsoft.Extensions.Logging;

namespace LangKeep.Infrastructure.Windows;

/// <summary>
/// Persists language preferences as a JSON file under <c>%AppData%\LangKeep\</c>.
/// </summary>
public sealed class JsonPreferenceRepository : IPreferenceRepository
{
    private readonly ILogger<JsonPreferenceRepository> _logger;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string FileName = "preferences.json";
    private const int CurrentVersion = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPreferenceRepository"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="baseDirectory">
    /// Optional base directory. If not specified, defaults to <c>%AppData%\LangKeep</c>.
    /// </param>
    public JsonPreferenceRepository(
        ILogger<JsonPreferenceRepository> logger,
        string? baseDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        string dir = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LangKeep");

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, FileName);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        _logger.LogInformation("Preferences file path: {FilePath}", _filePath);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguagePreference>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var store = await LoadStoreAsync(ct);
            return store.Preferences
                .Select(p => new LanguagePreference(
                    new ApplicationIdentity(p.ProcessName),
                    new KeyboardLayout(p.LanguageTag),
                    p.IsEnabled)
                { SortOrder = p.SortOrder })
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load preferences from {FilePath}.", _filePath);
            return Array.Empty<LanguagePreference>();
        }
    }

    /// <inheritdoc />
    public async Task<LanguagePreference?> GetForApplicationAsync(
        ApplicationIdentity application, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(p => p.Application.Equals(application));
    }

    /// <inheritdoc />
    public async Task SaveAsync(LanguagePreference preference, CancellationToken ct = default)
    {
        try
        {
            var store = await LoadStoreAsync(ct);
            var existing = store.Preferences.FindIndex(p =>
                string.Equals(p.ProcessName, preference.Application.ProcessName, StringComparison.OrdinalIgnoreCase));

            var dto = new PreferenceDto
            {
                ProcessName = preference.Application.ProcessName,
                LanguageTag = preference.Layout.LanguageTag,
                IsEnabled = preference.IsEnabled,
                SortOrder = preference.SortOrder,
            };

            if (existing >= 0)
            {
                store.Preferences[existing] = dto;
            }
            else
            {
                store.Preferences.Add(dto);
            }

            await SaveStoreAsync(store, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save preference for {ProcessName}.", preference.Application.ProcessName);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ApplicationIdentity application, CancellationToken ct = default)
    {
        try
        {
            var store = await LoadStoreAsync(ct);
            store.Preferences.RemoveAll(p =>
                string.Equals(p.ProcessName, application.ProcessName, StringComparison.OrdinalIgnoreCase));
            await SaveStoreAsync(store, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete preference for {ProcessName}.", application.ProcessName);
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportAsync(CancellationToken ct = default)
    {
        var store = await LoadStoreAsync(ct);
        return JsonSerializer.Serialize(store, _jsonOptions);
    }

    /// <inheritdoc />
    public async Task ImportAsync(string json, CancellationToken ct = default)
    {
        try
        {
            var store = JsonSerializer.Deserialize<PreferenceStoreDto>(json, _jsonOptions);
            if (store is null)
            {
                _logger.LogWarning("Import failed: invalid JSON format.");
                return;
            }

            await SaveStoreAsync(store, ct);
            _logger.LogInformation("Imported {Count} preferences.", store.Preferences.Count);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse imported JSON.");
        }
    }

    // ───────────────────── Internal Store ─────────────────────

    private async Task<PreferenceStoreDto> LoadStoreAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return new PreferenceStoreDto { Version = CurrentVersion };

        try
        {
            string json = await File.ReadAllTextAsync(_filePath, ct);
            var store = JsonSerializer.Deserialize<PreferenceStoreDto>(json, _jsonOptions);

            if (store is null)
                return new PreferenceStoreDto { Version = CurrentVersion };

            // Future: migration logic based on store.Version vs CurrentVersion
            return store;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Corrupted preferences file; starting fresh.");
            return new PreferenceStoreDto { Version = CurrentVersion };
        }
    }

    private async Task SaveStoreAsync(PreferenceStoreDto store, CancellationToken ct)
    {
        store.Version = CurrentVersion;
        string json = JsonSerializer.Serialize(store, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    // ───────────────────── DTOs ─────────────────────

    private sealed class PreferenceStoreDto
    {
        public int Version { get; set; }
        public List<PreferenceDto> Preferences { get; set; } = [];
    }

    private sealed class PreferenceDto
    {
        public string ProcessName { get; set; } = string.Empty;
        public string LanguageTag { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
