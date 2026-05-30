using System.IO;
using System.Windows;
using LangKeep.Application.Services;
using LangKeep.Core.Interfaces;
using LangKeep.UI.Wpf.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LangKeep.UI.Wpf.Services;

/// <summary>
/// Manages the system-tray icon and its context menu.
/// Uses <c>System.Windows.Forms.NotifyIcon</c> via the WindowsForms integration.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly ILogger<TrayIconService> _logger;
    private readonly IStartupManager _startupManager;
    private readonly LanguageTrackingService _trackingService;
    private readonly IServiceProvider _serviceProvider;

    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private System.Windows.Forms.ContextMenuStrip? _contextMenu;
    private SettingsWindow? _settingsWindow;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconService"/> class.
    /// </summary>
    public TrayIconService(
        ILogger<TrayIconService> logger,
        IStartupManager startupManager,
        LanguageTrackingService trackingService,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _startupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));
        _trackingService = trackingService ?? throw new ArgumentNullException(nameof(trackingService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Creates and shows the tray icon.
    /// </summary>
    public void Initialize()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = GetAppIcon(),
            Text = "LangKeep — Auto Language Switcher",
            Visible = true,
        };

        _notifyIcon.MouseClick += OnTrayIconClick;

        BuildContextMenu();
        _notifyIcon.ContextMenuStrip = _contextMenu;

        _logger.LogInformation("Tray icon initialized.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_notifyIcon is not null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            _contextMenu?.Dispose();
            _contextMenu = null;

            _disposed = true;
        }
    }

    /// <summary>
    /// Shows the settings window.
    /// </summary>
    public void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        _settingsWindow.Owner = null; // No owner for tray-launched window
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    /// <summary>
    /// Updates the tray icon text to reflect current state.
    /// </summary>
    public void UpdateStatus(string status)
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Text = $"LangKeep — {status}";
        }
    }

    // ───────────────────── Private Helpers ─────────────────────

    private void BuildContextMenu()
    {
        _contextMenu = new System.Windows.Forms.ContextMenuStrip();

        var openSettingsItem = new System.Windows.Forms.ToolStripMenuItem("Open Settings");
        openSettingsItem.Click += (_, _) => ShowSettings();
        _contextMenu.Items.Add(openSettingsItem);

        var toggleAutoItem = new System.Windows.Forms.ToolStripMenuItem("Enable Auto Switching")
        {
            Checked = _trackingService.IsEnabled,
        };
        toggleAutoItem.Click += (_, _) =>
        {
            _trackingService.IsEnabled = !_trackingService.IsEnabled;
            toggleAutoItem.Checked = _trackingService.IsEnabled;
            UpdateStatus(_trackingService.IsEnabled ? "Enabled" : "Disabled");
        };
        _contextMenu.Items.Add(toggleAutoItem);

        var toggleStartupItem = new System.Windows.Forms.ToolStripMenuItem("Start with Windows")
        {
            Checked = _startupManager.IsRegistered,
        };
        toggleStartupItem.Click += (_, _) =>
        {
            if (_startupManager.IsRegistered)
            {
                _startupManager.Unregister();
            }
            else
            {
                _startupManager.Register();
            }
            toggleStartupItem.Checked = _startupManager.IsRegistered;
        };
        _contextMenu.Items.Add(toggleStartupItem);

        var openLogsItem = new System.Windows.Forms.ToolStripMenuItem("Open Logs");
        openLogsItem.Click += (_, _) =>
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LangKeep",
                "logs");
            if (Directory.Exists(logPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", logPath);
            }
            else
            {
                _logger.LogWarning("Logs directory not found: {Path}", logPath);
            }
        };
        _contextMenu.Items.Add(openLogsItem);

        _contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            _notifyIcon!.Visible = false;
            System.Windows.Application.Current.Shutdown();
        };
        _contextMenu.Items.Add(exitItem);
    }

    private void OnTrayIconClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == System.Windows.Forms.MouseButtons.Left)
        {
            ShowSettings();
        }
    }

    private static System.Drawing.Icon GetAppIcon()
    {
        var assembly = typeof(TrayIconService).Assembly;
        using var stream = assembly.GetManifestResourceStream("LangKeep.UI.Wpf.Resources.LangKeep.ico");
        if (stream is not null)
        {
            return new System.Drawing.Icon(stream);
        }

        // Fallback: create a simple 16x16 icon if the resource is missing
        using var bitmap = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(System.Drawing.Color.Transparent);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 212));
        g.FillEllipse(brush, 0, 0, 15, 15);
        using var font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
        using var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
        g.DrawString("L", font, textBrush, 2, 1);
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }
}
