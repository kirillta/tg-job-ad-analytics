using MathNet.Numerics.Statistics;
using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Reports;


namespace TgJobAdAnalytics.Services.Salaries;

internal class SalaryCalculator
{
    public static ReportGroup CalculateAll(List<Message> messages)
    {
        var lowerBoundSalaries = messages.Select(message => message.Salary.LowerBound).ToArray();

        var reports = new List<Report>
        {
            GetMinimalByYear(messages),
            GetMaximumByYear(messages),
            GetMeanByYear(messages),
            GetMedianByYear(messages)
        };

        return new ReportGroup("Salary Statistics", reports);
    }


    private static Report GetMinimalByYear(List<Message> messages)
        => messages
            .Where(message => 0 < message.Salary.LowerBound)
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MinimalSalary = group.Select(group => group.Salary.LowerBound).Minimum()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => FormatSalary(group.MinimalSalary))
            .ToReport("Minimal Salary by Year");


    private static Report GetMaximumByYear(List<Message> messages)
        => messages
            .Where(message => 0 < message.Salary.UpperBound)
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MaximumSalary = group.Select(group => group.Salary.LowerBound).Maximum()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => FormatSalary(group.MaximumSalary))
            .ToReport("Maximum Salary by Year");


    private static Report GetMeanByYear(List<Message> messages)
        => messages
            .Where(message => 0 < message.Salary.LowerBound && 0 < message.Salary.UpperBound)
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MeanSalary = group.Select(GetSalaryValue)
                    .Where(salary => !double.IsNaN(salary))
                    .Mean()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => FormatSalary(group.MeanSalary))
            .ToReport("Average Salary by Year");


    private static Report GetMedianByYear(List<Message> messages)
        => messages
            .Where(message => 0 < message.Salary.LowerBound && 0 < message.Salary.UpperBound)
            .GroupBy(message => message.Date.Year)
            .Select(group => new
            {
                Year = group.Key,
                MedianSalary = group.Select(GetSalaryValue)
                    .Where(salary => !double.IsNaN(salary))
                    .Median()
            })
            .OrderBy(group => group.Year)
            .ToDictionary(group => group.Year.ToString(), group => FormatSalary(group.MedianSalary))
            .ToReport("Median Salary by Year");


    private static string FormatSalary(double salary) => salary.ToString("F2");


    private static double GetSalaryValue(Message message)
    {
        // No salary information
        if (double.IsNaN(message.Salary.LowerBound) && double.IsNaN(message.Salary.UpperBound))
            return double.NaN;

        if (double.IsNaN(message.Salary.LowerBound))
            return message.Salary.UpperBound;

        if (double.IsNaN(message.Salary.UpperBound))
            return message.Salary.LowerBound;

        return (message.Salary.LowerBound + message.Salary.UpperBound) / 2;
    }
}
