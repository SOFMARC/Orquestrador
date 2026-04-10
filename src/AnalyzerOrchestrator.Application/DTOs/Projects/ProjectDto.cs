namespace AnalyzerOrchestrator.Application.DTOs.Projects;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RepositoryPath { get; set; }
    public string? TechnologyStack { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalRuns { get; set; }
}
