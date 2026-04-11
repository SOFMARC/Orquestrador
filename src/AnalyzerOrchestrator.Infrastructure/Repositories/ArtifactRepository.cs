using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Domain.Enums;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerOrchestrator.Infrastructure.Repositories;

public class ArtifactRepository : BaseRepository<Artifact>, IArtifactRepository
{
    public ArtifactRepository(OrchestratorDbContext context) : base(context) { }

    public async Task<IEnumerable<Artifact>> GetByRunAsync(int pipelineRunId)
    {
        return await _context.Artifacts
            .Where(a => a.PipelineRunId == pipelineRunId)
            .OrderBy(a => a.Type)
            .ToListAsync();
    }

    public async Task<Artifact?> GetByRunAndTypeAsync(int pipelineRunId, ArtifactType type)
    {
        return await _context.Artifacts
            .FirstOrDefaultAsync(a => a.PipelineRunId == pipelineRunId && a.Type == type);
    }
}
