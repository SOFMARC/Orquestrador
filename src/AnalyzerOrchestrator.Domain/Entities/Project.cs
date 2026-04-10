namespace AnalyzerOrchestrator.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RepositoryPath { get; set; }
    public string? TechnologyStack { get; set; }
    public bool IsActive { get; set; } = true;

    // Navegação
    public ICollection<PipelineRun> PipelineRuns { get; set; } = new List<PipelineRun>();
}
