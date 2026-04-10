using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AnalyzerOrchestrator.Infrastructure.Persistence;

/// <summary>
/// Factory usada pelo dotnet-ef em design time para criar migrations.
/// Não é utilizada em runtime.
/// </summary>
public class OrchestratorDbContextFactory : IDesignTimeDbContextFactory<OrchestratorDbContext>
{
    public OrchestratorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrchestratorDbContext>();
        optionsBuilder.UseSqlite("Data Source=orchestrator_design.db");

        return new OrchestratorDbContext(optionsBuilder.Options);
    }
}
