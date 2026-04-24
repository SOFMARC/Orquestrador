using AnalyzerOrchestrator.Application.DTOs.Consolidation;
using AnalyzerOrchestrator.Application.DTOs.DataMapping;
using AnalyzerOrchestrator.Application.DTOs.Extraction;

namespace AnalyzerOrchestrator.Application.DTOs.SystemExtraction;

/// <summary>
/// Resultado consolidado da nova Etapa 1 — Extração do sistema.
///
/// Agrega os resultados das três subetapas internas:
///   - Subetapa 1.1: Extração Estrutural  (ExtractionResult)
///   - Subetapa 1.2: Consolidação Arquitetural (ConsolidationResult)
///   - Subetapa 1.3: Mapeamento Inicial de Dados (DataMappingResult)
///
/// Preserva os artefatos individuais de cada subetapa.
/// </summary>
public class SystemExtractionResultDto
{
    public int PipelineRunId { get; set; }

    /// <summary>True se todas as subetapas concluíram sem erro fatal.</summary>
    public bool Success { get; set; }

    /// <summary>Mensagem de erro da primeira subetapa que falhou, se houver.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Duração total das três subetapas somadas.</summary>
    public TimeSpan TotalDuration { get; set; }

    // ── Subetapa 1.1 — Extração Estrutural ──────────────────────────────────────
    public ExtractionResultDto? Extraction { get; set; }

    // ── Subetapa 1.2 — Consolidação Arquitetural ────────────────────────────────
    public ConsolidationResultDto? Consolidation { get; set; }

    // ── Subetapa 1.3 — Mapeamento Inicial de Dados ──────────────────────────────
    public DataMappingResultDto? DataMapping { get; set; }

    // ── Métricas resumidas para exibição rápida ──────────────────────────────────
    public int FilesFound => Extraction?.FilesFound ?? 0;
    public int RelevantFilesCount => Extraction?.RelevantFilesCount ?? 0;
    public int ModulesCount => Consolidation?.ModulesCount ?? 0;
    public int LayersCount => Consolidation?.LayersCount ?? 0;
    public int CentralFilesCount => Consolidation?.CentralFilesCount ?? 0;
    public int TablesCount => DataMapping?.TablesCount ?? 0;
    public int RelationsCount => DataMapping?.RelationsCount ?? 0;
}
