namespace AnalyzerOrchestrator.Application.Workflow;

/// <summary>
/// Define as etapas padrão do pipeline de análise técnica.
/// Esta classe centraliza a definição do fluxo de trabalho,
/// permitindo evolução futura sem alterar a estrutura de persistência.
/// </summary>
public class DefaultAnalysisWorkflow : IWorkflowDefinition
{
    public const int StepStructuralExtraction = 1;
    public const int StepStructureMapping = 2;
    public const int StepDependencyAnalysis = 3;
    public const int StepDatabaseAnalysis = 4;
    public const int StepContextPreparation = 5;

    private static readonly List<WorkflowStepDefinition> _steps = new()
    {
        new(StepStructuralExtraction,
            "Extração Estrutural",
            "Varredura do diretório, inventário de arquivos, árvore estrutural, classificação e identificação de arquivos relevantes."),

        new(StepStructureMapping,
            "Mapeamento de Estrutura",
            "Identificação dos módulos, camadas e componentes principais do projeto."),

        new(StepDependencyAnalysis,
            "Análise de Dependências",
            "Levantamento de dependências externas, pacotes e integrações."),

        new(StepDatabaseAnalysis,
            "Análise de Banco de Dados",
            "Identificação de tabelas, relacionamentos e convenções de dados.", isOptional: true),

        new(StepContextPreparation,
            "Preparação do Contexto",
            "Consolidação das informações coletadas em documento de contexto para uso com IA."),
    };

    public IReadOnlyList<WorkflowStepDefinition> GetSteps() => _steps.AsReadOnly();
}
