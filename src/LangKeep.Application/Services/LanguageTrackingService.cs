using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;
using Microsoft.Extensions.Logging;

namespace LangKeep.Application.Services;

/// <summary>
/// The central service that observes active-window and keyboard-layout changes,
/// triggers automatic layout restoration, and learns user preferences over time.
/// </summary>
public sealed class LanguageTrackingService : IDisposable
{
    private readonly IActiveWindowProvider _activeWindowProvider;
    private readonly IKeyboardLayoutProvider _layoutProvider;
    private readonly IKeyboardLayoutSwitcher _layoutSwitcher;
    private readonly IRuleMatcher _ruleMatcher;
    private readonly PreferenceManagementService _preferenceService;
    private readonly ILogger<LanguageTrackingService> _logger;

    private ApplicationIdentity? _lastApplication;
    private IntPtr _lastActiveHwnd = IntPtr.Zero;
    private bool _isEnabled = true;
    private bool _disposed;

    // ── Guards against false preference learning ──
    // Cooldown period after a window switch during which layout-change events
    // are ignored for learning. These changes are caused by the system restoring
    // per-window layouts or by LangKeep's own auto-switching, not the user.
    private DateTime _lastWindowSwitchTime = DateTime.MinValue;
    private static readonly TimeSpan LearningCooldown = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan ForegroundSettleDelay = TimeSpan.FromMilliseconds(50);

    // Set while LangKeep is actively switching a layout so the polling timer
    // doesn't re-learn the change as a "user" preference.
    private volatile bool _isSwitchingLayout;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageTrackingService"/> class.
    /// </summary>
    public LanguageTrackingService(
        IActiveWindowProvider activeWindowProvider,
        IKeyboardLayoutProvider layoutProvider,
        IKeyboardLayoutSwitcher layoutSwitcher,
        IRuleMatcher ruleMatcher,
        PreferenceManagementService preferenceService,
        ILogger<LanguageTrackingService> logger)
    {
        _activeWindowProvider = activeWindowProvider ?? throw new ArgumentNullException(nameof(activeWindowProvider));
        _layoutProvider = layoutProvider ?? throw new ArgumentNullException(nameof(layoutProvider));
        _layoutSwitcher = layoutSwitcher ?? throw new ArgumentNullException(nameof(layoutSwitcher));
        _ruleMatcher = ruleMatcher ?? throw new ArgumentNullException(nameof(ruleMatcher));
        _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to events
        _activeWindowProvider.ActiveWindowChanged += OnActiveWindowChanged;
        _layoutProvider.LayoutChanged += OnLayoutChanged;
    }

    /// <summary>
    /// Gets or sets whether automatic switching is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            _logger.LogInformation("Auto-switching {Status}.", value ? "enabled" : "disabled");
        }
    }

    /// <summary>
    /// Starts the tracking service by capturing the initial state
    /// and loading saved preferences into the rule matcher.
    /// </summary>
    public async Task StartAsync()
    {
        _logger.LogInformation("Language tracking service started.");

        // Load saved preferences from disk into the in-memory rule matcher
        await LoadPreferencesAsync().ConfigureAwait(false);

        // Capture the current foreground window state (runs on caller's context)
        var initial = _activeWindowProvider.GetActiveWindow();
        if (initial is not null)
        {
            _lastApplication = initial.Application;
            _logger.LogDebug(
                "Initial state: {ProcessName} / {Layout}",
                initial.Application.ProcessName,
                initial.CurrentLayout.LanguageTag);

            EvaluateAndSwitch(initial);
        }
    }

    /// <summary>
    /// Stops the tracking service and unsubscribes from events.
    /// </summary>
    public void Stop()
    {
        _logger.LogInformation("Language tracking service stopped.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _activeWindowProvider.ActiveWindowChanged -= OnActiveWindowChanged;
            _layoutProvider.LayoutChanged -= OnLayoutChanged;
            _disposed = true;
        }
    }

    // ───────────────────── Event Handlers ─────────────────────

    private void OnActiveWindowChanged(object? sender, ActiveWindowInfo info)
    {
        if (!_isEnabled)
            return;

        // Skip transient explorer.exe events (Alt+Tab shell, Start menu transitions).
        // These fire briefly before the real target window gets focus and cause
        // stale evaluations.
        if (string.Equals(info.Application.ProcessName, "explorer.exe",
                StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(info.WindowTitle))
        {
            _logger.LogDebug("Skipping transient explorer.exe foreground event (empty title).");
            return;
        }

        _logger.LogDebug(
            "Active window changed: {ProcessName} / {Title}",
            info.Application.ProcessName,
            info.WindowTitle ?? "(no title)");

        _lastApplication = info.Application;
        _lastActiveHwnd = info.WindowHandle;
        _lastWindowSwitchTime = DateTime.UtcNow;

        _logger.LogTrace(
            "Stored HWND 0x{Hwnd:X8} for {ProcessName}.",
            _lastActiveHwnd.ToInt64(), info.Application.ProcessName);

        // Offload the evaluation and switching to a ThreadPool thread.
        // This avoids blocking the WinEvent callback thread and gives the
        // system time to settle after the foreground transition.
        var snapshot = info;
        _ = Task.Run(() =>
        {
            // A short settle delay avoids racing focus transitions without making
            // every Alt+Tab feel delayed.
            Thread.Sleep(ForegroundSettleDelay);
            EvaluateAndSwitch(snapshot);
        });
    }

    private void OnLayoutChanged(object? sender, KeyboardLayout layout)
    {
        if (!_isEnabled)
            return;

        // Suppress learning when LangKeep itself is performing an auto-switch.
        // The polling timer would otherwise detect LangKeep's own layout change
        // and re-learn it as a "user" preference, creating a feedback loop.
        if (_isSwitchingLayout)
        {
            _logger.LogDebug(
                "Suppressing learning: layout change to {Layout} caused by auto-switch in progress.",
                layout.LanguageTag);
            return;
        }

        // Suppress learning during the cooldown after a window switch.
        // Layout changes detected in this window are caused by the system
        // restoring per-window layouts or by the focus transition itself,
        // not by the user manually switching.
        if (DateTime.UtcNow - _lastWindowSwitchTime < LearningCooldown)
        {
            _logger.LogDebug(
                "Suppressing learning: layout change to {Layout} within {CooldownMs}ms switch cooldown.",
                layout.LanguageTag, LearningCooldown.TotalMilliseconds);
            return;
        }

        // Re-read the actual foreground window to avoid the race where
        // _lastApplication was already updated by a WinEvent on another thread
        // but the layout change was detected for the *previous* window's thread.
        var currentWindow = _activeWindowProvider.GetActiveWindow();
        if (currentWindow is null)
            return;

        var application = currentWindow.Application;

        _logger.LogDebug(
            "Layout changed for {ProcessName}: {Layout}",
            application.ProcessName,
            layout.LanguageTag);

        // The user manually changed the layout → learn this as a preference.
        _ = LearnPreferenceAsync(application, layout);
    }

    // ───────────────────── Core Logic ─────────────────────

    private void EvaluateAndSwitch(ActiveWindowInfo info)
    {
        // Verify the foreground window hasn't changed since the WinEvent fired
        // by comparing the snapshot's HWND (info.WindowHandle) against the
        // current foreground window. Using the snapshot's HWND rather than the
        // global _lastActiveHwnd is essential because _lastActiveHwnd may have
        // already been overwritten by a subsequent WinEvent during our delay.
        var currentInfo = _activeWindowProvider.GetActiveWindow();
        if (currentInfo?.WindowHandle != info.WindowHandle)
        {
            _logger.LogDebug(
                "Foreground changed during delay: was 0x{SnapshotHwnd:X8}, now 0x{CurrentHwnd:X8}. " +
                "Skipping stale evaluation for {ProcessName}.",
                info.WindowHandle.ToInt64(), (currentInfo?.WindowHandle ?? IntPtr.Zero).ToInt64(),
                info.Application.ProcessName);
            return;
        }

        // Re-read the current keyboard layout at evaluation time.
        // The snapshot's CurrentLayout was captured when the WinEvent fired,
        // but the focus transition may not have been complete yet, or Windows
        // may have already remembered and switched the layout automatically.
        var actualLayout = _layoutProvider.GetCurrentLayout(info.Application) ?? info.CurrentLayout;

        _logger.LogDebug(
            "EvaluateAndSwitch: {ProcessName}, HWND: 0x{Hwnd:X8}, captured layout: {CapturedLayout}, actual layout: {ActualLayout}",
            info.Application.ProcessName,
            info.WindowHandle.ToInt64(),
            info.CurrentLayout.LanguageTag,
            actualLayout.LanguageTag);

        var result = _ruleMatcher.Evaluate(info.Application);

        if (!result.IsMatched || result.TargetLayout is null)
        {
            _logger.LogDebug(
                "No rule match for {ProcessName} — will learn preference on next layout change.",
                info.Application.ProcessName);
            return;
        }

        if (result.TargetLayout.Equals(actualLayout))
        {
            _logger.LogDebug(
                "Rule match but layout already correct for {ProcessName}: {Layout}",
                info.Application.ProcessName,
                result.TargetLayout.LanguageTag);
            return;
        }

        _logger.LogInformation(
            "Auto-switching {ProcessName} from {ActualLayout} → {TargetLayout} (captured was {CapturedLayout})",
            info.Application.ProcessName,
            actualLayout.LanguageTag,
            result.TargetLayout.LanguageTag,
            info.CurrentLayout.LanguageTag);

        // Pass the verified HWND to the switcher so it doesn't call
        // GetForegroundWindow() again (avoiding another race).
        // Set the _isSwitchingLayout flag so the polling timer doesn't
        // re-learn the layout change we're about to cause.
        _isSwitchingLayout = true;
        try
        {
            if (!_layoutSwitcher.TrySwitchLayout(
                    info.Application, result.TargetLayout, windowHandle: info.WindowHandle))
            {
                _logger.LogWarning(
                    "Failed to switch layout for {ProcessName} to {Layout}.",
                    info.Application.ProcessName,
                    result.TargetLayout.LanguageTag);
            }
        }
        finally
        {
            _isSwitchingLayout = false;
        }
    }

    /// <summary>
    /// Loads all saved preferences from the repository and seeds the in-memory rule matcher.
    /// Uses <c>ConfigureAwait(false)</c> to avoid capturing the WPF UI synchronization context
    /// and prevent deadlocks on startup.
    /// </summary>
    private async Task LoadPreferencesAsync()
    {
        try
        {
            var preferences = await _preferenceService.GetAllAsync().ConfigureAwait(false);
            if (_ruleMatcher is RuleEvaluationService ruleService)
            {
                foreach (var pref in preferences)
                {
                    ruleService.AddOrUpdate(new MatchingRule(
                        pref.Application.ProcessName,
                        pref.Layout.LanguageTag,
                        pref.IsEnabled));
                }
            }

            _logger.LogInformation(
                "Loaded {Count} saved preference(s) into the rule matcher.",
                preferences.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved preferences on startup.");
        }
    }

    private async Task LearnPreferenceAsync(ApplicationIdentity application, KeyboardLayout layout)
    {
        try
        {
            var existing = await _preferenceService.GetForApplicationAsync(application);
            if (existing is null || !existing.Layout.Equals(layout))
            {
                await _preferenceService.SetPreferenceAsync(application, layout);
                _logger.LogInformation(
                    "Learned preference: {ProcessName} → {Layout}",
                    application.ProcessName,
                    layout.LanguageTag);

                // Update the in-memory rule set
                if (_ruleMatcher is RuleEvaluationService ruleService)
                {
                    ruleService.AddOrUpdate(new MatchingRule(application.ProcessName, layout.LanguageTag));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to learn preference for {ProcessName}.", application.ProcessName);
        }
    }
}
