using FluentAssertions;
using LangKeep.Core.Models;
using Xunit;

namespace LangKeep.Core.Tests.Models;

public sealed class ApplicationIdentityTests
{
    [Fact]
    public void Constructor_WithValidProcessName_SetsProperties()
    {
        var identity = new ApplicationIdentity("Code.exe");
        identity.ProcessName.Should().Be("Code.exe");
        identity.ProcessPath.Should().BeNull();
        identity.WindowTitle.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        var identity = new ApplicationIdentity(
            "Teams.exe",
            processPath: @"C:\Users\test\AppData\Local\Teams.exe",
            windowTitle: "Microsoft Teams");

        identity.ProcessName.Should().Be("Teams.exe");
        identity.ProcessPath.Should().Be(@"C:\Users\test\AppData\Local\Teams.exe");
        identity.WindowTitle.Should().Be("Microsoft Teams");
    }

    [Fact]
    public void Constructor_WithNullProcessName_ThrowsArgumentNullException()
    {
        Action act = () => _ = new ApplicationIdentity(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equals_SameProcessName_ReturnsTrue()
    {
        var a = new ApplicationIdentity("Code.exe");
        var b = new ApplicationIdentity("Code.exe");

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue(); // Uses Equals
    }

    [Fact]
    public void Equals_DifferentProcessName_ReturnsFalse()
    {
        var a = new ApplicationIdentity("Code.exe");
        var b = new ApplicationIdentity("Teams.exe");

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_CaseInsensitive_ReturnsTrue()
    {
        var a = new ApplicationIdentity("Code.exe");
        var b = new ApplicationIdentity("code.EXE");

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameProcessName_ReturnsSameValue()
    {
        var a = new ApplicationIdentity("Code.exe");
        var b = new ApplicationIdentity("code.exe");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentProcessName_ReturnsDifferentValue()
    {
        var a = new ApplicationIdentity("Code.exe");
        var b = new ApplicationIdentity("Teams.exe");

        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsProcessName()
    {
        var identity = new ApplicationIdentity("Code.exe");
        identity.ToString().Should().Be("Code.exe");
    }
}
