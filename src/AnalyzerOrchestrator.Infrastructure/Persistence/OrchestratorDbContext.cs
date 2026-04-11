using AnalyzerOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerOrchestrator.Infrastructure.Persistence;

public class OrchestratorDbContext : DbContext
{
    public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectScanSettings> ProjectScanSettings => Set<ProjectScanSettings>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();
    public DbSet<PipelineStepExecution> PipelineStepExecutions => Set<PipelineStepExecution>();
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<ScannedFile> ScannedFiles => Set<ScannedFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrchestratorDbContext).Assembly);
    }
}
