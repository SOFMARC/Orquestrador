using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Domain.Entities;

public class PipelineStepExecution : BaseEntity
{
    public int PipelineRunId { get; set; }
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }

    // Métricas da execução (usadas pela extração estrutural)
    public int? FilesFound { get; set; }
    public int? FilesIgnored { get; set; }
    public int? ErrorCount { get; set; }

    // Revisão humana
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // Navegação
    public PipelineRun PipelineRun { get; set; } = null!;
}
