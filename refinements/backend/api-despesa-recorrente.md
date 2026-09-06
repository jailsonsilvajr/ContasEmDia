# Refinamento — Projeto API: Despesa Recorrente (Cadastro)

## Origem
Tela de design: **"Nova despesa"** (`design/Cadastro.dc.html` — "Nova despesa
recorrente"). Base já existente: `backend/Domain` (aggregate
`RecurringExpense`), `backend/Infrastructure` (persistência via EF Core 10) e
`backend/Application` (`CreateRecurringExpenseUseCase`), documentados em
[`domain-despesa-recorrente.md`](./domain-despesa-recorrente.md),
[`infrastructure-despesa-recorrente.md`](./infrastructure-despesa-recorrente.md)
e [`application-despesa-recorrente.md`](./application-despesa-recorrente.md).

## Feature
Criar, no backend, o projeto **API** (camada de Apresentação HTTP), expondo
como endpoint versionado a única ação que a tela "Nova despesa recorrente"
dispara contra o backend: cadastrar uma despesa recorrente. O endpoint
recebe o corpo da requisição, valida apenas forma/presença dos campos,
delega toda a orquestração ao `ICreateRecurringExpenseUseCase` já
implementado em `backend/Application`, e traduz o `Output` do Use Case em uma
resposta HTTP — sem introduzir nenhuma regra de negócio nova. Este é o
**primeiro** projeto de API do repositório: além do endpoint em si, este
refinamento precisa fixar, pela primeira vez, convenções obrigatórias pelo
Princípio XII da constituição que ainda não existem em código nenhum
(envelope de resposta padrão, versionamento, middleware de exceções,
documentação OpenAPI/SwaggerUI) — decisões que passam a valer para toda API
futura do projeto, não apenas para este endpoint.

## Escopo
Cobre o endpoint de cadastro de despesa recorrente (a tela analisada não
dispara nenhuma outra ação contra o backend) e, adicionalmente, dois
pré-requisitos/decorrências diretas dele que este refinamento passou a
absorver (ver "Premissas e pontos em aberto"). Cobre: a criação do novo
projeto `backend/Api`, o `DataRequest`/`DataResponse` do endpoint, o
mapeamento request/response ↔ Use Case Input/Output, o envelope de resposta
padrão da API (definido aqui pela primeira vez), o middleware global de
tratamento de exceções, a documentação OpenAPI/SwaggerUI, o versionamento de
rota, a composição de dependências (DI) necessária para o endpoint funcionar
de ponta a ponta (`Program.cs`, `appsettings.json`), a implementação
concreta de `ICurrentDateProvider` na Infrastructure (RF16) e a atualização
dos documentos de contrato do frontend (`specs/002-cadastro-despesa-recorrente/contracts/api-contract.md`
e `refinements/frontend/cadastro-despesa-recorrente.md`) para refletir a
rota versionada e o envelope definidos aqui (RF17).

Não cobre: qualquer regra de negócio nova (todas já fechadas no Domain e
orquestradas pela Application), qualquer alteração em Domain ou Application
já implementados, qualquer alteração em código de Infrastructure já
implementado além da nova classe de `ICurrentDateProvider` (RF16),
autenticação/autorização/CORS reais (exceção vigente do Princípio IV), e
qualquer outro endpoint além deste (listagem, detalhe, edição,
pausa/reativação, exclusão, pagamento de ocorrência, catálogo de
categorias).

Não há sugestão de código completo nas seções abaixo além do necessário para
fixar assinaturas e formas de dados — o foco é a estrutura conceitual,
contratos e decisões de arquitetura necessários antes da implementação.

## Referências
- Application: [`application-despesa-recorrente.md`](./application-despesa-recorrente.md);
  código em `backend/Application` (`CreateRecurringExpenseUseCase`,
  `CreateRecurringExpenseUseCaseInput`/`Output`, `ICurrentDateProvider`).
- Domain: [`domain-despesa-recorrente.md`](./domain-despesa-recorrente.md);
  código em `backend/Domain`.
- Infrastructure: [`infrastructure-despesa-recorrente.md`](./infrastructure-despesa-recorrente.md);
  código em `backend/Infrastructure` (`RepositoryManager`,
  `ContasEmDiaDbContext`).
- Contrato de API assumido pelo frontend:
  [`../frontend/cadastro-despesa-recorrente.md`](../frontend/cadastro-despesa-recorrente.md)
  e [`../../specs/002-cadastro-despesa-recorrente/contracts/api-contract.md`](../../specs/002-cadastro-despesa-recorrente/contracts/api-contract.md) —
  **ambos desatualizados frente às decisões deste refinamento**; esta feature
  passa a cobrir a atualização de ambos (ver "Divergência com o contrato já
  documentado para o frontend" em "Premissas e pontos em aberto" e RF17).
- Constituição do projeto: [`../../.specify/memory/constitution.md`](../../.specify/memory/constitution.md) —
  Princípio XII (API Layer Implementation) é a referência normativa central
  deste refinamento; Princípios I, II, IV, V e XI também se aplicam.

## Conceitos identificados

### Projeto `Api` (novo)
Novo projeto ASP.NET Core Web API, `backend/Api/ContasEmDia.Api.csproj`,
adicionado à `ContasEmDia.sln` seguindo a mesma convenção de nomenclatura dos
demais projetos (`ContasEmDia.<Camada>`). É a **raiz de composição** (DI) da
aplicação: referencia tanto `Application` quanto `Infrastructure` — a única
camada com permissão para conhecer ambas diretamente, papel distinto do
Princípio XI (que restringe apenas o projeto Application a depender só do
Domain).

### `RecurringExpensesController`
Controller único desta feature, em `/Controllers`, expondo uma única ação
HTTP `POST` que traduz a requisição em uma chamada a
`ICreateRecurringExpenseUseCase.ExecuteAsync`. Não contém nenhuma lógica de
negócio — apenas construção do Input, chamada ao Use Case e tradução do
Output em uma resposta HTTP (Princípio XII).

### `CreateRecurringExpenseDataRequest`
Tipo dedicado ao corpo da requisição, em `/Requests`, espelhando os mesmos
campos primitivos de `CreateRecurringExpenseUseCaseInput` (nome, categoria,
valor previsto mensal, dia de vencimento, data de início como texto,
frequência, status, observação opcional). Implementado como `record` com
anotações de validação **restritas a forma/tipo/presença** (Princípio XII) —
nunca regra de negócio.

### `CreateRecurringExpenseDataResponse`
Tipo dedicado à resposta de sucesso, em `/Responses`, espelhando os campos de
sucesso de `CreateRecurringExpenseUseCaseOutput` (dados da despesa recorrente
criada e da lista de ocorrências geradas). Contém uma lista de um tipo
aninhado próprio para a ocorrência (ver "Forma da ocorrência na resposta").

### Envelope de resposta padrão (`ApiResponse<TData>` / `ApiError`)
Como este é o primeiro endpoint do repositório, o envelope de resposta único
exigido pelo Princípio XII ainda não existe em nenhum lugar do código — este
refinamento o define pela primeira vez, em `/Responses`, como convenção
válida para toda a API, não apenas para este endpoint:
- `ApiResponse<TData>`: `Success` (`bool`), `Data` (`TData?`, presente apenas
  quando `Success` é `true`) e `Errors` (`IReadOnlyCollection<ApiError>?`,
  presente apenas quando `Success` é `false`).
- `ApiError`: `Field` (`string?`, nome do campo em `camelCase` igual ao da
  requisição — `null` quando o erro não é de um campo específico) e
  `Message` (`string`, mensagem voltada ao usuário final).

Toda resposta desta API — sucesso ou erro, deste endpoint ou de qualquer
futuro — deve ser serializada dentro de `ApiResponse<TData>`; nenhuma ação
pode devolver seu `DataResponse` (ou um erro) "cru" no corpo (Princípio XII).

### Mapeamentos dedicados
- `CreateRecurringExpenseDataRequestMapping`: `CreateRecurringExpenseDataRequest`
  → `CreateRecurringExpenseUseCaseInput`.
- `CreateRecurringExpenseDataResponseMapping`: `CreateRecurringExpenseUseCaseOutput`
  (caminho de sucesso) → `CreateRecurringExpenseDataResponse`.

Um arquivo por mapeamento, cada um cobrindo exatamente um par
DataRequest/DataResponse, sem lógica de negócio além de tradução de forma
(Princípio XII). A tradução do caminho de falha de negócio
(`Output.Errors`, uma coleção de `FieldError`) para `ApiError` é uma
transposição direta de campo (`FieldError.Field`/`Message` →
`ApiError.Field`/`Message`) — os nomes de campo já saem do Use Case em
`camelCase` idêntico ao da requisição (`name`, `category`, `monthlyAmount`,
`dueDay`, `startDate`, `frequency`, `status`), então não há tabela de
tradução de nomes a manter; ainda assim, essa transposição também deve viver
em um mapeamento dedicado (não inline no controller), por consistência com a
regra acima.

### Middleware global de exceções (`ExceptionHandlingMiddleware`)
Middleware único, em `/Middlewares`, exigido pelo Princípio XII
independentemente deste endpoint específico. Para esta feature, seu papel é
estritamente o de rede de segurança para falhas verdadeiramente inesperadas
(ex.: banco de dados indisponível) → `500`, convertidas para o mesmo
envelope `ApiResponse<TData>` (com `Data = null`); ver "Este endpoint não
depende do middleware para o caminho `400`" em "Regras técnicas adicionais"
para o porquê disso não conflitar com o caminho de falha de validação de
negócio, que não passa por exceção.

### Implementação concreta de `ICurrentDateProvider` (Infrastructure)
`ICurrentDateProvider` (declarada em `backend/Application/Ports`) ainda não
tem implementação concreta no repositório — apenas um dublê de teste
(`FixedCurrentDateProvider`, em `Application.Tests`). Este refinamento passa
a cobrir também a criação de uma implementação concreta na Infrastructure
(ex.: `SystemCurrentDateProvider`, em `backend/Infrastructure`), devolvendo
`DateOnly.FromDateTime(DateTime.Now)`, pelo mesmo padrão de inversão de
dependência já usado por `RepositoryManager` (interface consumida por
Application/API, implementação concreta na Infrastructure). É um
pré-requisito para a composição de DI do endpoint (RF15) — ver RF16 e
"Implementação de `ICurrentDateProvider`" em "Premissas e pontos em aberto".

## Requisitos Funcionais

### RF01 — Endpoint HTTP para o Use Case de cadastro
O sistema deve expor um único endpoint HTTP, `POST`, que traduz seu corpo de
requisição para `CreateRecurringExpenseUseCaseInput`, delega a execução a
`ICreateRecurringExpenseUseCase.ExecuteAsync` e traduz o
`CreateRecurringExpenseUseCaseOutput` resultante em uma resposta HTTP —
nenhuma outra ação é exposta por esta feature.

### RF02 — Rota versionada
O endpoint deve ser exposto sob um segmento de versão explícito na rota:
`POST /api/v1/recurring-expenses` (Princípio XII). Ver "Versionamento sem
pacote dedicado" em "Premissas e pontos em aberto" para a justificativa de
não introduzir uma biblioteca de versionamento nesta etapa.

### RF03 — `DataRequest` dedicado, validado apenas em forma/presença
O corpo da requisição deve ser recebido como `CreateRecurringExpenseDataRequest`,
único tipo dedicado a esta requisição, anotado apenas para validar
tipo/presença de cada campo obrigatório (nome, categoria, valor previsto
mensal, dia de vencimento, data de início, frequência, status) — nunca uma
regra de negócio (ex.: "valor > 0", "dia entre 1 e 31"), que permanece
exclusiva do Domain (Princípio VI, Princípio XII). Cada anotação de
forma/presença deve definir um `ErrorMessage` explícito em PT-BR (Princípio
XII, emenda 2026-09-05) — ver "Mensagens de erro de validação de
forma/presença em PT-BR" em "Regras técnicas adicionais". Ver também
"Presença de campos numéricos exige tipos anuláveis no DataRequest" em
"Regras técnicas adicionais" para a técnica necessária quanto a
`MonthlyAmount`/`DueDay`.

### RF04 — Mapeamento dedicado `DataRequest` → Use Case Input
A tradução de `CreateRecurringExpenseDataRequest` para
`CreateRecurringExpenseUseCaseInput` deve viver em um arquivo de mapeamento
dedicado (`CreateRecurringExpenseDataRequestMapping`), nunca inline no
controller (Princípio XII).

### RF05 — Nenhuma lógica de negócio no controller
O controller não deve conter nenhuma regra de negócio, validação de negócio
ou decisão sobre o resultado além de rotear o `Output` do Use Case para o
código HTTP e envelope corretos (Princípio XII) — toda decisão de negócio já
está fechada no Use Case (que por sua vez delega ao Domain).

### RF06 — Mapeamento dedicado Use Case Output → `DataResponse`
A tradução do caminho de sucesso de `CreateRecurringExpenseUseCaseOutput`
para `CreateRecurringExpenseDataResponse` deve viver em um arquivo de
mapeamento dedicado (`CreateRecurringExpenseDataResponseMapping`), nunca
inline no controller (Princípio XII).

### RF07 — Envelope de resposta único para sucesso e erro
Toda resposta deste endpoint — sucesso (`201`) ou falha de validação de
negócio (`400`) — deve ser serializada dentro do envelope
`ApiResponse<CreateRecurringExpenseDataResponse>`, nunca com o
`DataResponse` ou a lista de erros soltos diretamente no corpo (Princípio
XII).

### RF08 — Código `201 Created` em caso de sucesso
Quando `Output.IsSuccess` for `true`, o endpoint deve responder `201 Created`
com `ApiResponse<CreateRecurringExpenseDataResponse>.Success` preenchido a
partir do mapeamento de RF06.

### RF09 — Código `400 Bad Request` em caso de falha de validação de negócio
Quando `Output.IsSuccess` for `false`, o endpoint deve responder
`400 Bad Request` com `ApiResponse<CreateRecurringExpenseDataResponse>`
tendo `Success = false`, `Data = null` e `Errors` preenchido a partir de
`Output.Errors` (RF conceitual "Mapeamentos dedicados"). Este caminho **não**
passa pelo middleware global de exceções (RF12) — é um retorno normal do Use
Case, não uma exceção (ver "Este endpoint não depende do middleware para o
caminho `400`" em "Regras técnicas adicionais").

### RF10 — Falha de forma/presença no mesmo envelope de erro
Uma falha de validação de forma/presença do próprio `CreateRecurringExpenseDataRequest`
(ex.: campo obrigatório ausente no JSON) também deve ser respondida como
`400 Bad Request` usando o **mesmo** envelope `ApiResponse<TData>` e a mesma
forma de `ApiError` do RF09 — nunca a resposta padrão de
`ValidationProblemDetails` do ASP.NET Core, que tem um formato diferente. Ver
"Substituição da resposta automática de `ModelState` inválido" em "Regras
técnicas adicionais" para a técnica necessária.

### RF11 — Declaração explícita de todos os retornos possíveis
A ação do controller deve declarar, via `ProducesResponseType` (ou
equivalente do framework), todos os seus retornos possíveis: `201`
(`ApiResponse<CreateRecurringExpenseDataResponse>`), `400`
(`ApiResponse<CreateRecurringExpenseDataResponse>` com `Errors` preenchido,
cobrindo tanto RF09 quanto RF10) e `500` (via middleware global, RF12),
tornando o conjunto completo de resultados visível na especificação OpenAPI
(Princípio XII).

### RF12 — Middleware global de exceções para falhas inesperadas
O sistema deve registrar um único middleware global de tratamento de
exceções que capture qualquer exceção não tratada propagada pelo Use Case
(ex.: falha de acesso ao banco de dados) e a converta em `500 Internal
Server Error`, dentro do mesmo envelope `ApiResponse<TData>` (com
`Data = null` e um `ApiError` genérico, sem detalhes internos da exceção no
corpo da resposta). Nenhum controller deve implementar tratamento de exceção
próprio (Princípio XII).

### RF13 — Documentação OpenAPI e SwaggerUI
A API deve expor um documento OpenAPI machine-readable e uma SwaggerUI
interativa para explorar e exercitar o endpoint (Princípio XII), incluindo
os `DataRequest`/`DataResponse`/`ApiResponse`/`ApiError` no schema gerado.

### RF14 — Nenhuma autenticação, autorização ou CORS nesta etapa
Por força da exceção vigente de fase atual do Princípio IV (e reafirmada no
Princípio XII), este endpoint não deve implementar autenticação,
autorização ou política de CORS nesta etapa — isso permanece um débito
explícito, não uma omissão silenciosa, e deve ser fechado antes de qualquer
deploy em produção ou ambiente externamente acessível.

### RF15 — Composição de dependências do endpoint
O projeto `Api` deve compor, na inicialização da aplicação, todas as
dependências necessárias para `ICreateRecurringExpenseUseCase` funcionar de
ponta a ponta: `ContasEmDiaDbContext` (via configuração/connection string,
nunca hardcoded — Princípio IV), `IRepositoryManager` → `RepositoryManager`,
`ICreateRecurringExpenseUseCase` → `CreateRecurringExpenseUseCase`, e
`ICurrentDateProvider` → a implementação concreta definida em RF16.

### RF16 — Implementação concreta de `ICurrentDateProvider`
O sistema deve incluir, em `backend/Infrastructure`, uma implementação
concreta de `ICurrentDateProvider` (ex.: `SystemCurrentDateProvider`),
devolvendo `DateOnly.FromDateTime(DateTime.Now)`, registrada na composição
de DI do endpoint (RF15). Sem esta classe, RF15 não pode ser satisfeito —
ver "Implementação concreta de `ICurrentDateProvider` (Infrastructure)" em
"Conceitos identificados".

### RF17 — Atualização dos documentos de contrato do frontend
O sistema (esta feature) deve atualizar
`specs/002-cadastro-despesa-recorrente/contracts/api-contract.md` e a seção
"Contrato de API necessário" de
`refinements/frontend/cadastro-despesa-recorrente.md` para refletir a rota
versionada (`POST /api/v1/recurring-expenses`, RF02) e o envelope
`ApiResponse<TData>`/`ApiError` (RF07/RF09/RF10) definidos neste
refinamento, fechando a divergência descrita em "Divergência com o contrato
já documentado para o frontend" em "Premissas e pontos em aberto".

## Regras técnicas adicionais

### Estrutura de pastas do projeto `Api`
```
backend/Api/
  ContasEmDia.Api.csproj
  Program.cs
  appsettings.json
  appsettings.Development.json
  Controllers/
    RecurringExpensesController.cs
  Requests/
    CreateRecurringExpenseDataRequest.cs
  Responses/
    ApiResponse.cs
    ApiError.cs
    CreateRecurringExpenseDataResponse.cs
    OccurrenceDataResponse.cs
  Mappings/
    CreateRecurringExpenseDataRequestMapping.cs
    CreateRecurringExpenseDataResponseMapping.cs
  Middlewares/
    ExceptionHandlingMiddleware.cs
  Filters/
```
`/Filters` é criada vazia nesta etapa — nenhum filtro é necessário para este
único endpoint; a pasta existe apenas para já fixar a convenção estrutural
exigida pelo Princípio XII para quando um filtro futuro for necessário.

### Presença de campos numéricos exige tipos anuláveis no `DataRequest`
`[Required]` não detecta ausência em um tipo de valor não anulável (`decimal`,
`int`): se o campo faltar no JSON, o model binding assume o valor `default`
(`0`) silenciosamente, sem erro — o que tornaria "ausente" indistinguível de
"enviado como zero" (uma regra de negócio, não de forma). Por isso,
`MonthlyAmount` e `DueDay` devem ser declarados como `decimal?`/`int?` em
`CreateRecurringExpenseDataRequest` (com `[Required]`), e convertidos para os
tipos não anuláveis exigidos por `CreateRecurringExpenseUseCaseInput` apenas
dentro do mapeamento de RF04 — a essa altura, a validação de
`ModelState`/RF10 já garante que o valor não é nulo.

### Mensagens de erro de validação de forma/presença em PT-BR
Por exigência do Princípio XII (emenda 2026-09-05), toda anotação de
forma/presença em `CreateRecurringExpenseDataRequest` (`[Required]`,
`[MaxLength]`, `[Range]` etc.) deve definir explicitamente `ErrorMessage` em
PT-BR — as mensagens padrão do ASP.NET Core (em inglês, ex.: "The Name field
is required.") nunca devem chegar ao chamador. Isso vale tanto para as
mensagens agregadas em `ModelState` (RF10) quanto para qualquer outra
anotação de forma futura na API.

### Substituição da resposta automática de `ModelState` inválido
Por padrão, um controller anotado com `[ApiController]` intercepta um
`ModelState` inválido (RF10) e responde automaticamente com um
`ValidationProblemDetails` no formato padrão do ASP.NET Core — que não é o
envelope `ApiResponse<TData>` definido neste refinamento. É necessário
configurar `ApiBehaviorOptions.InvalidModelStateResponseFactory` (em
`Program.cs`) para produzir, a partir do `ModelStateDictionary`, o mesmo
envelope `ApiResponse<TData>`/`ApiError` usado pelo caminho de falha de
negócio (RF09), preservando a promessa do Princípio XII de um único formato
de erro em toda a API — o frontend não deve precisar distinguir se um `400`
veio de uma falha de forma ou de uma falha de negócio.

### Este endpoint não depende do middleware para o caminho `400`
`CreateRecurringExpenseUseCase` já agrega toda falha de validação de negócio
em `Output.Errors` e **não lança exceção** para isso (RF05/RF12 do
refinamento de Application) — exceção é reservada a falhas verdadeiramente
inesperadas. Por isso, o caminho `400` de negócio (RF09) é produzido
diretamente pelo controller a partir de um retorno normal do Use Case, não
pelo middleware global de exceções (RF12), que só entra em ação para uma
falha não tratada (ex.: banco de dados indisponível). Isso não é uma
contradição com o Princípio XII (que trata do mapeamento de *exceções* de
regra de negócio para `400`/`422`) — apenas reflete que, nesta feature, uma
violação de regra de negócio nunca chega à API como exceção, por decisão já
tomada na camada Application.

### Forma da ocorrência na resposta
`CreateRecurringExpenseDataResponse.Occurrences` é uma lista de
`OccurrenceDataResponse`, e cada item aninha `ReferencePeriod` como um objeto
próprio (`{ Year, Month }`) — não como dois campos soltos —, espelhando a
forma já assumida pelo contrato de API do frontend (`referencePeriod: {year,
month}`), embora `CreateRecurringExpenseUseCaseOutput`/`OccurrenceData` do
Use Case exponha `ReferenceYear`/`ReferenceMonth` como dois campos primitivos
separados; essa reninhagem é responsabilidade do mapeamento de RF06.

### Convenção de nomes JSON
Nenhuma configuração adicional de *casing* é necessária: a convenção padrão
do ASP.NET Core (`camelCase` para propriedades de `record`/classe C#
serializadas via `System.Text.Json`) já produz os mesmos nomes de campo
(`name`, `monthlyAmount`, `dueDay` etc.) assumidos pelo contrato de API já
documentado para o frontend.

### Ausência de header `Location` na resposta `201`
A resposta de sucesso não inclui o header HTTP `Location` (normalmente via
`CreatedAtAction`/`CreatedAtRoute`) porque não existe, nesta etapa, nenhum
endpoint de leitura (`GET`) de uma despesa recorrente para o qual apontar; o
`201` devolve o recurso criado apenas no corpo da resposta. Deve ser
revisitado quando uma feature de listagem/detalhe (painel mensal,
`design/Main.dc.html`) for implementada.

### Testes (`Api.Tests`)
Seguindo o padrão já usado por `Domain.Tests`, `Infrastructure.Tests` e
`Application.Tests`, este endpoint deve ter testes de integração/contrato
próprios (`Api.Tests`), usando `WebApplicationFactory` para exercitar o
endpoint HTTP de ponta a ponta (Princípio II), cobrindo pelo menos: sucesso
(`201`, com e sem ocorrência gerada, conforme `status` enviado), falha de
negócio (`400`, um ou mais campos inválidos) e falha de forma/presença
(`400`, campo obrigatório ausente no JSON) — todos verificando o mesmo
formato de envelope `ApiResponse<TData>`.

## Fora de escopo
- Qualquer outro endpoint além do cadastro: listagem de despesas recorrentes
  ou ocorrências (painel mensal), detalhe/`GET` por id, edição,
  pausa/reativação, exclusão, marcar ocorrência como paga, catálogo de
  categorias via API.
- Autenticação, autorização e CORS reais (Princípio IV, exceção vigente).
- Qualquer alteração em Domain ou Application já implementados, e qualquer
  alteração em código de Infrastructure já implementado além da nova classe
  de `ICurrentDateProvider` (RF16).
- Biblioteca dedicada de versionamento de API (`Asp.Versioning` ou
  equivalente) — o versionamento desta etapa é resolvido apenas por um
  prefixo de rota literal (ver "Versionamento sem pacote dedicado" abaixo).
- Implementação, no frontend, do `DespesaRecorrenteService` ou de qualquer
  código de consumo do endpoint — apenas os documentos de contrato
  (`api-contract.md` e o refinamento de frontend) são atualizados por esta
  feature (RF17), não código de frontend.

## Premissas e pontos em aberto

### Envelope de resposta `ApiResponse<TData>`/`ApiError` (decidido, primeira definição)
Como nenhum endpoint existia antes desta feature, o envelope exigido pelo
Princípio XII não tinha, até este refinamento, nenhuma forma concreta
definida em nenhum documento ou código do repositório. Decisão tomada aqui:
a forma descrita em "Envelope de resposta padrão" acima
(`Success`/`Data`/`Errors`, com `ApiError { Field, Message }`) passa a ser a
convenção de toda a API, não apenas deste endpoint. Consequência: qualquer
API futura deve reusar exatamente esses dois tipos, nunca redefinir um
envelope próprio.

### Versionamento sem pacote dedicado (decidido)
Decisão tomada: o versionamento exigido pelo Princípio XII é resolvido nesta
etapa apenas com um prefixo de rota literal (`api/v1/...`), sem introduzir a
biblioteca `Asp.Versioning` (ou equivalente) — haveria apenas uma versão e
um endpoint, então a biblioteca não teria uso concreto ainda (Princípio V,
YAGNI). Se uma segunda versão do mesmo endpoint for necessária no futuro,
essa decisão deve ser revisitada.

### Divergência com o contrato já documentado para o frontend (decidido — fechada nesta feature)
`specs/002-cadastro-despesa-recorrente/contracts/api-contract.md` e a seção
"Contrato de API necessário" de
`refinements/frontend/cadastro-despesa-recorrente.md` descrevem a rota sem
versão (`POST /api/recurring-expenses`) e corpos de resposta "crus", sem
envelope (`201` com os campos da despesa direto no corpo; `400` como
`{ "errors": [...] }` solto). Ambos os documentos são anteriores à emenda do
Princípio XII (2026-09-05) que introduziu a exigência do envelope único e do
versionamento de rota, e ficaram desatualizados frente a ela. Este
refinamento segue a Constituição (fonte normativa) em vez desses dois
documentos. Decisão tomada: em vez de apenas sinalizar a divergência para
fechamento posterior, esta própria feature passa a cobrir a atualização de
ambos os documentos (rota `api/v1/...`, corpo envelopado em
`ApiResponse<TData>`) — ver RF17 — para que o `DespesaRecorrenteService` do
frontend (ainda não implementado) seja construído já contra a forma
correta, sem depender de uma feature futura para fechar essa divergência.

### Implementação de `ICurrentDateProvider` (decidido — incluída nesta feature)
`ICurrentDateProvider` (declarada em `backend/Application/Ports`) não tem,
hoje, nenhuma implementação concreta no repositório — apenas um dublê de
teste (`FixedCurrentDateProvider`) existe, em `Application.Tests`. A
composição de DI desta API (RF15) depende de uma implementação real (ex.: um
provedor que devolve `DateOnly.FromDateTime(DateTime.Now)`), que deve viver
na Infrastructure, pelo mesmo padrão de inversão de dependência já usado por
`RepositoryManager` (interface consumida por Application/API, implementação
concreta na Infrastructure — decisão já registrada no refinamento de
Application). Decisão tomada: em vez de tratar essa implementação como um
pré-requisito bloqueante externo, coberto por uma atualização separada do
refinamento/código de Infrastructure, esta própria feature passa a incluir
sua criação — ver RF16 e "Implementação concreta de `ICurrentDateProvider`
(Infrastructure)" em "Conceitos identificados".

### Mensagens de erro de validação de forma/presença em PT-BR (decidido)
As mensagens padrão de validação de forma/presença do ASP.NET Core (RF10, ex.:
"The Name field is required.") saem em inglês por padrão, diferente das
mensagens de regra de negócio do Domain, que a Constituição já exige em
PT-BR (Princípio VI). O que neste refinamento era uma assunção recomendada
(sem obrigação constitucional explícita) passou a ser exigência normativa:
a Constituição foi emendada (Princípio XII, versão 1.8.0, 2026-09-05) para
exigir explicitamente que toda anotação de forma/presença em um
`DataRequest` (`[Required]`, `[MaxLength]`, `[Range]` etc.) defina
`ErrorMessage` em PT-BR. Este ponto deixa de ser uma questão em aberto: a
customização das mensagens de `CreateRecurringExpenseDataRequest` para
PT-BR é agora obrigatória (RF03, ver também "Mensagens de erro de validação
de forma/presença em PT-BR" em "Regras técnicas adicionais").

### Nomenclatura de tipo de resposta aninhado (assunção)
O Princípio XII exige o sufixo `DataResponse` para "o" tipo de resposta de
um endpoint, mas não deixa explícito se um tipo aninhado interno a um
`DataResponse` (aqui, `OccurrenceDataResponse` dentro de
`CreateRecurringExpenseDataResponse`, e o objeto `ReferencePeriod` aninhado
dentro dele) também precisa seguir esse sufixo. Assunção adotada: sim, todo
tipo que representa uma forma de resposta no schema OpenAPI — mesmo aninhado
— deve viver em `/Responses` e usar o sufixo `DataResponse`, por
consistência e para não exigir uma segunda convenção não escrita; o objeto
`{ Year, Month }` aninhado pode ser um tipo simples (`record ReferencePeriodDataResponse(int Year, int Month)`)
sem essa necessidade forçada de ligação a um "endpoint" específico. Sinalizado
aqui por não ser uma leitura 100% inequívoca do Princípio XII.

### Ausência de um princípio de constituição dedicado a testes de API
O Princípio II exige "testes de integração ou contrato para endpoints de
API", mas — assim como já apontado no refinamento de Application quanto à
ausência de um princípio dedicado à camada Application — a Constituição não
define uma estrutura de pastas ou convenção de nomenclatura própria para
`Api.Tests`. Este refinamento assume a estrutura mínima necessária
(`WebApplicationFactory` + um teste por cenário de resposta) sem introduzir
convenção adicional, por não haver ainda mais de um endpoint que a
justifique (Princípio V).
