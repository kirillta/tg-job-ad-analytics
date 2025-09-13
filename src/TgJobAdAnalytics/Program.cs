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
using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Pipelines;
using TgJobAdAnalytics.Services;
using TgJobAdAnalytics.Services.Levels;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Reports;
using TgJobAdAnalytics.Services.Reports.Html;
using TgJobAdAnalytics.Services.Salaries;
using TgJobAdAnalytics.Services.Uploads;
using TgJobAdAnalytics.Services.Vectors;


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
        services.Configure<UploadOptions>(options =>
        {
            options.BatchSize = int.Parse(context.Configuration["Upload:BatchSize"]!);
            options.Mode = context.Configuration["Upload:Mode"] switch
            {
                "Clean" => UploadMode.Clean,
                "OnlyNewMessages" => UploadMode.OnlyNewMessages,
                _ => UploadMode.Skip
            };
            options.SourcePath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "Sources");
        });

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

        services.AddSingleton<IVectorizationConfig, OptionVectorizationConfig>();
        services.AddSingleton<IMinHashVectorizer, MinHashVectorizer>();
        services.AddScoped<IVectorStore, VectorStore>();
        services.AddScoped<IVectorIndex, VectorIndex>();
        services.AddSingleton<ISimilarityService, SimilarityService>();
        services.AddTransient<VectorsBackfillService>();

        services.AddSingleton<IPipeline, SalaryLevelUpdatePipeline>();
        services.AddSingleton<IPipeline, DistinctAdsPipeline>();
        services.AddSingleton<IPipeline, InitVectorsPipeline>();
        services.AddSingleton<IPipelineRunner, PipelineRunner>();

        services.AddTransient<ProcessOrchestrator>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var logger = services.GetRequiredService<ILogger<Program>>();

using var dbContext = services.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.MigrateAsync();

var startTime = Stopwatch.GetTimestamp();

using var cancellationToken = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationToken.Cancel();
};

var orchestrator = services.GetRequiredService<ProcessOrchestrator>();
await orchestrator.Run([.. args], cancellationToken.Token);

logger.LogInformation("Completed in {ElapsedSeconds} seconds", Stopwatch.GetElapsedTime(startTime).TotalSeconds);
