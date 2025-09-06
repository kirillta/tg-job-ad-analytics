using TgJobAdAnalytics.Pipelines;
using TgJobAdAnalytics.Services.Reports;
using TgJobAdAnalytics.Services.Salaries;
using TgJobAdAnalytics.Services.Uploads;

namespace TgJobAdAnalytics.Services;

public class ProcessOrchestrator
{
    public ProcessOrchestrator(
        TelegramChatImportService telegramChatImportService, 
        SalaryExtractionProcessor salaryExtractionProcessor, 
        IPipelineRunner pipelineRunner,
        ReportGenerationService reportGenerationService,
        IReportExporter reportExporter)
    {
        _pipelineRunner = pipelineRunner;
        _reportExporter = reportExporter;
        _reportGenerationService = reportGenerationService;
        _salaryExtractionProcessor = salaryExtractionProcessor;
        _telegramChatImportService = telegramChatImportService;
    }


    public async Task Run(List<string> pipelineNames, CancellationToken cancellationToken)
    {
        await ImportData();

        if (pipelineNames.Count != 0)
            await ExecutePipelines(pipelineNames, cancellationToken);

        GenerateAndExportReport();
    }


    async Task ImportData()
    {
        await _telegramChatImportService.ImportFromJson();
        await _salaryExtractionProcessor.ExtractAndPersist();
    }


    async Task ExecutePipelines(List<string> args, CancellationToken cancellationToken)
    {
        var pipelineName = args.FirstOrDefault() ?? "update-levels";
        await _pipelineRunner.Run(pipelineName, cancellationToken);
    }


    void GenerateAndExportReport()
    {
        var reports = _reportGenerationService.Generate();
        _reportExporter.Write(reports);
    }

    
    private readonly IPipelineRunner _pipelineRunner;
    private readonly IReportExporter _reportExporter;
    private readonly ReportGenerationService _reportGenerationService;
    private readonly SalaryExtractionProcessor _salaryExtractionProcessor;
    private readonly TelegramChatImportService _telegramChatImportService;
}
