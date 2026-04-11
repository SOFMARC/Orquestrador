using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using AnalyzerOrchestrator.Infrastructure.Repositories;
using AnalyzerOrchestrator.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnalyzerOrchestrator.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<OrchestratorDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositórios
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IPipelineRunRepository, PipelineRunRepository>();
        services.AddScoped<IProjectScanSettingsRepository, ProjectScanSettingsRepository>();
        services.AddScoped<IScannedFileRepository, ScannedFileRepository>();
        services.AddScoped<IArtifactRepository, ArtifactRepository>();

        // Serviços de infraestrutura (acesso a disco)
        services.AddScoped<IStructuralExtractionService, StructuralExtractionService>();

        return services;
    }
}
