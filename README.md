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
| 3 | Mapeamento Inicial de Dados | ✅ Implementado | Detecção de tabelas por heurística, relações tabela↔arquivo, operações detectadas |
| 4 | Requisito Estruturado e Impacto Inicial | 🔜 Próxima etapa | Levantamento de requisitos estruturados e análise de impacto inicial |
| 5 | Preparação do Contexto | 🔜 Futuro | Consolidação final de todos os artefatos para uso com IA |

> As constantes de número de etapa estão centralizadas em `DefaultAnalysisWorkflow` — nunca use literais numéricos no código.

---

## Fluxo de uso

### Etapa 1 — Extração Estrutural

1. Crie um **Projeto** com o caminho do repositório local
2. Configure as opções de leitura em **Configurar Leitura** (extensões, pastas ignoradas, tamanho máximo)
3. Crie um **Novo Run** no projeto
4. Na tela de detalhes do Run, clique **Executar** na Etapa 1
5. Revise o resultado e clique **Revisar** para aprovar ou reprovar

**Artefatos gerados em** (Windows) `%APPDATA%\AnalyzerOrchestrator\workspace\{Projeto}\runs\run_{id}\step_1\`:

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

**Artefatos gerados em** (Windows) `%APPDATA%\AnalyzerOrchestrator\workspace\{Projeto}\runs\run_{id}\step_2\`:

| Arquivo | Conteúdo |
|---------|----------|
| `modules-map.json` | Mapa de módulos com distribuição por papel |
| `architecture-summary.md` | Resumo arquitetural em Markdown |
| `layer-distribution.json` | Distribuição de arquivos por camada |
| `central-files.json` | Arquivos centrais identificados |
| `step-2-summary.md` | Resumo executivo da consolidação |

### Etapa 3 — Mapeamento Inicial de Dados

> Requer que a Etapa 1 esteja **aprovada**. Pode ser executada em paralelo com a Etapa 2.

1. Na tela de detalhes do Run, clique **Executar** na Etapa 3
2. Aguarde a análise dos arquivos por heurística (SQL, ORM, convenção de nomes)
3. Visualize o resultado: tabelas detectadas com nível de confiança, relações tabela↔arquivo e operações
4. Clique **Revisar** para aprovar ou reprovar o mapeamento

**Artefatos gerados em** (Windows) `%APPDATA%\AnalyzerOrchestrator\workspace\{Projeto}\runs\run_{id}\step_3\`:

| Arquivo | Conteúdo |
|---------|----------|
| `detected-tables.json` | Tabelas detectadas com score de confiança e evidências |
| `table-file-relations.json` | Mapeamento tabela → arquivos onde é referenciada |
| `file-table-relations.json` | Mapeamento arquivo → tabelas que referencia |
| `table-operations.json` | Operações detectadas por tabela (SELECT, INSERT, UPDATE...) |
| `data-mapping-summary.md` | Resumo executivo do mapeamento de dados |

> **Linux/macOS:** os artefatos são salvos em `~/.analyzer-orchestrator/workspace/{Projeto}/runs/run_{id}/step_{n}/`.

---

## Entidades principais

| Entidade | Descrição |
|----------|-----------|
| `Project` | Projeto de software com caminho do repositório |
| `ProjectScanSettings` | Configurações de leitura (extensões, pastas ignoradas) |
| `PipelineRun` | Execução de análise vinculada a um projeto |
| `PipelineStepExecution` | Execução de uma etapa específica do workflow |
| `ScannedFile` | Arquivo descoberto na varredura estrutural |
| `Artifact` | Artefato gerado em disco, vinculado à run e à etapa (`StepNumber`) |
| `DetectedTable` | Tabela detectada por heurística na Etapa 3 |
| `TableFileRelation` | Relação entre tabela detectada e arquivo onde é referenciada |

---

## Migrations

| Migration | Descrição |
|-----------|----------|
| `InitialCreate` | Estrutura base: Project, PipelineRun, StepExecution, Artifact |
| `AddStep2Entities` | ProjectScanSettings, ScannedFile, campos de revisão humana |
| `AddDataMappingEntities` | DetectedTable, TableFileRelation para Etapa 3 |
| `StabilizationMetrics` | Campos explícitos de métricas por etapa (`FilesFound`, `TablesCount`, `ModulesCount`…) e `StepNumber` em Artifact |

---

## Decisões técnicas

- **SQLite** zero-config para uso local. O arquivo `orchestrator.db` fica na pasta do projeto Web.
- **Migrations automáticas** na inicialização simplificam o setup sem comandos adicionais.
- **Workflow definido em código** (`DefaultAnalysisWorkflow`) centraliza as etapas com constantes nomeadas — nunca use literais numéricos para referenciar etapas.
- **Artefatos em AppData** — no Windows os artefatos são salvos em `%APPDATA%\AnalyzerOrchestrator\workspace\`; no Linux/macOS em `~/.analyzer-orchestrator/workspace\`. O banco SQLite registra o caminho de cada artefato na tabela `Artifacts`.
- **Arquitetura em camadas** — Domain sem dependências externas, Application sem acesso a disco, Infrastructure com EF Core e acesso ao sistema de arquivos.
- **Métricas explícitas** — `PipelineStepExecution` possui campos tipados por etapa (`FilesFound`, `ModulesCount`, `TablesCount`…); nunca parse o campo `Notes` para extrair métricas.

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
