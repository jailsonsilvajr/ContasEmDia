# Quickstart: Validando a Application de Despesa Recorrente (Cadastro)

Guia para rodar e validar esta feature de ponta a ponta, uma vez implementada. Não contém código de implementação — apenas os passos de execução e os resultados esperados. Detalhes de Input/Output estão em [data-model.md](./data-model.md); a superfície pública do Use Case está em [contracts/application-public-api.md](./contracts/application-public-api.md).

## Pré-requisitos

- .NET SDK 10 (`net10.0`) instalado.
- Projeto `backend/Application` (produção) e `backend/Application.Tests` (testes unitários) adicionados a `backend/ContasEmDia.sln`, referenciando `ContasEmDia.Domain`.
- `IRepositoryManager` já extraída em `Domain/Repositories/IRepositoryManager.cs` e implementada por `RepositoryManager` na Infrastructure (pré-requisito estrutural desta feature, FR-015).
- Mensagens de erro dos Value Objects/aggregate do Domain já revisadas para PT-BR (pré-requisito estrutural desta feature, FR-016).

## Setup

```bash
cd backend
dotnet restore
```

## Rodar os testes unitários do Use Case

```bash
cd backend
dotnet test Application.Tests
```

**Resultado esperado**: nenhum acesso a banco de dados real (dublês de teste para `IRepositoryManager`/`IRecurringExpenseRepository` e `ICurrentDateProvider`, seguindo o padrão de `InMemoryRecurringExpenseRepository` já usado em `Domain.Tests`). Os testes cobrem as três User Stories da spec:

- **User Story 1 (Cadastrar despesa recorrente com dados válidos)**:
  - Dados válidos de uma despesa Ativa com início na competência corrente → despesa criada, persistida (via `AddAsync`), e Output de sucesso trazendo a despesa e a ocorrência do mês corrente gerada (Acceptance Scenario 1).
  - Dados válidos de uma despesa Pausada, ou Ativa com início em competência futura → despesa criada e persistida, Output de sucesso com lista de ocorrências vazia (Acceptance Scenario 2).
  - Todos os dados do Output de sucesso conferidos contra os métodos de leitura do aggregate/entidade (nunca contra estado interno) (Acceptance Scenario 3).
- **User Story 2 (Rejeitar cadastro com dados inválidos, agregando todos os erros)**:
  - Exatamente um campo inválido (ex.: nome vazio) → um único `FieldError`, mensagem PT-BR do Domain, nada persistido (Acceptance Scenario 1).
  - Mais de um campo inválido simultaneamente (ex.: nome vazio e categoria desconhecida) → todos os `FieldError` na mesma lista, nada persistido (Acceptance Scenario 2).
  - Categoria fora das cinco suportadas → `FieldError` de categoria reportado mesmo sem tentar construir `ExpenseCategory` (Acceptance Scenario 3).
  - `startDate` malformado ou correspondente a uma data inexistente no calendário (ex.: `"2026-02-31"`) → `FieldError` de `startDate` (Acceptance Scenario 4).
- **User Story 3 (Distinguir falha de validação de falha inesperada)**:
  - Repositório de teste que lança exceção em `AddAsync` (ex.: indisponibilidade simulada de banco), dados de entrada válidos → a exceção propaga através de `ExecuteAsync`, sem virar um Output de falha de validação (Acceptance Scenario 1).

## Validação manual rápida (opcional)

Para confirmar a agregação de erros e a ausência de regra de negócio duplicada na Application:

1. Instanciar `CreateRecurringExpenseUseCase` com um `IRepositoryManager` de teste (repositório em memória) e um `ICurrentDateProvider` de teste (data fixa).
2. Executar com um `CreateRecurringExpenseUseCaseInput` contendo `Name = ""`, `Category = "Inexistente"`, `MonthlyAmount = 0`, e os demais campos válidos.
3. Inspecionar `Output.Errors`.

**Resultado esperado**: `Output.IsSuccess == false`, com um `FieldError` para `name`, um para `category` e um para `monthlyAmount` — cada mensagem idêntica à lançada pelo respectivo Value Object do Domain (após a revisão PT-BR de FR-016), sem nenhum texto reescrito pela Application. Nenhuma chamada a `AddAsync` ocorre.

## Critério de conclusão

A feature está validada quando:
- Todos os testes de `Application.Tests` passam, sem nenhum acesso a banco de dados real.
- `dotnet build backend/ContasEmDia.sln` conclui sem warnings (warnings-as-errors habilitado nos projetos de produção).
- `Application.csproj` referencia exclusivamente `ContasEmDia.Domain` (nenhuma referência a `Infrastructure` ou a qualquer pacote de transporte HTTP).
- Nenhuma regra de validação de negócio está duplicada dentro do Use Case — toda mensagem de `FieldError` (exceto os dois casos documentados em `research.md` §4–5) é repassada tal como lançada pelo Domain.
