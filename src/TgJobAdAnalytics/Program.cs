using System.Collections.Concurrent;
using System.Text.Json;
using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services;
using TgJobAdAnalytics.Services.Salaries;

var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 };
var sourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources");

List<Message> messages = await GetMessages(sourcePath, parallelOptions);
messages = await TryAddSalaries(sourcePath, messages, parallelOptions);

List<ReportGroup> reports = [];

reports.Add(AdStatsCalculator.CalculateAll(messages));
reports.Add(SalaryCalculator.CalculateAll(messages));

ConsoleReportPrinter.Print(reports);

Console.ReadKey();


static async Task<List<Message>> GetMessages(string sourcePath, ParallelOptions parallelOptions)
{
    List<Message> adMessages = [];

    var messageProcessor = new MessageProcessor(parallelOptions);
    var fileNames = Directory.GetFiles(sourcePath);
    foreach (string fileName in fileNames)
    {
        if (!fileName.EndsWith(".json"))
            continue;

        using var json = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        var buffer = new byte[json.Length];
        await json.ReadExactlyAsync(buffer.AsMemory(0, (int)json.Length));
        var chat = JsonSerializer.Deserialize<TgChat>(buffer);

        var chatMessages = messageProcessor.Get(chat);
        adMessages.AddRange(chatMessages);
    }

    var orderedAdMessages = adMessages.OrderByDescending(message => message.Date)
        .ToList();

    var similarityCalculator = new SimilarityCalculator(parallelOptions);
    return similarityCalculator.Distinct(orderedAdMessages);
}


static async Task<List<Message>> TryAddSalaries(string sourcePath, List<Message> messages, ParallelOptions parallelOptions)
{
    var rateSourceManager = new RateSourceManager(Path.Combine(sourcePath, "rates.csv"));
    var rateApiClient = new RateApiClient();
    var rateService = new RateService(rateSourceManager, rateApiClient);
    var salaryService = new SalaryService(Currency.RUB, rateService);

    var results = new ConcurrentBag<Message>();
    await Parallel.ForEachAsync(messages, parallelOptions, async (message, CancellationToken) =>
    {
        var salary = await salaryService.Get(message.Text, message.Date);
        message = message with { Salary = salary };

        results.Add(message);
    });

    return [.. results];
}