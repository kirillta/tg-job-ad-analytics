using TgJobAdAnalytics.Pipelines;
using TgJobAdAnalytics.Services.Reports;
using TgJobAdAnalytics.Services.Salaries;
using TgJobAdAnalytics.Services.Uploads;
using TgJobAdAnalytics.Services.Vectors;

namespace TgJobAdAnalytics.Services;

public class ProcessOrchestrator
{
    public ProcessOrchestrator(
        TelegramChatImportService telegramChatImportService, 
        SalaryExtractionProcessor salaryExtractionProcessor, 
        IPipelineRunner pipelineRunner,
        ReportGenerationService reportGenerationService,
        IReportExporter reportExporter,
        VectorsBackfillService vectorsBackfillService)
    {
        _pipelineRunner = pipelineRunner;
        _reportExporter = reportExporter;
        _reportGenerationService = reportGenerationService;
        _salaryExtractionProcessor = salaryExtractionProcessor;
        _telegramChatImportService = telegramChatImportService;
        _vectorsBackfillService = vectorsBackfillService;
    }


    public async Task Run(List<string> pipelineNames, CancellationToken cancellationToken)
    {
        await ImportData(cancellationToken);

        if (pipelineNames.Count != 0)
            await ExecutePipelines(pipelineNames, cancellationToken);

        GenerateAndExportReport();
    }


    async Task ImportData(CancellationToken cancellationToken)
    {
        await _telegramChatImportService.ImportFromJson(cancellationToken);
        await _salaryExtractionProcessor.ExtractAndPersist(cancellationToken);
    }


    async Task ExecutePipelines(List<string> pipelineNames, CancellationToken cancellationToken)
    {
        foreach (var pipelineName in pipelineNames)
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
    private readonly VectorsBackfillService _vectorsBackfillService;
}
