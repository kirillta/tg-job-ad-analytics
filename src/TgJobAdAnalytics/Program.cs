using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Services.Levels;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Reports;
using TgJobAdAnalytics.Services.Reports.Html;
using TgJobAdAnalytics.Services.Salaries;
using TgJobAdAnalytics.Services.Uploads;


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

        services.Configure<ReportPrinterOptions>(options =>
        {
            options.OutputPath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Output");
            options.TemplatePath = Path.Combine("Views", "Reports");
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

        services.AddTransient<TelegramChatPersistenceService>();
        services.AddTransient<TelegramMessagePersistenceService>();
        services.AddTransient<TelegramAdPersistenceService>();
        services.AddTransient<SimilarityCalculator>();
        services.AddTransient<TelegramChatImportService>();

        services.AddTransient<PositionLevelExtractionService>();
        services.AddSingleton<PositionLevelResolver>();

        services.AddTransient(serviceProvider => 
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var rateServiceFactory = serviceProvider.GetRequiredService<RateServiceFactory>();

            return new SalaryProcessingServiceFactory(dbContext, rateServiceFactory);    
        });
        
        services.AddSingleton<SalaryExtractionService>();
        services.AddSingleton<SalaryPersistenceService>();
        services.AddTransient<SalaryExtractionProcessor>();
        services.AddTransient<SalaryLevelUpdateProcessor>();

        services.AddTransient<ReportGenerationService>();
        services.AddTransient<IReportExporter, HtmlReportExporter>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var logger = services.GetRequiredService<ILogger<Program>>();

using var dbContext = services.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.MigrateAsync();

var startTime = Stopwatch.GetTimestamp();

//var sourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources");
//var telegramChatImportService = services.GetRequiredService<TelegramChatImportService>();
//await telegramChatImportService.ImportFromJson(sourcePath);

//var salaryExtractionProcessor = services.GetRequiredService<SalaryExtractionProcessor>();
//await salaryExtractionProcessor.ExtractAndPersist();

// Backfill missing salary levels (idempotent)
var levelUpdater = services.GetRequiredService<SalaryLevelUpdateProcessor>();
await levelUpdater.UpdateMissingLevels();

//var reportGenerationService = services.GetRequiredService<ReportGenerationService>();
//var reports = reportGenerationService.Generate();

//var exporter = services.GetRequiredService<IReportExporter>();
//exporter.Write(reports);

logger.LogInformation("Completed in {ElapsedSeconds} seconds", Stopwatch.GetElapsedTime(startTime).TotalSeconds);
