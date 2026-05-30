using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;

namespace LangKeep.Application.Services;

/// <summary>
/// Evaluates <see cref="LangKeep.Core.Models.MatchingRule"/> instances against
/// an <see cref="ApplicationIdentity"/> to determine which keyboard layout
/// should be activated.
/// </summary>
public sealed class RuleEvaluationService : IRuleMatcher
{
    private readonly List<MatchingRule> _rules;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleEvaluationService"/> class.
    /// </summary>
    /// <param name="rules">The collection of matching rules to evaluate.</param>
    public RuleEvaluationService(IEnumerable<MatchingRule>? rules = null)
    {
        _rules = rules?.ToList() ?? [];
    }

    /// <summary>
    /// Gets a read-only view of the current rules.
    /// </summary>
    public IReadOnlyList<MatchingRule> Rules => _rules.AsReadOnly();

    /// <summary>
    /// Adds or updates a rule.
    /// </summary>
    /// <param name="rule">The rule to add or update.</param>
    public void AddOrUpdate(MatchingRule rule)
    {
        var existing = _rules.FindIndex(r =>
            string.Equals(r.ProcessName, rule.ProcessName, StringComparison.OrdinalIgnoreCase));

        if (existing >= 0)
        {
            _rules[existing] = rule;
        }
        else
        {
            _rules.Add(rule);
        }
    }

    /// <summary>
    /// Removes a rule by process name.
    /// </summary>
    /// <param name="processName">The process name to remove.</param>
    /// <returns><c>true</c> if a rule was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(string processName)
    {
        var count = _rules.RemoveAll(r =>
            string.Equals(r.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
        return count > 0;
    }

    /// <inheritdoc />
    public RuleMatchResult Evaluate(ApplicationIdentity application)
    {
        ArgumentNullException.ThrowIfNull(application);

        // Sorted by priority (higher = evaluated first).
        // MVP: only matches by process name. Future: match by title, URL, etc.
        var matched = _rules
            .Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.ProcessName)
            .FirstOrDefault(r =>
                string.Equals(r.ProcessName, application.ProcessName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(r.WindowTitleContains) ||
                 (application.WindowTitle?.Contains(r.WindowTitleContains, StringComparison.OrdinalIgnoreCase) ?? false)));

        if (matched is null)
            return RuleMatchResult.NoMatch();

        var targetLayout = new KeyboardLayout(matched.LanguageTag);
        return RuleMatchResult.Matched(matched, targetLayout);
    }
}
