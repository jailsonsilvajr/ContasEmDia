# Phase 0 Research: Infrastructure de Despesa Recorrente (Persistência)

Todas as incógnitas foram resolvidas via clarificação da spec, o refinamento de origem, a constituição do projeto, e leitura do código existente (`backend/Domain`). Não há itens `NEEDS CLARIFICATION` remanescentes.

## 1. Construtor privado de reconstrução (Domain)

- **Decision**: Adicionar a `RecurringExpense` e `Occurrence` um construtor privado de reconstrução que recebe todos os campos já persistidos (incluindo `Guid id` e, para `Occurrence`, `OccurrenceStatus status`) e apenas os atribui — sem repetir a lógica de decisão do construtor público (ex.: gerar ou não a ocorrência do mês corrente). Value Objects (`Money`, `DueDay`, `CalendarDate`, `Frequency`, `RecurringExpenseStatus`, `Note`, `OccurrenceStatus`, `ExpenseName`, `ExpenseCategory`, `ReferencePeriod`) não precisam de construtor privado adicional: seus construtores públicos já são triviais (validam e atribuem um único primitivo, sem efeitos colaterais nem decisões), então reconstruí-los a partir de um valor já validado no banco através do construtor público existente é seguro e não reaplica nem contorna nenhuma regra de negócio — a EF Core value conversion (`HasConversion`) chama esse construtor público diretamente.
- **Rationale**: O construtor público de `RecurringExpense` decide, com base em status e datas, se gera uma `Occurrence` — reexecutar essa decisão ao carregar do banco duplicaria ou perderia dados. `Occurrence` hoje é criada apenas com um construtor `internal` que gera um novo `Guid` e força `OccurrenceStatus.Pending`, incompatível com reconstrução (precisa do id e status reais persistidos).
- **Alternatives considered**: (a) Reconstruir via reflexão/`Activator.CreateInstance` sem tocar o Domain — rejeitado por violar Princípio VI (criação apenas via construtores) e por ser frágil a mudanças internas do Domain. (b) Expor setters públicos para popular o objeto após construção — rejeitado por violar diretamente a regra de imutabilidade/encapsulamento do Princípio VI. (c) Adicionar construtor privado também a todos os Value Objects — rejeitado por ser complexidade desnecessária (Princípio V/YAGNI): nenhum deles tem lógica que precise ser evitada na reconstrução.

## 2. Mapeamento de Value Objects no EF Core

- **Decision**: Usar `HasConversion` do EF Core (via `ValueConverter<TVo, TPrimitive>`) para cada Value Object, convertendo para o tipo de coluna primitivo equivalente: `ExpenseName`→`nvarchar`, `ExpenseCategory`→`int`(enum), `Money`→`decimal(18,2)`, `DueDay`→`int`, `CalendarDate`→`date`, `Frequency`→`int`(enum), `RecurringExpenseStatus`→`int`(enum), `Note`→`nvarchar` nullable, `OccurrenceStatus`→`int`(enum), `ReferencePeriod`→duas colunas (`int Year`, `int Month`) via propriedades sombra ou um conversor composto simples (ex.: `int` codificando `Year*12+Month` reconstruído por `new ReferencePeriod(y, m)`), decidido durante o design de `data-model.md`.
- **Rationale**: `HasConversion` é o mecanismo padrão e mais simples do EF Core 10 para VOs imutáveis com um valor primitivo interno, sem exigir alterar o Domain para expor os primitivos como propriedades públicas (RF02 do refinamento).
- **Alternatives considered**: Owned types (`OwnsOne`) — rejeitado porque a maioria dos VOs aqui é um wrapper de valor único (não um objeto com múltiplos campos relevantes ao schema), tornando `HasConversion` mais direto; e porque o refinamento já decidiu explicitamente que a relação `Occurrence`↔`RecurringExpense` não é owned entity (não se aplica a VOs, mas reforça a preferência por simplicidade).

## 3. Testes de persistência contra SQL Server real

- **Decision**: Usar `Testcontainers.MsSql` no projeto `Infrastructure.Tests`, subindo um container `mcr.microsoft.com/mssql/server` descartável por execução de teste (ou por coleção de testes, via `IAsyncLifetime`/`ICollectionFixture` do xUnit), aplicando as migrations reais (`context.Database.MigrateAsync()`) antes de cada suíte.
- **Rationale**: Já decidido explicitamente na clarificação da spec — necessário para comprovar rollback atômico (Acceptance Scenario 3, User Story 1), que o provedor InMemory do EF Core não reproduz fielmente (não impõe FKs nem transações reais).
- **Alternatives considered**: Provedor InMemory do EF Core — rejeitado explicitamente na clarificação por não reproduzir constraints/transações reais. SQL Server LocalDB — rejeitado por depender de uma instalação local específica do Windows, quebrando reprodutibilidade em CI/outros ambientes. Instância SQL Server compartilhada/manual — rejeitada por acoplar testes a estado externo compartilhado e dificultar paralelismo/CI.

## 4. Configuração de connection string

- **Decision**: A connection string de produção/desenvolvimento vem de `appsettings.json`/`appsettings.{Environment}.json` (não versionados com segredos reais) ou de variáveis de ambiente, seguindo o padrão nativo de configuração do .NET (`IConfiguration`), lida por quem registra o `DbContext` via injeção de dependência (fora do escopo desta feature, que não inclui um host/composition root de API — mas o `DbContext` deve aceitar `DbContextOptions` já configuradas externamente, nunca strings fixas no código). Nos testes, a connection string vem do container Testcontainers provisionado em tempo de execução.
- **Rationale**: Atende FR-011/SC-005 (nenhum segredo versionado) sem introduzir um projeto de host/API que está fora de escopo.
- **Alternatives considered**: Hardcoded connection string com placeholder — rejeitado diretamente por FR-011.

## 5. Estrutura de projeto e nomenclatura

- **Decision**: Novo projeto de classe `backend/Infrastructure/ContasEmDia.Infrastructure.csproj` (mesmo padrão de `TargetFramework net10.0`, `Nullable enable`, `TreatWarningsAsErrors true` do `Domain.csproj`), referenciando `ContasEmDia.Domain`; e `backend/Infrastructure.Tests/ContasEmDia.Infrastructure.Tests.csproj` (xUnit, mesmo padrão de `Domain.Tests`), referenciando `ContasEmDia.Infrastructure`. Ambos adicionados à solução `backend/ContasEmDia.sln`.
- **Rationale**: Consistência com o projeto já existente; a constituição (Princípio VII) exige as pastas `/Repositories`, `/Migrations`, `/Configs`, `/Contexts` dentro do projeto Infrastructure.
- **Alternatives considered**: Colocar Infrastructure e testes de integração no mesmo projeto do Domain — rejeitado por violar a separação de camadas já estabelecida pela estrutura existente e pela constituição.
