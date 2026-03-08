using Microsoft.Extensions.Logging;
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
    /// <param name="loggerFactory">Factory used to create loggers.</param>
    /// <param name="telegramChatImportService">Service responsible for importing chat, message, and advertisement data.</param>
    /// <param name="salaryExtractionProcessor">Processor that extracts salary information and persists results.</param>
    /// <param name="pipelineRunner">Pipeline runner used for executing named processing pipelines.</param>
    /// <param name="reportGenerationService">Service that produces report groups from the processed data.</param>
    /// <param name="reportExporter">Exporter that writes generated reports to the configured output.</param>
    public ProcessOrchestrator(
        ILoggerFactory loggerFactory,
        TelegramChatImportService telegramChatImportService,
        SalaryExtractionProcessor salaryExtractionProcessor,
        IPipelineRunner pipelineRunner,
        ReportGenerationService reportGenerationService,
        IReportExporter reportExporter)
    {
        _logger = loggerFactory.CreateLogger<ProcessOrchestrator>();
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
        CleanupHeapAfterImport();
        await _salaryExtractionProcessor.ExtractAndPersist(cancellationToken);
    }


    void CleanupHeapAfterImport()
    {
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: false);
        var memoryBeforeMB = memoryBefore / 1024.0 / 1024.0;

        _logger.LogInformation("Heap cleanup initiated. Memory before: {MemoryMB:F2} MB", memoryBeforeMB);

        GC.Collect(generation: 2, mode: GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(generation: 2, mode: GCCollectionMode.Aggressive, blocking: true, compacting: true);

        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var memoryAfterMB = memoryAfter / 1024.0 / 1024.0;
        var reclaimedMB = (memoryBefore - memoryAfter) / 1024.0 / 1024.0;

        _logger.LogInformation("Heap cleanup completed. Memory after: {MemoryMB:F2} MB, Reclaimed: {ReclaimedMB:F2} MB", memoryAfterMB, reclaimedMB);
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

    
    
    private readonly ILogger<ProcessOrchestrator> _logger;
    private readonly IPipelineRunner _pipelineRunner;
    private readonly IReportExporter _reportExporter;
    private readonly ReportGenerationService _reportGenerationService;
    private readonly SalaryExtractionProcessor _salaryExtractionProcessor;
    private readonly TelegramChatImportService _telegramChatImportService;
}
