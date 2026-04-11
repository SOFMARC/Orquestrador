using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Domain.Entities;

/// <summary>
/// Arquivo descoberto durante a varredura estrutural de um projeto.
/// Vinculado a uma PipelineRun específica.
/// </summary>
public class ScannedFile : BaseEntity
{
    public int PipelineRunId { get; set; }

    /// <summary>Caminho relativo a partir da pasta raiz de scan.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>Caminho absoluto no sistema de arquivos.</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>Nome do arquivo com extensão.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Extensão em minúsculo, ex: ".cs"</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>Tamanho em bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Papel classificado do arquivo.</summary>
    public FileRole Role { get; set; } = FileRole.Other;

    /// <summary>Se verdadeiro, o arquivo foi identificado como relevante.</summary>
    public bool IsRelevant { get; set; }

    /// <summary>Score de relevância (0–100) para ordenação.</summary>
    public int RelevanceScore { get; set; }

    /// <summary>Motivo da classificação ou da relevância.</summary>
    public string? ClassificationNotes { get; set; }

    // Navegação
    public PipelineRun PipelineRun { get; set; } = null!;
}
