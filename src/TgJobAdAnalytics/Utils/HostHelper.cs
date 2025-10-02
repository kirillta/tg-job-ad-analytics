using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TgJobAdAnalytics.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ClientModel;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Reports;
using TgJobAdAnalytics.Models.Reports.Metadata;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Stacks;
using TgJobAdAnalytics.Models.Uploads;
using TgJobAdAnalytics.Models.Uploads.Enums;
using TgJobAdAnalytics.Models.Vectors;
using TgJobAdAnalytics.Services;
using TgJobAdAnalytics.Services.Levels;
using TgJobAdAnalytics.Services.Localization;
using TgJobAdAnalytics.Services.Messages;
using TgJobAdAnalytics.Services.Pipelines;
using TgJobAdAnalytics.Services.Pipelines.Implementations;
using TgJobAdAnalytics.Services.Reports;
using TgJobAdAnalytics.Services.Reports.Html;
using TgJobAdAnalytics.Services.Reports.Metadata;
using TgJobAdAnalytics.Services.Salaries;
using TgJobAdAnalytics.Services.Stacks;
using TgJobAdAnalytics.Services.Uploads;
using TgJobAdAnalytics.Services.Vectors;

namespace TgJobAdAnalytics.Utils;

public static class HostHelper
{
    public async static Task ApplyDatabaseMigrations(IServiceProvider services)
    { 
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }


    public static IHost BuildHost(string[] args) =>
        Host.CreateDefaultBuilder(args)
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
                options.Mode = Enum.Parse<UploadMode>(context.Configuration["Upload:Mode"]!);
                options.SourcePath = GetOperationalPath("Sources");
            });

            services.Configure<ParallelOptions>(options =>
            {
                options.MaxDegreeOfParallelism = Environment.ProcessorCount;
            });

            services.Configure<VectorizationOptions>(context.Configuration.GetSection("Vectorization"));

            services.Configure<RateOptions>(options => 
            { 
                options.RateApiUrl = new Uri("https://www.cbr.ru/scripts/XML_dynamic.asp");
                options.RateSourcePath = GetOperationalPath("Sources", "rates.csv");
            });

            services.Configure<ReportPrinterOptions>(options =>
            {
                options.OutputPath = GetOperationalPath("Output");
                options.TemplatePath = Path.Combine("Views", "Reports");
            });

            services.Configure<SiteMetadataOptions>(options => 
            {
                options.BaseUrl = context.Configuration["SiteMetadata:BaseUrl"]!;
                options.SiteName = context.Configuration["SiteMetadata:SiteName"]!;
                options.DefaultOgImagePath = context.Configuration["SiteMetadata:DefaultOgImagePath"] ?? string.Empty;
                options.Locales = [.. context.Configuration.GetSection("SiteMetadata:Locales").Get<string[]>()!];
                options.PrimaryLocale = context.Configuration["SiteMetadata:PrimaryLocale"]!;
                options.JsonLdType = Enum.Parse<JsonLdType>(context.Configuration["SiteMetadata:JsonLdType"]!);
                options.LocalizationPath = GetOperationalPath(context.Configuration["SiteMetadata:LocalizationPath"]!);
            });

            services.Configure<StackMappingOptions>(options =>
            {
                options.MappingFilePath = GetOperationalPath("..", "..", "config", "stacks", "channel-stacks.json");
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
        
            services.AddSingleton<ChannelStackMappingManager>();
            services.AddScoped<ChannelStackResolverFactory>();
            services.AddTransient<StackBackfillService>();
        
            services.AddSingleton<IPipeline, SalaryLevelUpdatePipeline>();
            services.AddSingleton<IPipeline, DistinctAdsPipeline>();
            services.AddSingleton<IPipeline, InitVectorsPipeline>();
            services.AddSingleton<IPipeline, DeterministicIdMigrationPipeline>();
            services.AddSingleton<IPipeline, AssignDotnetStackToChatsPipeline>();
            services.AddSingleton<IPipelineRunner, PipelineRunner>();

            services.AddTransient<StackComparisonDataBuilder>();
            services.AddTransient<MetadataBuilder>();
            services.AddSingleton<ILocalizationProvider, InMemoryLocalizationProvider>();
            services.AddSingleton<ReportGroupLocalizer>();
            services.AddSingleton<UiLocalizer>();
            services.AddTransient<ReportGenerationService>();
            services.AddTransient<IReportExporter, HtmlReportExporter>();

            services.AddSingleton<OptionVectorizationConfig>();
            services.AddSingleton<MinHashVectorizer>();
            services.AddScoped<VectorStore>();
            services.AddScoped<VectorIndex>();
            services.AddTransient<VectorsBackfillService>();

            services.AddTransient<ProcessOrchestrator>();
        })
        .Build();


    static string GetOperationalPath(params string[] relativePathSegments) =>
        Path.Combine([Environment.CurrentDirectory, "..", "..", "..", .. relativePathSegments]);
}
