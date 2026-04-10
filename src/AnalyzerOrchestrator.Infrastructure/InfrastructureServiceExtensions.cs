using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Infrastructure.Persistence;
using AnalyzerOrchestrator.Infrastructure.Repositories;
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

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IPipelineRunRepository, PipelineRunRepository>();

        return services;
    }
}
