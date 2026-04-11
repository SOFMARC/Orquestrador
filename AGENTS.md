# AGENTS.md — Analyzer Orchestrator

Este documento descreve o estado atual do projeto, as decisões arquiteturais tomadas e as regras de operação para qualquer agente de IA que atue neste repositório.

---

## Estado do Projeto

**Etapa 1 — Fundação:** ✅ Concluída  
**Etapa 2 — Extração Estrutural:** ✅ Concluída  
**Etapa 3 em diante:** Pendente

### O que existe hoje

O sistema é uma aplicação **ASP.NET Core MVC (.NET 9)** com banco **SQLite** e arquitetura em 4 camadas:

```
src/
├── AnalyzerOrchestrator.Domain/          # Entidades e enums
├── AnalyzerOrchestrator.Application/     # DTOs, interfaces, serviços, workflow
├── AnalyzerOrchestrator.Infrastructure/  # EF Core, repositórios, varredura de disco
└── AnalyzerOrchestrator.Web/             # Controllers MVC, Views Razor
```

---

## Entidades do Domínio

| Entidade | Descrição |
|----------|-----------|
| `Project` | Projeto cadastrado. Contém `Name`, `RepositoryPath`, `TechnologyStack`, `IsActive` e configurações de scan via `ProjectScanSettings`. |
| `ProjectScanSettings` | Configurações de leitura por projeto: extensões permitidas, pastas ignoradas, limite de tamanho, ignorar binários. Relação 1:1 com `Project`. |
| `PipelineRun` | Execução de pipeline. Status: `Pending → Running → Completed/Failed/Cancelled`. |
| `PipelineStepExecution` | Etapa individual de uma run. Agora possui `ReviewStatus` e campos de revisão humana (`ReviewedAt`, `ReviewedBy`, `ReviewNotes`). |
| `ScannedFile` | Arquivo descoberto durante varredura. Armazena caminho, extensão, tamanho, classificação (`FileRole`) e flag de relevância. |
| `Artifact` | Artefato gerado por uma run. Tipos: `FileInventory`, `StructureTree`, `RelevantFilesList`, `ExecutionSummary`, etc. |

### Enums

| Enum | Valores |
|------|---------|
| `RunStatus` | `Pending, Running, Completed, Failed, Cancelled` |
| `StepStatus` | `Pending, Running, Executed, AwaitingReview, Approved, Rejected, Failed, Skipped` |
| `ReviewStatus` | `NotApplicable, AwaitingReview, Approved, Rejected` |
| `ArtifactType` | `Unknown, FileInventory, StructureTree, RelevantFilesList, ExecutionSummary, ContextDocument, StructureMap, AnalysisReport, Other` |
| `FileRole` | `Controller, Service, Repository, Domain, Entity, DTO, View, Config, SQL, Script, Other` |

---

## Workflow Padrão (5 etapas)

| # | Nome | Descrição |
|---|------|-----------|
| 1 | Extração Estrutural | Varredura do diretório, inventário, árvore, classificação e arquivos relevantes |
| 2 | Mapeamento de Estrutura | Identificação de módulos, camadas e componentes |
| 3 | Análise de Dependências | Pacotes, integrações e dependências externas |
| 4 | Análise de Banco de Dados | Tabelas, relacionamentos e convenções (opcional) |
| 5 | Preparação do Contexto | Consolidação para uso com IA |

A **Etapa 1 (Extração Estrutural)** é a única implementada com execução real. As demais estão como `Pending` aguardando implementação futura.

---

## Serviços Implementados

| Serviço | Interface | Responsabilidade |
|---------|-----------|-----------------|
| `ProjectService` | `IProjectService` | CRUD de projetos |
| `PipelineRunService` | `IPipelineRunService` | Criar, listar, detalhar, cancelar runs |
| `StructuralExtractionService` | `IStructuralExtractionService` | Executar varredura, classificar arquivos, gerar artefatos |
| `FileClassifierService` | `IFileClassifierService` | Classificar arquivo por nome/pasta/extensão → `FileRole` |

---

## Persistência em Disco

Artefatos são salvos em:

```
workspace/
└── {ProjectName}/
    └── runs/
        └── run_{RunId}/
            └── step_1/
                ├── inventory.json
                ├── tree.txt
                ├── relevant-files.json
                └── summary.md
```

A pasta `workspace/` fica dentro do diretório de trabalho da aplicação Web.

---

## Migrations

| Migration | Descrição |
|-----------|-----------|
| `20260410195506_InitialCreate` | Schema inicial: Projects, PipelineRuns, PipelineStepExecutions, Artifacts |
| `AddStep2Entities` | Adiciona: `ProjectScanSettings`, `ScannedFiles`, colunas de revisão em `PipelineStepExecutions`, novos valores de enum |

---

## Regras de Operação para Agentes

### Antes de qualquer modificação

1. Fazer `git pull origin main` para garantir código atualizado.
2. Ler este arquivo por completo.
3. Verificar o build atual: `dotnet build AnalyzerOrchestrator.sln -c Release`.

### Fluxo de Build

```bash
cd /path/to/Orquestrador
dotnet restore AnalyzerOrchestrator.sln
dotnet build AnalyzerOrchestrator.sln -c Release -warnaserror
```

### Após modificações com mudança de schema

```bash
dotnet ef migrations add <NomeDaMigration> \
  --project src/AnalyzerOrchestrator.Infrastructure \
  --startup-project src/AnalyzerOrchestrator.Web \
  --output-dir Persistence/Migrations
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
- **Novos serviços** devem ter interface na Application e implementação na Infrastructure ou Application conforme a responsabilidade.

---

## O que NÃO fazer

- Não integrar com IA ainda (etapas futuras).
- Não implementar análise profunda de SQL ou tabelas (etapa futura).
- Não implementar mapa de impacto (etapa futura).
- Não remover funcionalidades da Etapa 1.
- Não trocar SQLite por outro banco sem necessidade explícita.
- Não trocar a arquitetura em camadas.

---

*Atualizado após conclusão da Etapa 2 — Extração Estrutural.*
