namespace AnalyzerOrchestrator.Domain.Enums;

/// <summary>
/// Status de uma etapa individual de pipeline.
/// Inclui estados de revisão humana para etapas que requerem aprovação.
/// </summary>
public enum StepStatus
{
    Pending = 0,
    Running = 1,
    Executed = 2,
    AwaitingReview = 3,
    Approved = 4,
    Rejected = 5,
    Failed = 6,
    Skipped = 7,

    // Mantido para compatibilidade com código existente que usa Completed
    Completed = 4
}
