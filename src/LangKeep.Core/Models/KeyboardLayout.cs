using System.Globalization;

namespace LangKeep.Core.Models;

/// <summary>
/// Represents a keyboard input language/layout identified by its IETF language tag
/// (e.g., "en-US", "de-DE", "bg-BG").
/// </summary>
public sealed class KeyboardLayout : IEquatable<KeyboardLayout>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardLayout"/> class.
    /// </summary>
    /// <param name="languageTag">An IETF language tag (e.g., "en-US", "de-DE").</param>
    /// <param name="displayName">A human-readable display name (e.g., "English (United States)").</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="languageTag"/> is null.
    /// </exception>
    public KeyboardLayout(string languageTag, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
            throw new ArgumentException("Language tag cannot be empty.", nameof(languageTag));

        LanguageTag = languageTag;
        DisplayName = displayName ?? languageTag;
    }

    /// <summary>
    /// Gets the IETF language tag (e.g., "en-US", "de-DE").
    /// </summary>
    public string LanguageTag { get; }

    /// <summary>
    /// Gets a human-readable display name for this layout.
    /// </summary>
    public string DisplayName { get; }

    private static readonly CultureInfo[]? _installedCultures = GetInstalledCultures();

    private static CultureInfo[]? GetInstalledCultures()
    {
        try
        {
            return CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a <see cref="KeyboardLayout"/> from a Windows LCID (language ID).
    /// </summary>
    /// <param name="lcid">The Windows language ID (low word of HKL).</param>
    /// <returns>A new <see cref="KeyboardLayout"/> instance.</returns>
    public static KeyboardLayout FromLcid(int lcid)
    {
        // Try direct CultureInfo lookup first.
        try
        {
            var culture = new CultureInfo(lcid);
            // The invariant culture (LCID 0) has an empty Name — skip it and fall
            // through to the primary-language fallback below.
            if (!string.IsNullOrWhiteSpace(culture.Name))
                return new KeyboardLayout(culture.Name, culture.DisplayName);
        }
        catch (CultureNotFoundException)
        {
            // Fall through to the primary-language fallback.
        }

        // Primary-language fallback:
        // Some threads (e.g., console windows hosted by conhost.exe) report a
        // primary-language-only LangID (e.g., 0x0009 for English) instead of a
        // full LCID with sub-language (e.g., 0x0409 for en-US). Find any installed
        // culture sharing the same primary language.
        int primaryLangId = lcid & 0x03FF;
        if (_installedCultures is not null)
        {
            foreach (var culture in _installedCultures)
            {
                if ((culture.LCID & 0x03FF) == primaryLangId &&
                    !string.IsNullOrWhiteSpace(culture.Name))
                {
                    return new KeyboardLayout(culture.Name, culture.DisplayName);
                }
            }
        }

        return new KeyboardLayout($"unknown-0x{lcid:X4}", $"Unknown (LCID: 0x{lcid:X4})");
    }

    /// <inheritdoc />
    public bool Equals(KeyboardLayout? other)
    {
        if (other is null)
            return false;

        return string.Equals(LanguageTag, other.LanguageTag, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as KeyboardLayout);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(LanguageTag);

    /// <inheritdoc />
    public override string ToString() => $"{DisplayName} [{LanguageTag}]";
}
