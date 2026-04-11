using AnalyzerOrchestrator.Application.DTOs.Projects;

namespace AnalyzerOrchestrator.Application.Interfaces;

public interface IProjectScanSettingsService
{
    Task<ProjectScanSettingsDto?> GetByProjectIdAsync(int projectId);
    Task<ProjectScanSettingsDto> SaveAsync(SaveScanSettingsDto dto);
}
