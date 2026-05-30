namespace LangKeep.Core.Models;

/// <summary>
/// A rule that defines when a specific keyboard layout should be activated.
/// </summary>
/// <remarks>
/// Designed for future extensibility:
/// <list type="bullet">
///   <item>Current: matches by <see cref="ProcessName"/> only.</item>
///   <item>Future: may also match by <see cref="WindowTitleContains"/>, URL, or document type.</item>
/// </list>
/// </remarks>
public sealed class MatchingRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchingRule"/> class.
    /// </summary>
    /// <param name="processName">The process name to match (e.g., "Code.exe").</param>
    /// <param name="languageTag">The target language tag (e.g., "en-US").</param>
    /// <param name="isEnabled">Whether the rule is active.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="processName"/> or <paramref name="languageTag"/> is null.
    /// </exception>
    public MatchingRule(
        string processName,
        string languageTag,
        bool isEnabled = true)
    {
        ProcessName = processName ?? throw new ArgumentNullException(nameof(processName));
        LanguageTag = languageTag ?? throw new ArgumentNullException(nameof(languageTag));
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Gets the process name to match (e.g., "Teams.exe").
    /// </summary>
    public string ProcessName { get; }

    /// <summary>
    /// Gets the target language tag (e.g., "de-DE").
    /// </summary>
    public string LanguageTag { get; }

    /// <summary>
    /// Gets or sets whether this rule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets an optional window-title substring for future per-window matching.
    /// </summary>
    public string? WindowTitleContains { get; set; }

    /// <summary>
    /// Gets the sort order for evaluation priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a user-friendly display name for this rule.
    /// </summary>
    public string? DisplayName { get; set; }
}
