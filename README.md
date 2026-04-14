# Analyzer Orchestrator

Sistema local Windows para orquestrar a análise e preparação de contexto de projetos de software para desenvolvimento assistido por IA.

---

## Pré-requisitos

| Requisito | Versão mínima |
|-----------|---------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0 |
| Windows, macOS ou Linux | — |

---

## Como executar

```bash
git clone https://github.com/SOFMARC/Orquestrador.git
cd Orquestrador
dotnet run --project src/AnalyzerOrchestrator.Web
```

Acesse `http://localhost:5000`. O banco SQLite e as migrations são aplicados automaticamente na primeira execução.

Para aplicar migrations manualmente:

```bash
dotnet ef database update \
  --project src/AnalyzerOrchestrator.Infrastructure \
  --startup-project src/AnalyzerOrchestrator.Web
```

---

## Estrutura da solução

```
src/
├── AnalyzerOrchestrator.Domain/          # Entidades, enums, regras de domínio
├── AnalyzerOrchestrator.Application/     # DTOs, interfaces, serviços, workflow
├── AnalyzerOrchestrator.Infrastructure/  # EF Core, repositórios, serviços de disco
└── AnalyzerOrchestrator.Web/             # Controllers, Views, Program.cs
```

---

## Workflow implementado

| # | Etapa | Status | Descrição |
|---|-------|--------|-----------|
| 1 | Extração Estrutural | ✅ Implementado | Varredura real do diretório, inventário, árvore, classificação de arquivos |
| 2 | Consolidação Arquitetural | ✅ Implementado | Agrupamento por módulos/camadas, arquivos centrais, resumo arquitetural |
| 3 | Análise de Dependências | 🔜 Próxima etapa | Levantamento de pacotes, dependências externas e integrações |
| 4 | Análise de Banco de Dados | 🔜 Futuro | Identificação de tabelas, relacionamentos e convenções |
| 5 | Preparação do Contexto | 🔜 Futuro | Consolidação final para uso com IA |

---

## Fluxo de uso

### Etapa 1 — Extração Estrutural

1. Crie um **Projeto** com o caminho do repositório local
2. Configure as opções de leitura em **Configurar Leitura** (extensões, pastas ignoradas, tamanho máximo)
3. Crie um **Novo Run** no projeto
4. Na tela de detalhes do Run, clique **Executar** na Etapa 1
5. Revise o resultado e clique **Revisar** para aprovar ou reprovar

**Artefatos gerados em** `workspace/{Projeto}/runs/run_{id}/step_1/`:

| Arquivo | Conteúdo |
|---------|----------|
| `inventory.json` | Lista completa de arquivos com metadados |
| `tree.txt` | Árvore ASCII de pastas e arquivos |
| `relevant-files.json` | Arquivos mais relevantes com score e papel |
| `summary.md` | Resumo executivo da extração |

### Etapa 2 — Consolidação Arquitetural

> Requer que a Etapa 1 esteja **aprovada**.

1. Na tela de detalhes do Run, clique **Executar** na Etapa 2
2. Confirme a execução na tela de confirmação
3. Visualize o resultado: módulos, camadas, arquivos centrais, observações
4. Clique **Revisar** para aprovar ou reprovar a consolidação

**Artefatos gerados em** `workspace/{Projeto}/runs/run_{id}/step_2/`:

| Arquivo | Conteúdo |
|---------|----------|
| `modules-map.json` | Mapa de módulos com distribuição por papel |
| `architecture-summary.md` | Resumo arquitetural em Markdown |
| `layer-distribution.json` | Distribuição de arquivos por camada |
| `central-files.json` | Arquivos centrais identificados |
| `step-2-summary.md` | Resumo executivo da consolidação |

---

## Entidades principais

| Entidade | Descrição |
|----------|-----------|
| `Project` | Projeto de software com caminho do repositório |
| `ProjectScanSettings` | Configurações de leitura (extensões, pastas ignoradas) |
| `PipelineRun` | Execução de análise vinculada a um projeto |
| `PipelineStepExecution` | Execução de uma etapa específica do workflow |
| `ScannedFile` | Arquivo descoberto na varredura estrutural |
| `Artifact` | Artefato gerado em disco, vinculado à run |

---

## Migrations

| Migration | Descrição |
|-----------|-----------|
| `InitialCreate` | Estrutura base: Project, PipelineRun, StepExecution, Artifact |
| `AddStep2Entities` | ProjectScanSettings, ScannedFile, campos de revisão humana |

---

## Decisões técnicas

- **SQLite** zero-config para uso local. O arquivo `orchestrator.db` fica na pasta do projeto Web.
- **Migrations automáticas** na inicialização simplificam o setup sem comandos adicionais.
- **Workflow definido em código** (`DefaultAnalysisWorkflow`) centraliza as etapas, permitindo evolução sem alterar o banco.
- **Arquitetura em camadas** garante separação clara de responsabilidades.
- **Serviços de disco na Infrastructure** — `StructuralExtractionService` e `ArchitecturalConsolidationService` acessam o sistema de arquivos e são registrados na camada Infrastructure.

---

## Build

```bash
dotnet build -c Release -warnaserror
```

Build esperado: **succeeded** com **0 erros, 0 warnings**.

---

## Desenvolvimento

```bash
# Nova migration
dotnet ef migrations add NomeDaMigration \
  --project src/AnalyzerOrchestrator.Infrastructure \
  --startup-project src/AnalyzerOrchestrator.Web

# Aplicar migrations
dotnet ef database update \
  --project src/AnalyzerOrchestrator.Infrastructure \
  --startup-project src/AnalyzerOrchestrator.Web
```
