using System.Collections.Concurrent;
using System.Text.Json;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services.Analytics;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Reports;
using TgJobAdAnalytics.Services.Salaries;

var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 };
var sourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources");
var outputPath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Output");

var chats = await GetChats(sourcePath);
var messages = GetMessages(chats, parallelOptions);
messages = await TryAddSalaries(sourcePath, messages, parallelOptions);

List<ReportGroup> reports = [];

reports.Add(AdStatsCalculator.CalculateAll(messages));
reports.Add(SalaryCalculator.CalculateAll(messages));

var printer = new HtmlReportPrinter(outputPath, chats);
printer.Print(reports);
//ConsoleReportPrinter.Print(reports);

Console.WriteLine("Complete");
//Console.ReadKey();


static async Task<List<TgChat>> GetChats(string sourcePath)
{
    var chats = new List<TgChat>();
    var fileNames = Directory.GetFiles(sourcePath);
    foreach (string fileName in fileNames)
    {
        if (!fileName.EndsWith(".json"))
            continue;
        using var json = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        var buffer = new byte[json.Length];
        await json.ReadExactlyAsync(buffer.AsMemory(0, (int)json.Length));
        var chat = JsonSerializer.Deserialize<TgChat>(buffer);
        
        chats.Add(chat);
    }

    return chats;
}


static List<Message> GetMessages(List<TgChat> chats, ParallelOptions parallelOptions)
{
    List<Message> adMessages = [];

    var messageProcessor = new MessageProcessor(parallelOptions);
    foreach (var chat in chats)
    {
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
    var rateServiceFactory = new RateServiceFactory(sourcePath);

    var initialDate = messages.Min(message => message.Date);
    var rateService = await rateServiceFactory.Get(Currency.RUB, initialDate);

    var salaryService = new SalaryService(Currency.RUB, rateService);

    var results = new ConcurrentBag<Message>();
    Parallel.ForEach(messages, parallelOptions, message =>
    {
        var salary = salaryService.Get(message.Text, message.Date);
        message = message with { Salary = salary };

        results.Add(message);
    });

    return [.. results];
}