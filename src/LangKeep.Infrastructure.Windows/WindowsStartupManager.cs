using LangKeep.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace LangKeep.Infrastructure.Windows;

/// <summary>
/// Registers the application to start automatically with Windows using the
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> registry key.
/// </summary>
public sealed class WindowsStartupManager : IStartupManager
{
    private readonly ILogger<WindowsStartupManager> _logger;
    private readonly string _executablePath;
    private readonly string _registryValueName;

    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsStartupManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="executablePath">
    /// Optional path to the executable. If not specified, uses the entry-assembly location.
    /// </param>
    /// <param name="registryValueName">
    /// Optional registry value name. If not specified, uses "LangKeep".
    /// </param>
    public WindowsStartupManager(
        ILogger<WindowsStartupManager> logger,
        string? executablePath = null,
        string? registryValueName = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executablePath = executablePath ?? Environment.ProcessPath ?? string.Empty;
        _registryValueName = registryValueName ?? "LangKeep";
    }

    /// <inheritdoc />
    public bool IsRegistered
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                var value = key?.GetValue(_registryValueName) as string;
                return string.Equals(value, _executablePath, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check startup registration.");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool Register()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key is null)
            {
                _logger.LogError("Cannot open registry key: {KeyPath}", RegistryKeyPath);
                return false;
            }

            key.SetValue(_registryValueName, _executablePath);
            _logger.LogInformation("Registered to start with Windows: {Path}", _executablePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register startup.");
            return false;
        }
    }

    /// <inheritdoc />
    public bool Unregister()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key?.GetValue(_registryValueName) is not null)
            {
                key.DeleteValue(_registryValueName);
                _logger.LogInformation("Unregistered from Windows startup.");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister startup.");
            return false;
        }
    }
}
