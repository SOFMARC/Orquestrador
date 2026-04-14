using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerOrchestrator.Infrastructure.Repositories;

public class PipelineRunRepository : BaseRepository<PipelineRun>, IPipelineRunRepository
{
    public PipelineRunRepository(OrchestratorDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PipelineRun>> GetByProjectAsync(int projectId)
        => await _dbSet
            .Include(r => r.Project)
            .Include(r => r.StepExecutions)
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<PipelineRun?> GetWithStepsAsync(int id)
        => await _dbSet
            .Include(r => r.Project)
            .Include(r => r.StepExecutions.OrderBy(s => s.StepNumber))
            .Include(r => r.Artifacts)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<PipelineRun?> GetByStepIdAsync(int stepId)
        => await _dbSet
            .Include(r => r.Project)
            .Include(r => r.StepExecutions.OrderBy(s => s.StepNumber))
            .Include(r => r.Artifacts)
            .FirstOrDefaultAsync(r => r.StepExecutions.Any(s => s.Id == stepId));
}
