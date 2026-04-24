namespace AnalyzerOrchestrator.Application.Workflow;

/// <summary>
/// Define as 4 etapas oficiais do Analyzer Orchestrator.
///
/// Objetivo do produto:
///   Transformar um sistema complexo em um snapshot técnico limpo e enxuto,
///   otimizado para leitura por IA com menor custo e maior precisão.
///
/// Etapas oficiais:
///   Etapa 1 — Extração do sistema         (implementada)
///   Etapa 2 — Requisito estruturado        (próxima)
///   Etapa 3 — Mapa de impacto              (futura)
///   Etapa 4 — Geração do snapshot final    (futura)
///
/// Nota de compatibilidade:
///   Runs criadas antes deste realinhamento possuem steps 1, 2 e 3 separados
///   (Extração Estrutural, Consolidação Arquitetural, Mapeamento Inicial de Dados).
///   Esses registros são preservados no banco e continuam funcionando normalmente.
///   A interface detecta runs legadas pelo nome do step 1 e renderiza adequadamente.
/// </summary>
public class DefaultAnalysisWorkflow : IWorkflowDefinition
{
    // ── Constantes de número de etapa ────────────────────────────────────────────
    // Use sempre estas constantes para localizar etapas — nunca números literais.

    /// <summary>Etapa 1 — Extração do sistema: estrutura, arquitetura e dados.</summary>
    public const int StepSystemExtraction = 1;

    /// <summary>Etapa 2 — Requisito estruturado: objetivo, regras, restrições e critérios de aceite.</summary>
    public const int StepStructuredRequirement = 2;

    /// <summary>Etapa 3 — Mapa de impacto: onde mexer, o que pode quebrar, o que preservar.</summary>
    public const int StepImpactMap = 3;

    /// <summary>Etapa 4 — Geração do snapshot final para IA: contexto técnico limpo e enxuto.</summary>
    public const int StepFinalSnapshot = 4;

    // ── Aliases de compatibilidade com código legado ─────────────────────────────
    // Mantidos apenas para que código existente compile sem erros.
    // Não usar em código novo.

    /// <summary>Alias legado. Use StepSystemExtraction.</summary>
    [System.Obsolete("Use StepSystemExtraction. Mantido para compatibilidade com runs legadas.")]
    public const int StepStructuralExtraction = 1;

    /// <summary>Alias legado. Use StepStructuredRequirement.</summary>
    [System.Obsolete("Use StepStructuredRequirement.")]
    public const int StepArchitecturalConsolidation = 2;

    /// <summary>Alias legado. Use StepImpactMap.</summary>
    [System.Obsolete("Use StepImpactMap.")]
    public const int StepDataMapping = 3;

    /// <summary>Alias legado. Use StepFinalSnapshot.</summary>
    [System.Obsolete("Use StepFinalSnapshot.")]
    public const int StepContextPreparation = 4;

    // ── Definição das etapas ─────────────────────────────────────────────────────
    private static readonly List<WorkflowStepDefinition> _steps = new()
    {
        new(StepSystemExtraction,
            "Extração do sistema",
            "Consolida a estrutura do projeto, organização arquitetural e mapeamento de dados em uma visão técnica unificada."),

        new(StepStructuredRequirement,
            "Requisito estruturado",
            "Organiza a solicitação de mudança com objetivo, entrada, saída esperada, regras de negócio, restrições e critérios de aceite.",
            isOptional: false),

        new(StepImpactMap,
            "Mapa de impacto",
            "Identifica onde mexer, por que mexer ali, o que pode quebrar e o que não deve ser alterado.",
            isOptional: false),

        new(StepFinalSnapshot,
            "Geração do snapshot final para IA",
            "Gera o arquivo final enxuto com apenas o contexto necessário para a IA interpretar o sistema e a mudança solicitada.",
            isOptional: false),
    };

    public IReadOnlyList<WorkflowStepDefinition> GetSteps() => _steps.AsReadOnly();
}
