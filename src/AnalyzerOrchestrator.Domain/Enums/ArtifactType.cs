namespace AnalyzerOrchestrator.Domain.Enums;

public enum ArtifactType
{
    Unknown = 0,

    // Etapa 1 — Extração Estrutural (Step 1)
    FileInventory = 1,
    StructureTree = 2,
    RelevantFilesList = 3,
    ExecutionSummary = 4,

    // Etapa 2 — Consolidação Arquitetural (Step 2)
    ModulesMap = 5,
    ArchitectureSummary = 6,
    LayerDistribution = 7,
    CentralFiles = 8,
    Step2Summary = 9,

    // Etapas futuras
    ContextDocument = 10,
    AnalysisReport = 12,
    Other = 99
}
