using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.DTOs.Artifacts;

public class ArtifactDto
{
    public int Id { get; set; }
    public int PipelineRunId { get; set; }

    /// <summary>Número da etapa que gerou este artefato.</summary>
    public int StepNumber { get; set; }

    public string Name { get; set; } = string.Empty;
    public ArtifactType Type { get; set; }
    public string TypeLabel => Type.ToString();
    public string? FilePath { get; set; }
    public string? MimeType { get; set; }
    public long? SizeBytes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Tamanho formatado para exibição (ex: "12.4 KB").</summary>
    public string SizeLabel => SizeBytes.HasValue
        ? SizeBytes.Value >= 1_048_576
            ? $"{SizeBytes.Value / 1_048_576.0:F1} MB"
            : SizeBytes.Value >= 1_024
                ? $"{SizeBytes.Value / 1_024.0:F1} KB"
                : $"{SizeBytes.Value} B"
        : "—";

    /// <summary>Indica se o artefato é um arquivo Markdown (para exibição inline).</summary>
    public bool IsMarkdown => MimeType == "text/markdown"
        || Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    /// <summary>Indica se o artefato é JSON (para exibição inline).</summary>
    public bool IsJson => MimeType == "application/json"
        || Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
}
