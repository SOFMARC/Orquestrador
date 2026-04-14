namespace AnalyzerOrchestrator.Application.DTOs.DataMapping;

/// <summary>
/// Relação entre uma tabela detectada e um arquivo onde ela foi referenciada.
/// </summary>
public class TableFileRelationDto
{
    public string RelativeFilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileRole { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public string PrimaryOperation { get; set; } = string.Empty;
    public List<string> Operations { get; set; } = new();
    public string? ContextSnippet { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
}
