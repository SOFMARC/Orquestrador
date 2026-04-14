using AnalyzerOrchestrator.Application.DTOs.Extraction;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerOrchestrator.Web.Controllers;

/// <summary>
/// Controla as ações da Etapa 3 — Mapeamento Inicial de Dados.
/// </summary>
public class DataMappingController : Controller
{
    private readonly IDataMappingService _dataMappingService;
    private readonly IPipelineRunService _pipelineRunService;

    public DataMappingController(
        IDataMappingService dataMappingService,
        IPipelineRunService pipelineRunService)
    {
        _dataMappingService = dataMappingService;
        _pipelineRunService = pipelineRunService;
    }

    // GET /DataMapping/Execute/{runId}
    [HttpGet]
    public async Task<IActionResult> Execute(int id)
    {
        var run = await _pipelineRunService.GetByIdAsync(id);
        if (run is null) return NotFound();

        // Verificar se já foi executada e está em revisão/aprovada
        var step = run.StepExecutions.FirstOrDefault(s => s.StepNumber == 3);
        if (step is not null &&
            step.Status is not (StepStatus.Pending or StepStatus.Rejected or StepStatus.Failed))
        {
            return RedirectToAction(nameof(Result), new { id });
        }

        ViewBag.Run = run;
        return View();
    }

    // POST /DataMapping/Execute/{runId}
    [HttpPost, ActionName("Execute")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecutePost(int id)
    {
        var result = await _dataMappingService.ExecuteAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Execute), new { id });
        }

        TempData["Success"] = $"Mapeamento concluído: {result.TablesCount} tabelas detectadas em {result.FilesAnalyzed} arquivos.";
        return RedirectToAction(nameof(Result), new { id });
    }

    // GET /DataMapping/Result/{runId}
    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var run = await _pipelineRunService.GetByIdAsync(id);
        if (run is null) return NotFound();

        var result = await _dataMappingService.GetResultAsync(id);
        if (result is null)
        {
            TempData["Warning"] = "A Etapa 3 ainda não foi executada para esta run.";
            return RedirectToAction(nameof(Execute), new { id });
        }

        var step = run.StepExecutions.FirstOrDefault(s => s.StepNumber == 3);
        ViewBag.Run = run;
        ViewBag.Step = step;
        return View(result);
    }

    // GET /DataMapping/Review/{stepExecutionId}?runId={runId}
    [HttpGet]
    public async Task<IActionResult> Review(int id, int runId)
    {
        var run = await _pipelineRunService.GetByIdAsync(runId);
        if (run is null) return NotFound();

        var step = run.StepExecutions.FirstOrDefault(s => s.Id == id);
        if (step is null) return NotFound();

        if (step.Status != StepStatus.AwaitingReview)
        {
            TempData["Warning"] = "Esta etapa não está aguardando revisão.";
            return RedirectToAction("Details", "PipelineRuns", new { id = runId });
        }

        var dto = new StepReviewDto
        {
            StepExecutionId = id,
            RunId = runId
        };

        ViewBag.Run = run;
        ViewBag.Step = step;
        return View(dto);
    }

    // POST /DataMapping/Review/{stepExecutionId}
    [HttpPost, ActionName("Review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewPost(int id, StepReviewDto dto)
    {
        dto.StepExecutionId = id;

        if (dto.Decision != StepStatus.Approved && dto.Decision != StepStatus.Rejected)
        {
            ModelState.AddModelError("Decision", "Selecione Aprovar ou Reprovar.");
            var run = await _pipelineRunService.GetByIdAsync(dto.RunId);
            var step = run?.StepExecutions.FirstOrDefault(s => s.Id == id);
            ViewBag.Run = run;
            ViewBag.Step = step;
            return View(dto);
        }

        await _dataMappingService.ReviewStepAsync(dto);

        TempData["Success"] = dto.Decision == StepStatus.Approved
            ? "Etapa 3 aprovada com sucesso."
            : "Etapa 3 reprovada. Você pode executá-la novamente.";

        return RedirectToAction("Details", "PipelineRuns", new { id = dto.RunId });
    }
}
