namespace LangKeep.Core.Models;

/// <summary>
/// Represents a stored preference associating an application with a keyboard layout.
/// </summary>
public sealed class LanguagePreference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguagePreference"/> class.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="layout">The preferred keyboard layout.</param>
    /// <param name="isEnabled">Whether this preference is currently active.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="application"/> or <paramref name="layout"/> is null.
    /// </exception>
    public LanguagePreference(
        ApplicationIdentity application,
        KeyboardLayout layout,
        bool isEnabled = true)
    {
        Application = application ?? throw new ArgumentNullException(nameof(application));
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Gets the application this preference applies to.
    /// </summary>
    public ApplicationIdentity Application { get; }

    /// <summary>
    /// Gets the preferred keyboard layout.
    /// </summary>
    public KeyboardLayout Layout { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this preference is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the optional display order for the settings UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{Application.ProcessName} → {Layout.LanguageTag} ({(IsEnabled ? "enabled" : "disabled")})";
}
