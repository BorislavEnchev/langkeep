using System.Linq;
using System.Windows;
using LangKeep.Application;
using LangKeep.Application.Services;
using LangKeep.Infrastructure.Windows;
using LangKeep.UI.Wpf.Services;
using LangKeep.UI.Wpf.ViewModels;
using LangKeep.UI.Wpf.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LangKeep.UI.Wpf;

/// <summary>
/// The WPF application entry point that bootstraps dependency injection,
/// logging, and the main tray-application lifecycle.
/// </summary>
public sealed partial class App : System.Windows.Application
{
    private static readonly Mutex _instanceMutex = new(initiallyOwned: true, "Local\\LangKeep_5B8F3A2E_4C11_4A9E_8D7F_1E2C3B4A5D6F");
    private readonly IHost _host;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        var args = Environment.GetCommandLineArgs();
        bool isDebug = args.Contains("--debug", StringComparer.OrdinalIgnoreCase) ||
                       args.Contains("-d", StringComparer.OrdinalIgnoreCase);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.AddConsole();
                logging.AddFileLogging(); // Must be AFTER ClearProviders()
                logging.SetMinimumLevel(isDebug ? LogLevel.Debug : LogLevel.None);
            })
            .Build();
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Enforce single instance — if another LangKeep is already running, exit immediately.
        try
        {
            if (!_instanceMutex.WaitOne(TimeSpan.Zero, exitContext: false))
            {
                // Another instance is already running; exit immediately with no cleanup.
                Environment.Exit(0);
            }
        }
        catch (AbandonedMutexException)
        {
            // The previous instance terminated without releasing the mutex.
            // We still own it — proceed normally.
        }

        base.OnStartup(e);

        try
        {
            await _host.StartAsync();

            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("LangKeep starting up...");

            // Start WinEvent hook for foreground window changes
            var activeWindowProvider = _host.Services.GetRequiredService<Win32ActiveWindowProvider>();
            activeWindowProvider.StartHook();

            // Start keyboard layout polling
            var layoutProvider = _host.Services.GetRequiredService<Win32KeyboardLayoutProvider>();
            layoutProvider.StartPolling();

            // Initialize the tray icon service (creates the notify icon)
            var trayService = _host.Services.GetRequiredService<TrayIconService>();
            trayService.Initialize();

            // Start language tracking (async — loads preferences without blocking the UI thread)
            var trackingService = _host.Services.GetRequiredService<LanguageTrackingService>();
            await trackingService.StartAsync();

            logger.LogInformation("LangKeep started successfully.");
        }
        catch (Exception ex)
        {
            // Log the error before the process exits
            try
            {
                var logger = _host.Services.GetRequiredService<ILogger<App>>();
                logger.LogCritical(ex, "LangKeep failed to start.");
            }
            catch
            {
                // Best-effort — can't log if the host failed to initialize
            }

            System.Windows.MessageBox.Show(
                $"LangKeep encountered an error on startup:\n\n{ex.Message}",
                "LangKeep — Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Environment.Exit(1);
        }
    }

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs e)
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("LangKeep shutting down...");

        // Stop keyboard layout polling
        var layoutProvider = _host.Services.GetRequiredService<Win32KeyboardLayoutProvider>();
        layoutProvider.StopPolling();

        // Stop WinEvent hook
        var activeWindowProvider = _host.Services.GetRequiredService<Win32ActiveWindowProvider>();
        activeWindowProvider.StopHook();

        var trackingService = _host.Services.GetRequiredService<LanguageTrackingService>();
        trackingService.Stop();
        trackingService.Dispose();

        var hostLifetime = _host.Services.GetRequiredService<IHostLifetime>();
        await _host.StopAsync();
        _host.Dispose();

        // Release and dispose the single-instance mutex
        try
        {
            _instanceMutex.ReleaseMutex();
        }
        catch
        {
            // Best-effort cleanup
        }
        _instanceMutex.Dispose();

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core & Application
        services.AddApplicationServices();

        // Windows infrastructure
        services.AddWindowsInfrastructure();

        // WPF services
        services.AddSingleton<TrayIconService>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
    }

    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    internal static T GetService<T>() where T : notnull
    {
        var app = Current as App ?? throw new InvalidOperationException("App instance not available.");
        return app._host.Services.GetRequiredService<T>();
    }
}
