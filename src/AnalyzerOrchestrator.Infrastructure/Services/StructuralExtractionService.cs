using System.Text;
using System.Text.Json;
using AnalyzerOrchestrator.Application.DTOs.Extraction;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Application.Workflow;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Domain.Enums;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzerOrchestrator.Infrastructure.Services;

public class StructuralExtractionService : IStructuralExtractionService
{
    private readonly OrchestratorDbContext _context;
    private readonly IFileClassifierService _classifier;
    private readonly ILogger<StructuralExtractionService> _logger;

    // Extensões reconhecidamente binárias
    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".pdb", ".so", ".dylib", ".bin", ".dat",
        ".zip", ".tar", ".gz", ".rar", ".7z",
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".svg",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".mp3", ".mp4", ".avi", ".mov", ".wav",
        ".db", ".sqlite", ".mdf", ".ldf",
        ".nupkg", ".snupkg"
    };

    public StructuralExtractionService(
        OrchestratorDbContext context,
        IFileClassifierService classifier,
        ILogger<StructuralExtractionService> logger)
    {
        _context = context;
        _classifier = classifier;
        _logger = logger;
    }

    public async Task<ExtractionResultDto> ExecuteAsync(int pipelineRunId, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;

        // Carregar run com projeto e settings
        var run = await _context.PipelineRuns
            .Include(r => r.Project).ThenInclude(p => p.ScanSettings)
            .Include(r => r.StepExecutions)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Run {pipelineRunId} não encontrada.");

        var step = run.StepExecutions
            .FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepStructuralExtraction)
            ?? throw new InvalidOperationException("Etapa de Extração Estrutural não encontrada na run.");

        // Marcar como Running
        step.Status = StepStatus.Running;
        step.StartedAt = DateTime.UtcNow;
        run.Status = RunStatus.Running;
        run.CurrentStep = step.StepName;
        run.StartedAt ??= DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var result = new ExtractionResultDto
        {
            PipelineRunId = pipelineRunId,
            StepExecutionId = step.Id
        };

        try
        {
            var settings = run.Project.ScanSettings;
            var scanRoot = settings?.ScanRootPath ?? run.Project.RepositoryPath;

            if (string.IsNullOrWhiteSpace(scanRoot) || !Directory.Exists(scanRoot))
            {
                throw new InvalidOperationException(
                    $"Pasta raiz de scan não encontrada ou não configurada: '{scanRoot}'. " +
                    "Configure o caminho em 'Configurações de Leitura' do projeto.");
            }

            _logger.LogInformation("Iniciando varredura em: {Path}", scanRoot);

            // Configurações de filtro
            var allowedExt = settings?.GetAllowedExtensionsList().ToHashSet()
                ?? new HashSet<string> { ".cs", ".sql", ".json", ".cshtml", ".config", ".xml" };
            var ignoredFolders = settings?.GetIgnoredFoldersList().ToHashSet()
                ?? new HashSet<string> { "bin", "obj", ".git", "node_modules" };
            var maxBytes = settings?.MaxFileSizeKb.HasValue == true
                ? (long?)settings.MaxFileSizeKb.Value * 1024 : null;
            var ignoreBinary = settings?.IgnoreBinaryFiles ?? true;

            // Limpar arquivos anteriores desta run
            var oldFiles = _context.ScannedFiles.Where(f => f.PipelineRunId == pipelineRunId);
            _context.ScannedFiles.RemoveRange(oldFiles);
            await _context.SaveChangesAsync(cancellationToken);

            // Varredura
            var scannedFiles = new List<ScannedFile>();
            int ignored = 0;
            int errors = 0;
            var errorLog = new List<string>();

            ScanDirectory(scanRoot, scanRoot, allowedExt, ignoredFolders, maxBytes, ignoreBinary,
                scannedFiles, ref ignored, ref errors, errorLog, pipelineRunId, cancellationToken);

            _logger.LogInformation("Varredura concluída: {Found} arquivos, {Ignored} ignorados, {Errors} erros",
                scannedFiles.Count, ignored, errors);

            // Persistir arquivos escaneados
            if (scannedFiles.Count > 0)
            {
                _context.ScannedFiles.AddRange(scannedFiles);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Gerar artefatos
            var workspacePath = GetWorkspacePath(run.Project.Name, pipelineRunId);
            Directory.CreateDirectory(workspacePath);

            var inventoryPath = await WriteInventoryAsync(scannedFiles, workspacePath);
            var treePath = await WriteTreeAsync(scannedFiles, scanRoot, workspacePath);
            var relevantPath = await WriteRelevantFilesAsync(scannedFiles, workspacePath);
            var summaryPath = await WriteSummaryAsync(run.Project.Name, scannedFiles, ignored, errors,
                errorLog, DateTime.UtcNow - started, workspacePath);

            // Persistir artefatos no banco
            await PersistArtifactAsync(pipelineRunId, "inventory.json", ArtifactType.FileInventory,
                inventoryPath, "application/json", cancellationToken);
            await PersistArtifactAsync(pipelineRunId, "tree.txt", ArtifactType.StructureTree,
                treePath, "text/plain", cancellationToken);
            await PersistArtifactAsync(pipelineRunId, "relevant-files.json", ArtifactType.RelevantFilesList,
                relevantPath, "application/json", cancellationToken);
            await PersistArtifactAsync(pipelineRunId, "summary.md", ArtifactType.ExecutionSummary,
                summaryPath, "text/markdown", cancellationToken);

            // Atualizar step
            var relevantCount = scannedFiles.Count(f => f.IsRelevant);
            step.Status = StepStatus.AwaitingReview;
            step.FinishedAt = DateTime.UtcNow;
            step.FilesFound = scannedFiles.Count;
            step.FilesIgnored = ignored;
            step.ErrorCount = errors;
            step.Notes = $"Varredura concluída. {scannedFiles.Count} arquivos encontrados, {relevantCount} relevantes.";
            if (errors > 0)
                step.ErrorMessage = string.Join("\n", errorLog.Take(10));

            run.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Montar resultado
            result.Success = true;
            result.FilesFound = scannedFiles.Count;
            result.FilesIgnored = ignored;
            result.ErrorCount = errors;
            result.RelevantFilesCount = relevantCount;
            result.Duration = DateTime.UtcNow - started;
            result.InventoryFilePath = inventoryPath;
            result.TreeFilePath = treePath;
            result.RelevantFilesPath = relevantPath;
            result.SummaryFilePath = summaryPath;
            result.StructureTree = await File.ReadAllTextAsync(treePath, cancellationToken);
            result.AllFiles = scannedFiles.Select(MapToDto).ToList();
            result.RelevantFiles = scannedFiles.Where(f => f.IsRelevant)
                .OrderByDescending(f => f.RelevanceScore).Select(MapToDto).ToList();

            _logger.LogInformation("Extração estrutural concluída em {Duration}ms", result.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na extração estrutural da run {RunId}", pipelineRunId);

            step.Status = StepStatus.Failed;
            step.FinishedAt = DateTime.UtcNow;
            step.ErrorMessage = ex.Message;
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

    public async Task<ExtractionResultDto?> GetResultAsync(int pipelineRunId)
    {
        var run = await _context.PipelineRuns
            .Include(r => r.Project)
            .Include(r => r.StepExecutions)
            .Include(r => r.Artifacts)
            .Include(r => r.ScannedFiles)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId);

        if (run is null) return null;

        var step = run.StepExecutions
            .FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepStructuralExtraction);

        if (step is null || step.Status == StepStatus.Pending) return null;

        var treePath = run.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.StructureTree)?.FilePath;
        var treeContent = treePath != null && File.Exists(treePath)
            ? await File.ReadAllTextAsync(treePath) : string.Empty;

        return new ExtractionResultDto
        {
            PipelineRunId = pipelineRunId,
            StepExecutionId = step.Id,
            Success = step.Status != StepStatus.Failed,
            FilesFound = step.FilesFound ?? 0,
            FilesIgnored = step.FilesIgnored ?? 0,
            ErrorCount = step.ErrorCount ?? 0,
            RelevantFilesCount = run.ScannedFiles.Count(f => f.IsRelevant),
            StructureTree = treeContent,
            InventoryFilePath = run.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.FileInventory)?.FilePath,
            TreeFilePath = treePath,
            RelevantFilesPath = run.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.RelevantFilesList)?.FilePath,
            SummaryFilePath = run.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.ExecutionSummary)?.FilePath,
            AllFiles = run.ScannedFiles.OrderBy(f => f.RelativePath).Select(MapToDto).ToList(),
            RelevantFiles = run.ScannedFiles.Where(f => f.IsRelevant)
                .OrderByDescending(f => f.RelevanceScore).Select(MapToDto).ToList()
        };
    }

    // ── Varredura ────────────────────────────────────────────────────────────────

    private void ScanDirectory(
        string rootPath,
        string currentPath,
        HashSet<string> allowedExt,
        HashSet<string> ignoredFolders,
        long? maxBytes,
        bool ignoreBinary,
        List<ScannedFile> results,
        ref int ignored,
        ref int errors,
        List<string> errorLog,
        int runId,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(currentPath);
        }
        catch (Exception ex)
        {
            errors++;
            errorLog.Add($"Erro ao acessar '{currentPath}': {ex.Message}");
            _logger.LogWarning("Erro ao acessar diretório {Path}: {Error}", currentPath, ex.Message);
            return;
        }

        foreach (var entry in entries)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                if (Directory.Exists(entry))
                {
                    var dirName = Path.GetFileName(entry).ToLowerInvariant();
                    if (ignoredFolders.Contains(dirName))
                    {
                        ignored++;
                        continue;
                    }
                    ScanDirectory(rootPath, entry, allowedExt, ignoredFolders, maxBytes,
                        ignoreBinary, results, ref ignored, ref errors, errorLog, runId, ct);
                }
                else
                {
                    var ext = Path.GetExtension(entry).ToLowerInvariant();

                    // Ignorar binários
                    if (ignoreBinary && BinaryExtensions.Contains(ext))
                    {
                        ignored++;
                        continue;
                    }

                    // Filtrar por extensão
                    if (!allowedExt.Contains(ext))
                    {
                        ignored++;
                        continue;
                    }

                    var info = new FileInfo(entry);

                    // Filtrar por tamanho
                    if (maxBytes.HasValue && info.Length > maxBytes.Value)
                    {
                        ignored++;
                        _logger.LogDebug("Arquivo ignorado por tamanho: {File} ({Size} bytes)", entry, info.Length);
                        continue;
                    }

                    var relative = Path.GetRelativePath(rootPath, entry);
                    var role = _classifier.Classify(relative, ext);
                    var score = _classifier.CalculateRelevanceScore(relative, ext, role);
                    var notes = _classifier.GetClassificationNotes(relative, role);

                    results.Add(new ScannedFile
                    {
                        PipelineRunId = runId,
                        RelativePath = relative,
                        FullPath = entry,
                        FileName = info.Name,
                        Extension = ext,
                        SizeBytes = info.Length,
                        Role = role,
                        IsRelevant = score >= 40,
                        RelevanceScore = score,
                        ClassificationNotes = notes,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                errors++;
                errorLog.Add($"Erro ao processar '{entry}': {ex.Message}");
                _logger.LogWarning("Erro ao processar entrada {Entry}: {Error}", entry, ex.Message);
            }
        }
    }

    // ── Geração de artefatos ─────────────────────────────────────────────────────

    private static async Task<string> WriteInventoryAsync(List<ScannedFile> files, string workspacePath)
    {
        var path = Path.Combine(workspacePath, "inventory.json");
        var data = files.Select(f => new
        {
            f.RelativePath,
            f.FileName,
            f.Extension,
            SizeKb = Math.Round(f.SizeBytes / 1024.0, 2),
            Role = f.Role.ToString(),
            f.IsRelevant,
            f.RelevanceScore
        });
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    private static async Task<string> WriteTreeAsync(List<ScannedFile> files, string rootPath, string workspacePath)
    {
        var path = Path.Combine(workspacePath, "tree.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"Árvore Estrutural — {Path.GetFileName(rootPath)}");
        sb.AppendLine($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine(new string('─', 60));
        sb.AppendLine();

        // Agrupar por pasta
        var byFolder = files
            .GroupBy(f => Path.GetDirectoryName(f.RelativePath)?.Replace('\\', '/') ?? ".")
            .OrderBy(g => g.Key);

        foreach (var group in byFolder)
        {
            var depth = group.Key == "." ? 0 : group.Key.Count(c => c == '/') + 1;
            var indent = new string(' ', depth * 2);
            sb.AppendLine($"{indent}📁 {(group.Key == "." ? "(raiz)" : group.Key.Split('/').Last())}");

            foreach (var file in group.OrderBy(f => f.FileName))
            {
                var fileIndent = new string(' ', (depth + 1) * 2);
                var marker = file.IsRelevant ? "★" : "·";
                sb.AppendLine($"{fileIndent}{marker} {file.FileName}  [{file.Role}]");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Total: {files.Count} arquivos | ★ = relevante");
        await File.WriteAllTextAsync(path, sb.ToString());
        return path;
    }

    private static async Task<string> WriteRelevantFilesAsync(List<ScannedFile> files, string workspacePath)
    {
        var path = Path.Combine(workspacePath, "relevant-files.json");
        var relevant = files.Where(f => f.IsRelevant)
            .OrderByDescending(f => f.RelevanceScore)
            .Select(f => new
            {
                f.RelativePath,
                f.FileName,
                Role = f.Role.ToString(),
                f.RelevanceScore,
                f.ClassificationNotes
            });
        var json = JsonSerializer.Serialize(relevant, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    private static async Task<string> WriteSummaryAsync(
        string projectName,
        List<ScannedFile> files,
        int ignored,
        int errors,
        List<string> errorLog,
        TimeSpan duration,
        string workspacePath)
    {
        var path = Path.Combine(workspacePath, "summary.md");
        var sb = new StringBuilder();
        sb.AppendLine($"# Resumo da Extração Estrutural — {projectName}");
        sb.AppendLine($"**Data:** {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"**Duração:** {duration.TotalSeconds:F1}s");
        sb.AppendLine();
        sb.AppendLine("## Métricas");
        sb.AppendLine($"| Métrica | Valor |");
        sb.AppendLine($"|---------|-------|");
        sb.AppendLine($"| Arquivos encontrados | {files.Count} |");
        sb.AppendLine($"| Arquivos ignorados | {ignored} |");
        sb.AppendLine($"| Arquivos relevantes | {files.Count(f => f.IsRelevant)} |");
        sb.AppendLine($"| Erros de acesso | {errors} |");
        sb.AppendLine();

        // Distribuição por papel
        sb.AppendLine("## Distribuição por Papel");
        sb.AppendLine("| Papel | Quantidade |");
        sb.AppendLine("|-------|-----------|");
        foreach (var group in files.GroupBy(f => f.Role).OrderByDescending(g => g.Count()))
            sb.AppendLine($"| {group.Key} | {group.Count()} |");

        sb.AppendLine();
        sb.AppendLine("## Top 10 Arquivos Mais Relevantes");
        foreach (var f in files.Where(f => f.IsRelevant).OrderByDescending(f => f.RelevanceScore).Take(10))
            sb.AppendLine($"- **{f.FileName}** ({f.Role}, score: {f.RelevanceScore}) — `{f.RelativePath}`");

        if (errors > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Erros de Acesso");
            foreach (var e in errorLog.Take(20))
                sb.AppendLine($"- {e}");
        }

        await File.WriteAllTextAsync(path, sb.ToString());
        return path;
    }

    private async Task PersistArtifactAsync(int runId, string name, ArtifactType type,
        string filePath, string mimeType, CancellationToken ct)
    {
        var existing = await _context.Artifacts
            .FirstOrDefaultAsync(a => a.PipelineRunId == runId && a.Type == type, ct);

        var fileInfo = new FileInfo(filePath);

        if (existing is null)
        {
            _context.Artifacts.Add(new Artifact
            {
                PipelineRunId = runId,
                StepNumber = DefaultAnalysisWorkflow.StepStructuralExtraction,
                Name = name,
                Type = type,
                FilePath = filePath,
                MimeType = mimeType,
                SizeBytes = fileInfo.Exists ? fileInfo.Length : null,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.FilePath = filePath;
            existing.SizeBytes = fileInfo.Exists ? fileInfo.Length : null;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static string GetWorkspacePath(string projectName, int runId)
    {
        var safeName = string.Concat(projectName.Split(Path.GetInvalidFileNameChars()));
        var basePath = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnalyzerOrchestrator", "workspace")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".analyzer-orchestrator", "workspace");
        return Path.Combine(basePath, safeName, "runs", $"run_{runId}", "step_1");
    }

    private static ScannedFileDto MapToDto(ScannedFile f) => new()
    {
        Id = f.Id,
        RelativePath = f.RelativePath,
        FileName = f.FileName,
        Extension = f.Extension,
        SizeBytes = f.SizeBytes,
        Role = f.Role,
        IsRelevant = f.IsRelevant,
        RelevanceScore = f.RelevanceScore,
        ClassificationNotes = f.ClassificationNotes
    };
}
