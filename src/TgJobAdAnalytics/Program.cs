using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ClientModel;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Reports.Html;
using TgJobAdAnalytics.Services.Salaries;
using TgJobAdAnalytics.Services.Uploads;

System.Console.OutputEncoding = Encoding.UTF8;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole(options =>
        {
            options.FormatterName = "simple";
        });
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.IncludeScopes = false;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            options.UseUtcTimestamp = true;
        });
    })
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<ApplicationDbContext>();
        services.Configure<UploadOptions>(context.Configuration.GetSection("Upload"));

        services.Configure<ParallelOptions>(options =>
        {
            options.MaxDegreeOfParallelism = Environment.ProcessorCount;
        });

        services.Configure<VectorizationOptions>(context.Configuration.GetSection("Vectorization"));

        services.Configure<RateOptions>(options => 
        { 
            options.RateApiUrl = new Uri("https://www.cbr.ru/scripts/XML_dynamic.asp");
            options.RateSourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources", "rates.csv");
        });

        services.AddSingleton(_ => 
        { 
            var credentials = new ApiKeyCredential(Environment.GetEnvironmentVariable("PNKL_OPEN_AI_KEY")!);

            var options = new OpenAI.OpenAIClientOptions
            {
                UserAgentApplicationId = "TgJobAdAnalytics",
                RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(maxRetries: 3)
            };

            return new ChatClient("gpt-5-nano", credentials, options);
        });
        
        services.AddSingleton<RateApiClient>();
        services.AddSingleton<RateSourceManager>();
        services.AddSingleton<RateServiceFactory>();

        services.AddSingleton<SalaryPattenrFactory>();

        services.AddTransient<ChatDataService>();
        services.AddTransient<MessageDataService>();
        services.AddTransient<AdDataService>();
        services.AddTransient<SimilarityCalculator>();
        services.AddTransient<UploadService>();
        
        services.AddTransient(serviceProvider => 
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var rateServiceFactory = serviceProvider.GetRequiredService<RateServiceFactory>();

            return new SalaryProcessingServiceFactory(dbContext, rateServiceFactory);    
        });

        services.AddSingleton<SalaryExtractionService>();
        services.AddSingleton<SalaryPersistenceService>();
        services.AddTransient<AdProcessor>();

        //services.AddTransient<HtmlReportPrinter>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var logger = services.GetRequiredService<ILogger<Program>>();

var startTime = Stopwatch.GetTimestamp();

var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
var sourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources");
var outputPath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Output");
const string relativeTemplatePath = "Views/Reports";

using var dbContext = services.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.MigrateAsync();

var uploadService = services.GetRequiredService<UploadService>();
await uploadService.UpdateFromJson(sourcePath);

var adProcessor = services.GetRequiredService<AdProcessor>();
await adProcessor.Process();

//messages = await TryAddSalaries(sourcePath, messages, parallelOptions, dbContext);

//List<ReportGroup> reports = [];

//reports.Add(AdStatsCalculator.CalculateAll(messages));
//reports.Add(SalaryCalculator.CalculateAll(messages));

//var printer = new HtmlReportPrinter(outputPath, relativeTemplatePath, chats);
//printer.Print(reports);
//ConsoleReportPrinter.Print(reports);

logger.LogInformation("Completed in {ElapsedSeconds} seconds", Stopwatch.GetElapsedTime(startTime).TotalSeconds);
//Console.ReadKey();
