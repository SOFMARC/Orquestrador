using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStep2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErrorCount",
                table: "PipelineStepExecutions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FilesFound",
                table: "PipelineStepExecutions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FilesIgnored",
                table: "PipelineStepExecutions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "PipelineStepExecutions",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "PipelineStepExecutions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "PipelineStepExecutions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectScanSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScanRootPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AllowedExtensions = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IgnoredFolders = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    MaxFileSizeKb = table.Column<int>(type: "INTEGER", nullable: true),
                    IgnoreBinaryFiles = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectScanSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectScanSettings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScannedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PipelineRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FullPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsRelevant = table.Column<bool>(type: "INTEGER", nullable: false),
                    RelevanceScore = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassificationNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScannedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScannedFiles_PipelineRuns_PipelineRunId",
                        column: x => x.PipelineRunId,
                        principalTable: "PipelineRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectScanSettings_ProjectId",
                table: "ProjectScanSettings",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScannedFiles_PipelineRunId",
                table: "ScannedFiles",
                column: "PipelineRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ScannedFiles_PipelineRunId_IsRelevant",
                table: "ScannedFiles",
                columns: new[] { "PipelineRunId", "IsRelevant" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectScanSettings");

            migrationBuilder.DropTable(
                name: "ScannedFiles");

            migrationBuilder.DropColumn(
                name: "ErrorCount",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "FilesFound",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "FilesIgnored",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "PipelineStepExecutions");
        }
    }
}
