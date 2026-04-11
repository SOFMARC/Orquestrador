using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Application.Services;
using AnalyzerOrchestrator.Application.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace AnalyzerOrchestrator.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Serviços de domínio
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IPipelineRunService, PipelineRunService>();
        services.AddScoped<IProjectScanSettingsService, ProjectScanSettingsService>();

        // Classificador de arquivos (stateless, singleton)
        services.AddSingleton<IFileClassifierService, FileClassifierService>();

        // Workflow
        services.AddSingleton<IWorkflowDefinition, DefaultAnalysisWorkflow>();

        // O StructuralExtractionService é registrado na Infrastructure pois acessa disco
        // services.AddScoped<IStructuralExtractionService, StructuralExtractionService>();

        return services;
    }
}
