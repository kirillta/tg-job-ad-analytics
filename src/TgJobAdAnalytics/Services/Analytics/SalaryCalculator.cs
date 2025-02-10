using MathNet.Numerics.Statistics;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Reports;


namespace TgJobAdAnalytics.Services.Analytics;

internal class SalaryCalculator
{
    public static ReportGroup CalculateAll(List<Message> messages)
    {
        var filteredMessages = FilterOutliers(messages);

        var reports = new List<Report>
        {
            GetMinimalByYear(filteredMessages),
            GetMaximumByYear(filteredMessages),
            GetMeanByYear(filteredMessages),
            GetMedianByYear(filteredMessages)
        };

        return new ReportGroup("Статистика по зарплатам", reports);
    }


    private static Report GetMinimalByYear(List<Message> messages)
        => messages
            .Where(message => Math.Abs(message.Salary.LowerBoundNormalized) > Tolerance)
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MinimalSalary = group.Select(group => group.Salary.LowerBoundNormalized).Minimum()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MinimalSalary)
            .ToReport("Минимальная зарплата по годам");


    private static Report GetMaximumByYear(List<Message> messages)
        => messages
            .Where(message => Math.Abs(message.Salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MaximumSalary = group.Select(group => group.Salary.UpperBoundNormalized).Maximum()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MaximumSalary)
            .ToReport("Максимальная зарплата по годам");


    private static Report GetMeanByYear(List<Message> messages)
        => messages
            .Where(message => Math.Abs(message.Salary.LowerBoundNormalized) > Tolerance && Math.Abs(message.Salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MeanSalary = group.Select(GetSalaryValue)
                    .Where(salary => !double.IsNaN(salary))
                    .Mean()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MeanSalary)
            .ToReport("Средняя зарплата по годам");


    private static Report GetMedianByYear(List<Message> messages)
        => messages
            .Where(message => Math.Abs(message.Salary.LowerBoundNormalized) > Tolerance && Math.Abs(message.Salary.UpperBoundNormalized) > Tolerance)
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MedianSalary = group.Select(GetSalaryValue)
                    .Where(salary => !double.IsNaN(salary))
                    .Median()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => group.MedianSalary)
            .ToReport("Медианная зарплата по годам");


    private static double GetSalaryValue(Message message)
    {
        // No salary information
        if (double.IsNaN(message.Salary.LowerBoundNormalized) && double.IsNaN(message.Salary.UpperBoundNormalized))
            return double.NaN;

        if (double.IsNaN(message.Salary.LowerBoundNormalized))
            return message.Salary.UpperBoundNormalized;

        if (double.IsNaN(message.Salary.UpperBoundNormalized))
            return message.Salary.LowerBoundNormalized;

        return (message.Salary.LowerBoundNormalized + message.Salary.UpperBoundNormalized) / 2;
    }


    private static List<Message> FilterOutliers(List<Message> messages)
    {
        var excludedIds = GetExcludedIds(messages, message => message.Salary.LowerBoundNormalized)
            .Concat(GetExcludedIds(messages, message => message.Salary.UpperBoundNormalized))
            .ToHashSet();

        return messages
            .Where(message => !excludedIds.Contains(message.Id))
            .ToList();


        IEnumerable<long> GetExcludedIds(List<Message> messages, Func<Message, double> salarySelector)
        {
            var validLogValues = messages
                .Select(salarySelector)
                .Where(salary => !double.IsNaN(salary) && Math.Abs(salary) > Tolerance)
                .Select(salary => Math.Log(salary))
                .ToArray();

            var (lowerThreshold, upperThreshold) = GetThresholds(validLogValues);

            return messages.Where(message => IsOutlier(salarySelector(message), lowerThreshold, upperThreshold))
                .Select(message => message.Id);
        }


        static (double, double) GetThresholds(double[] validLogLowerBounds)
        {
            var q1 = validLogLowerBounds.Quantile(0.25);
            var q3 = validLogLowerBounds.Quantile(0.75);
            var iqr = q3 - q1;

            var lowerThreshold = q1 - 1.5 * iqr;
            var upperThreshold = q3 + 1.5 * iqr;

            return (lowerThreshold, upperThreshold);
        }


        static bool IsOutlier(double salary, double lowerThreshold, double upperThreshold)
        {
            if (double.IsNaN(salary))
                return false;

            var logSalary = Math.Log(salary);
            return logSalary <= lowerThreshold || upperThreshold <= logSalary;
        }
    }


    private const double Tolerance = 1e-10;
}
