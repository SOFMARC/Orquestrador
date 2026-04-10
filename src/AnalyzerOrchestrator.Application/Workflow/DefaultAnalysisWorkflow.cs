namespace AnalyzerOrchestrator.Application.Workflow;

/// <summary>
/// Define as etapas padrão do pipeline de análise técnica.
/// Esta classe centraliza a definição do fluxo de trabalho,
/// permitindo evolução futura sem alterar a estrutura de persistência.
/// </summary>
public class DefaultAnalysisWorkflow : IWorkflowDefinition
{
    private static readonly List<WorkflowStepDefinition> _steps = new()
    {
        new(1, "Coleta de Informações do Projeto",
            "Levantamento inicial: tecnologias, estrutura de pastas e arquivos relevantes."),

        new(2, "Mapeamento de Estrutura",
            "Identificação dos módulos, camadas e componentes principais do projeto."),

        new(3, "Análise de Dependências",
            "Levantamento de dependências externas, pacotes e integrações."),

        new(4, "Análise de Banco de Dados",
            "Identificação de tabelas, relacionamentos e convenções de dados.", isOptional: true),

        new(5, "Preparação do Contexto",
            "Consolidação das informações coletadas em documento de contexto para uso com IA."),
    };

    public IReadOnlyList<WorkflowStepDefinition> GetSteps() => _steps.AsReadOnly();
}
