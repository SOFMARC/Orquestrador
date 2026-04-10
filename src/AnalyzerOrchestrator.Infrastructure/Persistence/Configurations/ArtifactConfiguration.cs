using AnalyzerOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Configurations;

public class ArtifactConfiguration : IEntityTypeConfiguration<Artifact>
{
    public void Configure(EntityTypeBuilder<Artifact> builder)
    {
        builder.ToTable("Artifacts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.FilePath)
            .HasMaxLength(500);

        builder.Property(a => a.MimeType)
            .HasMaxLength(100);

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        // Content pode ser grande (documento de contexto)
        builder.Property(a => a.Content)
            .HasColumnType("TEXT");

        builder.HasIndex(a => a.PipelineRunId);
    }
}
