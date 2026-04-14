namespace AnalyzerOrchestrator.Application.Workflow;

/// <summary>
/// Define as etapas padrão do pipeline de análise técnica.
/// Esta classe centraliza a definição do fluxo de trabalho,
/// permitindo evolução futura sem alterar a estrutura de persistência.
///
/// Estado atual do roadmap:
///   Etapa 1 — Extração Estrutural         (implementada)
///   Etapa 2 — Consolidação Arquitetural    (implementada)
///   Etapa 3 — Mapeamento Inicial de Dados  (implementada)
///   Etapa 4 — Requisito Estruturado e Impacto Inicial (próxima)
///   Etapa 5 — Preparação do Contexto       (futura)
/// </summary>
public class DefaultAnalysisWorkflow : IWorkflowDefinition
{
    // ── Constantes de número de etapa ────────────────────────────────────────────
    // Use sempre estas constantes para localizar etapas — nunca números literais.

    /// <summary>Etapa 1 — Extração Estrutural: varredura, inventário, árvore, classificação.</summary>
    public const int StepStructuralExtraction = 1;

    /// <summary>Etapa 2 — Consolidação Arquitetural: módulos, camadas, arquivos centrais.</summary>
    public const int StepArchitecturalConsolidation = 2;

    /// <summary>Etapa 3 — Mapeamento Inicial de Dados: detecção de tabelas por heurística.</summary>
    public const int StepDataMapping = 3;

    /// <summary>Etapa 4 — Requisito Estruturado e Impacto Inicial (não implementada ainda).</summary>
    public const int StepStructuredRequirement = 4;

    /// <summary>Etapa 5 — Preparação do Contexto (não implementada ainda).</summary>
    public const int StepContextPreparation = 5;

    // ── Aliases mantidos para compatibilidade interna (não usar em código novo) ──
    [System.Obsolete("Use StepArchitecturalConsolidation")]
    public const int StepStructureMapping = StepArchitecturalConsolidation;

    [System.Obsolete("Use StepDataMapping")]
    public const int StepDependencyAnalysis = StepDataMapping;

    [System.Obsolete("Use StepStructuredRequirement")]
    public const int StepDatabaseAnalysis = StepStructuredRequirement;

    // ── Definição das etapas ─────────────────────────────────────────────────────
    private static readonly List<WorkflowStepDefinition> _steps = new()
    {
        new(StepStructuralExtraction,
            "Extração Estrutural",
            "Varredura do diretório, inventário de arquivos, árvore estrutural, classificação e identificação de arquivos relevantes."),

        new(StepArchitecturalConsolidation,
            "Consolidação Arquitetural",
            "Identificação dos módulos, camadas e componentes principais do projeto."),

        new(StepDataMapping,
            "Mapeamento Inicial de Dados",
            "Detecção de tabelas e estruturas de dados por heurística a partir dos arquivos escaneados."),

        new(StepStructuredRequirement,
            "Requisito Estruturado e Impacto Inicial",
            "Levantamento de requisitos estruturados e análise de impacto inicial (próxima etapa do roadmap).",
            isOptional: true),

        new(StepContextPreparation,
            "Preparação do Contexto",
            "Consolidação das informações coletadas em documento de contexto para uso com IA."),
    };

    public IReadOnlyList<WorkflowStepDefinition> GetSteps() => _steps.AsReadOnly();
}
