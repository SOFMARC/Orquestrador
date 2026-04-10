using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.DTOs.PipelineRuns;

public class PipelineRunDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public RunStatus Status { get; set; }
    public string StatusLabel => Status.ToString();
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? CurrentStep { get; set; }
    public string? Notes { get; set; }
    public string? TriggerSource { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public List<PipelineStepExecutionDto> StepExecutions { get; set; } = new();
}
