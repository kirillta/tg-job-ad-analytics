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

var minHashCalculator = new MinHashCalculator(128);
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
    var chat = await JsonSerializer.DeserializeAsync<TgChat>(json);
    Console.WriteLine(chat.Name);

    var chatMessages = messageProcessor.Get(chat.Messages);
    adMessages.AddRange(chatMessages);
}

var termFrequencies = adMessages.Select(message => message.TermFrequency)
    .ToList();
var inverseDocumentFrequency = SimilarityCalculator.GetInverseDocumentFrequency(termFrequencies);
foreach (var message in adMessages)
{
    var tfIdf = SimilarityCalculator.GetTfIdf(message.TermFrequency, inverseDocumentFrequency);
    foreach (var kvp in tfIdf)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }
}

List<ReportGroup> reports = [];

reports.Add(AdStatsCalculator.CalculateAll(adMessages));
reports.Add(SalaryCalculator.CalculateAll(adMessages));

ConsoleReportPrinter.Print(reports);


Console.ReadKey();