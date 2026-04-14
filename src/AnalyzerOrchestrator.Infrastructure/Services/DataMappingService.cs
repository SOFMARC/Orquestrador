using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnalyzerOrchestrator.Application.DTOs.DataMapping;
using AnalyzerOrchestrator.Application.DTOs.Extraction;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Application.Workflow;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Domain.Enums;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzerOrchestrator.Infrastructure.Services;

/// <summary>
/// Implementa a Etapa 3 do workflow: Mapeamento Inicial de Dados.
/// Detecta tabelas e estruturas de dados por heurística a partir dos
/// arquivos escaneados, gerando mapeamentos tabela→arquivos e arquivo→tabelas.
/// </summary>
public class DataMappingService : IDataMappingService
{
    private readonly OrchestratorDbContext _context;
    private readonly ILogger<DataMappingService> _logger;

    // ── Extensões analisadas ─────────────────────────────────────────────────────
    private static readonly HashSet<string> AnalyzableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".sql", ".json", ".xml", ".config", ".yaml", ".yml",
        ".ts", ".js", ".py", ".java", ".go", ".rb", ".php"
    };

    // ── Padrões SQL explícitos ───────────────────────────────────────────────────
    // Cada padrão captura o nome da tabela no grupo 1
    private static readonly (Regex Pattern, DataOperation Operation, string EvidenceType, int ConfidenceBonus)[] SqlPatterns =
    {
        (new Regex(@"\bFROM\s+[`\[""']?(\w+)[`\]""']?\b",                   RegexOptions.IgnoreCase | RegexOptions.Compiled), DataOperation.Read,        "SQL-FROM",          30),
        (new Regex(@"\bJOIN\s+[`\[""']?(\w+)[`\]""']?\b",                   RegexOptions.IgnoreCase | RegexOptions.Compiled), DataOperation.Join,         "SQL-JOIN",          28),
        (new Regex(@"\bINSERT\s+INTO\s+[`\[""']?(\w+)[`\]""']?\b",          RegexOptions.IgnoreCase | RegexOptions.Compiled), DataOperation.Insert,       "SQL-INSERT",        35),
        (new Regex(@"\bUPDATE\s+[`\[""']?(\w+)[`\]""']?\s+SET\b",           RegexOptions.IgnoreCase | RegexOptions.Compiled), DataOperation.Update,       "SQL-UPDATE",        35),
        (new Regex(@"\bDELETE\s+FROM\s+[`\[""']?(\w+)[`\]""']?\b",         RegexOptions.IgnoreCase | RegexOptions.Compiled), DataOperation.Delete,       "SQL-DELETE",        35),
        (new Regex(@"\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?[`\[""']?(\w+)[`\]""']?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), DataOperation.CreateTable, "SQL-CREATE",  40),
        (new Regex(@"\bALTER\s+TABLE\s+[`\[""']?(\w+)[`\]""']?\b",         RegexOptions.IgnoreCase | RegexOptions.Compiled), DataOperation.AlterTable,   "SQL-ALTER",         38),
        (new Regex(@"\bSELECT\b.+?\bFROM\s+[`\[""']?(\w+)[`\]""']?\b",    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline), DataOperation.Read, "SQL-SELECT", 32),
    };

    // ── Padrões para C# / ORM ────────────────────────────────────────────────────
    private static readonly (Regex Pattern, DataOperation Operation, string EvidenceType, int ConfidenceBonus)[] CSharpPatterns =
    {
        // EF Core: DbSet<Entity>, Set<Entity>()
        (new Regex(@"\bDbSet\s*<\s*(\w+)\s*>",                              RegexOptions.Compiled), DataOperation.Reference,  "EF-DbSet",          40),
        (new Regex(@"\bSet\s*<\s*(\w+)\s*>\s*\(",                           RegexOptions.Compiled), DataOperation.Reference,  "EF-Set",            30),
        // Repository pattern: IRepository<Entity>, Repository<Entity>
        (new Regex(@"\bI?Repository\s*<\s*(\w+)\s*>",                       RegexOptions.Compiled), DataOperation.Reference,  "Repository",        25),
        // Dapper: Query<Entity>, QueryAsync<Entity>
        (new Regex(@"\bQuery(?:Async)?\s*<\s*(\w+)\s*>",                    RegexOptions.Compiled), DataOperation.Read,       "Dapper-Query",      28),
        // ToTable("tableName") — Fluent API
        (new Regex(@"\.ToTable\s*\(\s*""(\w+)""",                           RegexOptions.Compiled), DataOperation.Reference,  "EF-ToTable",        45),
        // [Table("tableName")] attribute
        (new Regex(@"\[Table\s*\(\s*""(\w+)""",                             RegexOptions.Compiled), DataOperation.Reference,  "EF-TableAttr",      45),
        // HasColumnName, HasForeignKey patterns (lower confidence)
        (new Regex(@"\.HasForeignKey\s*\(\s*""(\w+)""",                     RegexOptions.Compiled), DataOperation.Reference,  "EF-ForeignKey",     15),
        // string SQL inline: "SELECT ... FROM tableName"
        (new Regex(@"""[^""]*\bFROM\s+(\w+)\b[^""]*""",                     RegexOptions.Compiled), DataOperation.Read,       "InlineSQL",         20),
        (new Regex(@"""[^""]*\bINSERT\s+INTO\s+(\w+)\b[^""]*""",            RegexOptions.Compiled), DataOperation.Insert,     "InlineSQL",         22),
        (new Regex(@"""[^""]*\bUPDATE\s+(\w+)\b[^""]*""",                   RegexOptions.Compiled), DataOperation.Update,     "InlineSQL",         20),
    };

    // ── Palavras reservadas SQL a ignorar como nomes de tabela ──────────────────
    private static readonly HashSet<string> SqlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT","FROM","WHERE","JOIN","ON","AND","OR","NOT","IN","IS","NULL",
        "AS","BY","GROUP","ORDER","HAVING","LIMIT","OFFSET","UNION","ALL",
        "DISTINCT","TOP","INTO","VALUES","SET","TABLE","INDEX","VIEW","TRIGGER",
        "PROCEDURE","FUNCTION","DATABASE","SCHEMA","WITH","CASE","WHEN","THEN",
        "ELSE","END","BEGIN","COMMIT","ROLLBACK","TRANSACTION","EXEC","EXECUTE",
        "INNER","LEFT","RIGHT","FULL","OUTER","CROSS","NATURAL","USING",
        "EXISTS","BETWEEN","LIKE","ILIKE","SIMILAR","CAST","CONVERT",
        "VARCHAR","NVARCHAR","INT","BIGINT","DECIMAL","FLOAT","DATETIME","DATE",
        "BIT","CHAR","TEXT","BLOB","BOOLEAN","BOOL","IDENTITY","PRIMARY","KEY",
        "FOREIGN","REFERENCES","CONSTRAINT","UNIQUE","CHECK","DEFAULT","NOT",
        "NULL","AUTO_INCREMENT","SERIAL","SEQUENCE","NEXTVAL","CURRVAL",
        "NOLOCK","NOCHECK","ROWLOCK","UPDLOCK","TABLOCK","HOLDLOCK",
        "dbo","sys","information_schema","pg_catalog","public",
        "var","new","return","class","interface","namespace","using","public",
        "private","protected","static","void","async","await","Task","List",
        "IEnumerable","string","int","bool","object","base","this","override",
        "virtual","abstract","sealed","partial","readonly","const","event",
        "delegate","enum","struct","record","init","get","set","value",
        "true","false","null","if","else","for","foreach","while","do","switch",
        "case","break","continue","throw","try","catch","finally","lock","yield",
    };

    public DataMappingService(
        OrchestratorDbContext context,
        ILogger<DataMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Execução principal ───────────────────────────────────────────────────────

    public async Task<DataMappingResultDto> ExecuteAsync(int pipelineRunId, CancellationToken ct = default)
    {
        var started = DateTime.UtcNow;

        var run = await _context.PipelineRuns
            .Include(r => r.Project)
            .Include(r => r.StepExecutions)
            .Include(r => r.ScannedFiles)
            .Include(r => r.Artifacts)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId, ct)
            ?? throw new InvalidOperationException($"Run {pipelineRunId} não encontrada.");

        // Pré-condição: Etapa 1 deve estar aprovada
        var step1 = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepStructuralExtraction);
        if (step1 is null || step1.Status != StepStatus.Approved)
        {
            return new DataMappingResultDto
            {
                PipelineRunId = pipelineRunId,
                Success = false,
                ErrorMessage = "A Etapa 1 (Extração Estrutural) precisa estar aprovada antes de executar o Mapeamento de Dados."
            };
        }

        var step3 = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepDataMapping)
            ?? throw new InvalidOperationException("Etapa de Mapeamento de Dados não encontrada na run.");

        // Marcar como Running
        step3.Status = StepStatus.Running;
        step3.StartedAt = DateTime.UtcNow;
        run.CurrentStep = step3.StepName;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        var result = new DataMappingResultDto
        {
            PipelineRunId = pipelineRunId,
            StepExecutionId = step3.Id
        };

        try
        {
            var scannedFiles = run.ScannedFiles.ToList();
            if (scannedFiles.Count == 0)
                throw new InvalidOperationException("Nenhum arquivo escaneado encontrado. Execute e aprove a Etapa 1 primeiro.");

            _logger.LogInformation("Iniciando mapeamento de dados da run {RunId} com {Count} arquivos", pipelineRunId, scannedFiles.Count);

            // Remover detecções anteriores desta run (re-execução)
            var oldTables = await _context.DetectedTables
                .Where(t => t.PipelineRunId == pipelineRunId)
                .ToListAsync(ct);
            _context.DetectedTables.RemoveRange(oldTables);
            await _context.SaveChangesAsync(ct);

            // ── Detecção ─────────────────────────────────────────────────────────
            var tableMap = new Dictionary<string, TableAccumulator>(StringComparer.OrdinalIgnoreCase);
            int filesAnalyzed = 0;
            int errors = 0;

            foreach (var sf in scannedFiles)
            {
                if (!AnalyzableExtensions.Contains(sf.Extension)) continue;
                if (!File.Exists(sf.FullPath)) continue;

                try
                {
                    var content = await File.ReadAllTextAsync(sf.FullPath, ct);
                    if (string.IsNullOrWhiteSpace(content)) continue;

                    filesAnalyzed++;
                    AnalyzeFile(sf, content, tableMap);
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning("Erro ao analisar {File}: {Msg}", sf.FullPath, ex.Message);
                }
            }

            // ── Filtrar e persistir tabelas ───────────────────────────────────────
            var detectedTables = new List<DetectedTable>();
            foreach (var (name, acc) in tableMap.OrderByDescending(x => x.Value.ConfidenceScore))
            {
                if (acc.ConfidenceScore < 15) continue; // descarta ruído

                var entity = new DetectedTable
                {
                    PipelineRunId = pipelineRunId,
                    TableName = NormalizeTableName(name),
                    OriginalName = name,
                    ConfidenceScore = Math.Min(acc.ConfidenceScore, 100),
                    EvidenceType = acc.PrimaryEvidenceType,
                    FileCount = acc.FileOccurrences.Count,
                    OccurrenceCount = acc.TotalOccurrences,
                    OperationsJson = JsonSerializer.Serialize(acc.Operations.Distinct().Select(o => o.ToString()).ToList()),
                    Notes = BuildTableNotes(acc),
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var (filePath, fileAcc) in acc.FileOccurrences)
                {
                    entity.FileRelations.Add(new TableFileRelation
                    {
                        PipelineRunId = pipelineRunId,
                        RelativeFilePath = fileAcc.RelativePath,
                        FileName = fileAcc.FileName,
                        FileRole = fileAcc.Role,
                        Extension = fileAcc.Extension,
                        OccurrenceCount = fileAcc.Count,
                        PrimaryOperation = fileAcc.PrimaryOperation,
                        OperationsJson = JsonSerializer.Serialize(fileAcc.Operations.Distinct().Select(o => o.ToString()).ToList()),
                        ContextSnippet = fileAcc.ContextSnippet,
                        EvidenceType = fileAcc.EvidenceType,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _context.DetectedTables.Add(entity);
                detectedTables.Add(entity);
            }

            await _context.SaveChangesAsync(ct);

            // ── Artefatos ────────────────────────────────────────────────────────
            var workspacePath = GetWorkspacePath(run.Project.Name, pipelineRunId);
            Directory.CreateDirectory(workspacePath);

            var detectedTablesPath   = await WriteDetectedTablesAsync(detectedTables, workspacePath, ct);
            var tableFileRelPath     = await WriteTableFileRelationsAsync(detectedTables, workspacePath, ct);
            var fileTableRelPath     = await WriteFileTableRelationsAsync(detectedTables, scannedFiles, workspacePath, ct);
            var tableOpsPath         = await WriteTableOperationsAsync(detectedTables, workspacePath, ct);
            var summaryPath          = await WriteDataMappingSummaryAsync(run.Project.Name, detectedTables, filesAnalyzed, errors, DateTime.UtcNow - started, workspacePath, ct);

            await PersistArtifactAsync(pipelineRunId, "detected-tables.json",      ArtifactType.DetectedTables,     detectedTablesPath, "application/json", ct);
            await PersistArtifactAsync(pipelineRunId, "table-file-relations.json", ArtifactType.TableFileRelations, tableFileRelPath,   "application/json", ct);
            await PersistArtifactAsync(pipelineRunId, "file-table-relations.json", ArtifactType.FileTableRelations, fileTableRelPath,   "application/json", ct);
            await PersistArtifactAsync(pipelineRunId, "table-operations.json",     ArtifactType.TableOperations,    tableOpsPath,       "application/json", ct);
            await PersistArtifactAsync(pipelineRunId, "data-mapping-summary.md",   ArtifactType.DataMappingSummary, summaryPath,        "text/markdown",    ct);

            // Atualizar step com métricas explícitas (sem depender de parsing de Notes)
            step3.Status = StepStatus.AwaitingReview;
            step3.FinishedAt = DateTime.UtcNow;
            step3.FilesFound = filesAnalyzed;
            step3.ErrorCount = errors;
            step3.TablesCount = detectedTables.Count;
            step3.RelationsCount = detectedTables.Sum(t => t.FileRelations.Count);
            step3.Notes = $"Mapeamento concluído. {detectedTables.Count} tabelas detectadas em {filesAnalyzed} arquivos.";
            run.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            // Montar resultado
            result.Success = true;
            result.TablesCount = detectedTables.Count;
            result.FilesAnalyzed = filesAnalyzed;
            result.RelationsCount = detectedTables.Sum(t => t.FileRelations.Count);
            result.ErrorCount = errors;
            result.Duration = DateTime.UtcNow - started;
            result.Tables = MapTablesToDto(detectedTables);
            result.DetectedTablesPath = detectedTablesPath;
            result.TableFileRelationsPath = tableFileRelPath;
            result.FileTableRelationsPath = fileTableRelPath;
            result.TableOperationsPath = tableOpsPath;
            result.DataMappingSummaryPath = summaryPath;
            result.SummaryContent = await File.ReadAllTextAsync(summaryPath, ct);

            _logger.LogInformation("Mapeamento de dados concluído em {Duration}ms. {Tables} tabelas, {Files} arquivos.",
                result.Duration.TotalMilliseconds, result.TablesCount, result.FilesAnalyzed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no mapeamento de dados da run {RunId}", pipelineRunId);
            step3.Status = StepStatus.Failed;
            step3.FinishedAt = DateTime.UtcNow;
            step3.ErrorMessage = ex.Message;
            run.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(CancellationToken.None);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task ReviewStepAsync(StepReviewDto dto)
    {
        var step = await _context.PipelineStepExecutions
            .Include(s => s.PipelineRun)
            .FirstOrDefaultAsync(s => s.Id == dto.StepExecutionId)
            ?? throw new InvalidOperationException($"Etapa {dto.StepExecutionId} não encontrada.");

        if (dto.Decision != StepStatus.Approved && dto.Decision != StepStatus.Rejected)
            throw new ArgumentException("Decisão deve ser Approved ou Rejected.");

        step.Status = dto.Decision;
        step.ReviewedAt = DateTime.UtcNow;
        step.ReviewedBy = dto.ReviewedBy?.Trim();
        step.ReviewNotes = dto.ReviewNotes?.Trim();
        step.UpdatedAt = DateTime.UtcNow;
        step.PipelineRun.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Etapa {StepId} revisada: {Decision} por {Reviewer}",
            dto.StepExecutionId, dto.Decision, dto.ReviewedBy ?? "anônimo");
    }

    public async Task<DataMappingResultDto?> GetResultAsync(int pipelineRunId)
    {
        var run = await _context.PipelineRuns
            .Include(r => r.Project)
            .Include(r => r.StepExecutions)
            .Include(r => r.Artifacts)
            .Include(r => r.DetectedTables)
                .ThenInclude(t => t.FileRelations)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId);

        if (run is null) return null;

        var step3 = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepDataMapping);
        if (step3 is null || step3.Status == StepStatus.Pending) return null;

        var summaryPath = run.Artifacts
            .FirstOrDefault(a => a.Type == ArtifactType.DataMappingSummary)?.FilePath;

        var tables = run.DetectedTables.ToList();

        var result = new DataMappingResultDto
        {
            PipelineRunId = pipelineRunId,
            StepExecutionId = step3.Id,
            Success = step3.Status != StepStatus.Failed,
            ErrorMessage = step3.ErrorMessage,
            TablesCount = tables.Count,
            FilesAnalyzed = step3.FilesFound ?? 0,
            RelationsCount = tables.Sum(t => t.FileRelations.Count),
            ErrorCount = step3.ErrorCount ?? 0,
            Tables = MapTablesToDto(tables),
            DetectedTablesPath    = run.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.DetectedTables)?.FilePath,
            TableFileRelationsPath = run.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.TableFileRelations)?.FilePath,
            FileTableRelationsPath = run.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.FileTableRelations)?.FilePath,
            TableOperationsPath   = run.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.TableOperations)?.FilePath,
            DataMappingSummaryPath = summaryPath
        };

        if (summaryPath != null && File.Exists(summaryPath))
            result.SummaryContent = await File.ReadAllTextAsync(summaryPath);

        return result;
    }

    // ── Análise de arquivo ───────────────────────────────────────────────────────

    private void AnalyzeFile(ScannedFile sf, string content, Dictionary<string, TableAccumulator> tableMap)
    {
        var patterns = sf.Extension.Equals(".sql", StringComparison.OrdinalIgnoreCase)
            ? SqlPatterns
            : sf.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
                ? CSharpPatterns.Concat(SqlPatterns).ToArray()
                : SqlPatterns;

        foreach (var (pattern, operation, evidenceType, confidenceBonus) in patterns)
        {
            foreach (Match match in pattern.Matches(content))
            {
                var rawName = match.Groups[1].Value.Trim();
                if (!IsValidTableName(rawName)) continue;

                var normalized = NormalizeTableName(rawName);
                if (!tableMap.TryGetValue(normalized, out var acc))
                {
                    acc = new TableAccumulator(normalized);
                    tableMap[normalized] = acc;
                }

                // Extrair snippet de contexto
                var start = Math.Max(0, match.Index - 60);
                var len = Math.Min(200, content.Length - start);
                var snippet = content.Substring(start, len).Replace('\n', ' ').Replace('\r', ' ').Trim();

                acc.AddOccurrence(sf, operation, evidenceType, confidenceBonus, snippet);
            }
        }
    }

    // ── Helpers de validação ─────────────────────────────────────────────────────

    private static bool IsValidTableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length < 2 || name.Length > 128) return false;
        if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$")) return false;
        if (SqlKeywords.Contains(name)) return false;
        // Ignorar nomes genéricos de tipos primitivos e palavras comuns
        if (name.StartsWith("I", StringComparison.Ordinal) && char.IsUpper(name[1])) return false; // interfaces C#
        return true;
    }

    private static string NormalizeTableName(string name)
    {
        // Normaliza para PascalCase sem prefixos comuns
        var n = name.Trim('_');
        if (n.StartsWith("tbl", StringComparison.OrdinalIgnoreCase) && n.Length > 3)
            n = n[3..];
        if (n.StartsWith("tb_", StringComparison.OrdinalIgnoreCase) && n.Length > 3)
            n = n[3..];
        // Preservar capitalização original
        return n.Length > 0 ? char.ToUpper(n[0]) + n[1..] : n;
    }

    private static string BuildTableNotes(TableAccumulator acc)
    {
        var ops = acc.Operations.Distinct().ToList();
        var parts = new List<string>();
        if (ops.Contains(DataOperation.CreateTable)) parts.Add("criação detectada");
        if (ops.Contains(DataOperation.Insert)) parts.Add("inserções");
        if (ops.Contains(DataOperation.Update)) parts.Add("atualizações");
        if (ops.Contains(DataOperation.Delete)) parts.Add("exclusões");
        if (ops.Contains(DataOperation.Read) || ops.Contains(DataOperation.Join)) parts.Add("leituras/joins");
        return parts.Count > 0 ? string.Join(", ", parts) : "referenciada";
    }

    // ── Geração de artefatos ─────────────────────────────────────────────────────

    private static async Task<string> WriteDetectedTablesAsync(List<DetectedTable> tables, string workspacePath, CancellationToken ct)
    {
        var path = Path.Combine(workspacePath, "detected-tables.json");
        var data = tables.OrderByDescending(t => t.ConfidenceScore).Select(t => new
        {
            tableName = t.TableName,
            originalName = t.OriginalName,
            confidenceScore = t.ConfidenceScore,
            evidenceType = t.EvidenceType,
            fileCount = t.FileCount,
            occurrenceCount = t.OccurrenceCount,
            operations = JsonSerializer.Deserialize<List<string>>(t.OperationsJson),
            notes = t.Notes
        });
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }), ct);
        return path;
    }

    private static async Task<string> WriteTableFileRelationsAsync(List<DetectedTable> tables, string workspacePath, CancellationToken ct)
    {
        var path = Path.Combine(workspacePath, "table-file-relations.json");
        var data = tables.OrderByDescending(t => t.ConfidenceScore).Select(t => new
        {
            tableName = t.TableName,
            confidenceScore = t.ConfidenceScore,
            files = t.FileRelations.OrderByDescending(r => r.OccurrenceCount).Select(r => new
            {
                file = r.RelativeFilePath,
                role = r.FileRole,
                occurrences = r.OccurrenceCount,
                primaryOperation = r.PrimaryOperation.ToString(),
                evidenceType = r.EvidenceType,
                context = r.ContextSnippet
            })
        });
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }), ct);
        return path;
    }

    private static async Task<string> WriteFileTableRelationsAsync(List<DetectedTable> tables, List<ScannedFile> scannedFiles, string workspacePath, CancellationToken ct)
    {
        var path = Path.Combine(workspacePath, "file-table-relations.json");

        // Inverter: arquivo → tabelas
        var fileMap = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
        {
            foreach (var rel in table.FileRelations)
            {
                if (!fileMap.TryGetValue(rel.RelativeFilePath, out var list))
                {
                    list = new List<object>();
                    fileMap[rel.RelativeFilePath] = list;
                }
                list.Add(new
                {
                    tableName = table.TableName,
                    occurrences = rel.OccurrenceCount,
                    primaryOperation = rel.PrimaryOperation.ToString(),
                    evidenceType = rel.EvidenceType
                });
            }
        }

        var data = fileMap.OrderBy(kv => kv.Key).Select(kv => new
        {
            file = kv.Key,
            tables = kv.Value
        });
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }), ct);
        return path;
    }

    private static async Task<string> WriteTableOperationsAsync(List<DetectedTable> tables, string workspacePath, CancellationToken ct)
    {
        var path = Path.Combine(workspacePath, "table-operations.json");
        var data = tables.OrderByDescending(t => t.ConfidenceScore).Select(t => new
        {
            tableName = t.TableName,
            operations = t.FileRelations
                .SelectMany(r => JsonSerializer.Deserialize<List<string>>(r.OperationsJson) ?? new List<string>())
                .Distinct()
                .OrderBy(o => o)
                .ToList(),
            byFile = t.FileRelations.OrderByDescending(r => r.OccurrenceCount).Select(r => new
            {
                file = r.RelativeFilePath,
                operation = r.PrimaryOperation.ToString(),
                occurrences = r.OccurrenceCount
            })
        });
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }), ct);
        return path;
    }

    private static async Task<string> WriteDataMappingSummaryAsync(
        string projectName, List<DetectedTable> tables, int filesAnalyzed, int errors,
        TimeSpan duration, string workspacePath, CancellationToken ct)
    {
        var path = Path.Combine(workspacePath, "data-mapping-summary.md");
        var sb = new StringBuilder();

        sb.AppendLine($"# Resumo do Mapeamento de Dados — {projectName}");
        sb.AppendLine($"**Data:** {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"**Duração:** {duration.TotalSeconds:F1}s");
        sb.AppendLine();

        sb.AppendLine("## Métricas");
        sb.AppendLine("| Métrica | Valor |");
        sb.AppendLine("|---------|-------|");
        sb.AppendLine($"| Arquivos analisados | {filesAnalyzed} |");
        sb.AppendLine($"| Tabelas detectadas | {tables.Count} |");
        sb.AppendLine($"| Relações arquivo-tabela | {tables.Sum(t => t.FileRelations.Count)} |");
        sb.AppendLine($"| Erros de leitura | {errors} |");
        sb.AppendLine();

        if (tables.Any())
        {
            sb.AppendLine("## Tabelas Detectadas");
            sb.AppendLine("| Tabela | Confiança | Arquivos | Ocorrências | Operações |");
            sb.AppendLine("|--------|-----------|----------|-------------|-----------|");
            foreach (var t in tables.OrderByDescending(x => x.ConfidenceScore).Take(20))
            {
                var ops = JsonSerializer.Deserialize<List<string>>(t.OperationsJson) ?? new();
                sb.AppendLine($"| {t.TableName} | {t.ConfidenceScore}% | {t.FileCount} | {t.OccurrenceCount} | {string.Join(", ", ops)} |");
            }
            if (tables.Count > 20)
                sb.AppendLine($"\n*... e mais {tables.Count - 20} tabelas.*");
            sb.AppendLine();

            // Alta confiança
            var highConf = tables.Where(t => t.ConfidenceScore >= 70).ToList();
            if (highConf.Any())
            {
                sb.AppendLine("## Tabelas de Alta Confiança (≥70%)");
                foreach (var t in highConf)
                    sb.AppendLine($"- **{t.TableName}** ({t.ConfidenceScore}%) — {t.Notes}");
                sb.AppendLine();
            }

            // Arquivos com mais relações
            var topFiles = tables
                .SelectMany(t => t.FileRelations)
                .GroupBy(r => r.RelativeFilePath)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToList();

            if (topFiles.Any())
            {
                sb.AppendLine("## Arquivos com Mais Referências a Tabelas");
                foreach (var g in topFiles)
                    sb.AppendLine($"- `{g.Key}` — {g.Count()} tabela(s)");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("## Resultado");
            sb.AppendLine("Nenhuma tabela foi detectada com confiança suficiente nos arquivos analisados.");
            sb.AppendLine();
            sb.AppendLine("Possíveis causas:");
            sb.AppendLine("- O projeto não contém arquivos `.sql`, `.cs` ou outros formatos analisados");
            sb.AppendLine("- O acesso a dados é feito por mecanismos não cobertos pelas heurísticas atuais");
            sb.AppendLine("- Os arquivos relevantes estão em pastas excluídas pela configuração de scan");
        }

        await File.WriteAllTextAsync(path, sb.ToString(), ct);
        return path;
    }

    // ── Persistência de artefatos ────────────────────────────────────────────────

    private async Task PersistArtifactAsync(int runId, string name, ArtifactType type, string filePath, string mimeType, CancellationToken ct)
    {
        // Remove artefato anterior do mesmo tipo nesta run
        var existing = await _context.Artifacts
            .FirstOrDefaultAsync(a => a.PipelineRunId == runId && a.Type == type, ct);
        if (existing != null) _context.Artifacts.Remove(existing);

        var fileInfo = new FileInfo(filePath);
        _context.Artifacts.Add(new Artifact
        {
            PipelineRunId = runId,
            StepNumber = DefaultAnalysisWorkflow.StepDataMapping,
            Name = name,
            Type = type,
            FilePath = filePath,
            MimeType = mimeType,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(ct);
    }

    // ── Workspace ────────────────────────────────────────────────────────────────

    private static string GetWorkspacePath(string projectName, int runId)
    {
        var safe = string.Concat(projectName.Split(Path.GetInvalidFileNameChars()));
        var basePath = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnalyzerOrchestrator", "workspace")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".analyzer-orchestrator", "workspace");
        return Path.Combine(basePath, safe, "runs", $"run_{runId}", "step_3");
    }

    // ── Mapeamento para DTOs ─────────────────────────────────────────────────────

    private static List<DetectedTableDto> MapTablesToDto(List<DetectedTable> tables)
        => tables.OrderByDescending(t => t.ConfidenceScore).Select(t => new DetectedTableDto
        {
            Id = t.Id,
            TableName = t.TableName,
            OriginalName = t.OriginalName,
            ConfidenceScore = t.ConfidenceScore,
            EvidenceType = t.EvidenceType,
            FileCount = t.FileCount,
            OccurrenceCount = t.OccurrenceCount,
            Operations = JsonSerializer.Deserialize<List<string>>(t.OperationsJson) ?? new(),
            Notes = t.Notes,
            FileRelations = t.FileRelations.OrderByDescending(r => r.OccurrenceCount).Select(r => new TableFileRelationDto
            {
                RelativeFilePath = r.RelativeFilePath,
                FileName = r.FileName,
                FileRole = r.FileRole,
                Extension = r.Extension,
                OccurrenceCount = r.OccurrenceCount,
                PrimaryOperation = r.PrimaryOperation.ToString(),
                Operations = JsonSerializer.Deserialize<List<string>>(r.OperationsJson) ?? new(),
                ContextSnippet = r.ContextSnippet,
                EvidenceType = r.EvidenceType
            }).ToList()
        }).ToList();
}

// ── Acumulador interno (não exposto) ────────────────────────────────────────────

internal class TableAccumulator
{
    public string Name { get; }
    public int ConfidenceScore { get; private set; }
    public int TotalOccurrences { get; private set; }
    public string PrimaryEvidenceType { get; private set; } = string.Empty;
    public List<DataOperation> Operations { get; } = new();
    public Dictionary<string, FileAccumulator> FileOccurrences { get; } = new(StringComparer.OrdinalIgnoreCase);

    public TableAccumulator(string name) => Name = name;

    public void AddOccurrence(ScannedFile sf, DataOperation op, string evidenceType, int bonus, string snippet)
    {
        TotalOccurrences++;
        Operations.Add(op);

        if (bonus > 0)
        {
            ConfidenceScore += bonus;
            if (string.IsNullOrEmpty(PrimaryEvidenceType) || bonus > GetBonusForEvidence(PrimaryEvidenceType))
                PrimaryEvidenceType = evidenceType;
        }

        if (!FileOccurrences.TryGetValue(sf.RelativePath, out var fa))
        {
            fa = new FileAccumulator
            {
                RelativePath = sf.RelativePath,
                FileName = sf.FileName,
                Role = sf.Role.ToString(),
                Extension = sf.Extension,
                EvidenceType = evidenceType,
                ContextSnippet = snippet.Length > 500 ? snippet[..500] : snippet
            };
            FileOccurrences[sf.RelativePath] = fa;
        }

        fa.Count++;
        fa.Operations.Add(op);
        if (op > fa.PrimaryOperation) fa.PrimaryOperation = op;
    }

    private static int GetBonusForEvidence(string ev) => ev switch
    {
        "SQL-CREATE" => 40, "EF-DbSet" => 40, "EF-ToTable" => 45, "EF-TableAttr" => 45,
        "SQL-INSERT" => 35, "SQL-UPDATE" => 35, "SQL-DELETE" => 35, "SQL-ALTER" => 38,
        "SQL-SELECT" => 32, "SQL-FROM" => 30, "EF-Set" => 30,
        "SQL-JOIN" => 28, "Dapper-Query" => 28,
        "Repository" => 25, "InlineSQL" => 22,
        _ => 10
    };
}

internal class FileAccumulator
{
    public string RelativePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public int Count { get; set; }
    public DataOperation PrimaryOperation { get; set; } = DataOperation.Unknown;
    public List<DataOperation> Operations { get; } = new();
    public string? ContextSnippet { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
}
