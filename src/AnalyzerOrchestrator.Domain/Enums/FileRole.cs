namespace AnalyzerOrchestrator.Domain.Enums;

/// <summary>
/// Papel provável de um arquivo dentro da estrutura do projeto.
/// Classificado por heurística baseada em nome, pasta e extensão.
/// </summary>
public enum FileRole
{
    Other = 0,
    Controller = 1,
    Service = 2,
    Repository = 3,
    Domain = 4,
    Entity = 5,
    DTO = 6,
    View = 7,
    Config = 8,
    SQL = 9,
    Script = 10,
    Migration = 11,
    Test = 12,
    Startup = 13
}
