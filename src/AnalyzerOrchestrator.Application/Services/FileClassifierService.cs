using AnalyzerOrchestrator.Application.Interfaces;
using AnalyzerOrchestrator.Domain.Enums;

namespace AnalyzerOrchestrator.Application.Services;

/// <summary>
/// Classifica arquivos por papel provável usando heurísticas simples e extensíveis.
/// Baseado em: nome do arquivo, pasta e extensão.
/// </summary>
public class FileClassifierService : IFileClassifierService
{
    public FileRole Classify(string relativePath, string extension)
    {
        var lower = relativePath.Replace('\\', '/').ToLowerInvariant();
        var fileName = Path.GetFileNameWithoutExtension(lower);
        var ext = extension.ToLowerInvariant();

        // SQL
        if (ext is ".sql") return FileRole.SQL;

        // Scripts
        if (ext is ".ps1" or ".sh" or ".bat" or ".cmd") return FileRole.Script;

        // Config
        if (ext is ".json" or ".xml" or ".config" or ".yaml" or ".yml" or ".env")
        {
            if (fileName.Contains("appsettings") || fileName.Contains("config") ||
                fileName.Contains("settings") || ext is ".config")
                return FileRole.Config;
        }

        // Views
        if (ext is ".cshtml" or ".razor" or ".html" or ".htm")
            return FileRole.View;

        // Migrations
        if (lower.Contains("/migrations/") || lower.Contains("\\migrations\\"))
            return FileRole.Migration;

        // Startup / Program
        if (fileName is "program" or "startup" or "appsettings")
            return FileRole.Startup;

        // Controllers
        if (fileName.EndsWith("controller") || lower.Contains("/controllers/"))
            return FileRole.Controller;

        // Services
        if (fileName.EndsWith("service") || lower.Contains("/services/"))
            return FileRole.Service;

        // Repositories
        if (fileName.EndsWith("repository") || fileName.EndsWith("repo") ||
            lower.Contains("/repositories/") || lower.Contains("/repository/"))
            return FileRole.Repository;

        // DTOs
        if (fileName.EndsWith("dto") || lower.Contains("/dtos/") || lower.Contains("/dto/"))
            return FileRole.DTO;

        // Entities
        if (lower.Contains("/entities/") || lower.Contains("/entity/"))
            return FileRole.Entity;

        // Domain
        if (lower.Contains("/domain/") || lower.Contains("/enums/") ||
            lower.Contains("/valueobjects/"))
            return FileRole.Domain;

        // Tests
        if (lower.Contains("/tests/") || lower.Contains("/test/") ||
            fileName.EndsWith("tests") || fileName.EndsWith("test") ||
            fileName.EndsWith("spec"))
            return FileRole.Test;

        return FileRole.Other;
    }

    public int CalculateRelevanceScore(string relativePath, string extension, FileRole role)
    {
        var lower = relativePath.Replace('\\', '/').ToLowerInvariant();
        var fileName = Path.GetFileNameWithoutExtension(lower);
        int score = 0;

        // Papel altamente relevante
        score += role switch
        {
            FileRole.Startup => 90,
            FileRole.Controller => 70,
            FileRole.Service => 65,
            FileRole.Repository => 60,
            FileRole.Entity => 55,
            FileRole.Domain => 50,
            FileRole.DTO => 45,
            FileRole.Config => 40,
            FileRole.SQL => 35,
            FileRole.View => 30,
            FileRole.Migration => 20,
            FileRole.Script => 15,
            FileRole.Test => 10,
            _ => 5
        };

        // Arquivos centrais por nome
        if (fileName is "program" or "startup") score += 20;
        if (fileName.Contains("appsettings")) score += 15;
        if (fileName.Contains("dbcontext") || fileName.Contains("context")) score += 15;
        if (fileName.Contains("interface") || fileName.StartsWith("i") && fileName.Length > 2) score += 5;

        // Profundidade: arquivos na raiz ou em pastas de primeiro nível são mais relevantes
        var depth = lower.Count(c => c == '/');
        if (depth <= 2) score += 10;
        else if (depth <= 4) score += 5;

        return Math.Min(score, 100);
    }

    public string GetClassificationNotes(string relativePath, FileRole role)
    {
        var lower = relativePath.Replace('\\', '/').ToLowerInvariant();
        var fileName = Path.GetFileNameWithoutExtension(lower);

        return role switch
        {
            FileRole.Startup => $"Arquivo de inicialização/configuração principal: {fileName}",
            FileRole.Controller => $"Controller detectado por nome ou pasta",
            FileRole.Service => $"Service detectado por nome ou pasta",
            FileRole.Repository => $"Repository detectado por nome ou pasta",
            FileRole.Entity => $"Entity detectada pela pasta /entities/",
            FileRole.Domain => $"Arquivo de domínio (enums, value objects, etc.)",
            FileRole.DTO => $"DTO detectado por nome ou pasta",
            FileRole.Config => $"Arquivo de configuração",
            FileRole.SQL => $"Script SQL",
            FileRole.View => $"View/template de interface",
            FileRole.Migration => $"Migration de banco de dados",
            FileRole.Script => $"Script de automação",
            FileRole.Test => $"Arquivo de teste",
            _ => "Classificação genérica"
        };
    }
}
