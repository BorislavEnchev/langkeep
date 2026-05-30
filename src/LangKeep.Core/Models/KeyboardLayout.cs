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

    /// <summary>
    /// Creates a <see cref="KeyboardLayout"/> from a Windows LCID (language ID).
    /// </summary>
    /// <param name="lcid">The Windows language ID (low word of HKL).</param>
    /// <returns>A new <see cref="KeyboardLayout"/> instance.</returns>
    public static KeyboardLayout FromLcid(int lcid)
    {
        try
        {
            var culture = new CultureInfo(lcid);
            return new KeyboardLayout(culture.Name, culture.DisplayName);
        }
        catch (CultureNotFoundException)
        {
            return new KeyboardLayout($"unknown-0x{lcid:X4}", $"Unknown (LCID: 0x{lcid:X4})");
        }
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
