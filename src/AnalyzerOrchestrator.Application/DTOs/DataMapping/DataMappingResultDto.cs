namespace AnalyzerOrchestrator.Application.DTOs.DataMapping;

/// <summary>
/// Resultado completo da Etapa 3 — Mapeamento Inicial de Dados.
/// </summary>
public class DataMappingResultDto
{
    public int PipelineRunId { get; set; }
    public int StepExecutionId { get; set; }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Métricas
    public int TablesCount { get; set; }
    public int FilesAnalyzed { get; set; }
    public int RelationsCount { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan Duration { get; set; }

    // Dados
    public List<DetectedTableDto> Tables { get; set; } = new();

    // Caminhos dos artefatos
    public string? DetectedTablesPath { get; set; }
    public string? TableFileRelationsPath { get; set; }
    public string? FileTableRelationsPath { get; set; }
    public string? TableOperationsPath { get; set; }
    public string? DataMappingSummaryPath { get; set; }

    // Conteúdo do summary para exibição inline
    public string? SummaryContent { get; set; }
}
