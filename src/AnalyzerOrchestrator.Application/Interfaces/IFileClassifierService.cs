using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.Interfaces;

/// <summary>
/// Classifica arquivos por papel (FileRole) e calcula score de relevância.
/// Usa heurísticas baseadas em nome, pasta e extensão.
/// </summary>
public interface IFileClassifierService
{
    FileRole Classify(string relativePath, string extension);
    int CalculateRelevanceScore(string relativePath, string extension, FileRole role);
    string GetClassificationNotes(string relativePath, FileRole role);
}
