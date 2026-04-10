using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerOrchestrator.Infrastructure.Repositories;

public class ProjectRepository : BaseRepository<Project>, IProjectRepository
{
    public ProjectRepository(OrchestratorDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<Project>> GetAllAsync()
        => await _dbSet
            .Include(p => p.PipelineRuns)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<Project?> GetWithRunsAsync(int id)
        => await _dbSet
            .Include(p => p.PipelineRuns.OrderByDescending(r => r.CreatedAt))
            .FirstOrDefaultAsync(p => p.Id == id);
}
