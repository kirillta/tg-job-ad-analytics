namespace TgJobAdAnalytics.Models.Pipelines;

/// <summary>
/// Describes a pipeline registered in the system.
/// </summary>
public sealed record PipelineMetadata(string Name, string Description, bool IsIdempotent);
