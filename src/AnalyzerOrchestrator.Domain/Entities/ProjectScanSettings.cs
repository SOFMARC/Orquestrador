namespace AnalyzerOrchestrator.Domain.Entities;

/// <summary>
/// Configurações de leitura/varredura por projeto.
/// Relação 1:1 com Project.
/// </summary>
public class ProjectScanSettings : BaseEntity
{
    public int ProjectId { get; set; }

    /// <summary>
    /// Pasta raiz do código a ser analisado (pode ser diferente de RepositoryPath).
    /// </summary>
    public string? ScanRootPath { get; set; }

    /// <summary>
    /// Extensões permitidas separadas por vírgula. Ex: ".cs,.sql,.json,.cshtml"
    /// </summary>
    public string AllowedExtensions { get; set; } = ".cs,.sql,.json,.cshtml,.config,.xml,.md,.txt,.csproj,.sln";

    /// <summary>
    /// Pastas a ignorar separadas por vírgula. Ex: "bin,obj,.git,node_modules"
    /// </summary>
    public string IgnoredFolders { get; set; } = "bin,obj,.git,node_modules,packages,dist,.vs,wwwroot/lib";

    /// <summary>
    /// Limite de tamanho por arquivo em KB. Null = sem limite.
    /// </summary>
    public int? MaxFileSizeKb { get; set; } = 500;

    /// <summary>
    /// Se verdadeiro, arquivos binários são ignorados automaticamente.
    /// </summary>
    public bool IgnoreBinaryFiles { get; set; } = true;

    // Navegação
    public Project Project { get; set; } = null!;

    // Helpers
    public IEnumerable<string> GetAllowedExtensionsList() =>
        AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : $".{e.ToLowerInvariant()}");

    public IEnumerable<string> GetIgnoredFoldersList() =>
        IgnoredFolders.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(f => f.ToLowerInvariant());
}
