using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerOrchestrator.Infrastructure.Repositories;

public class ProjectScanSettingsRepository : IProjectScanSettingsRepository
{
    private readonly OrchestratorDbContext _context;

    public ProjectScanSettingsRepository(OrchestratorDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectScanSettings?> GetByProjectIdAsync(int projectId)
    {
        return await _context.ProjectScanSettings
            .FirstOrDefaultAsync(s => s.ProjectId == projectId);
    }

    public async Task SaveAsync(ProjectScanSettings settings)
    {
        if (settings.Id == 0)
            _context.ProjectScanSettings.Add(settings);
        else
            _context.ProjectScanSettings.Update(settings);

        await _context.SaveChangesAsync();
    }
}
