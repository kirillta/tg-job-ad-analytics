using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Reports.Html;
using TgJobAdAnalytics.Services.Salaries;

System.Console.OutputEncoding = Encoding.UTF8;

var host = Host.CreateDefaultBuilder(args)    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<ApplicationDbContext>();
        //services.AddTransient<MessageProcessor>();
        //services.AddTransient<SimilarityCalculator>();
        //services.AddTransient<RateServiceFactory>();
        //services.AddTransient<SalaryService>();
        //services.AddTransient<HtmlReportPrinter>();
        services.AddTransient<UploadService>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

var startTime = Stopwatch.GetTimestamp();

var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
var sourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources");
var outputPath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Output");
const string relativeTemplatePath = "Views/Reports";

using var dbContext = services.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.MigrateAsync();

var uploadService = services.GetRequiredService<UploadService>();
await uploadService.UpdateFromJson(sourcePath);
//var messages = GetMessages(chats, parallelOptions);

//messages = await TryAddSalaries(sourcePath, messages, parallelOptions, dbContext);

//List<ReportGroup> reports = [];

//reports.Add(AdStatsCalculator.CalculateAll(messages));
//reports.Add(SalaryCalculator.CalculateAll(messages));

//var printer = new HtmlReportPrinter(outputPath, relativeTemplatePath, chats);
//printer.Print(reports);
//ConsoleReportPrinter.Print(reports);

Console.WriteLine($"Completed in {Stopwatch.GetElapsedTime(startTime).TotalSeconds} seconds");
//Console.ReadKey();


//static List<Message> GetMessages(List<TgChat> chats, ParallelOptions parallelOptions)
//{
//    List<Message> adMessages = [];

//    var messageProcessor = new MessageProcessor(parallelOptions);
//    foreach (var chat in chats)
//    {
//        var chatMessages = messageProcessor.Get(chat);
//        adMessages.AddRange(chatMessages);
//    }

//    var orderedAdMessages = adMessages.OrderByDescending(message => message.Date)
//        .ToList();

//    var similarityCalculator = new SimilarityCalculator(parallelOptions);
//    return similarityCalculator.Distinct(orderedAdMessages);
//}


//static async Task<List<Message>> TryAddSalaries(string sourcePath, List<Message> messages, ParallelOptions parallelOptions, ApplicationDbContext dbContext)
//{
//    var rateServiceFactory = new RateServiceFactory(sourcePath);

//    var initialDate = messages.Min(message => message.Date);
//    var rateService = await rateServiceFactory.Get(Currency.RUB, initialDate);

//    var salaryService = new SalaryService(Currency.RUB, rateService);

//    var results = new ConcurrentBag<Message>();
//    Parallel.ForEach(messages, parallelOptions, message =>
//    {
//        var salary = salaryService.Get(message.Text, message.Date);
//        var updatedMessage = message with { Salary = salary };
//        results.Add(updatedMessage);

//        // Optionally update the database
//        var messageEntity = dbContext.TgMessages.Find(message.Id);
//        if (messageEntity != null)
//        {
//            messageEntity.Salary = new SalaryEntity
//            {
//                LowerBound = salary.LowerBound,
//                LowerBoundNormalized = salary.LowerBoundNormalized,
//                UpperBound = salary.UpperBound,
//                UpperBoundNormalized = salary.UpperBoundNormalized,
//                Currency = salary.Currency,
//                MessageId = message.Id
//            };
//        }
//    });

//    await dbContext.SaveChangesAsync();

//    return [.. results];
//}
