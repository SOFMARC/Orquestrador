namespace AnalyzerOrchestrator.Domain.Enums;

public enum ArtifactType
{
    Unknown = 0,
    // Etapa 2 — Extração Estrutural
    FileInventory = 1,
    StructureTree = 2,
    RelevantFilesList = 3,
    ExecutionSummary = 4,
    // Etapas futuras
    ContextDocument = 10,
    StructureMap = 11,
    AnalysisReport = 12,
    Other = 99
}
