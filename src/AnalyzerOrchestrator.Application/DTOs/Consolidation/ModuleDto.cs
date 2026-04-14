namespace AnalyzerOrchestrator.Application.DTOs.Consolidation;

/// <summary>
/// Representa um módulo/pasta de primeiro nível detectado na consolidação arquitetural.
/// </summary>
public class ModuleDto
{
    /// <summary>Nome do módulo (pasta de primeiro nível).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Quantidade total de arquivos no módulo.</summary>
    public int FileCount { get; set; }

    /// <summary>Camada arquitetural detectada por heurística.</summary>
    public string DetectedLayer { get; set; } = string.Empty;

    /// <summary>Distribuição de arquivos por papel (FileRole).</summary>
    public Dictionary<string, int> FilesByRole { get; set; } = new();

    /// <summary>Caminhos relativos dos arquivos com maior score de relevância.</summary>
    public List<string> TopFiles { get; set; } = new();

    /// <summary>Observação automática sobre o módulo.</summary>
    public string Observations { get; set; } = string.Empty;
}
