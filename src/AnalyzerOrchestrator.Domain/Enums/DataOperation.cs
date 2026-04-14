namespace AnalyzerOrchestrator.Domain.Enums;

/// <summary>
/// Operação de dados detectada por heurística em um arquivo.
/// </summary>
public enum DataOperation
{
    Unknown = 0,
    Read = 1,
    Insert = 2,
    Update = 3,
    Delete = 4,
    Join = 5,
    Filter = 6,
    CreateTable = 7,
    AlterTable = 8,
    Reference = 9,   // nome da tabela aparece como referência (ex: propriedade, classe)
}
