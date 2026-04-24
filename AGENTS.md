# AGENTS.md — Analyzer Orchestrator v2

Este documento descreve o estado atual do projeto, as decisões arquiteturais e as regras de operação para qualquer agente de IA que atue neste repositório.

---

## Propósito do sistema

O Analyzer Orchestrator existe para transformar um sistema complexo em um snapshot técnico limpo, enxuto e fácil de ser lido por uma IA.

**O produto NÃO é:**
- Um orquestrador de múltiplas IAs.
- Uma ferramenta de implementação automática de código.
- Uma ferramenta de revisão de código por IA.
- Um validador de diffs ou PRs.

**O produto É:**
- Uma ferramenta de destilação de contexto técnico para IA.
- Um sistema que extrai o que importa de um projeto complexo.
- Um gerador de snapshots técnicos limpos e enxutos.

---

## Critério de decisão para mudanças

Antes de alterar qualquer parte do sistema, responder internamente:

1. Isso melhora a extração do sistema?
2. Isso melhora a estruturação do requisito?
3. Isso melhora o mapa de impacto?
4. Isso melhora o snapshot final entregue à IA?
5. Isso reduz ruído para leitura por IA?

Se a resposta for não para a maioria, a mudança provavelmente está fora do escopo.

---

## Estado do Projeto

**Etapa 1 — Extração do sistema:** ✅ Implementada
**Etapa 2 — Requisito estruturado:** 🔜 Próxima
**Etapa 3 — Mapa de impacto:** 🔜 Futura
**Etapa 4 — Geração do snapshot final para IA:** 🔜 Futura

### O que existe hoje

O sistema é uma aplicação **ASP.NET Core MVC (.NET 9)** com banco **SQLite** e arquitetura em 4 camadas:

```
src/
├── AnalyzerOrchestrator.Domain/          # Entidades e enums
├── AnalyzerOrchestrator.Application/     # DTOs, interfaces, serviços, workflow
├── AnalyzerOrchestrator.Infrastructure/  # EF Core, repositórios, serviços de disco
└── AnalyzerOrchestrator.Web/             # Controllers MVC, Views Razor
```

---

## Etapas Oficiais do Produto

| # | Etapa | Descrição |
|---|-------|-----------|
| 1 | **Extração do sistema** | Consolida estrutura, arquitetura e dados em visão técnica unificada |
| 2 | **Requisito estruturado** | Organiza a solicitação de mudança com objetivo, regras e critérios |
| 3 | **Mapa de impacto** | Identifica onde mexer, o que pode quebrar e o que preservar |
| 4 | **Geração do snapshot final para IA** | Gera o arquivo final enxuto para leitura eficiente por IA |

### Composição interna da Etapa 1

A Etapa 1 é executada com um único clique e internamente realiza três subetapas em sequência:

| Subetapa | Serviço | Artefatos |
|----------|---------|-----------|
| 1.1 — Extração Estrutural | `StructuralExtractionService` | `step_1/inventory.json`, `tree.txt`, `relevant-files.json`, `summary.md` |
| 1.2 — Consolidação Arquitetural | `ArchitecturalConsolidationService` | `step_2/modules-map.json`, `architecture-summary.md`, `layer-distribution.json`, `central-files.json` |
| 1.3 — Mapeamento Inicial de Dados | `DataMappingService` | `step_3/detected-tables.json`, `table-file-relations.json`, `table-operations.json` |

---

## Entidades do Domínio

| Entidade | Descrição |
|----------|-----------|
| `Project` | Projeto cadastrado. Contém `Name`, `RepositoryPath`, `TechnologyStack`, `IsActive`. |
| `ProjectScanSettings` | Configurações de leitura por projeto. Relação 1:1 com `Project`. |
| `PipelineRun` | Execução de pipeline. Status: `Pending → Running → Completed/Failed/Cancelled`. |
| `PipelineStepExecution` | Etapa individual de uma run. Possui `ReviewStatus` e campos de revisão humana. |
| `ScannedFile` | Arquivo descoberto durante varredura. Armazena caminho, extensão, tamanho e classificação. |
| `Artifact` | Artefato gerado por uma run, vinculado a uma subpasta em disco. |
| `DetectedTable` | Tabela detectada por heurística na subetapa 1.3. |
| `TableFileRelation` | Relação entre tabela detectada e arquivo onde é referenciada. |

### Enums

| Enum | Valores |
|------|---------|
| `RunStatus` | `Pending, Running, Completed, Failed, Cancelled` |
| `StepStatus` | `Pending, Running, Executed, AwaitingReview, Approved, Rejected, Failed, Skipped` |
| `ArtifactType` | `FileInventory, StructureTree, RelevantFilesList, ExecutionSummary, ModulesMap, ArchitectureSummary, DetectedTables, ...` |
| `FileRole` | `Controller, Service, Repository, Domain, Entity, DTO, View, Config, SQL, Script, Migration, Test, Startup` |

---

## Serviços Implementados

| Serviço | Interface | Responsabilidade |
|---------|-----------|-----------------|
| `SystemExtractionService` | `ISystemExtractionService` | **Orquestrador da Etapa 1.** Chama os 3 serviços internos em sequência. |
| `StructuralExtractionService` | `IStructuralExtractionService` | Subetapa 1.1 — varredura, classificação, artefatos de estrutura. |
| `ArchitecturalConsolidationService` | `IArchitecturalConsolidationService` | Subetapa 1.2 — módulos, camadas, arquivos centrais. |
| `DataMappingService` | `IDataMappingService` | Subetapa 1.3 — detecção de tabelas por heurística. |
| `ProjectService` | `IProjectService` | CRUD de projetos. |
| `PipelineRunService` | `IPipelineRunService` | Criar, listar, detalhar, cancelar runs. |
| `FileClassifierService` | `IFileClassifierService` | Classificar arquivo por nome/pasta/extensão → `FileRole`. |

---

## Compatibilidade com Runs Legadas

Runs criadas antes do realinhamento v2 possuem `PipelineStepExecution` com `StepNumber` 1, 2 e 3 separados (Extração Estrutural, Consolidação Arquitetural, Mapeamento Inicial de Dados). Esses registros são **preservados no banco** e continuam funcionando.

A interface detecta runs legadas verificando o `StepName` do step 1:
- `"Extração do sistema"` → run nova (workflow v2).
- `"Extração Estrutural"` → run legada (workflow v1).

Os controllers `ConsolidationController` e `DataMappingController` são mantidos para suporte a runs legadas.

---

## Persistência em Disco

Artefatos são salvos em:

```
workspace/
└── {ProjectName}/
    └── runs/
        └── run_{RunId}/
            ├── step_1/   ← Subetapa 1.1 — Extração Estrutural
            ├── step_2/   ← Subetapa 1.2 — Consolidação Arquitetural
            └── step_3/   ← Subetapa 1.3 — Mapeamento Inicial de Dados
```

A pasta `workspace/` fica dentro do diretório de trabalho da aplicação Web.

---

## Migrations

| Migration | Descrição |
|-----------|-----------|
| `20260410195506_InitialCreate` | Schema inicial: Projects, PipelineRuns, PipelineStepExecutions, Artifacts |
| `AddStep2Entities` | ProjectScanSettings, ScannedFiles, colunas de revisão humana |
| `AddDataMappingEntities` | DetectedTable, TableFileRelation para mapeamento de dados |
| `StabilizationMetrics` | Métricas de execução por etapa |

---

## Regras de Operação para Agentes

### Antes de qualquer modificação

1. Fazer `git pull origin main` para garantir código atualizado.
2. Ler este arquivo por completo.
3. Verificar o build atual: `dotnet build AnalyzerOrchestrator.sln -c Release`.
4. Responder ao critério de decisão antes de implementar qualquer mudança.

### Fluxo de Build

```bash
cd /path/to/Orquestrador
dotnet restore AnalyzerOrchestrator.sln
dotnet build AnalyzerOrchestrator.sln -c Release -warnaserror
```

### Critério de entrega

- Build `Release` sem erros e sem warnings.
- Migrations aplicadas e consistentes com as entidades.
- Commits coerentes por camada.

---

## Diretrizes Arquiteturais

- **Domain** nunca referencia Application, Infrastructure ou Web.
- **Application** nunca referencia Infrastructure ou Web. Usa apenas interfaces.
- **Infrastructure** implementa as interfaces da Application. Pode referenciar Domain e Application.
- **Web** orquestra tudo via DI. Usa DTOs da Application, nunca entidades do Domain diretamente.
- **Mapeamento** é manual (sem AutoMapper). Feito nos serviços da Application.
- **Logging** fica na Infrastructure, não nos serviços da Application.
- **Novos serviços** devem ter interface na Application e implementação na Infrastructure conforme a responsabilidade.

---

## O que NÃO fazer

- Não integrar com IA (etapas futuras).
- Não implementar análise profunda de SQL ou tabelas além do que já existe.
- Não remover os serviços de extração, consolidação e mapeamento existentes.
- Não trocar SQLite por outro banco sem necessidade explícita.
- Não trocar a arquitetura em camadas.
- Não expandir o produto para automação de implementação de código.
- Não criar migrations sem necessidade real de mudança de schema.

---

*Atualizado após realinhamento v2 — Etapa 1 — Extração do sistema.*
