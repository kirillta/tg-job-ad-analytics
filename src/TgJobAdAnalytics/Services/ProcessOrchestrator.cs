using TgJobAdAnalytics.Services.Pipelines;
using TgJobAdAnalytics.Services.Reports;
using TgJobAdAnalytics.Services.Salaries;
using TgJobAdAnalytics.Services.Uploads;

namespace TgJobAdAnalytics.Services;

/// <summary>
/// Orchestrates the end-to-end processing workflow: imports raw Telegram chat data, extracts and persists salaries,
/// executes selected data processing pipelines, and finally generates and exports analytical reports.
/// </summary>
public sealed class ProcessOrchestrator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessOrchestrator"/>.
    /// </summary>
    /// <param name="telegramChatImportService">Service responsible for importing chat, message, and advertisement data.</param>
    /// <param name="salaryExtractionProcessor">Processor that extracts salary information and persists results.</param>
    /// <param name="pipelineRunner">Pipeline runner used for executing named processing pipelines.</param>
    /// <param name="reportGenerationService">Service that produces report groups from the processed data.</param>
    /// <param name="reportExporter">Exporter that writes generated reports to the configured output.</param>
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


    /// <summary>
    /// Executes the orchestrated workflow: imports data, optionally runs the specified pipelines, and generates/export reports.
    /// </summary>
    /// <param name="pipelineNames">Names of pipelines to execute; when empty, pipeline execution is skipped.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
}
