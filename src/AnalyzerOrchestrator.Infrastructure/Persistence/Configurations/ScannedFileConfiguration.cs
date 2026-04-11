using AnalyzerOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Configurations;

public class ScannedFileConfiguration : IEntityTypeConfiguration<ScannedFile>
{
    public void Configure(EntityTypeBuilder<ScannedFile> builder)
    {
        builder.ToTable("ScannedFiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RelativePath).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.FullPath).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.Extension).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ClassificationNotes).HasMaxLength(500);

        builder.HasOne(x => x.PipelineRun)
            .WithMany(r => r.ScannedFiles)
            .HasForeignKey(x => x.PipelineRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PipelineRunId);
        builder.HasIndex(x => new { x.PipelineRunId, x.IsRelevant });
    }
}
