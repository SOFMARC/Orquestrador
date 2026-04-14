using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Domain.Entities;

/// <summary>
/// Relação entre uma tabela detectada e um arquivo escaneado.
/// Registra onde a tabela aparece, com que frequência e qual operação foi percebida.
/// </summary>
public class TableFileRelation : BaseEntity
{
    public int DetectedTableId { get; set; }
    public int PipelineRunId { get; set; }

    /// <summary>Caminho relativo do arquivo.</summary>
    public string RelativeFilePath { get; set; } = string.Empty;

    /// <summary>Nome do arquivo.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Papel classificado do arquivo (FileRole).</summary>
    public string FileRole { get; set; } = string.Empty;

    /// <summary>Extensão do arquivo.</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>Quantidade de ocorrências da tabela neste arquivo.</summary>
    public int OccurrenceCount { get; set; }

    /// <summary>Operação principal percebida neste arquivo para esta tabela.</summary>
    public DataOperation PrimaryOperation { get; set; } = DataOperation.Unknown;

    /// <summary>Todas as operações percebidas (serializado como JSON).</summary>
    public string OperationsJson { get; set; } = "[]";

    /// <summary>Trecho de contexto onde a tabela foi encontrada (até 500 chars).</summary>
    public string? ContextSnippet { get; set; }

    /// <summary>Tipo de indício que originou esta relação.</summary>
    public string EvidenceType { get; set; } = string.Empty;

    // Navegação
    public DetectedTable DetectedTable { get; set; } = null!;
}
