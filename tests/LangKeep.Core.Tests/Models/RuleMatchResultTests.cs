using FluentAssertions;
using LangKeep.Core.Models;
using Xunit;

namespace LangKeep.Core.Tests.Models;

public sealed class RuleMatchResultTests
{
    private readonly MatchingRule _rule = new("Code.exe", "en-US");
    private readonly KeyboardLayout _layout = new("en-US");

    [Fact]
    public void Matched_CreatesSuccessfulResult()
    {
        var result = RuleMatchResult.Matched(_rule, _layout);

        result.IsMatched.Should().BeTrue();
        result.MatchedRule.Should().Be(_rule);
        result.TargetLayout.Should().Be(_layout);
    }

    [Fact]
    public void NoMatch_CreatesNegativeResult()
    {
        var result = RuleMatchResult.NoMatch();

        result.IsMatched.Should().BeFalse();
        result.MatchedRule.Should().BeNull();
        result.TargetLayout.Should().BeNull();
    }

    [Fact]
    public void Matched_WithDifferentRule_HasCorrectRule()
    {
        var otherRule = new MatchingRule("Teams.exe", "de-DE");
        var otherLayout = new KeyboardLayout("de-DE");

        var result = RuleMatchResult.Matched(otherRule, otherLayout);

        result.IsMatched.Should().BeTrue();
        result.MatchedRule.Should().Be(otherRule);
        result.TargetLayout.Should().Be(otherLayout);
    }
}
