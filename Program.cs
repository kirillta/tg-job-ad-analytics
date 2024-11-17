using System.Text;
using System.Text.Json;
using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services;
using TgJobAdAnalytics.Services.Salaries;

Console.OutputEncoding = Encoding.UTF8;

var sourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources");

var rateSourceManager = new RateSourceManager(Path.Combine(sourcePath, "rates.csv"));
var rateApiClient = new RateApiClient();
var rateService = new RateService(rateSourceManager, rateApiClient);

var salaryService = new SalaryService(Currency.RUB, rateService);
var messageProcessor = new MessageProcessor(salaryService);

List<Message> adMessages = [];

var fileNames = Directory.GetFiles(sourcePath);
foreach (string fileName in fileNames)
{
    if (!fileName.EndsWith(".json"))
        continue;

    Console.WriteLine(fileName);
    using var json = new FileStream(fileName, FileMode.Open, FileAccess.Read);
    var buffer = new byte[json.Length];
    await json.ReadExactlyAsync(buffer.AsMemory(0, (int)json.Length));
    var chat = JsonSerializer.Deserialize<TgChat>(buffer);

    Console.WriteLine(chat.Name);

    var chatMessages = messageProcessor.Get(chat.Messages);
    adMessages.AddRange(chatMessages);
}

var orderedAdMessages = adMessages.OrderByDescending(message => message.Date).ToList();
var messages = SimilarityCalculator.Distinct(orderedAdMessages);

List<ReportGroup> reports = [];

reports.Add(AdStatsCalculator.CalculateAll(messages));
reports.Add(SalaryCalculator.CalculateAll(messages));

ConsoleReportPrinter.Print(reports);


Console.ReadKey();