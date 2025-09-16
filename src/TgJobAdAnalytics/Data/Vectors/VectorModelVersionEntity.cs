using System.ComponentModel.DataAnnotations;

namespace TgJobAdAnalytics.Data.Vectors;

public class VectorModelVersionEntity
{
    [Key]
    public int Version { get; set; }

    [Required]
    public string NormalizationVersion { get; set; } = string.Empty;

    public int ShingleSize { get; set; }

    public int HashFunctionCount { get; set; }

    public int MinHashSeed { get; set; }

    public int LshBandCount { get; set; }

    public int LshRowsPerBand { get; set; }

    public int VocabularySize { get; set; }

    public double DuplicateThreshold { get; set; }

    public double SimilarThreshold { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
