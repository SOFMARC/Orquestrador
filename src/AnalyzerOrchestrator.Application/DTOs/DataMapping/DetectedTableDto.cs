namespace AnalyzerOrchestrator.Application.DTOs.DataMapping;

/// <summary>
/// Tabela ou estrutura de dados detectada por heurística.
/// </summary>
public class DetectedTableDto
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>Nível de confiança da detecção (0–100).</summary>
    public int ConfidenceScore { get; set; }

    /// <summary>Rótulo de confiança para exibição.</summary>
    public string ConfidenceLabel => ConfidenceScore switch
    {
        >= 80 => "Alta",
        >= 50 => "Média",
        >= 25 => "Baixa",
        _ => "Muito Baixa"
    };

    /// <summary>Classe Bootstrap para a badge de confiança.</summary>
    public string ConfidenceBadgeClass => ConfidenceScore switch
    {
        >= 80 => "success",
        >= 50 => "warning",
        >= 25 => "secondary",
        _ => "danger"
    };

    public string EvidenceType { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int OccurrenceCount { get; set; }
    public List<string> Operations { get; set; } = new();
    public string? Notes { get; set; }
    public List<TableFileRelationDto> FileRelations { get; set; } = new();
}
