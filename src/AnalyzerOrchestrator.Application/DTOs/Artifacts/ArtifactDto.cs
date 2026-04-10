using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.DTOs.Artifacts;

public class ArtifactDto
{
    public int Id { get; set; }
    public int PipelineRunId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ArtifactType Type { get; set; }
    public string TypeLabel => Type.ToString();
    public string? FilePath { get; set; }
    public string? MimeType { get; set; }
    public long? SizeBytes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
