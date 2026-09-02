# Quickstart: Validando a Infrastructure de Despesa Recorrente

Guia para rodar e validar esta feature de ponta a ponta, uma vez implementada. Não contém código de implementação — apenas os passos de execução e os resultados esperados. Detalhes de mapeamento estão em [data-model.md](./data-model.md); o formato do contrato de repositório está em [contracts/repository-contract.md](./contracts/repository-contract.md).

## Pré-requisitos

- .NET SDK 10 (`net10.0`) instalado.
- Docker (ou outro runtime de container compatível) em execução — necessário para os testes de integração via Testcontainers subirem um container SQL Server descartável.
- Projeto `backend/Infrastructure` (produção) e `backend/Infrastructure.Tests` (testes de integração) adicionados a `backend/ContasEmDia.sln`, com `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design` e `Testcontainers.MsSql` restaurados.

## Setup

```bash
cd backend
dotnet restore
```

## Gerar/aplicar a migration inicial

```bash
cd backend
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project Infrastructure
dotnet ef database update --project Infrastructure --startup-project Infrastructure --connection "<connection string de desenvolvimento>"
```

**Resultado esperado**: schema criado no SQL Server de destino com as tabelas `RecurringExpenses` e `Occurrences` (ver [data-model.md](./data-model.md)), incluindo a FK obrigatória de `Occurrences.RecurringExpenseId` para `RecurringExpenses.Id`. Nenhuma migration de outra feature é alterada (não há nenhuma anterior no momento).

## Rodar os testes de integração (Testcontainers)

```bash
cd backend
dotnet test Infrastructure.Tests
```

**Resultado esperado**: um container SQL Server descartável sobe automaticamente por execução, as migrations são aplicadas nele, e os testes cobrem as três User Stories da spec:

- **User Story 1 (Salvar despesa recorrente)**:
  - Salvar uma despesa Ativa com ocorrência do mês corrente gerada → ambas persistidas em uma única confirmação de escrita (Acceptance Scenario 1).
  - Salvar uma despesa Pausada (ou Ativa com início futuro) sem ocorrência → apenas a despesa persistida (Acceptance Scenario 2).
  - Forçar uma falha na escrita (ex.: violação de restrição) → nem despesa nem ocorrência ficam parcialmente persistidas (Acceptance Scenario 3).
- **User Story 2 (Recuperar por identificador)**:
  - Buscar por id existente → despesa retornada com todas as suas ocorrências intactas (Acceptance Scenario 1).
  - Buscar por id inexistente → ausência de resultado, sem exceção (Acceptance Scenario 2).
- **User Story 3 (Listar ativas)**:
  - Conjunto misto de status → apenas as Ativas retornadas, cada uma com suas ocorrências (Acceptance Scenario 1).
  - Nenhuma despesa ativa → lista vazia, sem exceção (Acceptance Scenario 2).

## Validação manual rápida (opcional)

Para confirmar que a reconstrução via banco preserva fielmente os dados (edge case da spec), após rodar os testes automatizados:

1. Inserir uma despesa recorrente Ativa via `RecurringExpenseRepository.AddAsync` (ou diretamente pelos testes existentes) com uma ocorrência do mês corrente.
2. Recuperar a mesma despesa via `GetByIdAsync` em uma nova instância de `ContasEmDiaDbContext` (nova consulta, sem cache do `DbContext` original).
3. Comparar todos os campos (nome, categoria, valor, dia de vencimento, data de início, frequência, status, observação) e os campos da ocorrência (período de referência, data de vencimento, status, nome, categoria, valor esperado) com os valores originais.

**Resultado esperado**: todos os campos idênticos aos originais (SC-002), sem nenhuma exceção de validação do Domain disparada durante a reconstrução (a reconstrução usa o construtor privado dedicado, não o construtor público de criação).

## Critério de conclusão

A feature está validada quando:
- Todos os testes de `Infrastructure.Tests` passam contra SQL Server real via Testcontainers (não InMemory).
- `dotnet build backend/ContasEmDia.sln` conclui sem warnings (warnings-as-errors habilitado).
- Nenhuma migration pré-existente foi alterada, renomeada ou removida (não há nenhuma neste momento — apenas a `InitialCreate` desta feature).
- Nenhuma connection string ou segredo aparece hardcoded em código-fonte versionado (SC-005).
