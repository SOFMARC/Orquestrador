using AnalyzerOrchestrator.Application.DTOs.Projects;

namespace AnalyzerOrchestrator.Application.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetAllAsync();
    Task<ProjectDto?> GetByIdAsync(int id);
    Task<ProjectDto> CreateAsync(CreateProjectDto dto);
    Task<ProjectDto?> UpdateAsync(int id, EditProjectDto dto);
    Task<bool> DeleteAsync(int id);
}
