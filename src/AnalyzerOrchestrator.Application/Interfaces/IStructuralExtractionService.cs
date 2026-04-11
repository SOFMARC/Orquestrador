using AnalyzerOrchestrator.Application.DTOs.Extraction;

namespace AnalyzerOrchestrator.Application.Interfaces;

/// <summary>
/// Serviço responsável por executar a etapa de extração estrutural de um projeto.
/// </summary>
public interface IStructuralExtractionService
{
    /// <summary>
    /// Executa a varredura estrutural para a run informada.
    /// Atualiza o PipelineStepExecution correspondente e persiste artefatos.
    /// </summary>
    Task<ExtractionResultDto> ExecuteAsync(int pipelineRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra a decisão de revisão humana (aprovação ou reprovação) de uma etapa.
    /// </summary>
    Task ReviewStepAsync(StepReviewDto dto);

    /// <summary>
    /// Retorna o resultado da extração para uma run já executada.
    /// </summary>
    Task<ExtractionResultDto?> GetResultAsync(int pipelineRunId);
}
