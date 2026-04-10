using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Application.Services;
using AnalyzerOrchestrator.Application.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace AnalyzerOrchestrator.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IPipelineRunService, PipelineRunService>();
        services.AddSingleton<IWorkflowDefinition, DefaultAnalysisWorkflow>();

        return services;
    }
}
