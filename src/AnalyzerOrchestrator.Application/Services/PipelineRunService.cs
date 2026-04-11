using AnalyzerOrchestrator.Application.DTOs.PipelineRuns;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Application.Workflow;
using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.Services;

public class PipelineRunService : IPipelineRunService
{
    private readonly IPipelineRunRepository _runRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkflowDefinition _workflowDefinition;

    public PipelineRunService(
        IPipelineRunRepository runRepository,
        IProjectRepository projectRepository,
        IWorkflowDefinition workflowDefinition)
    {
        _runRepository = runRepository;
        _projectRepository = projectRepository;
        _workflowDefinition = workflowDefinition;
    }

    public async Task<IEnumerable<PipelineRunDto>> GetByProjectAsync(int projectId)
    {
        var runs = await _runRepository.GetByProjectAsync(projectId);
        return runs.Select(MapToDto);
    }

    public async Task<PipelineRunDto?> GetByIdAsync(int id)
    {
        var run = await _runRepository.GetWithStepsAsync(id);
        return run is null ? null : MapToDtoWithSteps(run);
    }

    public async Task<PipelineRunDto> CreateAsync(CreatePipelineRunDto dto)
    {
        var project = await _projectRepository.GetByIdAsync(dto.ProjectId)
            ?? throw new InvalidOperationException($"Projeto {dto.ProjectId} não encontrado.");

        var run = new PipelineRun
        {
            ProjectId = dto.ProjectId,
            Status = RunStatus.Pending,
            Notes = dto.Notes?.Trim(),
            TriggerSource = dto.TriggerSource?.Trim() ?? "Manual",
            CreatedAt = DateTime.UtcNow
        };

        // Criar as etapas do workflow automaticamente
        var steps = _workflowDefinition.GetSteps();
        foreach (var stepDef in steps)
        {
            run.StepExecutions.Add(new PipelineStepExecution
            {
                StepNumber = stepDef.StepNumber,
                StepName = stepDef.Name,
                Status = StepStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _runRepository.AddAsync(run);
        return MapToDtoWithSteps(run);
    }

    public async Task<bool> CancelAsync(int id)
    {
        var run = await _runRepository.GetByIdAsync(id);
        if (run is null) return false;
        if (run.Status == RunStatus.Completed || run.Status == RunStatus.Cancelled) return false;

        run.Status = RunStatus.Cancelled;
        run.FinishedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;

        await _runRepository.UpdateAsync(run);
        return true;
    }

    private static PipelineRunDto MapToDto(PipelineRun r) => new()
    {
        Id = r.Id,
        ProjectId = r.ProjectId,
        ProjectName = r.Project?.Name ?? string.Empty,
        Status = r.Status,
        StartedAt = r.StartedAt,
        FinishedAt = r.FinishedAt,
        CurrentStep = r.CurrentStep,
        Notes = r.Notes,
        TriggerSource = r.TriggerSource,
        CreatedAt = r.CreatedAt,
        TotalSteps = r.StepExecutions?.Count ?? 0,
        CompletedSteps = r.StepExecutions?.Count(s =>
            s.Status == StepStatus.Completed ||
            s.Status == StepStatus.Approved) ?? 0
    };

    private static PipelineRunDto MapToDtoWithSteps(PipelineRun r)
    {
        var dto = MapToDto(r);
        dto.StepExecutions = r.StepExecutions?
            .OrderBy(s => s.StepNumber)
            .Select(s => new PipelineStepExecutionDto
            {
                Id = s.Id,
                PipelineRunId = s.PipelineRunId,
                StepNumber = s.StepNumber,
                StepName = s.StepName,
                Status = s.Status,
                StartedAt = s.StartedAt,
                FinishedAt = s.FinishedAt,
                Notes = s.Notes,
                ErrorMessage = s.ErrorMessage,
                FilesFound = s.FilesFound,
                FilesIgnored = s.FilesIgnored,
                ErrorCount = s.ErrorCount,
                ReviewedAt = s.ReviewedAt,
                ReviewedBy = s.ReviewedBy,
                ReviewNotes = s.ReviewNotes
            }).ToList() ?? new();
        return dto;
    }
}
