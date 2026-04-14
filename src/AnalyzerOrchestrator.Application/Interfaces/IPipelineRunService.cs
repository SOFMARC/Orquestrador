using AnalyzerOrchestrator.Application.DTOs.PipelineRuns;

namespace AnalyzerOrchestrator.Application.Interfaces;

public interface IPipelineRunService
{
    Task<IEnumerable<PipelineRunDto>> GetByProjectAsync(int projectId);
    Task<PipelineRunDto?> GetByIdAsync(int id);
    Task<PipelineRunDto> CreateAsync(CreatePipelineRunDto dto);
    Task<bool> CancelAsync(int id);

    /// <summary>Retorna a run que contém o step com o Id informado.</summary>
    Task<PipelineRunDto?> GetByStepIdAsync(int stepId);
}
