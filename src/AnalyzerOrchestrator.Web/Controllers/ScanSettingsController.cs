using AnalyzerOrchestrator.Application.DTOs.Projects;
using AnalyzerOrchestrator.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerOrchestrator.Web.Controllers;

public class ScanSettingsController : Controller
{
    private readonly IProjectScanSettingsService _settingsService;
    private readonly IProjectService _projectService;

    public ScanSettingsController(
        IProjectScanSettingsService settingsService,
        IProjectService projectService)
    {
        _settingsService = settingsService;
        _projectService = projectService;
    }

    // GET /ScanSettings/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null) return NotFound();

        var settings = await _settingsService.GetByProjectIdAsync(id);

        var dto = settings is not null
            ? new SaveScanSettingsDto
            {
                ProjectId = id,
                ScanRootPath = settings.ScanRootPath,
                AllowedExtensions = settings.AllowedExtensions,
                IgnoredFolders = settings.IgnoredFolders,
                MaxFileSizeKb = settings.MaxFileSizeKb,
                IgnoreBinaryFiles = settings.IgnoreBinaryFiles
            }
            : new SaveScanSettingsDto
            {
                ProjectId = id,
                ScanRootPath = project.RepositoryPath
            };

        ViewBag.ProjectName = project.Name;
        ViewBag.ProjectId = id;
        return View(dto);
    }

    // POST /ScanSettings/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SaveScanSettingsDto dto)
    {
        if (id != dto.ProjectId) return BadRequest();

        if (!ModelState.IsValid)
        {
            var project = await _projectService.GetByIdAsync(id);
            ViewBag.ProjectName = project?.Name;
            ViewBag.ProjectId = id;
            return View(dto);
        }

        await _settingsService.SaveAsync(dto);
        TempData["Success"] = "Configurações de leitura salvas com sucesso.";
        return RedirectToAction("Details", "Projects", new { id });
    }
}
