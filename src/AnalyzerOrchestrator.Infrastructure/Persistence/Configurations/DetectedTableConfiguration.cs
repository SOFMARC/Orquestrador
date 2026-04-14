using AnalyzerOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Configurations;

public class DetectedTableConfiguration : IEntityTypeConfiguration<DetectedTable>
{
    public void Configure(EntityTypeBuilder<DetectedTable> builder)
    {
        builder.ToTable("DetectedTables");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TableName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.OriginalName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.EvidenceType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OperationsJson)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.PipelineRun)
            .WithMany(r => r.DetectedTables)
            .HasForeignKey(x => x.PipelineRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FileRelations)
            .WithOne(r => r.DetectedTable)
            .HasForeignKey(r => r.DetectedTableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PipelineRunId);
        builder.HasIndex(x => new { x.PipelineRunId, x.TableName });
    }
}
