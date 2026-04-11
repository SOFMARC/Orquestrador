using AnalyzerOrchestrator.Domain.Entities;

namespace AnalyzerOrchestrator.Application.Interfaces;

public interface IScannedFileRepository
{
    Task AddRangeAsync(IEnumerable<ScannedFile> files);
    Task<IEnumerable<ScannedFile>> GetByRunAsync(int pipelineRunId);
    Task<IEnumerable<ScannedFile>> GetRelevantByRunAsync(int pipelineRunId);
    Task DeleteByRunAsync(int pipelineRunId);
}
