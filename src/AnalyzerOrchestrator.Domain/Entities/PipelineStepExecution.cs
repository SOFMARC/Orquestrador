using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Domain.Entities;

public class PipelineStepExecution : BaseEntity
{
    public int PipelineRunId { get; set; }
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }

    // ── Métricas genéricas ───────────────────────────────────────────────────────
    /// <summary>Arquivos encontrados/analisados nesta etapa.</summary>
    public int? FilesFound { get; set; }
    /// <summary>Arquivos ignorados (filtrados) nesta etapa.</summary>
    public int? FilesIgnored { get; set; }
    /// <summary>Erros de acesso/leitura durante a execução.</summary>
    public int? ErrorCount { get; set; }

    // ── Métricas específicas por etapa ───────────────────────────────────────────
    // Etapa 2 — Consolidação Arquitetural
    /// <summary>Número de módulos detectados (Etapa 2).</summary>
    public int? ModulesCount { get; set; }
    /// <summary>Número de camadas arquiteturais identificadas (Etapa 2).</summary>
    public int? LayersCount { get; set; }
    /// <summary>Número de arquivos centrais identificados (Etapa 2).</summary>
    public int? CentralFilesCount { get; set; }

    // Etapa 3 — Mapeamento Inicial de Dados
    /// <summary>Número de tabelas detectadas por heurística (Etapa 3).</summary>
    public int? TablesCount { get; set; }
    /// <summary>Número de relações tabela↔arquivo detectadas (Etapa 3).</summary>
    public int? RelationsCount { get; set; }

    // ── Revisão humana ───────────────────────────────────────────────────────────
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // ── Navegação ────────────────────────────────────────────────────────────────
    public PipelineRun PipelineRun { get; set; } = null!;
}
