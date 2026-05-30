namespace LangKeep.Core.Interfaces;

/// <summary>
/// Provides platform-specific startup registration (e.g., "Start with Windows").
/// </summary>
/// <remarks>
/// Each supported platform provides its own implementation.
/// </remarks>
public interface IStartupManager
{
    /// <summary>
    /// Gets whether the application is currently registered to start automatically.
    /// </summary>
    bool IsRegistered { get; }

    /// <summary>
    /// Registers the application to start automatically when the user logs in.
    /// </summary>
    /// <returns><c>true</c> if registration succeeded; otherwise, <c>false</c>.</returns>
    bool Register();

    /// <summary>
    /// Unregisters the application from starting automatically.
    /// </summary>
    /// <returns><c>true</c> if unregistration succeeded; otherwise, <c>false</c>.</returns>
    bool Unregister();
}
