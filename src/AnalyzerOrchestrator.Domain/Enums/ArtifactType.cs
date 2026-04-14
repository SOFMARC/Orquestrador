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

    // Etapa 3 — Mapeamento de Dados (Step 3)
    DetectedTables = 11,
    TableFileRelations = 12,
    FileTableRelations = 13,
    TableOperations = 14,
    DataMappingSummary = 15,

    // Etapas futuras
    ContextDocument = 20,
    AnalysisReport = 21,
    Other = 99
}
