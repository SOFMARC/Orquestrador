using AnalyzerOrchestrator.Domain.Entities;

namespace AnalyzerOrchestrator.Application.Interfaces;

public interface IProjectScanSettingsRepository
{
    Task<ProjectScanSettings?> GetByProjectIdAsync(int projectId);
    Task SaveAsync(ProjectScanSettings settings);
}
