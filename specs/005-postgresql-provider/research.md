# Research: Provider de Banco de Dados PostgreSQL

## Contexto

Esta troca de provider afeta exclusivamente a camada `Infrastructure` (e seus
testes) e a composição em `Api/Program.cs` + `appsettings*.json`. Uma busca
por referências a SQL Server/MsSql no repositório confirmou que Domain e
Application não têm nenhuma dependência de infraestrutura de persistência
(Principle XI já exige isso), então nenhuma mudança é necessária ali.

Arquivos com referência direta a SQL Server hoje:
- `backend/Infrastructure/ContasEmDia.Infrastructure.csproj` (pacote
  `Microsoft.EntityFrameworkCore.SqlServer`)
- `backend/Infrastructure/Contexts/ContasEmDiaDbContextFactory.cs`
  (`UseSqlServer` de design-time)
- `backend/Infrastructure/Migrations/*` (migração `InitialCreate` gerada com
  tipos T-SQL: `uniqueidentifier`, `nvarchar`, `decimal(18,2)`, `date`)
- `backend/Infrastructure.Tests/ContasEmDia.Infrastructure.Tests.csproj`
  (pacote `Testcontainers.MsSql`)
- `backend/Infrastructure.Tests/SqlServerContainerFixture.cs`
- `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`
  (usa a fixture acima via `[Collection(nameof(SqlServerCollection))]`)
- `backend/Api/Program.cs` (`UseSqlServer` em tempo de execução)
- `backend/Api/appsettings.Development.json` (connection string SQL Server)

`Api.Tests` usa EF Core InMemory (`CustomWebApplicationFactory.cs`) e não
depende de nenhum provider real — nenhuma mudança necessária ali.

## Decisão: Provider EF Core

**Decisão**: Usar `Npgsql.EntityFrameworkCore.PostgreSQL` como substituto de
`Microsoft.EntityFrameworkCore.SqlServer`, na versão majoritária compatível
com EF Core 10 (linha `10.x` do pacote Npgsql, mesma prática já usada para
fixar `Microsoft.EntityFrameworkCore.*` em `10.0.11`).

**Rationale**: É o provider EF Core oficial e mais maduro para PostgreSQL,
mantido pela comunidade Npgsql com suporte a cada versão major do EF Core no
mesmo ciclo de vida, e é o único caminho realista para não reescrever a
camada de acesso a dados manualmente (o que violaria Principle V —
Simplicity — ao reinventar o que o EF Core já resolve).

**Alternatives considered**:
- Dapper + SQL manual: rejeitado — obrigaria reescrever toda a camada
  `Repositories`/`Configs` fora do modelo EF Core já adotado (Principle
  VII), aumentando complexidade sem necessidade concreta.
- Manter dual-provider (SQL Server + PostgreSQL) via abstração própria:
  rejeitado — o spec (Assumptions) já define explicitamente que a troca é
  completa, não coexistência.

## Decisão: Versão do PostgreSQL

**Decisão**: Fixar a versão major estável mais recente disponível no
momento da implementação (imagem de container `postgres:<major>-alpine`).
Como o spec não exige compatibilidade com uma versão específica mais antiga
(ver Assumptions), a tarefa de implementação MUST confirmar, no momento em
que rodar, qual é a tag `postgres:<major>` estável mais recente publicada no
Docker Hub e usar essa tag de forma consistente em: Testcontainers
(`Infrastructure.Tests`), `docker-compose.yml` de desenvolvimento local, e
documentação (`quickstart.md`/README).

**Rationale**: Fixar uma tag concreta demais neste documento arriscaria
ficar desatualizada entre o planejamento e a implementação; a spec já
autoriza usar sempre a mais recente estável. O que importa é que a mesma
versão seja usada de forma consistente em dev, teste e (futuramente)
produção.

**Alternatives considered**: Fixar uma versão hoje (ex.: `postgres:17`) —
rejeitado como valor fixo neste research.md porque criaria obsolescência
desnecessária; a tarefa de implementação faz essa checagem pontual.

**Tag efetivamente usada na implementação (T001)**: `postgres:18-alpine`
(PostgreSQL 18.6), confirmada como a tag major estável mais recente
publicada no Docker Hub em 2026-09-06. Usada de forma consistente em
`Testcontainers.PostgreSql` (`Infrastructure.Tests/PostgreSqlContainerFixture.cs`)
e em `docker-compose.yml`.

## Decisão: Testcontainers para testes de infraestrutura

**Decisão**: Substituir `Testcontainers.MsSql` por `Testcontainers.PostgreSql`
(mesma major line do Testcontainers já usada, `4.x`), e renomear
`SqlServerContainerFixture`/`SqlServerCollection` para
`PostgreSqlContainerFixture`/`PostgreSqlCollection`.

**Rationale**: Mantém o mesmo padrão de fixture reutilizável por classe de
teste (xUnit `ICollectionFixture`) já estabelecido, trocando apenas o
container subjacente — menor diff possível, consistente com Principle V.

**Alternatives considered**: Usar SQLite in-memory para os testes de
repositório — rejeitado porque User Story 3 exige explicitamente uma
instância PostgreSQL real via container descartável, não um substituto.

## Decisão: Mapeamento de tipos de coluna SQL Server → PostgreSQL

**Decisão**:
| Uso atual (T-SQL, via `HasColumnType`) | Equivalente PostgreSQL | Ação |
|---|---|---|
| `uniqueidentifier` (Id, FKs — via convenção `Guid`) | `uuid` | Nenhuma mudança de config: o provider Npgsql já mapeia `Guid` para `uuid` nativamente por convenção. |
| `decimal(18,2)` (`Money`) | `numeric(18,2)` | Trocar `.HasColumnType("decimal(18,2)")` por `.HasPrecision(18, 2)` nas duas configs (`RecurringExpenseConfigurations`, `OccurrenceConfigurations`) — API agnóstica de provider, elimina string de tipo específica de SQL Server. |
| `date` (`CalendarDate`) | `date` | Nenhuma mudança: PostgreSQL também tem tipo nativo `date`; `.HasColumnType("date")` continua válido nos dois providers. |
| `nvarchar(200)` / `nvarchar(1000)` (via `HasMaxLength`, sem `HasColumnType` explícito) | `character varying(200)` / `(1000)` | Nenhuma mudança: `HasMaxLength` já é agnóstico de provider; cada provider gera seu próprio tipo de texto variável. |
| `int` (enums armazenados como int) | `integer` | Nenhuma mudança: conversão já é para `int` primitivo, mapeado nativamente. |

**Rationale**: Apenas a coluna `Money` usa uma string de tipo específica de
T-SQL (`decimal(18,2)`) hoje. Trocar para `.HasPrecision(18, 2)` é a forma
recomendada pelo próprio EF Core para expressar precisão/escala de forma
portável entre providers, satisfazendo FR-006 (equivalente PostgreSQL mais
próximo, preservando o comportamento observável de precisão monetária) sem
introduzir uma string de tipo PostgreSQL hardcoded no lugar de uma T-SQL.

**Alternatives considered**: Trocar diretamente para
`.HasColumnType("numeric(18,2)")` — rejeitado por reintroduzir uma string de
tipo específica de provider exatamente onde o EF Core já oferece uma API
agnóstica (`HasPrecision`), tornando a config menos portável para o futuro.

## Decisão: Migração de schema (histórico do EF Core)

**Decisão**: Remover a migração `20260831201615_InitialCreate` (Up/Down em
T-SQL, inexecutável contra PostgreSQL) e o respectivo `Designer.cs`, e gerar
uma nova migração inicial equivalente para o provider Npgsql, junto com um
novo `ContasEmDiaDbContextModelSnapshot.cs`.

**Rationale**: O corpo de uma migração EF Core é gerado especificamente para
o provider ativo no momento da geração (`migrationBuilder.CreateTable` usa
tipos T-SQL). Não existe forma de "portar" essa migração para PostgreSQL —
ela precisa ser regerada. O próprio spec antecipa e autoriza isso
explicitamente na seção Edge Cases: *"não há dados de produção existentes
até o momento desta troca... o histórico de migração é reiniciado do zero
para o novo provider"*. Isso é tratado como uma exceção justificada a
Principle VII ("migrações... MUST NOT be altered, renamed, or deleted") na
seção Constitution Check do plano — a regra de Principle VII protege
migrações de *features já entregues* de terem seu histórico reescrito
silenciosamente; aqui a própria feature (troca de provider) exige e
documenta a substituição completa do histórico como seu objetivo central.

**Alternatives considered**: Manter a migração SQL Server antiga e adicionar
uma segunda migração "de correção" apenas para PostgreSQL — rejeitado: não
faz sentido ter duas migrações iniciais incompatíveis coexistindo quando
não há dado de produção a preservar (ver Assumptions do spec).

## Decisão: Ambiente de desenvolvimento local (User Story 2)

**Decisão**: Adicionar um `docker-compose.yml` (na raiz do repo ou em
`backend/`) que sobe um container PostgreSQL local com credenciais de
desenvolvimento fixas, e atualizar `appsettings.Development.json` com a
connection string Npgsql correspondente.

**Rationale**: Hoje não existe nenhum arquivo de orquestração de banco de
dados local no repositório — o ambiente de dev presumivelmente dependia de
uma instalação manual de SQL Server. Sem um `docker-compose.yml`,
User Story 2 (rodar localmente sem instalar/licenciar SQL Server) e SC-003
não seriam verificáveis de ponta a ponta. Docker é uma ferramenta gratuita
já usada pelo Testcontainers nos testes, então não introduz custo de
licença.

**Alternatives considered**: Deixar a cargo do desenvolvedor instalar
PostgreSQL manualmente — rejeitado, pois não atende ao critério de
independent test de User Story 2 ("levantar um banco PostgreSQL local"
seguindo instruções do projeto).

## Decisão: Connection strings por ambiente (FR-004)

**Decisão**: Manter o padrão já existente — `ConnectionStrings:ContasEmDia`
em `appsettings.json` (vazio, placeholder) e `appsettings.Development.json`
(valor real de dev), lido via `builder.Configuration.GetConnectionString(...)`
em `Program.cs`, apenas trocando o formato da string de conexão de
SQL Server para o formato Npgsql (`Host=...;Port=...;Database=...;
Username=...;Password=...`).

**Rationale**: O mecanismo de configuração por ambiente do ASP.NET Core
(appsettings por ambiente + variáveis de ambiente/user-secrets em produção)
já satisfaz FR-004 sem nenhuma mudança estrutural — só o conteúdo/formato da
string muda. Nenhuma abstração nova é necessária (Principle V).

**Alternatives considered**: Introduzir um objeto de configuração
fortemente tipado (`PostgresOptions`) — rejeitado por não haver necessidade
concreta hoje; a leitura direta de `ConnectionStrings` já é suficiente e é o
padrão já em uso.
