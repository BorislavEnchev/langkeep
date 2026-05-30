namespace LangKeep.Core.Models;

/// <summary>
/// The result of evaluating matching rules against an <see cref="ApplicationIdentity"/>.
/// </summary>
public sealed class RuleMatchResult
{
    private RuleMatchResult() { }

    /// <summary>
    /// Gets a value indicating whether a matching rule was found.
    /// </summary>
    public bool IsMatched { get; private init; }

    /// <summary>
    /// Gets the matched rule, if any.
    /// </summary>
    public MatchingRule? MatchedRule { get; private init; }

    /// <summary>
    /// Gets the target layout from the matched rule, if any.
    /// </summary>
    public KeyboardLayout? TargetLayout { get; private init; }

    /// <summary>
    /// Creates a successful match result.
    /// </summary>
    public static RuleMatchResult Matched(MatchingRule rule, KeyboardLayout layout) =>
        new()
        {
            IsMatched = true,
            MatchedRule = rule,
            TargetLayout = layout,
        };

    /// <summary>
    /// Creates a "no match" result.
    /// </summary>
    public static RuleMatchResult NoMatch() => new() { IsMatched = false };
}
