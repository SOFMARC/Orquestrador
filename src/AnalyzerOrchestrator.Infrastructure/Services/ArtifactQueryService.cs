using AnalyzerOrchestrator.Application.DTOs.Artifacts;
using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Enums;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerOrchestrator.Infrastructure.Services;

/// <summary>
/// Implementação de IArtifactQueryService.
/// Consulta artefatos no banco e lê conteúdo do disco quando necessário.
/// </summary>
public class ArtifactQueryService : IArtifactQueryService
{
    private readonly OrchestratorDbContext _context;

    public ArtifactQueryService(OrchestratorDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ArtifactDto>> GetByRunAsync(int pipelineRunId)
    {
        var artifacts = await _context.Artifacts
            .Where(a => a.PipelineRunId == pipelineRunId)
            .OrderBy(a => a.StepNumber)
            .ThenBy(a => a.Name)
            .ToListAsync();

        return artifacts.Select(MapToDto);
    }

    public async Task<IEnumerable<ArtifactDto>> GetByRunAndStepAsync(int pipelineRunId, int stepNumber)
    {
        var artifacts = await _context.Artifacts
            .Where(a => a.PipelineRunId == pipelineRunId && a.StepNumber == stepNumber)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return artifacts.Select(MapToDto);
    }

    public async Task<string?> GetContentAsync(int artifactId)
    {
        var artifact = await _context.Artifacts.FindAsync(artifactId);
        if (artifact is null) return null;

        // Prioriza conteúdo em banco (para artefatos pequenos)
        if (!string.IsNullOrEmpty(artifact.Content))
            return artifact.Content;

        // Lê do disco se houver caminho
        if (!string.IsNullOrEmpty(artifact.FilePath) && File.Exists(artifact.FilePath))
            return await File.ReadAllTextAsync(artifact.FilePath);

        return null;
    }

    public async Task<Dictionary<ArtifactType, string>> GetApprovedContentsAsync(int pipelineRunId)
    {
        // Busca run com steps e artefatos
        var run = await _context.PipelineRuns
            .Include(r => r.StepExecutions)
            .Include(r => r.Artifacts)
            .FirstOrDefaultAsync(r => r.Id == pipelineRunId);

        if (run is null) return new();

        // Apenas artefatos de etapas aprovadas
        var approvedStepNumbers = run.StepExecutions
            .Where(s => s.Status == Domain.Enums.StepStatus.Approved)
            .Select(s => s.StepNumber)
            .ToHashSet();

        var result = new Dictionary<ArtifactType, string>();

        foreach (var artifact in run.Artifacts.Where(a => approvedStepNumbers.Contains(a.StepNumber)))
        {
            string? content = null;

            if (!string.IsNullOrEmpty(artifact.Content))
                content = artifact.Content;
            else if (!string.IsNullOrEmpty(artifact.FilePath) && File.Exists(artifact.FilePath))
                content = await File.ReadAllTextAsync(artifact.FilePath);

            if (content is not null)
                result[artifact.Type] = content;
        }

        return result;
    }

    private static ArtifactDto MapToDto(Domain.Entities.Artifact a) => new()
    {
        Id = a.Id,
        PipelineRunId = a.PipelineRunId,
        StepNumber = a.StepNumber,
        Name = a.Name,
        Type = a.Type,
        FilePath = a.FilePath,
        MimeType = a.MimeType,
        SizeBytes = a.SizeBytes,
        Notes = a.Notes,
        CreatedAt = a.CreatedAt
    };
}
