namespace AnalyzerOrchestrator.Application.DTOs.Consolidation;

/// <summary>
/// Resultado completo da Etapa 2 — Consolidação Arquitetural.
/// Usado tanto para retorno imediato da execução quanto para reconstrução a partir dos artefatos.
/// </summary>
public class ConsolidationResultDto
{
    public int PipelineRunId { get; set; }
    public int StepExecutionId { get; set; }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Métricas resumidas
    public int ModulesCount { get; set; }
    public int LayersCount { get; set; }
    public int CentralFilesCount { get; set; }
    public TimeSpan Duration { get; set; }

    // Dados estruturados
    public List<ModuleDto> Modules { get; set; } = new();
    public List<LayerDto> Layers { get; set; } = new();
    public List<CentralFileDto> CentralFiles { get; set; } = new();
    public List<string> Observations { get; set; } = new();

    // Conteúdo inline para exibição na view
    public string ArchitectureSummaryContent { get; set; } = string.Empty;

    // Caminhos dos artefatos gerados em disco
    public string? ModulesMapPath { get; set; }
    public string? ArchitectureSummaryPath { get; set; }
    public string? LayerDistributionPath { get; set; }
    public string? CentralFilesPath { get; set; }
    public string? Step2SummaryPath { get; set; }
}
