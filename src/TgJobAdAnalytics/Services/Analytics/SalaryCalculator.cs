using MathNet.Numerics.Statistics;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Reports;


namespace TgJobAdAnalytics.Services.Analytics;

internal class SalaryCalculator
{
    public static ReportGroup CalculateAll(List<Message> messages)
    {
        var filteredMessages = FilterOutliners(messages);

        var reports = new List<Report>
        {
            GetMinimalByYear(filteredMessages),
            GetMaximumByYear(filteredMessages),
            GetMeanByYear(filteredMessages),
            GetMedianByYear(filteredMessages)
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
            .ToDictionary(group => group.Year.ToString(), group => group.MinimalSalary)
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
            .ToDictionary(group => group.Year.ToString(), group => group.MaximumSalary)
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
            .ToDictionary(group => group.Year.ToString(), group => group.MeanSalary)
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
            .ToDictionary(group => group.Year.ToString(), group => group.MedianSalary)
            .ToReport("Median Salary by Year");


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


    private static List<Message> FilterOutliners(List<Message> messages)
    {
        var validLowerBounds = messages
            .Where(message => !double.IsNaN(message.Salary.LowerBound) && 0 < message.Salary.LowerBound)
            .Select(message => message.Salary.LowerBound)
            .ToArray();

        var lowerThreshold = validLowerBounds.Quantile(0.1);

        var validUpperBounds = messages
            .Where(message => !double.IsNaN(message.Salary.UpperBound) && 0 < message.Salary.UpperBound)
            .Select(message => message.Salary.UpperBound)
            .ToArray();

        var sortedUpperBounds = validUpperBounds.OrderBy(value => value).ToArray();

        var upperThreshold = validUpperBounds.Quantile(0.95);

        // TODO: exculde minimal outliners from upper bounds and visa versa
        return messages.Where(message => 
                (double.IsNaN(message.Salary.LowerBound) || message.Salary.LowerBound >= lowerThreshold) &&
                (double.IsNaN(message.Salary.UpperBound) || message.Salary.UpperBound <= upperThreshold)
            ).ToList();
    }
}
