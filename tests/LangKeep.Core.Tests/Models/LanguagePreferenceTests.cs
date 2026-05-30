using FluentAssertions;
using LangKeep.Core.Models;
using Xunit;

namespace LangKeep.Core.Tests.Models;

public sealed class LanguagePreferenceTests
{
    private readonly ApplicationIdentity _app = new("Code.exe");
    private readonly KeyboardLayout _layout = new("en-US");

    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        var pref = new LanguagePreference(_app, _layout);
        pref.Application.Should().Be(_app);
        pref.Layout.Should().Be(_layout);
        pref.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithIsEnabledFalse_SetsIsEnabled()
    {
        var pref = new LanguagePreference(_app, _layout, isEnabled: false);
        pref.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullApplication_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LanguagePreference(null!, _layout);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLayout_ThrowsArgumentNullException()
    {
        Action act = () => _ = new LanguagePreference(_app, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsEnabled_CanBeModified()
    {
        var pref = new LanguagePreference(_app, _layout);
        pref.IsEnabled = false;
        pref.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SortOrder_CanBeSet()
    {
        var pref = new LanguagePreference(_app, _layout) { SortOrder = 42 };
        pref.SortOrder.Should().Be(42);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var pref = new LanguagePreference(_app, _layout);
        pref.ToString().Should().Be("Code.exe → en-US (enabled)");
    }

    [Fact]
    public void ToString_WhenDisabled_ShowsDisabled()
    {
        var pref = new LanguagePreference(_app, _layout, isEnabled: false);
        pref.ToString().Should().Be("Code.exe → en-US (disabled)");
    }
}
