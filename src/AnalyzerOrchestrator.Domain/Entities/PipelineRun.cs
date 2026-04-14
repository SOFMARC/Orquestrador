using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Domain.Entities;

public class PipelineRun : BaseEntity
{
    public int ProjectId { get; set; }
    public RunStatus Status { get; set; } = RunStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? CurrentStep { get; set; }
    public string? Notes { get; set; }
    public string? TriggerSource { get; set; }

    // Navegação
    public Project Project { get; set; } = null!;
    public ICollection<PipelineStepExecution> StepExecutions { get; set; } = new List<PipelineStepExecution>();
    public ICollection<Artifact> Artifacts { get; set; } = new List<Artifact>();
    public ICollection<ScannedFile> ScannedFiles { get; set; } = new List<ScannedFile>();
    public ICollection<DetectedTable> DetectedTables { get; set; } = new List<DetectedTable>();
}
