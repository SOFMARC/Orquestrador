using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.DTOs.PipelineRuns;

public class PipelineStepExecutionDto
{
    public int Id { get; set; }
    public int PipelineRunId { get; set; }
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public string StatusLabel => Status.ToString();
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }

    // Métricas de execução
    public int? FilesFound { get; set; }
    public int? FilesIgnored { get; set; }
    public int? ErrorCount { get; set; }

    // Revisão humana
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // Helpers
    public bool IsExecuted => Status is StepStatus.Executed or StepStatus.AwaitingReview
        or StepStatus.Approved or StepStatus.Rejected;
    public bool CanBeReviewed => Status == StepStatus.AwaitingReview;
    public bool CanBeExecuted => Status is StepStatus.Pending or StepStatus.Rejected;
}
