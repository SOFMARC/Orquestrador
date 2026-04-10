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

    // Navegação
    public PipelineRun PipelineRun { get; set; } = null!;
}
