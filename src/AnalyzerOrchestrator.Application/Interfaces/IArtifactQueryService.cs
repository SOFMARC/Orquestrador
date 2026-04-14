using AnalyzerOrchestrator.Application.DTOs.Artifacts;
using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.Interfaces;

/// <summary>
/// Serviço de consulta de artefatos gerados pelas etapas do workflow.
/// Permite recuperar artefatos por run, etapa ou tipo para exibição e uso pela Etapa 5.
/// </summary>
public interface IArtifactQueryService
{
    /// <summary>Retorna todos os artefatos de uma run, ordenados por etapa.</summary>
    Task<IEnumerable<ArtifactDto>> GetByRunAsync(int pipelineRunId);

    /// <summary>Retorna os artefatos de uma etapa específica de uma run.</summary>
    Task<IEnumerable<ArtifactDto>> GetByRunAndStepAsync(int pipelineRunId, int stepNumber);

    /// <summary>Retorna o conteúdo textual de um artefato (lê do disco se necessário).</summary>
    Task<string?> GetContentAsync(int artifactId);

    /// <summary>
    /// Retorna o conteúdo de todos os artefatos aprovados de uma run,
    /// consolidados em um dicionário keyed pelo ArtifactType.
    /// Usado pela Etapa 5 para montar o contexto final para a IA.
    /// </summary>
    Task<Dictionary<ArtifactType, string>> GetApprovedContentsAsync(int pipelineRunId);
}
