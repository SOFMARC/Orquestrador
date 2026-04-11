using AnalyzerOrchestrator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Configurations;

public class ProjectScanSettingsConfiguration : IEntityTypeConfiguration<ProjectScanSettings>
{
    public void Configure(EntityTypeBuilder<ProjectScanSettings> builder)
    {
        builder.ToTable("ProjectScanSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScanRootPath).HasMaxLength(500);
        builder.Property(x => x.AllowedExtensions).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IgnoredFolders).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.MaxFileSizeKb);
        builder.Property(x => x.IgnoreBinaryFiles).HasDefaultValue(true);

        builder.HasOne(x => x.Project)
            .WithOne(p => p.ScanSettings)
            .HasForeignKey<ProjectScanSettings>(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProjectId).IsUnique();
    }
}
