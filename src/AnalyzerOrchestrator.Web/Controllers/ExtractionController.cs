using AnalyzerOrchestrator.Application.DTOs.Extraction;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Application.Workflow;
using AnalyzerOrchestrator.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerOrchestrator.Web.Controllers;

public class ExtractionController : Controller
{
    private readonly IStructuralExtractionService _extractionService;
    private readonly IPipelineRunService _runService;

    public ExtractionController(
        IStructuralExtractionService extractionService,
        IPipelineRunService runService)
    {
        _extractionService = extractionService;
        _runService = runService;
    }

    // GET /Extraction/Execute/5  (runId)
    [HttpGet]
    public async Task<IActionResult> Execute(int id)
    {
        var run = await _runService.GetByIdAsync(id);
        if (run is null) return NotFound();

        // Verificar se a etapa já foi executada
        var step = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepStructuralExtraction);
        if (step is not null && step.Status != StepStatus.Pending && step.Status != StepStatus.Rejected)
        {
            TempData["Info"] = "A etapa de Extração Estrutural já foi executada. Veja os resultados abaixo.";
            return RedirectToAction("Result", new { id });
        }

        ViewBag.Run = run;
        return View(run);
    }

    // POST /Extraction/Execute/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(int id, CancellationToken ct)
    {
        var run = await _runService.GetByIdAsync(id);
        if (run is null) return NotFound();

        try
        {
            var result = await _extractionService.ExecuteAsync(id, ct);

            if (result.Success)
            {
                TempData["Success"] = $"Extração concluída: {result.FilesFound} arquivos encontrados, " +
                    $"{result.RelevantFilesCount} relevantes. Aguardando revisão.";
            }
            else
            {
                TempData["Error"] = $"Extração falhou: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Erro ao executar extração: {ex.Message}";
        }

        return RedirectToAction("Result", new { id });
    }

    // GET /Extraction/Result/5
    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var run = await _runService.GetByIdAsync(id);
        if (run is null) return NotFound();

        var result = await _extractionService.GetResultAsync(id);

        ViewBag.Run = run;
        ViewBag.Step = run.StepExecutions.FirstOrDefault(s => s.StepNumber == DefaultAnalysisWorkflow.StepStructuralExtraction);
        return View(result);
    }

    // GET /Extraction/Review/5  (stepExecutionId)
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        // id = stepExecutionId
        // Precisamos encontrar a run pelo step
        // Vamos buscar via run service passando o runId que vem via query string
        var runId = int.TryParse(Request.Query["runId"], out var rid) ? rid : 0;
        if (runId == 0) return BadRequest("runId é obrigatório.");

        var run = await _runService.GetByIdAsync(runId);
        if (run is null) return NotFound();

        var step = run.StepExecutions.FirstOrDefault(s => s.Id == id);
        if (step is null) return NotFound();

        var dto = new StepReviewDto { StepExecutionId = id };
        ViewBag.Run = run;
        ViewBag.Step = step;
        return View(dto);
    }

    // POST /Extraction/Review/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, StepReviewDto dto)
    {
        var runId = int.TryParse(Request.Form["RunId"], out var rid) ? rid : 0;

        if (!ModelState.IsValid || (dto.Decision != StepStatus.Approved && dto.Decision != StepStatus.Rejected))
        {
            TempData["Error"] = "Decisão inválida. Selecione Aprovado ou Reprovado.";
            return RedirectToAction("Result", new { id = runId });
        }

        dto.StepExecutionId = id;

        try
        {
            await _extractionService.ReviewStepAsync(dto);
            var label = dto.Decision == StepStatus.Approved ? "aprovada" : "reprovada";
            TempData["Success"] = $"Etapa {label} com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Erro ao registrar revisão: {ex.Message}";
        }

        return RedirectToAction("Details", "PipelineRuns", new { id = runId });
    }
}
