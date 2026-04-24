using AnalyzerOrchestrator.Application.DTOs.Extraction;
using AnalyzerOrchestrator.Application.DTOs.SystemExtraction;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Application.Workflow;
using AnalyzerOrchestrator.Domain.Enums;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzerOrchestrator.Infrastructure.Services;

/// <summary>
/// Orquestrador da Etapa 1 — Extração do sistema (Workflow v2).
///
/// Chama os três serviços internos em sequência:
///   1.1 IStructuralExtractionService  → varredura, classificação, artefatos de estrutura
///   1.2 IArchitecturalConsolidationService → módulos, camadas, arquivos centrais
///   1.3 IDataMappingService → detecção de tabelas por heurística
///
/// Gerencia o PipelineStepExecution da Etapa 1 diretamente, pois no workflow v2
/// há apenas um step no banco para toda a extração.
///
/// Compatibilidade: opera apenas em runs novas (workflow v2).
/// Runs legadas continuam usando os serviços individuais via seus controllers.
/// </summary>
public class SystemExtractionService : ISystemExtractionService
{
    private readonly OrchestratorDbContext _context;
    private readonly IStructuralExtractionService _structuralService;
    private readonly IArchitecturalConsolidationService _consolidationService;
    private readonly IDataMappingService _dataMappingService;
    private readonly ILogger<SystemExtractionService> _logger;

    public SystemExtractionService(
        OrchestratorDbContext context,
        IStructuralExtractionService structuralService,
        IArchitecturalConsolidationService consolidationService,
        IDataMappingService dataMappingService,
        ILogger<SystemExtractionService> logger)
    {
        _context = context;
        _structuralService = structuralService;
        _consolidationService = consolidationService;
        _dataMappingService = dataMappingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SystemExtractionResultDto> ExecuteAsync(int pipelineRunId, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;

        _logger.LogInformation("Iniciando Etapa 1 — Extração do sistema para run {RunId}", pipelineRunId);

        // Carregar run e verificar que é workflow v2
        var run = await _context.PipelineRuns
            .Include(r => r.Project)
            .Include(r => r.StepExecutions)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Run {pipelineRunId} não encontrada.");

        var step1 = run.StepExecutions
            .FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepSystemExtraction)
            ?? throw new InvalidOperationException("Etapa 1 (Extração do sistema) não encontrada na run.");

        // Marcar a run e o step como Running
        step1.Status = StepStatus.Running;
        step1.StartedAt = DateTime.UtcNow;
        run.Status = RunStatus.Running;
        run.CurrentStep = step1.StepName;
        run.StartedAt ??= DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var result = new SystemExtractionResultDto { PipelineRunId = pipelineRunId };

        try
        {
            // ── Subetapa 1.1 — Extração Estrutural ──────────────────────────────
            _logger.LogInformation("Run {RunId}: iniciando subetapa 1.1 — Extração Estrutural", pipelineRunId);
            var extractionResult = await _structuralService.ExecuteAsync(pipelineRunId, cancellationToken);
            result.Extraction = extractionResult;

            if (!extractionResult.Success)
            {
                _logger.LogWarning("Run {RunId}: subetapa 1.1 falhou: {Msg}", pipelineRunId, extractionResult.ErrorMessage);
                await MarkStepFailedAsync(step1, run,
                    $"Subetapa 1.1 (Extração Estrutural) falhou: {extractionResult.ErrorMessage}",
                    cancellationToken);
                result.Success = false;
                result.ErrorMessage = extractionResult.ErrorMessage;
                result.TotalDuration = DateTime.UtcNow - started;
                return result;
            }

            // A subetapa 1.1 deixa o step em AwaitingReview — precisamos revertê-lo
            // para Running para continuar o fluxo unificado.
            await ResetStepToRunningAsync(step1, run, cancellationToken);

            // ── Subetapa 1.2 — Consolidação Arquitetural ─────────────────────────
            // O ArchitecturalConsolidationService exige que o step 1 esteja Approved.
            // Como no workflow v2 o step 1 é único, aprovamos temporariamente para
            // permitir a execução da subetapa 1.2, e revertemos logo após.
            _logger.LogInformation("Run {RunId}: iniciando subetapa 1.2 — Consolidação Arquitetural", pipelineRunId);
            await ApproveStepTemporarilyAsync(step1, run, cancellationToken);

            var consolidationResult = await _consolidationService.ExecuteAsync(pipelineRunId, cancellationToken);
            result.Consolidation = consolidationResult;

            if (!consolidationResult.Success)
            {
                _logger.LogWarning("Run {RunId}: subetapa 1.2 falhou: {Msg}", pipelineRunId, consolidationResult.ErrorMessage);
                // Não é fatal — continua para a subetapa 1.3
            }

            // Reverter step para Running antes da subetapa 1.3
            await ResetStepToRunningAsync(step1, run, cancellationToken);

            // ── Subetapa 1.3 — Mapeamento Inicial de Dados ───────────────────────
            // O DataMappingService também exige step 1 aprovado.
            _logger.LogInformation("Run {RunId}: iniciando subetapa 1.3 — Mapeamento Inicial de Dados", pipelineRunId);
            await ApproveStepTemporarilyAsync(step1, run, cancellationToken);

            var dataMappingResult = await _dataMappingService.ExecuteAsync(pipelineRunId, cancellationToken);
            result.DataMapping = dataMappingResult;

            if (!dataMappingResult.Success)
            {
                _logger.LogWarning("Run {RunId}: subetapa 1.3 falhou: {Msg}", pipelineRunId, dataMappingResult.ErrorMessage);
                // Não é fatal — a extração estrutural e arquitetural já foram concluídas
            }

            // ── Finalizar o step 1 como AwaitingReview ───────────────────────────
            step1 = await _context.PipelineStepExecutions
                .FirstAsync(s => s.PipelineRunId == pipelineRunId &&
                                 s.StepNumber == DefaultAnalysisWorkflow.StepSystemExtraction, cancellationToken);

            step1.Status = StepStatus.AwaitingReview;
            step1.FinishedAt = DateTime.UtcNow;
            step1.FilesFound = extractionResult.FilesFound;
            step1.FilesIgnored = extractionResult.FilesIgnored;
            step1.ErrorCount = extractionResult.ErrorCount;
            step1.ModulesCount = consolidationResult.Success ? consolidationResult.ModulesCount : null;
            step1.LayersCount = consolidationResult.Success ? consolidationResult.LayersCount : null;
            step1.CentralFilesCount = consolidationResult.Success ? consolidationResult.CentralFilesCount : null;
            step1.TablesCount = dataMappingResult.Success ? dataMappingResult.TablesCount : null;
            step1.RelationsCount = dataMappingResult.Success ? dataMappingResult.RelationsCount : null;
            step1.Notes = BuildStepNotes(extractionResult, consolidationResult, dataMappingResult);
            step1.UpdatedAt = DateTime.UtcNow;

            run.CurrentStep = step1.StepName;
            run.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.TotalDuration = DateTime.UtcNow - started;

            _logger.LogInformation(
                "Run {RunId}: Etapa 1 concluída. Arquivos: {Files}, Módulos: {Modules}, Tabelas: {Tables}",
                pipelineRunId, result.FilesFound, result.ModulesCount, result.TablesCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Run {RunId}: erro inesperado na Etapa 1", pipelineRunId);

            // Recarregar step para garantir estado consistente
            var stepFresh = await _context.PipelineStepExecutions
                .FirstOrDefaultAsync(s => s.PipelineRunId == pipelineRunId &&
                                          s.StepNumber == DefaultAnalysisWorkflow.StepSystemExtraction,
                    CancellationToken.None);

            if (stepFresh is not null)
            {
                await MarkStepFailedAsync(stepFresh, run, ex.Message, CancellationToken.None);
            }

            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.TotalDuration = DateTime.UtcNow - started;
            return result;
        }
    }

    /// <inheritdoc />
    public async Task ReviewStepAsync(StepReviewDto dto)
    {
        var step = await _context.PipelineStepExecutions.FindAsync(dto.StepExecutionId)
            ?? throw new InvalidOperationException($"Step {dto.StepExecutionId} não encontrado.");

        step.Status = dto.Decision;
        step.ReviewedAt = DateTime.UtcNow;
        step.ReviewedBy = dto.ReviewedBy?.Trim();
        step.ReviewNotes = dto.ReviewNotes?.Trim();
        step.UpdatedAt = DateTime.UtcNow;

        // Atualizar status da run se aprovado
        if (dto.Decision == StepStatus.Approved)
        {
            var run = await _context.PipelineRuns
                .Include(r => r.StepExecutions)
                .FirstOrDefaultAsync(r => r.Id == step.PipelineRunId);

            if (run is not null)
            {
                var allApproved = run.StepExecutions
                    .Where(s => s.StepNumber <= DefaultAnalysisWorkflow.StepSystemExtraction)
                    .All(s => s.Status == StepStatus.Approved);

                if (allApproved)
                {
                    run.CurrentStep = "Aguardando Etapa 2";
                    run.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<SystemExtractionResultDto?> GetResultAsync(int pipelineRunId)
    {
        var run = await _context.PipelineRuns
            .Include(r => r.StepExecutions)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId);

        if (run is null) return null;

        var step1 = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepSystemExtraction);
        if (step1 is null || step1.Status == StepStatus.Pending) return null;

        // Reconstituir os resultados individuais a partir dos serviços existentes
        var extractionResult = await _structuralService.GetResultAsync(pipelineRunId);
        var consolidationResult = await _consolidationService.GetResultAsync(pipelineRunId);
        var dataMappingResult = await _dataMappingService.GetResultAsync(pipelineRunId);

        return new SystemExtractionResultDto
        {
            PipelineRunId = pipelineRunId,
            Success = step1.Status != StepStatus.Failed,
            ErrorMessage = step1.ErrorMessage,
            TotalDuration = step1.StartedAt.HasValue && step1.FinishedAt.HasValue
                ? step1.FinishedAt.Value - step1.StartedAt.Value
                : TimeSpan.Zero,
            Extraction = extractionResult,
            Consolidation = consolidationResult,
            DataMapping = dataMappingResult
        };
    }

    // ── Helpers privados ──────────────────────────────────────────────────────────

    private async Task MarkStepFailedAsync(
        PipelineStepExecution step,
        PipelineRun run,
        string errorMessage,
        CancellationToken ct)
    {
        step.Status = StepStatus.Failed;
        step.FinishedAt = DateTime.UtcNow;
        step.ErrorMessage = errorMessage;
        step.UpdatedAt = DateTime.UtcNow;
        run.Status = RunStatus.Failed;
        run.FinishedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Aprova temporariamente o step 1 para que os serviços internos (que exigem
    /// step 1 aprovado como pré-condição) possam executar no contexto do workflow v2.
    /// </summary>
    private async Task ApproveStepTemporarilyAsync(
        PipelineStepExecution step,
        PipelineRun run,
        CancellationToken ct)
    {
        // Recarregar o step para garantir que temos o estado mais recente
        var freshStep = await _context.PipelineStepExecutions
            .FirstAsync(s => s.Id == step.Id, ct);

        freshStep.Status = StepStatus.Approved;
        freshStep.UpdatedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Reverte o step para Running após a aprovação temporária.
    /// </summary>
    private async Task ResetStepToRunningAsync(
        PipelineStepExecution step,
        PipelineRun run,
        CancellationToken ct)
    {
        var freshStep = await _context.PipelineStepExecutions
            .FirstAsync(s => s.Id == step.Id, ct);

        freshStep.Status = StepStatus.Running;
        freshStep.UpdatedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    private static string BuildStepNotes(
        Application.DTOs.Extraction.ExtractionResultDto extraction,
        Application.DTOs.Consolidation.ConsolidationResultDto consolidation,
        Application.DTOs.DataMapping.DataMappingResultDto dataMapping)
    {
        var parts = new List<string>();

        parts.Add($"Estrutura: {extraction.FilesFound} arquivos encontrados, {extraction.RelevantFilesCount} relevantes.");

        if (consolidation.Success)
            parts.Add($"Arquitetura: {consolidation.ModulesCount} módulos, {consolidation.LayersCount} camadas, {consolidation.CentralFilesCount} arquivos centrais.");
        else
            parts.Add("Arquitetura: não concluída.");

        if (dataMapping.Success)
            parts.Add($"Dados: {dataMapping.TablesCount} tabelas detectadas, {dataMapping.RelationsCount} relações.");
        else
            parts.Add("Dados: não concluído.");

        return string.Join(" ", parts);
    }
}
