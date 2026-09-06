# Phase 0 Research: API de Despesa Recorrente (Cadastro)

**Feature**: `004-api-despesa-recorrente` | **Date**: 2026-09-05

Todas as decisões técnicas desta feature já foram fechadas no refinamento
([`refinements/backend/api-despesa-recorrente.md`](../../refinements/backend/api-despesa-recorrente.md)),
que resolveu, pela primeira vez no repositório, as convenções obrigatórias
do Princípio XII da constituição. Não há nenhum item "NEEDS CLARIFICATION"
pendente — este documento apenas consolida cada decisão já tomada, sua
justificativa e as alternativas descartadas, no formato exigido pelo
workflow de planejamento.

## 1. Pacote de documentação OpenAPI/SwaggerUI

**Decision**: `Swashbuckle.AspNetCore` (gera o documento OpenAPI e serve a
SwaggerUI a partir dele), referenciado apenas pelo novo projeto `backend/Api`.

**Rationale**: É o único pacote entre as opções avaliadas que entrega, com
uma única dependência, tanto o documento OpenAPI machine-readable quanto a
interface interativa SwaggerUI exigidos por FR-012/Princípio XII — o gerador
nativo do .NET 10 (`Microsoft.AspNetCore.OpenApi`) produz apenas o documento
`openapi.json`, sem UI própria, exigindo uma segunda dependência para a
SwaggerUI de qualquer forma.

**Alternatives considered**:
- `Microsoft.AspNetCore.OpenApi` (nativo) + `Scalar.AspNetCore` para a UI —
  duas dependências em vez de uma, sem ganho concreto para este único
  endpoint (Princípio V, YAGNI).
- `NSwag` — feature-set equivalente ao Swashbuckle para este caso de uso,
  porém com superfície de configuração maior que o necessário para um único
  endpoint; Swashbuckle é a opção mais estabelecida no ecossistema ASP.NET
  Core para o par OpenAPI+SwaggerUI.

Esta é uma nova dependência NuGet externa introduzida pela feature; fica
registrada aqui (e no `Technical Context` de `plan.md`) para rastreabilidade,
seguindo o espírito da salvaguarda de dependências do AI Agent Guardrails
(que menciona explicitamente pacotes npm do frontend, mas o mesmo princípio
de transparência se aplica a uma nova dependência de backend).

## 2. Envelope de resposta padrão

**Decision**: `ApiResponse<TData>` (`Success`, `Data`, `Errors`) e
`ApiError` (`Field`, `Message`), definidos em `backend/Api/Responses/`,
conforme já especificado no refinamento — ver "Envelope de resposta padrão"
em `refinements/backend/api-despesa-recorrente.md`.

**Rationale**: Como este é o primeiro endpoint do repositório, esta forma
passa a ser a única convenção de envelope da API (Princípio XII); qualquer
API futura reusa exatamente estes dois tipos, sem redefinição.

**Alternatives considered**: `ProblemDetails` (RFC 7807) puro — descartado
por não separar naturalmente "erro de campo" (`ApiError.Field`) da forma
usada pelo restante do envelope de sucesso, exigindo uma segunda convenção
paralela só para erros.

## 3. Versionamento de rota

**Decision**: Prefixo literal de rota `api/v1/...`, sem biblioteca dedicada
de versionamento.

**Rationale**: Há apenas uma versão e um endpoint nesta etapa; introduzir
`Asp.Versioning` (ou equivalente) não teria uso concreto ainda
(Princípio V, YAGNI). Revisitar quando uma segunda versão do mesmo endpoint
for necessária.

**Alternatives considered**: `Asp.Versioning.Http` com atributos de versão —
descartado por complexidade não justificada por um único endpoint.

## 4. Validação de forma/presença e substituição da resposta automática de `ModelState`

**Decision**: Data annotations (`[Required]`, com `ErrorMessage` explícito
em PT-BR) em `CreateRecurringExpenseDataRequest`, com `MonthlyAmount`/`DueDay`
declarados como `decimal?`/`int?` para que a ausência do campo no JSON seja
detectável (em vez de assumir silenciosamente o valor `0`). A resposta
automática de `ValidationProblemDetails` do `[ApiController]` é substituída
via `ApiBehaviorOptions.InvalidModelStateResponseFactory` em `Program.cs`,
para produzir o mesmo envelope `ApiResponse<TData>`/`ApiError` do caminho de
falha de negócio.

**Rationale**: Mantém FR-009 — o chamador nunca recebe o formato
padrão do framework, apenas o envelope único da API. Tipos anuláveis nos
campos numéricos evitam que "ausente" seja confundido com "enviado como
zero" (uma regra de negócio, não de forma).

**Alternatives considered**: FluentValidation na API — descartado por não
ser necessário para validação puramente de forma/presença (data annotations
já resolvem o caso) e por introduzir uma dependência não justificada por
Princípio V.

## 5. Implementação concreta de `ICurrentDateProvider`

**Decision**: `SystemCurrentDateProvider`, em `backend/Infrastructure`,
implementando `ICurrentDateProvider.GetCurrentDate()` como
`DateOnly.FromDateTime(DateTime.Now)`.

**Rationale**: `ICreateRecurringExpenseUseCase` depende de
`ICurrentDateProvider` (Application/Ports) para determinar a competência do
mês corrente; hoje só existe um dublê de teste
(`FixedCurrentDateProvider`, em `Application.Tests`). Sem uma implementação
real, a composição de DI do endpoint (FR-014) não pode ser fechada. O mesmo
padrão de inversão de dependência já usado por `RepositoryManager`
(interface em Domain/Application, implementação concreta em Infrastructure)
é seguido aqui.

**Alternatives considered**: Implementar diretamente no projeto `Api` — 
descartado porque o padrão já estabelecido no repositório coloca
implementações concretas de portas/abstrações na Infrastructure, e a
Infrastructure já é referenciada pela `Api` (raiz de composição).

## 6. Testes de integração do endpoint (`Api.Tests`)

**Decision**: Novo projeto `backend/Api.Tests` (xUnit), usando
`WebApplicationFactory<Program>` para exercitar o pipeline HTTP completo,
sem pasta/estrutura adicional além do mínimo (um arquivo de testes por
controller/cenário), seguindo a mesma decisão já registrada no refinamento
("Testes (`Api.Tests`)").

**Rationale**: Princípio II exige `WebApplicationFactory` para testes de
endpoint, com um teste por cenário de resposta distinto
(sucesso com/sem ocorrência, falha de negócio, falha de forma/presença).
Uma convenção de pastas dedicada (análoga a `Domain.Tests`) não é
justificada com apenas um endpoint (Princípio V) — mesma lógica já aplicada
a `Application.Tests`.

**Alternatives considered**: Testar via `HttpClient` apontando para uma
instância real do Kestrel — descartado por não ser hermético/CI-friendly e
por `WebApplicationFactory` já ser a exigência explícita da Constituição.

Para isolar os testes de um banco de dados real, `ContasEmDiaDbContext`
deve ser substituído no `WebApplicationFactory` por um provider EF Core
InMemory (ou SQLite in-memory) apenas na composição de testes — decisão de
implementação, não de contrato: não altera nenhum tipo público desta
feature.

## 7. Fonte da connection string (FR-014, Princípio IV)

**Decision**: `appsettings.json`/`appsettings.Development.json` +
`IConfiguration` padrão do ASP.NET Core (`builder.Configuration`), nunca uma
string fixa no código-fonte.

**Rationale**: Exigência direta de FR-014/Princípio IV (nenhuma credencial ou
connection string hardcoded). `appsettings.Development.json` pode apontar
para uma instância local de SQL Server/LocalDB para desenvolvimento; um
ambiente real (fora do escopo desta feature) usaria variável de ambiente ou
outro provedor de configuração seguro.

**Alternatives considered**: Nenhuma — é a forma padrão e já usada
implicitamente por `ContasEmDiaDbContextFactory` (Infrastructure) para
migrations.
