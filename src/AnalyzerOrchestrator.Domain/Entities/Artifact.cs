using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Domain.Entities;

public class Artifact : BaseEntity
{
    public int PipelineRunId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ArtifactType Type { get; set; } = ArtifactType.Unknown;
    public string? FilePath { get; set; }
    public string? Content { get; set; }
    public string? MimeType { get; set; }
    public long? SizeBytes { get; set; }
    public string? Notes { get; set; }

    // Navegação
    public PipelineRun PipelineRun { get; set; } = null!;
}
