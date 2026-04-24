using AnalyzerOrchestrator.Application.DTOs.Extraction;
using AnalyzerOrchestrator.Application.DTOs.SystemExtraction;

namespace AnalyzerOrchestrator.Application.Interfaces;

/// <summary>
/// Orquestrador da Etapa 1 — Extração do sistema.
///
/// Executa as três subetapas internas em sequência:
///   1.1 Extração Estrutural   → IStructuralExtractionService
///   1.2 Consolidação Arquitetural → IArchitecturalConsolidationService
///   1.3 Mapeamento Inicial de Dados → IDataMappingService
///
/// Preserva os artefatos individuais de cada subetapa e retorna
/// um resultado consolidado para exibição unificada ao usuário.
///
/// Compatibilidade: este serviço opera apenas em runs novas (workflow v2).
/// Runs legadas continuam usando os serviços individuais via seus controllers.
/// </summary>
public interface ISystemExtractionService
{
    /// <summary>
    /// Executa as três subetapas em sequência para a run informada.
    /// Atualiza o PipelineStepExecution da Etapa 1 ao longo da execução.
    /// </summary>
    Task<SystemExtractionResultDto> ExecuteAsync(int pipelineRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra a decisão de revisão humana (aprovação ou reprovação) da Etapa 1.
    /// </summary>
    Task ReviewStepAsync(StepReviewDto dto);

    /// <summary>
    /// Reconstrói o resultado consolidado a partir dos artefatos já persistidos.
    /// Retorna null se a Etapa 1 ainda não foi executada.
    /// </summary>
    Task<SystemExtractionResultDto?> GetResultAsync(int pipelineRunId);
}
