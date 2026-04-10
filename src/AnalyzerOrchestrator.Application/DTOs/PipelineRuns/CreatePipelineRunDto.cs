using System.ComponentModel.DataAnnotations;

namespace AnalyzerOrchestrator.Application.DTOs.PipelineRuns;

public class CreatePipelineRunDto
{
    [Required]
    public int ProjectId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? TriggerSource { get; set; }
}
