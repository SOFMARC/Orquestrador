using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalyzerOrchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StabilizationMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CentralFilesCount",
                table: "PipelineStepExecutions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LayersCount",
                table: "PipelineStepExecutions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModulesCount",
                table: "PipelineStepExecutions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelationsCount",
                table: "PipelineStepExecutions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TablesCount",
                table: "PipelineStepExecutions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StepNumber",
                table: "Artifacts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_PipelineRunId_StepNumber",
                table: "Artifacts",
                columns: new[] { "PipelineRunId", "StepNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Artifacts_PipelineRunId_StepNumber",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "CentralFilesCount",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "LayersCount",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "ModulesCount",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "RelationsCount",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "TablesCount",
                table: "PipelineStepExecutions");

            migrationBuilder.DropColumn(
                name: "StepNumber",
                table: "Artifacts");
        }
    }
}
