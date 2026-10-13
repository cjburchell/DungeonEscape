namespace DungeonEscape.AgentOrchestrator;

internal static class QaResultValidator
{
    private const string PassedStatus = "passed";

    public static bool IsValid(QaToManager? value)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.WorkflowId) || value.Attempt <= 0
            || !Enum.IsDefined(value.Outcome) || value.TestSuitesRun is null
            || value.TestResults is null || value.ValidationFindings is null
            || value.SkippedChecks is null || value.ResidualRisks is null || value.ToolUses is null
            || value.TestResults.Any(item => string.IsNullOrWhiteSpace(item.Suite)
                || string.IsNullOrWhiteSpace(item.Status) || item.FailedTests is null
                || string.IsNullOrWhiteSpace(item.Summary)))
            return false;

        return value.Outcome != QaOutcome.Pass || HasPassingEvidence(value);
    }

    private static bool HasPassingEvidence(QaToManager value)
    {
        if (value.TestSuitesRun.Count == 0 || value.TestResults.Count == 0
            || !value.ToolUses.Any(item => item.Name == "run_tests" && item.Succeeded))
            return false;

        var suites = value.TestSuitesRun.ToHashSet(StringComparer.Ordinal);
        if (suites.Count != value.TestSuitesRun.Count || suites.Count != value.TestResults.Count)
            return false;

        var resultSuites = value.TestResults.Select(item => item.Suite).ToHashSet(StringComparer.Ordinal);
        return resultSuites.Count == value.TestResults.Count
            && suites.SetEquals(resultSuites)
            && value.TestResults.All(item => item.Status == PassedStatus
                && item.ExitCode == 0 && item.FailedTests.Count == 0);
    }
}
