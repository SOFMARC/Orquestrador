using AnalyzerOrchestrator.Application.DTOs.Projects;
using AnalyzerOrchestrator.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerOrchestrator.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    // GET: /Projects
    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();
        return View(projects);
    }

    // GET: /Projects/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null) return NotFound();
        return View(project);
    }

    // GET: /Projects/Create
    public IActionResult Create()
    {
        return View(new CreateProjectDto());
    }

    // POST: /Projects/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProjectDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var created = await _projectService.CreateAsync(dto);
        TempData["Success"] = $"Projeto \"{created.Name}\" criado com sucesso.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    // GET: /Projects/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null) return NotFound();

        var dto = new EditProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            RepositoryPath = project.RepositoryPath,
            TechnologyStack = project.TechnologyStack,
            IsActive = project.IsActive
        };
        return View(dto);
    }

    // POST: /Projects/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditProjectDto dto)
    {
        if (id != dto.Id) return BadRequest();
        if (!ModelState.IsValid) return View(dto);

        var updated = await _projectService.UpdateAsync(id, dto);
        if (updated is null) return NotFound();

        TempData["Success"] = $"Projeto \"{updated.Name}\" atualizado com sucesso.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Projects/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await _projectService.DeleteAsync(id);
        if (!deleted) return NotFound();

        TempData["Success"] = "Projeto removido com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
