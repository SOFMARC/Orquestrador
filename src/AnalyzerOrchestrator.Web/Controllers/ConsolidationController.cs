using AnalyzerOrchestrator.Application.DTOs.Consolidation;
using AnalyzerOrchestrator.Application.DTOs.Extraction;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Application.Workflow;
using AnalyzerOrchestrator.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerOrchestrator.Web.Controllers;

/// <summary>
/// Controller responsável pela Etapa 2 do workflow: Consolidação Arquitetural.
/// Gerencia execução, exibição de resultado e revisão humana.
/// </summary>
public class ConsolidationController : Controller
{
    private readonly IArchitecturalConsolidationService _consolidationService;
    private readonly IPipelineRunService _runService;

    public ConsolidationController(
        IArchitecturalConsolidationService consolidationService,
        IPipelineRunService runService)
    {
        _consolidationService = consolidationService;
        _runService = runService;
    }

    // ── Execute ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET: Exibe tela de confirmação antes de executar a consolidação arquitetural.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Execute(int runId)
    {
        var run = await _runService.GetByIdAsync(runId);
        if (run is null)
            return NotFound();

        // Verificar se a Etapa 1 está aprovada
        var step1 = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepStructuralExtraction);
        var step2 = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepArchitecturalConsolidation);

        if (step1 is null || step1.Status != StepStatus.Approved)
        {
            TempData["Error"] = "A Etapa 1 (Extração Estrutural) precisa estar aprovada antes de executar a Consolidação Arquitetural.";
            return RedirectToAction("Details", "PipelineRuns", new { id = runId });
        }

        if (step2 is not null && (step2.Status == StepStatus.Running))
        {
            TempData["Warning"] = "A Consolidação Arquitetural já está em execução.";
            return RedirectToAction("Details", "PipelineRuns", new { id = runId });
        }

        ViewBag.Run = run;
        ViewBag.Step1 = step1;
        ViewBag.Step2 = step2;
        return View();
    }

    /// <summary>
    /// POST: Executa a consolidação arquitetural e redireciona para o resultado.
    /// </summary>
    [HttpPost, ActionName("Execute")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteConfirmed(int runId)
    {
        try
        {
            var result = await _consolidationService.ExecuteAsync(runId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage ?? "Erro desconhecido na consolidação arquitetural.";
                return RedirectToAction("Details", "PipelineRuns", new { id = runId });
            }

            TempData["Success"] = $"Consolidação arquitetural concluída: {result.ModulesCount} módulos, {result.CentralFilesCount} arquivos centrais.";
            return RedirectToAction("Result", new { runId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Erro ao executar consolidação: {ex.Message}";
            return RedirectToAction("Details", "PipelineRuns", new { id = runId });
        }
    }

    // ── Result ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET: Exibe o resultado da consolidação arquitetural com módulos, camadas e arquivos centrais.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Result(int runId)
    {
        var run = await _runService.GetByIdAsync(runId);
        if (run is null)
            return NotFound();

        var result = await _consolidationService.GetResultAsync(runId);
        if (result is null)
        {
            TempData["Warning"] = "A Consolidação Arquitetural ainda não foi executada para esta run.";
            return RedirectToAction("Details", "PipelineRuns", new { id = runId });
        }

        var step2 = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepArchitecturalConsolidation);

        ViewBag.Run = run;
        ViewBag.Step2 = step2;
        return View(result);
    }

    // ── Review ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET: Exibe formulário de revisão humana da consolidação arquitetural.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Review(int stepId)
    {
        var run = await _runService.GetByStepIdAsync(stepId);
        if (run is null)
            return NotFound();

        var step2 = run.StepExecutions.FirstOrDefault(s => s.Id == stepId);
        if (step2 is null)
            return NotFound();

        if (step2.Status != StepStatus.AwaitingReview)
        {
            TempData["Warning"] = "Esta etapa não está aguardando revisão.";
            return RedirectToAction("Details", "PipelineRuns", new { id = run.Id });
        }

        var result = await _consolidationService.GetResultAsync(run.Id);

        ViewBag.Run = run;
        ViewBag.Step2 = step2;
        ViewBag.Result = result;

        var dto = new StepReviewDto { StepExecutionId = stepId };
        return View(dto);
    }

    /// <summary>
    /// POST: Persiste a revisão humana (aprovação ou reprovação).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(StepReviewDto dto)
    {
        if (!ModelState.IsValid)
        {
            var run = await _runService.GetByStepIdAsync(dto.StepExecutionId);
            ViewBag.Run = run;
            return View(dto);
        }

        try
        {
            await _consolidationService.ReviewStepAsync(dto);

            var decision = dto.Decision == StepStatus.Approved ? "aprovada" : "reprovada";
            TempData["Success"] = $"Consolidação Arquitetural {decision} com sucesso.";

            var run = await _runService.GetByStepIdAsync(dto.StepExecutionId);
            return RedirectToAction("Details", "PipelineRuns", new { id = run?.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Erro ao registrar revisão: {ex.Message}";
            return RedirectToAction("Details", "PipelineRuns",
                new { id = (await _runService.GetByStepIdAsync(dto.StepExecutionId))?.Id });
        }
    }
}
