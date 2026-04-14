namespace AnalyzerOrchestrator.Domain.Entities;

/// <summary>
/// Tabela ou estrutura de dados detectada por heurística durante a Etapa 3 (Mapeamento de Dados).
/// Vinculada a uma PipelineRun específica.
/// </summary>
public class DetectedTable : BaseEntity
{
    public int PipelineRunId { get; set; }

    /// <summary>Nome normalizado da tabela (ex: "Users", "OrderItems").</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Nome original como encontrado no código (pode diferir do normalizado).</summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// Nível de confiança da detecção (0–100).
    /// 80+ = alta confiança (SQL explícito), 50–79 = média (convenção de nome), &lt;50 = baixa.
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>Tipo de indício que originou a detecção.</summary>
    public string EvidenceType { get; set; } = string.Empty;

    /// <summary>Quantidade de arquivos onde a tabela foi referenciada.</summary>
    public int FileCount { get; set; }

    /// <summary>Quantidade total de ocorrências em todos os arquivos.</summary>
    public int OccurrenceCount { get; set; }

    /// <summary>Operações detectadas (serializado como JSON para simplicidade).</summary>
    public string OperationsJson { get; set; } = "[]";

    /// <summary>Observações automáticas sobre a tabela.</summary>
    public string? Notes { get; set; }

    // Navegação
    public PipelineRun PipelineRun { get; set; } = null!;
    public ICollection<TableFileRelation> FileRelations { get; set; } = new List<TableFileRelation>();
}
