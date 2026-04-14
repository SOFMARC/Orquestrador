using AnalyzerOrchestrator.Application.DTOs.Consolidation;
using AnalyzerOrchestrator.Application.DTOs.Extraction;

namespace AnalyzerOrchestrator.Application.Interfaces;

/// <summary>
/// Contrato para a Etapa 2 do workflow: Consolidação Arquitetural.
/// Consome os ScannedFiles da Extração Estrutural e produz artefatos
/// de mapa de módulos, distribuição por camada e arquivos centrais.
/// </summary>
public interface IArchitecturalConsolidationService
{
    /// <summary>
    /// Executa a consolidação arquitetural para a run especificada.
    /// Requer que a Etapa 1 (Extração Estrutural) esteja aprovada.
    /// </summary>
    Task<ConsolidationResultDto> ExecuteAsync(int pipelineRunId, CancellationToken ct = default);

    /// <summary>
    /// Registra a revisão humana (aprovação ou reprovação) da etapa.
    /// </summary>
    Task ReviewStepAsync(StepReviewDto dto);

    /// <summary>
    /// Reconstrói o resultado da consolidação a partir dos artefatos persistidos.
    /// Retorna null se a etapa ainda não foi executada.
    /// </summary>
    Task<ConsolidationResultDto?> GetResultAsync(int pipelineRunId);
}
