using System.ComponentModel.DataAnnotations;

namespace AnalyzerOrchestrator.Application.DTOs.Projects;

public class CreateProjectDto
{
    [Required(ErrorMessage = "O nome do projeto é obrigatório.")]
    [StringLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "O caminho do repositório deve ter no máximo 500 caracteres.")]
    public string? RepositoryPath { get; set; }

    [StringLength(300, ErrorMessage = "A stack tecnológica deve ter no máximo 300 caracteres.")]
    public string? TechnologyStack { get; set; }
}
