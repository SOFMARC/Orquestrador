using AnalyzerOrchestrator.Application.DTOs.DataMapping;
using AnalyzerOrchestrator.Application.DTOs.Extraction;

namespace AnalyzerOrchestrator.Application.Interfaces;

/// <summary>
/// Contrato para a Etapa 3 do workflow: Mapeamento Inicial de Dados.
/// Detecta tabelas e estruturas de dados por heurística e gera os 5 artefatos obrigatórios.
/// </summary>
public interface IDataMappingService
{
    /// <summary>Executa a detecção de tabelas e gera os artefatos para a run informada.</summary>
    Task<DataMappingResultDto> ExecuteAsync(int pipelineRunId, CancellationToken ct = default);

    /// <summary>Registra a revisão humana (aprovação ou reprovação) da etapa.</summary>
    Task ReviewStepAsync(StepReviewDto dto);

    /// <summary>Retorna o resultado já persistido de uma execução anterior, ou null se não houver.</summary>
    Task<DataMappingResultDto?> GetResultAsync(int pipelineRunId);
}
