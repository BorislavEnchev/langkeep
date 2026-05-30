using FluentAssertions;
using LangKeep.Application.Services;
using LangKeep.Core.Models;
using Xunit;

namespace LangKeep.Application.Tests.Services;

public sealed class RuleEvaluationServiceTests
{
    [Fact]
    public void Evaluate_WithNoRules_ReturnsNoMatch()
    {
        var service = new RuleEvaluationService();
        var app = new ApplicationIdentity("Code.exe");

        var result = service.Evaluate(app);

        result.IsMatched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithMatchingRule_ReturnsMatch()
    {
        var service = new RuleEvaluationService(new[]
        {
            new MatchingRule("Code.exe", "en-US"),
        });

        var app = new ApplicationIdentity("Code.exe");
        var result = service.Evaluate(app);

        result.IsMatched.Should().BeTrue();
        result.TargetLayout!.LanguageTag.Should().Be("en-US");
    }

    [Fact]
    public void Evaluate_WithDisabledRule_ReturnsNoMatch()
    {
        var service = new RuleEvaluationService(new[]
        {
            new MatchingRule("Code.exe", "en-US", isEnabled: false),
        });

        var app = new ApplicationIdentity("Code.exe");
        var result = service.Evaluate(app);

        result.IsMatched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithNonMatchingRule_ReturnsNoMatch()
    {
        var service = new RuleEvaluationService(new[]
        {
            new MatchingRule("Teams.exe", "de-DE"),
        });

        var app = new ApplicationIdentity("Code.exe");
        var result = service.Evaluate(app);

        result.IsMatched.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_CaseInsensitive_ReturnsMatch()
    {
        var service = new RuleEvaluationService(new[]
        {
            new MatchingRule("code.exe", "en-US"),
        });

        var app = new ApplicationIdentity("CODE.EXE");
        var result = service.Evaluate(app);

        result.IsMatched.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WithMultipleRules_ReturnsCorrectMatch()
    {
        var service = new RuleEvaluationService(new[]
        {
            new MatchingRule("Teams.exe", "de-DE"),
            new MatchingRule("Code.exe", "en-US"),
            new MatchingRule("chrome.exe", "bg-BG"),
        });

        var app = new ApplicationIdentity("Teams.exe");
        var result = service.Evaluate(app);

        result.IsMatched.Should().BeTrue();
        result.TargetLayout!.LanguageTag.Should().Be("de-DE");
    }

    [Fact]
    public void Evaluate_WithHigherPriority_RuleWins()
    {
        var service = new RuleEvaluationService(new[]
        {
            new MatchingRule("Code.exe", "de-DE") { Priority = 5 },
            new MatchingRule("Code.exe", "en-US") { Priority = 10 },
        });

        var app = new ApplicationIdentity("Code.exe");
        var result = service.Evaluate(app);

        result.IsMatched.Should().BeTrue();
        result.TargetLayout!.LanguageTag.Should().Be("en-US"); // Higher priority
    }

    [Fact]
    public void AddOrUpdate_NewRule_AddsToCollection()
    {
        var service = new RuleEvaluationService();
        service.AddOrUpdate(new MatchingRule("Code.exe", "en-US"));

        service.Rules.Should().HaveCount(1);
        service.Rules[0].ProcessName.Should().Be("Code.exe");
    }

    [Fact]
    public void AddOrUpdate_ExistingRule_Updates()
    {
        var service = new RuleEvaluationService(new[]
        {
            new MatchingRule("Code.exe", "en-US"),
        });

        service.AddOrUpdate(new MatchingRule("Code.exe", "de-DE"));

        service.Rules.Should().HaveCount(1);
        service.Rules[0].LanguageTag.Should().Be("de-DE");
    }

    [Fact]
    public void Remove_ExistingRule_RemovesAndReturnsTrue()
    {
        var service = new RuleEvaluationService(new[]
        {
            new MatchingRule("Code.exe", "en-US"),
        });

        var removed = service.Remove("Code.exe");

        removed.Should().BeTrue();
        service.Rules.Should().BeEmpty();
    }

    [Fact]
    public void Remove_NonExistingRule_ReturnsFalse()
    {
        var service = new RuleEvaluationService();

        var removed = service.Remove("Code.exe");

        removed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithNullApplication_ThrowsArgumentNullException()
    {
        var service = new RuleEvaluationService();

        Action act = () => service.Evaluate(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
