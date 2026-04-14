namespace AnalyzerOrchestrator.Application.DTOs.Consolidation;

/// <summary>
/// Representa um arquivo identificado como central para entendimento da arquitetura.
/// </summary>
public class CentralFileDto
{
    /// <summary>Caminho relativo a partir da raiz do scan.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>Nome do arquivo com extensão.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Papel classificado (FileRole).</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Score de relevância (0–100).</summary>
    public int RelevanceScore { get; set; }

    /// <summary>Motivo pelo qual o arquivo foi identificado como central.</summary>
    public string Reason { get; set; } = string.Empty;
}
