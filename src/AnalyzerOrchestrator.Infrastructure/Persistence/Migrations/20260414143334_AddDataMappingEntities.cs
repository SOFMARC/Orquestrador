using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataMappingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectedTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PipelineRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ConfidenceScore = table.Column<int>(type: "INTEGER", nullable: false),
                    EvidenceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OperationsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectedTables_PipelineRuns_PipelineRunId",
                        column: x => x.PipelineRunId,
                        principalTable: "PipelineRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TableFileRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DetectedTableId = table.Column<int>(type: "INTEGER", nullable: false),
                    PipelineRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelativeFilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    FileRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimaryOperation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OperationsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ContextSnippet = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EvidenceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableFileRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableFileRelations_DetectedTables_DetectedTableId",
                        column: x => x.DetectedTableId,
                        principalTable: "DetectedTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetectedTables_PipelineRunId",
                table: "DetectedTables",
                column: "PipelineRunId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedTables_PipelineRunId_TableName",
                table: "DetectedTables",
                columns: new[] { "PipelineRunId", "TableName" });

            migrationBuilder.CreateIndex(
                name: "IX_TableFileRelations_DetectedTableId",
                table: "TableFileRelations",
                column: "DetectedTableId");

            migrationBuilder.CreateIndex(
                name: "IX_TableFileRelations_PipelineRunId",
                table: "TableFileRelations",
                column: "PipelineRunId");

            migrationBuilder.CreateIndex(
                name: "IX_TableFileRelations_PipelineRunId_RelativeFilePath",
                table: "TableFileRelations",
                columns: new[] { "PipelineRunId", "RelativeFilePath" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TableFileRelations");

            migrationBuilder.DropTable(
                name: "DetectedTables");
        }
    }
}
