namespace LangKeep.Core.Enums;

/// <summary>
/// Defines the scope of a matching rule.
/// </summary>
/// <remarks>
/// Only <see cref="PerApplication"/> is implemented in the MVP.
/// Future values enable per-window, per-document, and browser-tab matching.
/// </remarks>
public enum RuleType
{
    /// <summary>
    /// Match by process name / executable.
    /// </summary>
    PerApplication = 0,

    /// <summary>
    /// Match by window title (future).
    /// </summary>
    PerWindow = 1,

    /// <summary>
    /// Match by document type / file extension (future).
    /// </summary>
    PerDocument = 2,

    /// <summary>
    /// Match by browser URL / tab content (future).
    /// </summary>
    BrowserTab = 3,
}
