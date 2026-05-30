using LangKeep.Core.Interfaces;
using LangKeep.Infrastructure.Windows.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LangKeep.Infrastructure.Windows;

/// <summary>
/// Registers Windows infrastructure services with the dependency injection container.
/// </summary>
public static class InfrastructureModule
{
    /// <summary>
    /// Adds Windows infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWindowsInfrastructure(this IServiceCollection services)
    {
        // Active window provider (singleton — maintains the event hook)
        services.TryAddSingleton<Win32ActiveWindowProvider>();
        services.TryAddSingleton<IActiveWindowProvider>(sp => sp.GetRequiredService<Win32ActiveWindowProvider>());

        // Keyboard layout provider (singleton — maintains the polling timer)
        services.TryAddSingleton<Win32KeyboardLayoutProvider>();
        services.TryAddSingleton<IKeyboardLayoutProvider>(sp => sp.GetRequiredService<Win32KeyboardLayoutProvider>());

        // Keyboard layout switcher
        services.TryAddSingleton<IKeyboardLayoutSwitcher, Win32KeyboardLayoutSwitcher>();

        // Preference repository
        services.TryAddSingleton<IPreferenceRepository, JsonPreferenceRepository>();

        // Startup manager
        services.TryAddSingleton<IStartupManager, WindowsStartupManager>();

        return services;
    }

    /// <summary>
    /// Adds a file logger provider that writes to <c>%AppData%\LangKeep\logs\</c>.
    /// Must be called AFTER <c>ClearProviders()</c> in <c>ConfigureLogging</c>
    /// to avoid being removed by <c>RemoveAll&lt;ILoggerProvider&gt;()</c>.
    /// </summary>
    /// <param name="logging">The logging builder.</param>
    /// <returns>The same logging builder for chaining.</returns>
    public static ILoggingBuilder AddFileLogging(this ILoggingBuilder logging)
    {
        var logsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LangKeep",
            "logs");
        logging.AddProvider(new FileLoggerProvider(logsDirectory));
        return logging;
    }
}
