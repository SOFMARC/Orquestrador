using AnalyzerOrchestrator.Application.DTOs.Projects;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Entities;

namespace AnalyzerOrchestrator.Application.Services;

public class ProjectScanSettingsService : IProjectScanSettingsService
{
    private readonly IProjectScanSettingsRepository _repository;

    public ProjectScanSettingsService(IProjectScanSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProjectScanSettingsDto?> GetByProjectIdAsync(int projectId)
    {
        var settings = await _repository.GetByProjectIdAsync(projectId);
        return settings is null ? null : MapToDto(settings);
    }

    public async Task<ProjectScanSettingsDto> SaveAsync(SaveScanSettingsDto dto)
    {
        var existing = await _repository.GetByProjectIdAsync(dto.ProjectId);

        if (existing is null)
        {
            existing = new ProjectScanSettings
            {
                ProjectId = dto.ProjectId,
                CreatedAt = DateTime.UtcNow
            };
        }

        existing.ScanRootPath = dto.ScanRootPath?.Trim();
        existing.AllowedExtensions = dto.AllowedExtensions.Trim();
        existing.IgnoredFolders = dto.IgnoredFolders.Trim();
        existing.MaxFileSizeKb = dto.MaxFileSizeKb;
        existing.IgnoreBinaryFiles = dto.IgnoreBinaryFiles;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveAsync(existing);
        return MapToDto(existing);
    }

    private static ProjectScanSettingsDto MapToDto(ProjectScanSettings s) => new()
    {
        Id = s.Id,
        ProjectId = s.ProjectId,
        ScanRootPath = s.ScanRootPath,
        AllowedExtensions = s.AllowedExtensions,
        IgnoredFolders = s.IgnoredFolders,
        MaxFileSizeKb = s.MaxFileSizeKb,
        IgnoreBinaryFiles = s.IgnoreBinaryFiles
    };
}
