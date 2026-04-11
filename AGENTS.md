# Regras e Habilidades para Agentes de IA no Projeto Mercado Livre

Este documento define as regras de operação e as habilidades técnicas esperadas de qualquer agente de IA que atue neste repositório. O objetivo é garantir a qualidade, a estabilidade e a consistência arquitetural do projeto.

## Estrutura Real do Repositório

O repositório contém uma solução principal em .NET 9 (`net9.0`) 


**Atenção:** Atualmente, **não existem projetos de teste** neste repositório.

## Regras de Operação

Antes de finalizar qualquer tarefa, o agente deve obrigatoriamente validar se a solução compila sem erros.

### Fluxo de Validação de Build (.NET 9)

A solução principal está localizada na pasta `Orquestrador`. Para validar o build, siga esta ordem de execução:

1. **Navegar para a pasta da solução:**
   ```bash
   cd Orquestrador
   ```

2. **Restaurar dependências:**
   ```bash
   dotnet restore AnalyzerOrchestrator.sln
   ```

3. **Compilar a solução:**
   ```bash
   dotnet build AnalyzerOrchestrator.sln -c Debug
   ```

### Tratamento de Falhas de Build

Se o build falhar, o agente deve:
1. Analisar os erros apresentados no log de compilação.
2. Corrigir os erros no código ou nos arquivos de projeto (`.csproj`).
3. Rodar o build novamente.
4. Repetir este ciclo até que o build termine sem erros.

**Atenção:** Só considere a tarefa concluída quando o build terminar sem erros.

### Diretrizes de Modificação de Código

- **Mudanças Mínimas:** Faça mudanças mínimas e seguras, sem alterar partes não relacionadas ao escopo da tarefa.
- **Preservação Arquitetural:** Ao implementar correções ou novas funcionalidades, preserve o padrão arquitetural já existente no projeto (Domain -> Application -> Infrastructure -> Web).
- **Transparência:** Sempre explique no final o que foi alterado e quais comandos foram executados para validar as mudanças.
- **Correção Guiada por Validação:** Utilize sempre os erros de build como fonte de verdade para orientar as correções.

---

## Habilidades Técnicas Documentadas

O agente deve demonstrar as seguintes capacidades técnicas ao atuar neste repositório:

### 1. Compreensão da Estrutura da Solução
- Entender a estrutura da solução e identificar o projeto correto dentro do repositório.
- Identificar o projeto de inicialização (`Orquestrador`) e a solução correta (`AnalyzerOrchestrator.sln`).

### 2. Validação como Fonte de Verdade
- Compilar a solução como fonte de verdade da validação.
- Utilizar erros de compilação como base principal para diagnóstico e correção.

### 3. Resolução de Problemas de Compilação
- Corrigir erros de compilação em projetos .NET 9.
- Ajustar referências, namespaces, diretivas `using`, assinaturas de métodos e dependências quebradas.
- Rodar novamente o build após cada correção até eliminar todos os erros.

### 4. Disciplina de Código
- Respeitar a arquitetura existente (Domain, Application, Infrastructure, Web).
- Evitar mudanças desnecessárias fora do escopo da tarefa.
- Não ignorar falhas de build para encerrar a tarefa.

---

*Este arquivo serve como um guia de comportamento para agentes autônomos e deve ser consultado antes do início de qualquer modificação no código-fonte.*
