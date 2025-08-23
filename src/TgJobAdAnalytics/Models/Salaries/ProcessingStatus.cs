namespace TgJobAdAnalytics.Models.Salaries;

public enum ProcessingStatus
{
    NotStarted = 0,
    Extracted = 1,
    Enriched = 2,
    Failed = 3,
    Skipped = 4
}
