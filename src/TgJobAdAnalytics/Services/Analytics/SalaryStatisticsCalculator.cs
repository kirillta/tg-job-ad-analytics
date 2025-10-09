using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Reports;

namespace TgJobAdAnalytics.Services.Analytics;

/// <summary>
/// Generates salary statistics reports (yearly min/max/avg/median) including per-level variants.
/// </summary>
public sealed class SalaryStatisticsCalculator
{
    /// <summary>
    /// Generates the salary statistics report group with level variants.
    /// </summary>
    public static ReportGroup GenerateAll(List<SalaryEntity> salaries)
    {
        var filtered = SalaryStatisticsCore.RemoveOutliers(salaries).ToList();
        var yearly = SalaryStatisticsCore.ComputeYearly(filtered, includePerLevel: true);

        var reports = new List<Report>
        {
            BuildReport("report.salary.min_years", yearly.MinimumByYear, yearly.ByLevel.ToDictionary(kv => kv.Key, kv => kv.Value.MinimumByYear)),
            BuildReport("report.salary.max_years", yearly.MaximumByYear, yearly.ByLevel.ToDictionary(kv => kv.Key, kv => kv.Value.MaximumByYear)),
            BuildReport("report.salary.avg_years", yearly.AverageByYear, yearly.ByLevel.ToDictionary(kv => kv.Key, kv => kv.Value.AverageByYear)),
            BuildReport("report.salary.median_years", yearly.MedianByYear, yearly.ByLevel.ToDictionary(kv => kv.Key, kv => kv.Value.MedianByYear))
        };

        return new ReportGroup("group.salary.stats", reports);
    }


    private static Report BuildReport(string title, Dictionary<string, double> baseResults, Dictionary<string, Dictionary<string, double>> levelData)
    {
        var variants = new Dictionary<string, Dictionary<string, double>>
        {
            ["All"] = baseResults
        };

        foreach (var (level, data) in levelData)
        {
            if (data.Count == 0)
                continue;

            variants[level] = data;
        }

        return new Report(
            title: title,
            results: baseResults,
            variants: variants
        );
    }
}
