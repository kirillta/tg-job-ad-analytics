namespace TgJobAdAnalytics.Models.Messages;

public class VectorizationOptions
{
    public int CurrentVersion { get; set; }
    public int HashFunctionCount { get; set; } = 100;
    public int LshBandCount { get; set; } = 20;
    public int MinHashSeed { get; set; } = 1000;
    public string NormalizationVersion { get; set; } = string.Empty;
    public int ShingleSize { get; set; } = 5;
}
