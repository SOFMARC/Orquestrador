using AnalyzerOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Configurations;

public class PipelineStepExecutionConfiguration : IEntityTypeConfiguration<PipelineStepExecution>
{
    public void Configure(EntityTypeBuilder<PipelineStepExecution> builder)
    {
        builder.ToTable("PipelineStepExecutions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StepName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Notes)
            .HasMaxLength(2000);

        builder.Property(s => s.ErrorMessage)
            .HasMaxLength(4000);

        // Métricas de execução
        builder.Property(s => s.FilesFound);
        builder.Property(s => s.FilesIgnored);
        builder.Property(s => s.ErrorCount);

        // Revisão humana
        builder.Property(s => s.ReviewedBy).HasMaxLength(200);
        builder.Property(s => s.ReviewNotes).HasMaxLength(2000);

        builder.HasIndex(s => s.PipelineRunId);
        builder.HasIndex(s => new { s.PipelineRunId, s.StepNumber });
    }
}
