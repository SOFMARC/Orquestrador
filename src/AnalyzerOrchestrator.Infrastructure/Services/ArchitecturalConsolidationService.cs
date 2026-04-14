using System.Text;
using System.Text.Json;
using AnalyzerOrchestrator.Application.DTOs.Consolidation;
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
/// Implementa a Etapa 2 do workflow: Consolidação Arquitetural.
/// Consome os ScannedFiles gerados pela Extração Estrutural e produz
/// um mapa de módulos, distribuição por camada, arquivos centrais e
/// resumo arquitetural em Markdown.
/// </summary>
public class ArchitecturalConsolidationService : IArchitecturalConsolidationService
{
    private readonly OrchestratorDbContext _context;
    private readonly ILogger<ArchitecturalConsolidationService> _logger;

    // Camadas reconhecidas por convenção de pasta ou namespace
    private static readonly Dictionary<string, string> LayerKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "domain",         "Domain" },
        { "entities",       "Domain" },
        { "entity",         "Domain" },
        { "application",    "Application" },
        { "services",       "Application" },
        { "service",        "Application" },
        { "dtos",           "Application" },
        { "dto",            "Application" },
        { "interfaces",     "Application" },
        { "usecases",       "Application" },
        { "infrastructure", "Infrastructure" },
        { "repositories",   "Infrastructure" },
        { "repository",     "Infrastructure" },
        { "persistence",    "Infrastructure" },
        { "migrations",     "Infrastructure" },
        { "data",           "Infrastructure" },
        { "web",            "Presentation" },
        { "api",            "Presentation" },
        { "controllers",    "Presentation" },
        { "views",          "Presentation" },
        { "pages",          "Presentation" },
        { "wwwroot",        "Presentation" },
        { "tests",          "Tests" },
        { "test",           "Tests" },
        { "specs",          "Tests" },
        { "scripts",        "Scripts/SQL" },
        { "sql",            "Scripts/SQL" },
        { "database",       "Scripts/SQL" },
        { "db",             "Scripts/SQL" },
    };

    public ArchitecturalConsolidationService(
        OrchestratorDbContext context,
        ILogger<ArchitecturalConsolidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Execução principal ───────────────────────────────────────────────────────

    public async Task<ConsolidationResultDto> ExecuteAsync(int pipelineRunId, CancellationToken ct = default)
    {
        var started = DateTime.UtcNow;

        var run = await _context.PipelineRuns
            .Include(r => r.Project)
            .Include(r => r.StepExecutions)
            .Include(r => r.ScannedFiles)
            .Include(r => r.Artifacts)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId, ct)
            ?? throw new InvalidOperationException($"Run {pipelineRunId} não encontrada.");

        // Verificar pré-condição: Etapa 1 deve estar aprovada
        var step1 = run.StepExecutions
            .FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepStructuralExtraction);

        if (step1 is null || step1.Status != StepStatus.Approved)
        {
            return new ConsolidationResultDto
            {
                PipelineRunId = pipelineRunId,
                Success = false,
                ErrorMessage = "A Etapa 1 (Extração Estrutural) precisa estar aprovada antes de executar a Consolidação Arquitetural."
            };
        }

        var step2 = run.StepExecutions
            .FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepArchitecturalConsolidation)
            ?? throw new InvalidOperationException("Etapa de Consolidação Arquitetural não encontrada na run.");

        // Marcar como Running
        step2.Status = StepStatus.Running;
        step2.StartedAt = DateTime.UtcNow;
        run.CurrentStep = step2.StepName;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        var result = new ConsolidationResultDto
        {
            PipelineRunId = pipelineRunId,
            StepExecutionId = step2.Id
        };

        try
        {
            var files = run.ScannedFiles.ToList();

            if (files.Count == 0)
            {
                throw new InvalidOperationException(
                    "Nenhum arquivo escaneado encontrado para esta run. Execute e aprove a Etapa 1 primeiro.");
            }

            _logger.LogInformation("Iniciando consolidação arquitetural da run {RunId} com {Count} arquivos",
                pipelineRunId, files.Count);

            // ── Análise ──────────────────────────────────────────────────────────

            var modules = BuildModules(files);
            var layers = BuildLayerDistribution(files);
            var centralFiles = BuildCentralFiles(files);
            var observations = BuildObservations(files, modules, layers);

            // ── Artefatos ────────────────────────────────────────────────────────

            var workspacePath = GetWorkspacePath(run.Project.Name, pipelineRunId);
            Directory.CreateDirectory(workspacePath);

            var modulesMapPath    = await WriteModulesMapAsync(modules, workspacePath);
            var archSummaryPath   = await WriteArchitectureSummaryAsync(run.Project.Name, files, modules, layers, centralFiles, observations, workspacePath);
            var layerDistPath     = await WriteLayerDistributionAsync(layers, workspacePath);
            var centralFilesPath  = await WriteCentralFilesAsync(centralFiles, workspacePath);
            var step2SummaryPath  = await WriteStep2SummaryAsync(run.Project.Name, files, modules, layers, centralFiles, observations, DateTime.UtcNow - started, workspacePath);

            // Persistir artefatos no banco
            await PersistArtifactAsync(pipelineRunId, "modules-map.json",          ArtifactType.ModulesMap,          modulesMapPath,   "application/json", ct);
            await PersistArtifactAsync(pipelineRunId, "architecture-summary.md",   ArtifactType.ArchitectureSummary, archSummaryPath,  "text/markdown",    ct);
            await PersistArtifactAsync(pipelineRunId, "layer-distribution.json",   ArtifactType.LayerDistribution,   layerDistPath,    "application/json", ct);
            await PersistArtifactAsync(pipelineRunId, "central-files.json",        ArtifactType.CentralFiles,        centralFilesPath, "application/json", ct);
            await PersistArtifactAsync(pipelineRunId, "step-2-summary.md",         ArtifactType.Step2Summary,        step2SummaryPath, "text/markdown",    ct);

            // Atualizar step com métricas explícitas (sem depender de parsing de Notes)
            step2.Status = StepStatus.AwaitingReview;
            step2.FinishedAt = DateTime.UtcNow;
            step2.FilesFound = files.Count;
            step2.ModulesCount = modules.Count;
            step2.LayersCount = layers.Count;
            step2.CentralFilesCount = centralFiles.Count;
            step2.Notes = $"Consolidação concluída. {modules.Count} módulos, {layers.Count} camadas, {centralFiles.Count} arquivos centrais.";
            run.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            // Montar resultado
            result.Success = true;
            result.ModulesCount = modules.Count;
            result.LayersCount = layers.Count;
            result.CentralFilesCount = centralFiles.Count;
            result.Duration = DateTime.UtcNow - started;
            result.Modules = modules;
            result.Layers = layers;
            result.CentralFiles = centralFiles;
            result.Observations = observations;
            result.ModulesMapPath = modulesMapPath;
            result.ArchitectureSummaryPath = archSummaryPath;
            result.LayerDistributionPath = layerDistPath;
            result.CentralFilesPath = centralFilesPath;
            result.Step2SummaryPath = step2SummaryPath;
            result.ArchitectureSummaryContent = await File.ReadAllTextAsync(archSummaryPath, ct);

            _logger.LogInformation("Consolidação arquitetural concluída em {Duration}ms", result.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na consolidação arquitetural da run {RunId}", pipelineRunId);

            step2.Status = StepStatus.Failed;
            step2.FinishedAt = DateTime.UtcNow;
            step2.ErrorMessage = ex.Message;
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

    public async Task<ConsolidationResultDto?> GetResultAsync(int pipelineRunId)
    {
        var run = await _context.PipelineRuns
            .Include(r => r.Project)
            .Include(r => r.StepExecutions)
            .Include(r => r.Artifacts)
            .Include(r => r.ScannedFiles)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId);

        if (run is null) return null;

        var step2 = run.StepExecutions
            .FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepArchitecturalConsolidation);

        if (step2 is null || step2.Status == StepStatus.Pending) return null;

        // Reconstruir resultado a partir dos artefatos persistidos
        var archSummaryPath = run.Artifacts
            .FirstOrDefault(a => a.Type == ArtifactType.ArchitectureSummary)?.FilePath;
        var modulesMapPath = run.Artifacts
            .FirstOrDefault(a => a.Type == ArtifactType.ModulesMap)?.FilePath;
        var layerDistPath = run.Artifacts
            .FirstOrDefault(a => a.Type == ArtifactType.LayerDistribution)?.FilePath;
        var centralFilesPath = run.Artifacts
            .FirstOrDefault(a => a.Type == ArtifactType.CentralFiles)?.FilePath;
        var step2SummaryPath = run.Artifacts
            .FirstOrDefault(a => a.Type == ArtifactType.Step2Summary)?.FilePath;

        var archContent = archSummaryPath != null && File.Exists(archSummaryPath)
            ? await File.ReadAllTextAsync(archSummaryPath) : string.Empty;

        // Recarregar módulos do JSON se disponível
        List<ModuleDto> modules = new();
        List<LayerDto> layers = new();
        List<CentralFileDto> centralFiles = new();

        if (modulesMapPath != null && File.Exists(modulesMapPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(modulesMapPath);
                modules = JsonSerializer.Deserialize<List<ModuleDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch { /* tolerante a falhas de leitura */ }
        }

        if (layerDistPath != null && File.Exists(layerDistPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(layerDistPath);
                layers = JsonSerializer.Deserialize<List<LayerDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch { }
        }

        if (centralFilesPath != null && File.Exists(centralFilesPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(centralFilesPath);
                centralFiles = JsonSerializer.Deserialize<List<CentralFileDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch { }
        }

        return new ConsolidationResultDto
        {
            PipelineRunId = pipelineRunId,
            StepExecutionId = step2.Id,
            Success = step2.Status != StepStatus.Failed,
            ModulesCount = modules.Count,
            LayersCount = layers.Count,
            CentralFilesCount = centralFiles.Count,
            Modules = modules,
            Layers = layers,
            CentralFiles = centralFiles,
            ArchitectureSummaryContent = archContent,
            ModulesMapPath = modulesMapPath,
            ArchitectureSummaryPath = archSummaryPath,
            LayerDistributionPath = layerDistPath,
            CentralFilesPath = centralFilesPath,
            Step2SummaryPath = step2SummaryPath,
            ErrorMessage = step2.ErrorMessage
        };
    }

    // ── Lógica de análise ────────────────────────────────────────────────────────

    private static List<ModuleDto> BuildModules(List<ScannedFile> files)
    {
        // Agrupa por pasta de primeiro nível (módulo)
        var groups = files
            .GroupBy(f =>
            {
                var parts = f.RelativePath.Replace('\\', '/').Split('/');
                return parts.Length > 1 ? parts[0] : "(raiz)";
            })
            .OrderByDescending(g => g.Count());

        var result = new List<ModuleDto>();
        foreach (var g in groups)
        {
            var moduleFiles = g.ToList();
            var byRole = moduleFiles
                .GroupBy(f => f.Role.ToString())
                .ToDictionary(r => r.Key, r => r.Count());

            var detectedLayer = DetectLayer(g.Key, moduleFiles);
            var topFiles = moduleFiles
                .OrderByDescending(f => f.RelevanceScore)
                .Take(5)
                .Select(f => f.RelativePath)
                .ToList();

            result.Add(new ModuleDto
            {
                Name = g.Key,
                FileCount = moduleFiles.Count,
                DetectedLayer = detectedLayer,
                FilesByRole = byRole,
                TopFiles = topFiles,
                Observations = BuildModuleObservation(g.Key, moduleFiles, detectedLayer)
            });
        }

        return result;
    }

    private static string DetectLayer(string folderName, List<ScannedFile> files)
    {
        // Tentar pelo nome da pasta
        if (LayerKeywords.TryGetValue(folderName, out var layer))
            return layer;

        // Tentar por sub-pastas predominantes
        var subFolders = files
            .Select(f =>
            {
                var parts = f.RelativePath.Replace('\\', '/').Split('/');
                return parts.Length > 2 ? parts[1] : string.Empty;
            })
            .Where(s => !string.IsNullOrEmpty(s))
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(3);

        foreach (var sub in subFolders)
        {
            if (LayerKeywords.TryGetValue(sub, out var subLayer))
                return subLayer;
        }

        // Tentar pelo papel predominante dos arquivos
        var dominantRole = files
            .GroupBy(f => f.Role)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return dominantRole switch
        {
            FileRole.Controller or FileRole.View => "Presentation",
            FileRole.Service or FileRole.DTO => "Application",
            FileRole.Repository or FileRole.Migration => "Infrastructure",
            FileRole.Entity or FileRole.Domain => "Domain",
            FileRole.SQL or FileRole.Script => "Scripts/SQL",
            FileRole.Test => "Tests",
            FileRole.Config or FileRole.Startup => "Configuration",
            _ => "Unknown"
        };
    }

    private static List<LayerDto> BuildLayerDistribution(List<ScannedFile> files)
    {
        var byLayer = files
            .GroupBy(f =>
            {
                var parts = f.RelativePath.Replace('\\', '/').Split('/');
                var folder = parts.Length > 1 ? parts[0] : "(raiz)";
                return DetectLayer(folder, new List<ScannedFile> { f });
            })
            .OrderByDescending(g => g.Count());

        return byLayer.Select(g => new LayerDto
        {
            Name = g.Key,
            FileCount = g.Count(),
            Roles = g.GroupBy(f => f.Role.ToString())
                      .ToDictionary(r => r.Key, r => r.Count())
        }).ToList();
    }

    private static List<CentralFileDto> BuildCentralFiles(List<ScannedFile> files)
    {
        var central = new List<CentralFileDto>();

        // Entry points
        var entryPoints = files.Where(f =>
            f.Role == FileRole.Startup ||
            f.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ||
            f.FileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase) ||
            f.FileName.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase));

        foreach (var f in entryPoints)
            central.Add(new CentralFileDto
            {
                RelativePath = f.RelativePath,
                FileName = f.FileName,
                Role = f.Role.ToString(),
                RelevanceScore = f.RelevanceScore,
                Reason = "Entry point / configuração principal"
            });

        // DbContext / configuração de banco
        var dbContextFiles = files.Where(f =>
            f.FileName.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
            f.FileName.Contains("Context", StringComparison.OrdinalIgnoreCase));

        foreach (var f in dbContextFiles)
            if (!central.Any(c => c.RelativePath == f.RelativePath))
                central.Add(new CentralFileDto
                {
                    RelativePath = f.RelativePath,
                    FileName = f.FileName,
                    Role = f.Role.ToString(),
                    RelevanceScore = f.RelevanceScore,
                    Reason = "Contexto de banco de dados"
                });

        // Top controllers (score alto)
        var topControllers = files
            .Where(f => f.Role == FileRole.Controller)
            .OrderByDescending(f => f.RelevanceScore)
            .Take(5);

        foreach (var f in topControllers)
            if (!central.Any(c => c.RelativePath == f.RelativePath))
                central.Add(new CentralFileDto
                {
                    RelativePath = f.RelativePath,
                    FileName = f.FileName,
                    Role = f.Role.ToString(),
                    RelevanceScore = f.RelevanceScore,
                    Reason = "Controller principal"
                });

        // Top services
        var topServices = files
            .Where(f => f.Role == FileRole.Service)
            .OrderByDescending(f => f.RelevanceScore)
            .Take(5);

        foreach (var f in topServices)
            if (!central.Any(c => c.RelativePath == f.RelativePath))
                central.Add(new CentralFileDto
                {
                    RelativePath = f.RelativePath,
                    FileName = f.FileName,
                    Role = f.Role.ToString(),
                    RelevanceScore = f.RelevanceScore,
                    Reason = "Serviço principal"
                });

        // Top repositories
        var topRepos = files
            .Where(f => f.Role == FileRole.Repository)
            .OrderByDescending(f => f.RelevanceScore)
            .Take(3);

        foreach (var f in topRepos)
            if (!central.Any(c => c.RelativePath == f.RelativePath))
                central.Add(new CentralFileDto
                {
                    RelativePath = f.RelativePath,
                    FileName = f.FileName,
                    Role = f.Role.ToString(),
                    RelevanceScore = f.RelevanceScore,
                    Reason = "Repositório principal"
                });

        // Arquivos com score máximo ainda não incluídos
        var highScore = files
            .Where(f => f.RelevanceScore >= 80)
            .OrderByDescending(f => f.RelevanceScore)
            .Take(10);

        foreach (var f in highScore)
            if (!central.Any(c => c.RelativePath == f.RelativePath))
                central.Add(new CentralFileDto
                {
                    RelativePath = f.RelativePath,
                    FileName = f.FileName,
                    Role = f.Role.ToString(),
                    RelevanceScore = f.RelevanceScore,
                    Reason = $"Alta relevância (score {f.RelevanceScore})"
                });

        return central.OrderByDescending(c => c.RelevanceScore).ToList();
    }

    private static List<string> BuildObservations(
        List<ScannedFile> files,
        List<ModuleDto> modules,
        List<LayerDto> layers)
    {
        var obs = new List<string>();

        // Concentração em módulo único
        if (modules.Count == 1)
            obs.Add("Todos os arquivos estão em um único módulo — estrutura monolítica ou projeto pequeno.");

        // Módulo dominante
        var dominant = modules.OrderByDescending(m => m.FileCount).FirstOrDefault();
        if (dominant != null && modules.Count > 1 && dominant.FileCount > files.Count * 0.5)
            obs.Add($"O módulo '{dominant.Name}' concentra mais de 50% dos arquivos — possível ponto de atenção para organização.");

        // Ausência de camadas esperadas
        var layerNames = layers.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!layerNames.Contains("Domain") && !layerNames.Contains("Entities"))
            obs.Add("Camada Domain/Entities não identificada — pode ser um projeto sem separação explícita de domínio.");
        if (!layerNames.Contains("Infrastructure") && !layerNames.Contains("Repositories"))
            obs.Add("Camada Infrastructure/Repositories não identificada — acesso a dados pode estar acoplado à apresentação.");

        // Presença de testes
        if (layerNames.Contains("Tests"))
            obs.Add("Projeto possui arquivos de testes — boa prática identificada.");
        else
            obs.Add("Nenhum arquivo de teste identificado — considere adicionar cobertura de testes.");

        // Arquivos SQL
        var sqlCount = files.Count(f => f.Role == FileRole.SQL || f.Role == FileRole.Script);
        if (sqlCount > 0)
            obs.Add($"{sqlCount} arquivo(s) SQL/Script encontrado(s) — pode indicar scripts de banco ou migrations manuais.");

        // Arquivos de configuração
        var configCount = files.Count(f => f.Role == FileRole.Config || f.Role == FileRole.Startup);
        if (configCount == 0)
            obs.Add("Nenhum arquivo de configuração identificado — verifique se estão fora da pasta escaneada.");

        return obs;
    }

    private static string BuildModuleObservation(string name, List<ScannedFile> files, string layer)
    {
        var roles = files.GroupBy(f => f.Role).OrderByDescending(g => g.Count()).Take(2)
            .Select(g => g.Key.ToString());
        return $"Camada detectada: {layer}. Papéis predominantes: {string.Join(", ", roles)}.";
    }

    // ── Geração de artefatos ─────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private static async Task<string> WriteModulesMapAsync(List<ModuleDto> modules, string path)
    {
        var file = Path.Combine(path, "modules-map.json");
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(modules, JsonOpts));
        return file;
    }

    private static async Task<string> WriteLayerDistributionAsync(List<LayerDto> layers, string path)
    {
        var file = Path.Combine(path, "layer-distribution.json");
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(layers, JsonOpts));
        return file;
    }

    private static async Task<string> WriteCentralFilesAsync(List<CentralFileDto> centralFiles, string path)
    {
        var file = Path.Combine(path, "central-files.json");
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(centralFiles, JsonOpts));
        return file;
    }

    private static async Task<string> WriteArchitectureSummaryAsync(
        string projectName,
        List<ScannedFile> files,
        List<ModuleDto> modules,
        List<LayerDto> layers,
        List<CentralFileDto> centralFiles,
        List<string> observations,
        string path)
    {
        var file = Path.Combine(path, "architecture-summary.md");
        var sb = new StringBuilder();

        sb.AppendLine($"# Resumo Arquitetural — {projectName}");
        sb.AppendLine($"**Gerado em:** {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## Visão Geral");
        sb.AppendLine($"O projeto **{projectName}** possui **{files.Count} arquivos** distribuídos em " +
                      $"**{modules.Count} módulo(s)** e **{layers.Count} camada(s)** identificada(s).");
        sb.AppendLine();

        sb.AppendLine("## Camadas Detectadas");
        sb.AppendLine("| Camada | Arquivos |");
        sb.AppendLine("|--------|---------|");
        foreach (var l in layers.OrderByDescending(x => x.FileCount))
            sb.AppendLine($"| {l.Name} | {l.FileCount} |");
        sb.AppendLine();

        sb.AppendLine("## Módulos Detectados");
        sb.AppendLine("| Módulo | Camada | Arquivos |");
        sb.AppendLine("|--------|--------|---------|");
        foreach (var m in modules)
            sb.AppendLine($"| {m.Name} | {m.DetectedLayer} | {m.FileCount} |");
        sb.AppendLine();

        sb.AppendLine("## Distribuição por Papel");
        sb.AppendLine("| Papel | Quantidade |");
        sb.AppendLine("|-------|-----------|");
        foreach (var g in files.GroupBy(f => f.Role).OrderByDescending(g => g.Count()))
            sb.AppendLine($"| {g.Key} | {g.Count()} |");
        sb.AppendLine();

        sb.AppendLine("## Arquivos Centrais");
        foreach (var cf in centralFiles.Take(15))
            sb.AppendLine($"- **{cf.FileName}** ({cf.Role}) — `{cf.RelativePath}` — {cf.Reason}");
        sb.AppendLine();

        sb.AppendLine("## Observações Automáticas");
        foreach (var o in observations)
            sb.AppendLine($"- {o}");

        await File.WriteAllTextAsync(file, sb.ToString());
        return file;
    }

    private static async Task<string> WriteStep2SummaryAsync(
        string projectName,
        List<ScannedFile> files,
        List<ModuleDto> modules,
        List<LayerDto> layers,
        List<CentralFileDto> centralFiles,
        List<string> observations,
        TimeSpan duration,
        string path)
    {
        var file = Path.Combine(path, "step-2-summary.md");
        var sb = new StringBuilder();

        sb.AppendLine($"# Resumo da Consolidação Arquitetural — {projectName}");
        sb.AppendLine($"**Data:** {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"**Duração:** {duration.TotalSeconds:F1}s");
        sb.AppendLine();
        sb.AppendLine("## Métricas");
        sb.AppendLine("| Métrica | Valor |");
        sb.AppendLine("|---------|-------|");
        sb.AppendLine($"| Arquivos analisados | {files.Count} |");
        sb.AppendLine($"| Módulos detectados | {modules.Count} |");
        sb.AppendLine($"| Camadas detectadas | {layers.Count} |");
        sb.AppendLine($"| Arquivos centrais | {centralFiles.Count} |");
        sb.AppendLine();

        sb.AppendLine("## Módulos");
        foreach (var m in modules)
        {
            sb.AppendLine($"### {m.Name} ({m.DetectedLayer})");
            sb.AppendLine($"- **Arquivos:** {m.FileCount}");
            sb.AppendLine($"- **Observação:** {m.Observations}");
            if (m.TopFiles.Any())
                sb.AppendLine($"- **Principais:** {string.Join(", ", m.TopFiles.Select(Path.GetFileName))}");
            sb.AppendLine();
        }

        sb.AppendLine("## Observações");
        foreach (var o in observations)
            sb.AppendLine($"- {o}");

        await File.WriteAllTextAsync(file, sb.ToString());
        return file;
    }

    // ── Utilitários ──────────────────────────────────────────────────────────────

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
                StepNumber = DefaultAnalysisWorkflow.StepArchitecturalConsolidation,
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
        return Path.Combine("workspace", safeName, "runs", $"run_{runId}", "step_2");
    }
}
