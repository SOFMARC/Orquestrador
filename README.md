# Analyzer Orchestrator

Sistema local para Windows que orquestra etapas de análise técnica de projetos, preparando contexto estruturado para uso posterior com IA.

> **Esta é a Etapa 1 — Fundação.** O sistema ainda não executa análise real de código, extração de arquivos ou integração com IA. A base está preparada para receber essas funcionalidades nas próximas etapas.

---

## Pré-requisitos

| Requisito | Versão mínima |
|-----------|---------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0 |
| Windows, macOS ou Linux | — |

Verifique a instalação:

```bash
dotnet --version
# Esperado: 9.x.x
```

---

## Como executar localmente

### 1. Clonar o repositório

```bash
git clone https://github.com/SOFMARC/Orquestrador.git
cd Orquestrador
```

### 2. Restaurar dependências

```bash
dotnet restore
```

### 3. Executar a aplicação

```bash
dotnet run --project src/AnalyzerOrchestrator.Web
```

A aplicação iniciará em `http://localhost:5000` (ou porta indicada no terminal).

> **Migrations são aplicadas automaticamente** na primeira execução. O banco SQLite `orchestrator.db` será criado na pasta `src/AnalyzerOrchestrator.Web/`.

### 4. (Opcional) Aplicar migration manualmente

Caso prefira aplicar a migration antes de rodar:

```bash
dotnet ef database update \
  --project src/AnalyzerOrchestrator.Infrastructure \
  --startup-project src/AnalyzerOrchestrator.Web
```

---

## Estrutura da solução

```
Orquestrador/
├── AnalyzerOrchestrator.sln
└── src/
    ├── AnalyzerOrchestrator.Domain/          # Entidades, enums e regras de domínio
    │   ├── Entities/
    │   │   ├── BaseEntity.cs
    │   │   ├── Project.cs
    │   │   ├── PipelineRun.cs
    │   │   ├── PipelineStepExecution.cs
    │   │   └── Artifact.cs
    │   └── Enums/
    │       ├── RunStatus.cs
    │       ├── StepStatus.cs
    │       └── ArtifactType.cs
    │
    ├── AnalyzerOrchestrator.Application/     # Contratos, DTOs e serviços de aplicação
    │   ├── DTOs/
    │   ├── Interfaces/
    │   ├── Services/
    │   ├── Workflow/
    │   └── ApplicationServiceExtensions.cs
    │
    ├── AnalyzerOrchestrator.Infrastructure/  # EF Core, SQLite, repositórios
    │   ├── Persistence/
    │   │   ├── OrchestratorDbContext.cs
    │   │   ├── OrchestratorDbContextFactory.cs
    │   │   ├── Configurations/
    │   │   └── Migrations/
    │   ├── Repositories/
    │   └── InfrastructureServiceExtensions.cs
    │
    └── AnalyzerOrchestrator.Web/             # ASP.NET Core MVC
        ├── Controllers/
        │   ├── HomeController.cs
        │   ├── ProjectsController.cs
        │   └── PipelineRunsController.cs
        ├── Views/
        │   ├── Home/
        │   ├── Projects/
        │   └── PipelineRuns/
        ├── Program.cs
        └── appsettings.json
```

---

## Funcionalidades disponíveis nesta etapa

| Funcionalidade | Status |
|----------------|--------|
| Tela inicial | ✅ |
| Cadastro de projeto | ✅ |
| Listagem de projetos | ✅ |
| Detalhe de projeto | ✅ |
| Edição de projeto | ✅ |
| Remoção de projeto | ✅ |
| Criação de Pipeline Run | ✅ |
| Listagem de runs por projeto | ✅ |
| Detalhe do run com etapas | ✅ |
| Cancelamento de run | ✅ |
| Etapas do workflow criadas automaticamente | ✅ |
| Persistência SQLite com EF Core | ✅ |
| Migration automática na inicialização | ✅ |

---

## Workflow padrão de análise

Cada Pipeline Run cria automaticamente as seguintes etapas:

| # | Etapa | Descrição |
|---|-------|-----------|
| 1 | Coleta de Informações do Projeto | Levantamento inicial: tecnologias, estrutura de pastas |
| 2 | Mapeamento de Estrutura | Identificação de módulos, camadas e componentes |
| 3 | Análise de Dependências | Pacotes, integrações e dependências externas |
| 4 | Análise de Banco de Dados *(opcional)* | Tabelas, relacionamentos e convenções de dados |
| 5 | Preparação do Contexto | Consolidação em documento de contexto para IA |

---

## Decisões técnicas

- **SQLite** foi escolhido por ser zero-config e adequado para uso local. O arquivo `orchestrator.db` fica na pasta do projeto Web.
- **Migrations automáticas** na inicialização simplificam o setup local sem necessidade de comandos adicionais.
- **Workflow definido em código** (`DefaultAnalysisWorkflow`) centraliza a definição das etapas, permitindo evolução sem alterar o banco de dados.
- **Arquitetura em camadas** garante separação clara de responsabilidades e facilita a evolução futura (ex: adicionar análise real de código, integração com IA).
- **DTOs na camada Application** isolam o domínio da interface, seguindo boas práticas de mapeamento.

---

## Próximas etapas planejadas

- Leitura e extração de estrutura de arquivos do repositório
- Análise de código-fonte (estrutura, namespaces, dependências)
- Análise de scripts SQL e tabelas de banco de dados
- Geração de documento de contexto consolidado
- Integração com IA para análise assistida

---

## Build

```bash
dotnet build -c Release
```

Build esperado: **succeeded** sem erros.
