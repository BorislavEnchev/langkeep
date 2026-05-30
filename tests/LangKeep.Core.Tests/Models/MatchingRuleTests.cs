using FluentAssertions;
using LangKeep.Core.Models;
using Xunit;

namespace LangKeep.Core.Tests.Models;

public sealed class MatchingRuleTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        var rule = new MatchingRule("Code.exe", "en-US");
        rule.ProcessName.Should().Be("Code.exe");
        rule.LanguageTag.Should().Be("en-US");
        rule.IsEnabled.Should().BeTrue();
        rule.WindowTitleContains.Should().BeNull();
        rule.DisplayName.Should().BeNull();
        rule.Priority.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullProcessName_ThrowsArgumentNullException()
    {
        Action act = () => _ = new MatchingRule(null!, "en-US");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLanguageTag_ThrowsArgumentNullException()
    {
        Action act = () => _ = new MatchingRule("Code.exe", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsEnabled_CanBeModified()
    {
        var rule = new MatchingRule("Code.exe", "en-US");
        rule.IsEnabled = false;
        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void WindowTitleContains_CanBeSet()
    {
        var rule = new MatchingRule("Teams.exe", "de-DE")
        {
            WindowTitleContains = "Hans" // Future per-window matching
        };
        rule.WindowTitleContains.Should().Be("Hans");
    }

    [Fact]
    public void Priority_CanBeSet()
    {
        var rule = new MatchingRule("Code.exe", "en-US") { Priority = 10 };
        rule.Priority.Should().Be(10);
    }

    [Fact]
    public void DisplayName_CanBeSet()
    {
        var rule = new MatchingRule("Code.exe", "en-US")
        {
            DisplayName = "VS Code - English"
        };
        rule.DisplayName.Should().Be("VS Code - English");
    }
}
