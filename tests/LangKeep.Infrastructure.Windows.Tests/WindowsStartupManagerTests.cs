using FluentAssertions;
using LangKeep.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NSubstitute;
using Xunit;

namespace LangKeep.Infrastructure.Windows.Tests;

/// <summary>
/// Tests for <see cref="WindowsStartupManager"/>.
/// Uses a unique registry value name per test class so tests never interfere
/// with the real "LangKeep" startup registration or with each other.
/// All test registry entries are cleaned up after each test via <see cref="Dispose"/>.
/// </summary>
public sealed class WindowsStartupManagerTests : IDisposable
{
    private static readonly string TestRegistryValueName = $"LangKeep_Test_{Guid.NewGuid():N}";
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string TestExecutablePath = @"C:\Test\LangKeep.exe";

    private readonly ILogger<WindowsStartupManager> _logger;
    private readonly WindowsStartupManager _sut;

    public WindowsStartupManagerTests()
    {
        _logger = Substitute.For<ILogger<WindowsStartupManager>>();
        _sut = new WindowsStartupManager(_logger, TestExecutablePath, TestRegistryValueName);
    }

    /// <summary>
    /// Clean up the test registry value after each test.
    /// </summary>
    public void Dispose()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
        if (key?.GetValue(TestRegistryValueName) is not null)
        {
            key.DeleteValue(TestRegistryValueName);
        }
    }

    // ───────────────────── Constructor ─────────────────────

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Action act = () => _ = new WindowsStartupManager(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNoExecutablePath_UsesProcessPath()
    {
        var manager = new WindowsStartupManager(_logger, registryValueName: TestRegistryValueName);
        manager.IsRegistered.Should().BeFalse();
    }

    // ───────────────────── IsRegistered ─────────────────────

    [Fact]
    public void IsRegistered_WhenNotRegistered_ReturnsFalse()
    {
        _sut.IsRegistered.Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_AfterRegister_ReturnsTrue()
    {
        _sut.Register();

        _sut.IsRegistered.Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_AfterUnregister_ReturnsFalse()
    {
        _sut.Register();
        _sut.Unregister();

        _sut.IsRegistered.Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_WhenRegistryValueDiffers_ReturnsFalse()
    {
        // Write a different path under the test value name
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
        key!.SetValue(TestRegistryValueName, @"C:\Different\Path.exe");

        _sut.IsRegistered.Should().BeFalse();
    }

    // ───────────────────── Register ─────────────────────

    [Fact]
    public void Register_ReturnsTrue()
    {
        var result = _sut.Register();

        result.Should().BeTrue();
    }

    [Fact]
    public void Register_WritesExpectedPathToRegistry()
    {
        _sut.Register();

        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        var value = key!.GetValue(TestRegistryValueName) as string;
        value.Should().Be(TestExecutablePath);
    }

    [Fact]
    public void Register_IsIdempotent()
    {
        _sut.Register();
        _sut.Register();

        // Should still be registered and have correct path
        _sut.IsRegistered.Should().BeTrue();
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        var value = key!.GetValue(TestRegistryValueName) as string;
        value.Should().Be(TestExecutablePath);
    }

    // ───────────────────── Unregister ─────────────────────

    [Fact]
    public void Unregister_ReturnsTrue()
    {
        _sut.Register();

        var result = _sut.Unregister();

        result.Should().BeTrue();
    }

    [Fact]
    public void Unregister_RemovesRegistryValue()
    {
        _sut.Register();
        _sut.Unregister();

        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        var value = key?.GetValue(TestRegistryValueName);
        value.Should().BeNull();
    }

    [Fact]
    public void Unregister_WhenNotRegistered_StillReturnsTrue()
    {
        var result = _sut.Unregister();

        result.Should().BeTrue();
    }

    // ───────────────────── Full Cycle ─────────────────────

    [Fact]
    public void FullRegisterUnregisterCycle_WorksCorrectly()
    {
        // Initially not registered
        _sut.IsRegistered.Should().BeFalse();

        // Register
        _sut.Register().Should().BeTrue();
        _sut.IsRegistered.Should().BeTrue();

        // Unregister
        _sut.Unregister().Should().BeTrue();
        _sut.IsRegistered.Should().BeFalse();

        // Can re-register again
        _sut.Register().Should().BeTrue();
        _sut.IsRegistered.Should().BeTrue();
    }

    // ───────────────────── IStartupManager interface ─────────────────────

    [Fact]
    public void Implements_IStartupManager()
    {
        _sut.Should().BeAssignableTo<IStartupManager>();
    }
}
