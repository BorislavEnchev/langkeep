namespace LangKeep.Core.Models;

/// <summary>
/// Identifies an application (or window) that the language-tracking system
/// can associate with a keyboard layout preference.
/// </summary>
/// <remarks>
/// Equality is based on the <see cref="ProcessName"/> alone for MVP per-application
/// matching. Future extensions (per-window, per-document) will add the optional
/// fields <see cref="WindowTitle"/> and <see cref="ProcessPath"/>.
/// </remarks>
public sealed class ApplicationIdentity : IEquatable<ApplicationIdentity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationIdentity"/> class.
    /// </summary>
    /// <param name="processName">The executable name (e.g., "Code.exe").</param>
    /// <param name="processPath">Optional full path to the executable.</param>
    /// <param name="windowTitle">Optional window title for future per-window matching.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="processName"/> is null.</exception>
    public ApplicationIdentity(string processName, string? processPath = null, string? windowTitle = null)
    {
        ProcessName = processName ?? throw new ArgumentNullException(nameof(processName));
        ProcessPath = processPath;
        WindowTitle = windowTitle;
    }

    /// <summary>
    /// Gets the executable name, including the ".exe" extension if applicable.
    /// </summary>
    public string ProcessName { get; }

    /// <summary>
    /// Gets the optional full file-system path of the executable.
    /// </summary>
    public string? ProcessPath { get; }

    /// <summary>
    /// Gets the optional window title for future per-window matching.
    /// </summary>
    public string? WindowTitle { get; }

    /// <inheritdoc />
    public bool Equals(ApplicationIdentity? other)
    {
        if (other is null)
            return false;

        // MVP: match on process name only (case-insensitive on Windows).
        return string.Equals(ProcessName, other.ProcessName, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ApplicationIdentity);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(ProcessName);

    /// <summary>
    /// Determines whether two <see cref="ApplicationIdentity"/> instances are equal.
    /// </summary>
    public static bool operator ==(ApplicationIdentity? left, ApplicationIdentity? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="ApplicationIdentity"/> instances are not equal.
    /// </summary>
    public static bool operator !=(ApplicationIdentity? left, ApplicationIdentity? right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override string ToString() => ProcessName;
}
