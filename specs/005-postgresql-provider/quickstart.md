# Quickstart: Validando a troca para PostgreSQL

Guia para confirmar, de ponta a ponta, que as três User Stories da spec
estão satisfeitas. Pressupõe Docker instalado (gratuito, sem custo de
licença — satisfaz SC-003) e o SDK do .NET 10.

## Pré-requisitos

- Docker instalado e em execução.
- .NET 10 SDK instalado.
- Nenhuma instância de SQL Server necessária em nenhum passo abaixo.

## 1. Subir PostgreSQL local (User Story 2)

```bash
docker compose up -d postgres
```

**Resultado esperado**: um container PostgreSQL local sobe e aceita
conexões na porta configurada em `docker-compose.yml`.

Inspecione `backend/Api/appsettings.Development.json` e confirme que a
`ConnectionStrings:ContasEmDia` usa formato Npgsql
(`Host=...;Port=...;Database=...;Username=...;Password=...`) e não contém
nenhuma referência a SQL Server.

## 2. Aplicar as migrações (User Story 1, cenário 1)

```bash
cd backend
dotnet ef database update --project Infrastructure --startup-project Api
```

**Resultado esperado**: todas as tabelas (`RecurringExpenses`,
`Occurrences`), colunas, chaves e restrições descritas em
[data-model.md](./data-model.md) são criadas com sucesso no PostgreSQL
local sem erros.

## 3. Rodar a API e exercitar o endpoint existente (User Story 1, cenários 2 e 3)

```bash
dotnet run --project Api
```

Com a API no ar, use o endpoint de cadastro de despesa recorrente já
documentado no SwaggerUI (`/swagger`) para:

1. Criar uma despesa recorrente.
2. Consultar/atualizar/remover essa despesa através das operações já
   existentes.

**Resultado esperado**: mesmo comportamento e mesmos valores observados
anteriormente sobre SQL Server (ver contrato da API em
`specs/004-api-despesa-recorrente/contracts/`, inalterado por esta
feature).

## 4. Rodar a suíte de testes automatizados (User Story 3)

```bash
cd backend
dotnet test ContasEmDia.sln
```

**Resultado esperado**:
- `Infrastructure.Tests` sobe um container PostgreSQL descartável via
  Testcontainers (`PostgreSqlContainerFixture`), aplica as migrações nele e
  executa todos os testes de repositório com sucesso — sem subir nenhum
  container SQL Server.
- `Api.Tests` continua passando normalmente (usa EF Core InMemory, não é
  afetado por esta troca).
- `Domain.Tests` e `Application.Tests` continuam passando normalmente (sem
  dependência de infraestrutura).

## 5. Confirmar ausência de dependência de SQL Server (FR-007)

```bash
grep -ril "sqlserver\|mssql" backend --include="*.csproj"
```

**Resultado esperado**: nenhum resultado (nenhum `.csproj` referencia mais
`Microsoft.EntityFrameworkCore.SqlServer` ou `Testcontainers.MsSql`).
