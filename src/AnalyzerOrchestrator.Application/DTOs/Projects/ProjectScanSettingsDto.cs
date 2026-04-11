using System.ComponentModel.DataAnnotations;

namespace AnalyzerOrchestrator.Application.DTOs.Projects;

public class ProjectScanSettingsDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? ScanRootPath { get; set; }
    public string AllowedExtensions { get; set; } = string.Empty;
    public string IgnoredFolders { get; set; } = string.Empty;
    public int? MaxFileSizeKb { get; set; }
    public bool IgnoreBinaryFiles { get; set; }
}

public class SaveScanSettingsDto
{
    public int ProjectId { get; set; }

    [MaxLength(500)]
    public string? ScanRootPath { get; set; }

    [Required, MaxLength(500)]
    public string AllowedExtensions { get; set; } = ".cs,.sql,.json,.cshtml,.config,.xml,.md,.txt,.csproj,.sln";

    [Required, MaxLength(1000)]
    public string IgnoredFolders { get; set; } = "bin,obj,.git,node_modules,packages,dist,.vs,wwwroot/lib";

    public int? MaxFileSizeKb { get; set; } = 500;

    public bool IgnoreBinaryFiles { get; set; } = true;
}
