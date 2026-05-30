using FluentAssertions;
using LangKeep.Core.Models;
using Xunit;

namespace LangKeep.Core.Tests.Models;

public sealed class KeyboardLayoutTests
{
    [Fact]
    public void Constructor_WithValidLanguageTag_SetsProperties()
    {
        var layout = new KeyboardLayout("en-US");
        layout.LanguageTag.Should().Be("en-US");
        layout.DisplayName.Should().Be("en-US"); // Falls back to tag
    }

    [Fact]
    public void Constructor_WithDisplayName_SetsDisplayName()
    {
        var layout = new KeyboardLayout("de-DE", "German (Germany)");
        layout.LanguageTag.Should().Be("de-DE");
        layout.DisplayName.Should().Be("German (Germany)");
    }

    [Fact]
    public void Constructor_WithEmptyLanguageTag_ThrowsArgumentException()
    {
        Action act = () => _ = new KeyboardLayout(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespaceLanguageTag_ThrowsArgumentException()
    {
        Action act = () => _ = new KeyboardLayout("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromLcid_WithValidLcid_ReturnsLayout()
    {
        // 0x0409 = en-US
        var layout = KeyboardLayout.FromLcid(0x0409);
        layout.LanguageTag.Should().Be("en-US");
        layout.DisplayName.Should().Contain("English");
    }

    [Fact]
    public void FromLcid_WithInvalidLcid_ReturnsFallbackLayout()
    {
        // 0xFFFF is not a valid LCID
        var layout = KeyboardLayout.FromLcid(0xFFFF);
        layout.LanguageTag.Should().StartWith("unknown-0x");
    }

    [Fact]
    public void Equals_SameLanguageTag_ReturnsTrue()
    {
        var a = new KeyboardLayout("en-US");
        var b = new KeyboardLayout("en-US");

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentLanguageTag_ReturnsFalse()
    {
        var a = new KeyboardLayout("en-US");
        var b = new KeyboardLayout("de-DE");

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_CaseInsensitive_ReturnsTrue()
    {
        var a = new KeyboardLayout("en-US");
        var b = new KeyboardLayout("en-us");

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsDisplayNameWithTag()
    {
        var layout = new KeyboardLayout("bg-BG", "Bulgarian (Bulgaria)");
        layout.ToString().Should().Be("Bulgarian (Bulgaria) [bg-BG]");
    }
}