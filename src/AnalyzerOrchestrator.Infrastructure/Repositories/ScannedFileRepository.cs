using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerOrchestrator.Infrastructure.Repositories;

public class ScannedFileRepository : IScannedFileRepository
{
    private readonly OrchestratorDbContext _context;

    public ScannedFileRepository(OrchestratorDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<ScannedFile> files)
    {
        _context.ScannedFiles.AddRange(files);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ScannedFile>> GetByRunAsync(int pipelineRunId)
    {
        return await _context.ScannedFiles
            .Where(f => f.PipelineRunId == pipelineRunId)
            .OrderBy(f => f.RelativePath)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScannedFile>> GetRelevantByRunAsync(int pipelineRunId)
    {
        return await _context.ScannedFiles
            .Where(f => f.PipelineRunId == pipelineRunId && f.IsRelevant)
            .OrderByDescending(f => f.RelevanceScore)
            .ThenBy(f => f.RelativePath)
            .ToListAsync();
    }

    public async Task DeleteByRunAsync(int pipelineRunId)
    {
        var files = _context.ScannedFiles.Where(f => f.PipelineRunId == pipelineRunId);
        _context.ScannedFiles.RemoveRange(files);
        await _context.SaveChangesAsync();
    }
}
