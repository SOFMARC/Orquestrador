using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.DTOs.PipelineRuns;

public class PipelineStepExecutionDto
{
    public int Id { get; set; }
    public int PipelineRunId { get; set; }
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public string StatusLabel => Status.ToString();
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }

    // ── Métricas genéricas ───────────────────────────────────────────────────────
    public int? FilesFound { get; set; }
    public int? FilesIgnored { get; set; }
    public int? ErrorCount { get; set; }

    // ── Métricas específicas por etapa ───────────────────────────────────────────
    // Etapa 2 — Consolidação Arquitetural
    public int? ModulesCount { get; set; }
    public int? LayersCount { get; set; }
    public int? CentralFilesCount { get; set; }

    // Etapa 3 — Mapeamento Inicial de Dados
    /// <summary>Número de tabelas detectadas — campo explícito, sem parsing de Notes.</summary>
    public int? TablesCount { get; set; }
    /// <summary>Número de relações tabela↔arquivo detectadas.</summary>
    public int? RelationsCount { get; set; }

    // ── Revisão humana ───────────────────────────────────────────────────────────
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    // ── Helpers de estado ────────────────────────────────────────────────────────
    public bool IsExecuted => Status is StepStatus.Executed or StepStatus.AwaitingReview
        or StepStatus.Approved or StepStatus.Rejected;
    public bool CanBeReviewed => Status == StepStatus.AwaitingReview;
    public bool CanBeExecuted => Status is StepStatus.Pending or StepStatus.Rejected;

    /// <summary>Duração da execução calculada a partir de StartedAt/FinishedAt.</summary>
    public TimeSpan? Duration => (StartedAt.HasValue && FinishedAt.HasValue)
        ? FinishedAt.Value - StartedAt.Value
        : null;
}
