using AnalyzerOrchestrator.Domain.Entities;
using AnalyzerOrchestrator.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Configurations;

public class TableFileRelationConfiguration : IEntityTypeConfiguration<TableFileRelation>
{
    public void Configure(EntityTypeBuilder<TableFileRelation> builder)
    {
        builder.ToTable("TableFileRelations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RelativeFilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(x => x.FileRole)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Extension)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.PrimaryOperation)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.OperationsJson)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(x => x.ContextSnippet)
            .HasMaxLength(500);

        builder.Property(x => x.EvidenceType)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.DetectedTableId);
        builder.HasIndex(x => x.PipelineRunId);
        builder.HasIndex(x => new { x.PipelineRunId, x.RelativeFilePath });
    }
}
