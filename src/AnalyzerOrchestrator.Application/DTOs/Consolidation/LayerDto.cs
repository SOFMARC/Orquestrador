namespace AnalyzerOrchestrator.Application.DTOs.Consolidation;

/// <summary>
/// Representa uma camada arquitetural detectada com sua distribuição de arquivos.
/// </summary>
public class LayerDto
{
    /// <summary>Nome da camada (Domain, Application, Infrastructure, Presentation, etc.).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Quantidade total de arquivos nesta camada.</summary>
    public int FileCount { get; set; }

    /// <summary>Distribuição de arquivos por papel dentro da camada.</summary>
    public Dictionary<string, int> Roles { get; set; } = new();
}
