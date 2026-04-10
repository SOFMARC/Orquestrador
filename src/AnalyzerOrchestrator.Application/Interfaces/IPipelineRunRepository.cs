using AnalyzerOrchestrator.Domain.Entities;

namespace AnalyzerOrchestrator.Application.Interfaces;

public interface IPipelineRunRepository : IRepository<PipelineRun>
{
    Task<IEnumerable<PipelineRun>> GetByProjectAsync(int projectId);
    Task<PipelineRun?> GetWithStepsAsync(int id);
}
