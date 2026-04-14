using System.ComponentModel.DataAnnotations;
using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.DTOs.Extraction;

public class StepReviewDto
{
    public int StepExecutionId { get; set; }
    public int RunId { get; set; }

    [Required]
    public StepStatus Decision { get; set; } // Approved ou Rejected

    [MaxLength(200)]
    public string? ReviewedBy { get; set; }

    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }
}
