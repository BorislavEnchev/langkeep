using LangKeep.Core.Models;

namespace LangKeep.Core.Interfaces;

/// <summary>
/// Evaluates matching rules against an <see cref="ApplicationIdentity"/>
/// to determine which keyboard layout should be active.
/// </summary>
public interface IRuleMatcher
{
    /// <summary>
    /// Evaluates all rules and returns the best match for the given application.
    /// </summary>
    /// <param name="application">The application identity to match against.</param>
    /// <returns>A <see cref="RuleMatchResult"/> indicating whether a match was found.</returns>
    RuleMatchResult Evaluate(ApplicationIdentity application);
}
