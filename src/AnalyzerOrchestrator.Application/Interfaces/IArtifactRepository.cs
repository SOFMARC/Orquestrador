using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.Interfaces;

public interface IArtifactRepository : IRepository<Artifact>
{
    Task<IEnumerable<Artifact>> GetByRunAsync(int pipelineRunId);
    Task<Artifact?> GetByRunAndTypeAsync(int pipelineRunId, ArtifactType type);
}
