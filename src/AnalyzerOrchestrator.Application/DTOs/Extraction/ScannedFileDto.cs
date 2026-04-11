using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.DTOs.Extraction;

public class ScannedFileDto
{
    public int Id { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public FileRole Role { get; set; }
    public string RoleLabel => Role.ToString();
    public bool IsRelevant { get; set; }
    public int RelevanceScore { get; set; }
    public string? ClassificationNotes { get; set; }
}
