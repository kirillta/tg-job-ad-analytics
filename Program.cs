using System.Text;
using System.Text.Json;
using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services;
using TgJobAdAnalytics.Services.Salaries;

Console.OutputEncoding = Encoding.UTF8;

var salaryNormalizer = new SalaryNormalizer(Currency.USD);
var salaryService = new SalaryService(salaryNormalizer);
var messageProcessor = new MessageProcessor(salaryService);

List<Message> adMessages = [];

var sourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources");
var fileNames = Directory.GetFiles(sourcePath);
foreach (string fileName in fileNames)
{
    Console.WriteLine(fileName);
    using var json = new FileStream(fileName, FileMode.Open, FileAccess.Read);
    var chat = await JsonSerializer.DeserializeAsync<TgChat>(json);
    Console.WriteLine(chat.Name);

    var chatMessages = messageProcessor.Get(chat.Messages);
    adMessages.AddRange(chatMessages);
}

List<ReportGroup> reports = [];

//var adStatGroup = new ReportGroup("Ad Statistics");
//adStatGroup.Reports.Add(AdStatsCalculator.GetMaximumNumberOfAdsByMonthAndYear(adMessages));
//adStatGroup.Reports.Add(AdStatsCalculator.GetNumberOfAdsByMonth(adMessages));
//adStatGroup.Reports.Add(AdStatsCalculator.GetNumberOfAdsByYear(adMessages));
//adStatGroup.Reports.Add(AdStatsCalculator.PrintNumberOfAdsByYearAndMonth(adMessages));
//reports.Add(adStatGroup);

reports.Add(SalaryCalculator.CalculateAll(adMessages));

ConsoleReportPrinter.Print(reports);


Console.ReadKey();