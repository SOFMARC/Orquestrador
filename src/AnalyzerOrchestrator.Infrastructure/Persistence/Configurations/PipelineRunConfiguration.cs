using AnalyzerOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Configurations;

public class PipelineRunConfiguration : IEntityTypeConfiguration<PipelineRun>
{
    public void Configure(EntityTypeBuilder<PipelineRun> builder)
    {
        builder.ToTable("PipelineRuns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.CurrentStep)
            .HasMaxLength(200);

        builder.Property(r => r.Notes)
            .HasMaxLength(2000);

        builder.Property(r => r.TriggerSource)
            .HasMaxLength(100);

        builder.HasMany(r => r.StepExecutions)
            .WithOne(s => s.PipelineRun)
            .HasForeignKey(s => s.PipelineRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Artifacts)
            .WithOne(a => a.PipelineRun)
            .HasForeignKey(a => a.PipelineRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.ProjectId);
        builder.HasIndex(r => r.Status);
    }
}
