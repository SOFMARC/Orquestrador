using AnalyzerOrchestrator.Domain.Entities;

namespace AnalyzerOrchestrator.Application.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetWithRunsAsync(int id);
}
