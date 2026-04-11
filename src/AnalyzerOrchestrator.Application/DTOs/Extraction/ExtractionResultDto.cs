namespace AnalyzerOrchestrator.Application.DTOs.Extraction;

public class ExtractionResultDto
{
    public int PipelineRunId { get; set; }
    public int StepExecutionId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public int FilesFound { get; set; }
    public int FilesIgnored { get; set; }
    public int ErrorCount { get; set; }
    public int RelevantFilesCount { get; set; }

    public TimeSpan Duration { get; set; }

    public List<ScannedFileDto> AllFiles { get; set; } = new();
    public List<ScannedFileDto> RelevantFiles { get; set; } = new();
    public string StructureTree { get; set; } = string.Empty;

    // Caminhos dos artefatos gerados em disco
    public string? InventoryFilePath { get; set; }
    public string? TreeFilePath { get; set; }
    public string? RelevantFilesPath { get; set; }
    public string? SummaryFilePath { get; set; }
}
