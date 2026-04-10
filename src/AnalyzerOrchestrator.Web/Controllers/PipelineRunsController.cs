using AnalyzerOrchestrator.Application.DTOs.PipelineRuns;
using AnalyzerOrchestrator.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerOrchestrator.Web.Controllers;

public class PipelineRunsController : Controller
{
    private readonly IPipelineRunService _runService;
    private readonly IProjectService _projectService;

    public PipelineRunsController(
        IPipelineRunService runService,
        IProjectService projectService)
    {
        _runService = runService;
        _projectService = projectService;
    }

    // GET: /PipelineRuns/ByProject/5
    public async Task<IActionResult> ByProject(int projectId)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        var runs = await _runService.GetByProjectAsync(projectId);
        ViewBag.Project = project;
        return View(runs);
    }

    // GET: /PipelineRuns/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var run = await _runService.GetByIdAsync(id);
        if (run is null) return NotFound();
        return View(run);
    }

    // GET: /PipelineRuns/Create?projectId=5
    public async Task<IActionResult> Create(int projectId)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        ViewBag.Project = project;
        var dto = new CreatePipelineRunDto { ProjectId = projectId };
        return View(dto);
    }

    // POST: /PipelineRuns/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePipelineRunDto dto)
    {
        if (!ModelState.IsValid)
        {
            var project = await _projectService.GetByIdAsync(dto.ProjectId);
            ViewBag.Project = project;
            return View(dto);
        }

        var run = await _runService.CreateAsync(dto);
        TempData["Success"] = $"Pipeline Run #{run.Id} criado com sucesso.";
        return RedirectToAction(nameof(Details), new { id = run.Id });
    }

    // POST: /PipelineRuns/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var run = await _runService.GetByIdAsync(id);
        if (run is null) return NotFound();

        await _runService.CancelAsync(id);
        TempData["Info"] = $"Pipeline Run #{id} cancelado.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
