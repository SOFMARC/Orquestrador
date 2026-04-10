using AnalyzerOrchestrator.Application.DTOs.Projects;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Entities;

namespace AnalyzerOrchestrator.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<IEnumerable<ProjectDto>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        return projects.Select(MapToDto);
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        var project = await _projectRepository.GetWithRunsAsync(id);
        return project is null ? null : MapToDtoWithRuns(project);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
    {
        var project = new Project
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            RepositoryPath = dto.RepositoryPath?.Trim(),
            TechnologyStack = dto.TechnologyStack?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _projectRepository.AddAsync(project);
        return MapToDto(project);
    }

    public async Task<ProjectDto?> UpdateAsync(int id, EditProjectDto dto)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project is null) return null;

        project.Name = dto.Name.Trim();
        project.Description = dto.Description?.Trim();
        project.RepositoryPath = dto.RepositoryPath?.Trim();
        project.TechnologyStack = dto.TechnologyStack?.Trim();
        project.IsActive = dto.IsActive;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project);
        return MapToDto(project);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project is null) return false;

        await _projectRepository.DeleteAsync(project);
        return true;
    }

    private static ProjectDto MapToDto(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        RepositoryPath = p.RepositoryPath,
        TechnologyStack = p.TechnologyStack,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        TotalRuns = p.PipelineRuns?.Count ?? 0
    };

    private static ProjectDto MapToDtoWithRuns(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        RepositoryPath = p.RepositoryPath,
        TechnologyStack = p.TechnologyStack,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        TotalRuns = p.PipelineRuns?.Count ?? 0
    };
}
