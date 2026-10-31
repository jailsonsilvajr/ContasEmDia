# Refinamento — Backend: Deletar Despesa Recorrente

## Origem
Refinamento funcional: [`deletar-despesa-recorrente.md`](../deletar-despesa-recorrente.md).

## Feature
Refinamento técnico (sem código, sem assinaturas de método completas — só
conceitos e contratos) das quatro camadas do backend necessárias para
excluir uma despesa recorrente já cadastrada nas duas abrangências definidas
pelo refinamento funcional: um novo método de negócio no aggregate
`RecurringExpense` para a exclusão parcial (Domain), um novo método de
repositório para a exclusão total (Infrastructure/Domain), um novo UseCase
(Application), e um novo endpoint `DELETE` (API) — além de um ajuste pontual
em duas leituras já implementadas (`GetByIdAsync`), necessário para que uma
despesa excluída pare de ser encontrável por edição ou por uma segunda
exclusão (RF14/EC07 do refinamento funcional).

Este documento parte do **código já implementado** (`ChangeEndDate`,
`GetRecurringExpenseById`, `UpdateRecurringExpense`, seus respectivos
controllers), não de um projeto do zero.

## Escopo
Cobre:
- Um novo método de negócio no aggregate `RecurringExpense` (`Domain`) para
  a abrangência "excluir apenas as pendentes/vencidas" (RF04/RF05 do
  refinamento funcional).
- Um novo campo de estado no aggregate para marcar essa exclusão parcial
  como definitiva (RF05/RF08), e o ajuste necessário em `GetByIdAsync` para
  que esse estado torne a despesa "não encontrada" para leitura/edição
  (RF14/EC07 do refinamento funcional).
- Um novo método de repositório para a abrangência "excluir todo o
  histórico" (RF06), e a migração EF Core necessária para o novo campo de
  estado da exclusão parcial.
- Um novo UseCase na `Application`: excluir uma despesa recorrente,
  recebendo a abrangência escolhida.
- Um novo endpoint `DELETE` na `Api`, seguindo o mesmo envelope de resposta
  e mapeamentos dedicados já estabelecidos pelos endpoints existentes.

Não cobre:
- Qualquer alteração em `OccurrencesController` ou nos UseCases de
  pagamento — não são afetados por esta feature (RF08 do refinamento
  funcional: exclusão independe de status; nenhuma regra nova se aplica ao
  pagamento de uma ocorrência já existente).
- Autenticação/autorização e CORS — mesma exceção temporária já registrada
  na constituição (Princípio IV).
- Testes (nomes de classes/métodos) — apenas a exigência, herdada do
  Princípio II, de que cada cenário de resposta declarado tenha um teste
  correspondente.
- Mecanismo de "desfazer exclusão" ou auditoria — fora de escopo também no
  refinamento funcional ("Pontos em aberto" daquele documento).

## Referências
- Refinamento funcional (RF/EC/CA citados abaixo são os dele, salvo
  indicação contrária): [`deletar-despesa-recorrente.md`](../deletar-despesa-recorrente.md).
- Código atual do aggregate: `backend/Domain/Aggregates/RecurringExpense.cs`
  — em especial `ChangeEndDate` (linhas 132–168), o precedente direto de
  "remover ocorrências da coleção em memória e deixar o
  `SaveChangesAsync()` do repositório apagar as linhas órfãs no banco", que
  este refinamento reaproveita.
- Código atual do repositório: `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs`
  — em especial `UpdateAsync` (reaproveitado sem alteração) e `GetByIdAsync`
  (ajustado por este refinamento).
- Configuração EF Core atual: `backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs`
  — `builder.HasMany<Occurrence>("_occurrences").WithOne().HasForeignKey("RecurringExpenseId").IsRequired()`,
  já a base do comportamento de "excluir órfãos" reaproveitado abaixo.
- Precedente de um `DELETE` já implementado no mesmo estilo de resposta:
  `backend/Api/Controllers/OccurrencesController.cs`, ação `UndoPayment`
  (`DELETE /api/v1/occurrences/{occurrenceId}/payment`) — devolve `200 OK`
  com o envelope `ApiResponse<TData>` preenchido, **nunca** `204 No
  Content`; este é o precedente que este refinamento segue para o novo
  `DELETE` de despesa recorrente, em vez de inventar uma convenção nova.
- Refinamento técnico já existente de maior semelhança estrutural (dois
  ramos de comportamento dependendo de um valor de entrada, mais um
  endpoint num controller já existente): [`editar-despesa-recorrente.md`](editar-despesa-recorrente.md).
- Constituição do projeto: Princípios VI (Domain), VII (Infrastructure), XI
  (Application) e XII (API).

## Camada Domain

### Novo método de negócio: exclusão parcial ("apenas pendentes/vencidas")
Pelo Princípio VI, a exclusão parcial não pode ser um `setter`; precisa de
um método de intenção de negócio no aggregate. Nome de trabalho:
`DeleteKeepingPaidHistory()`, sem parâmetros. Comportamento:

1. Remove de `_occurrences` toda ocorrência cujo status persistido não seja
   Paga — `_occurrences.RemoveAll(o => o.GetStatus().GetValue() !=
   OccurrenceStatusType.Paid)` — reaproveitando exatamente o mecanismo já
   comprovado por `ChangeEndDate` (linha 162 hoje): remover da coleção em
   memória e deixar o repositório (`UpdateAsync`, ver "Infrastructure"
   abaixo) apagar as linhas órfãs no `SaveChangesAsync()`. Nenhuma
   ocorrência Paga é tocada (RF04 do refinamento funcional) —
   diferente de `ChangeEndDate`, que **bloqueia** a operação se isso
   removeria uma ocorrência paga (`hasPaidOccurrenceBeingRemoved`), este
   método nunca bloqueia, porque nunca tenta remover uma ocorrência paga em
   primeiro lugar.
2. Marca a despesa como definitivamente excluída (ver campo novo abaixo),
   para que nunca mais gere uma ocorrência nova (RF05) e nunca mais seja
   encontrável para edição ou para uma segunda exclusão (RF14/EC07).

### Novo campo de estado: exclusão definitiva
RF05/RF08 do refinamento funcional exigem um estado terminal, sem volta,
independente do `RecurringExpenseStatus` (Ativa/Pausada) já existente.
**Decisão proposta**: um novo campo próprio, não um terceiro valor do
enum `RecurringExpenseStatusType`. Motivo: aquele enum é hoje um conjunto
fechado de exatamente dois valores editáveis pela tela de edição
(`editar-despesa-recorrente.md`, RF01: "é possível alterar (...) status
(Ativa/Pausada)"); acrescentar um terceiro valor ("Excluída") a esse mesmo
enum obrigaria a tela de edição e o `UpdateRecurringExpenseUseCase` a
tratar um valor que eles nunca devem aceitar como entrada válida — mais
simples (Princípio V) manter esse enum como está e representar "excluída"
como um dado ortogonal.

Nome de trabalho: `_deletedAt` (`CalendarDate?`, nulo enquanto a despesa não
foi excluída parcialmente). Regras:
- Só é definido por `DeleteKeepingPaidHistory()`; nenhum outro método do
  aggregate o lê ou altera.
- Novo Value Object nenhum é necessário só para este campo — `CalendarDate`
  já existe e já é nullable em outros pontos do aggregate (ex.: `Occurrence._paymentDate`).
- Um `GetDeletedAt()` (ou `IsDeleted()` booleano) precisa ser exposto pelo
  aggregate, para a Application decidir "não encontrado" onde for preciso
  (ver "Application").

### Exclusão total ("todo o histórico") não precisa de método no aggregate
RF06 do refinamento funcional remove a despesa recorrente por completo,
sem excecionar nada — não há nenhuma regra de negócio para o Domain
validar (ao contrário de `ChangeEndDate`, que valida se a redução é segura
antes de remover ocorrências). Por isso, a exclusão total é modelada como
uma operação puramente de repositório (ver "Infrastructure"), sem precisar
de um método novo em `RecurringExpense` — o aggregate não precisa "saber"
como se autodestruir; ele só precisa continuar existindo em memória até o
repositório apagar sua linha.

### Nenhuma validação de forma nesta camada
Igual às features anteriores: o Domain só teria algo a validar se a
abrangência escolhida chegasse até ele — e ela não chega (ver
"Application": o UseCase decide qual dos dois caminhos seguir, sem passar
a abrangência para o aggregate).

## Camada Infrastructure

### Novo método no repositório: exclusão total
`IRecurringExpenseRepository` (Domain) ganha um novo método, nome de
trabalho `RemoveAsync(RecurringExpense recurringExpense)`. Implementação em
`RecurringExpenseRepository`: `_context.RecurringExpenses.Remove(recurringExpense);
await _context.SaveChangesAsync();`. O relacionamento já configurado em
`RecurringExpenseConfigurations` (`HasMany<Occurrence>("_occurrences").WithOne().HasForeignKey("RecurringExpenseId").IsRequired()`)
já é uma relação obrigatória 1:N — o comportamento padrão do EF Core para
remover o principal de uma relação obrigatória já rastreada é apagar em
cascata os dependentes na mesma operação, sem necessidade de configurar
`OnDelete(DeleteBehavior.Cascade)` explicitamente nem de uma migração nova
só para isso (o comportamento de cascade para relações obrigatórias já é o
padrão desde que a entidade principal, com seus dependentes, tenha sido
carregada via `Include` — que é exatamente o que `GetByIdAsync` já faz
hoje). **Ponto a confirmar em teste de integração** (`Infrastructure.Tests`),
não em suposição: que remover o aggregate via `GetByIdAsync` + `Remove` de
fato apaga as linhas de `Occurrences` no PostgreSQL de teste, e não deixa
linhas órfãs por causa de alguma particularidade do provider.

### Exclusão parcial reaproveita `UpdateAsync` sem alteração
Igual ao que já acontece com `ChangeEndDate` hoje: depois de
`DeleteKeepingPaidHistory()` remover as ocorrências não pagas de
`_occurrences` (em memória), chamar o já existente `UpdateAsync` é
suficiente — o mesmo mecanismo de "detectar occurrences que saíram da
coleção e deixá-las órfãs para o `SaveChangesAsync()` apagar" que já
sustenta a redução de vigência sustenta também esta exclusão parcial, sem
nenhuma linha de código nova em `UpdateAsync`.

### Nova migração EF Core: coluna `DeletedAt`
O novo campo `_deletedAt` do aggregate (ver "Domain") precisa de uma nova
coluna em `RecurringExpenses` (`RecurringExpenseConfigurations`, nova
`builder.Property<CalendarDate?>("_deletedAt").HasColumnName("DeletedAt").HasColumnType("date")`,
sem `.IsRequired()`, seguindo o mesmo padrão de `Occurrence._paymentDate`)
e de uma migração EF Core nova — Princípio VII: migrações de features já
entregues (a mais recente sendo a de `EndDate`) não podem ser alteradas;
esta é sempre uma migração adicional.

### `GetByIdAsync` passa a excluir despesas parcialmente excluídas
RF14/EC07 do refinamento funcional exigem que uma despesa parcialmente
excluída (`_deletedAt` preenchido) se torne "não encontrada" para leitura
por id e para edição. **Decisão proposta**: aplicar esse filtro dentro do
próprio `GetByIdAsync` (`WHERE` adicional por `_deletedAt IS NULL`), em vez
de filtrar em cada UseCase que o chama. Consequência intencional: o mesmo
`GetByIdAsync` que o novo `DeleteRecurringExpenseUseCase` usa para carregar
a despesa antes de excluir também passa a devolver `null` para uma despesa
já excluída — o que produz automaticamente o "não encontrado" de RF14 (uma
segunda exclusão) sem nenhuma lógica extra no UseCase, e sem duplicar o
filtro em três lugares diferentes.

**Este filtro NÃO deve ser replicado em `GetByReferencePeriodAsync` nem em
`GetByOccurrenceIdAsync`.** Essas duas leituras alimentam o Painel Mensal
(`GetMonthlyPanelUseCase`) e o fluxo de pagamento
(`MarkOccurrenceAsPaid`/`UndoOccurrencePayment`), respectivamente, e
precisam continuar encontrando uma despesa parcialmente excluída **quando
ela ainda tem ocorrências pagas** — é exatamente essa a garantia que RF04
do refinamento funcional promete ("o histórico pago continua acessível").
Filtrar por `_deletedAt` nessas duas leituras apagaria do Painel Mensal o
próprio histórico que a abrangência "apenas pendentes/vencidas" existe
para preservar.

## Camada Application

Um novo UseCase, `Application/UseCases/DeleteRecurringExpense/`, seguindo a
convenção já usada por `UpdateRecurringExpense` (Princípio XI: interface
`IDeleteRecurringExpenseUseCase`, implementação
`DeleteRecurringExpenseUseCase`, `Input`/`Output` próprios).

### `DeleteRecurringExpense`
- **Input**: identificador da despesa recorrente + abrangência escolhida
  (RF10 do refinamento funcional), em sua forma "crua" (string), no mesmo
  padrão que `UpdateRecurringExpenseUseCaseInput` já usa para campos como
  `Status`.
- Orquestração:
  1. Valida a abrangência recebida contra os dois valores válidos (RF12 do
     refinamento funcional); se inválida ou ausente, devolve falha com um
     `FieldError` no campo da abrangência, sem tocar no repositório — mesmo
     padrão de "constrói/valida antes de mexer em qualquer estado" já usado
     por `UpdateRecurringExpenseUseCase` para os enums de categoria/status.
  2. Busca a despesa via `GetByIdAsync` (já ajustado, ver "Infrastructure",
     para devolver `null` também para uma despesa já parcialmente
     excluída); se não encontrada, sinaliza "não encontrado" — mesmo
     `KeyNotFoundException` capturado pelo middleware global que
     `GetRecurringExpenseByIdUseCase`/`UpdateRecurringExpenseUseCase` já
     lançam hoje, cobrindo RF11 e RF14 do refinamento funcional com o
     mesmo mecanismo, sem código novo de tratamento de erro.
  3. Se a abrangência for "todo o histórico" (RF06), chama o novo
     `RemoveAsync` do repositório (a despesa deixa de existir).
  4. Se a abrangência for "apenas pendentes/vencidas" (RF04/RF05), chama
     `DeleteKeepingPaidHistory()` no aggregate e então `UpdateAsync` do
     repositório (já existente, sem alteração).
- **Output — sucesso**: identificador da despesa e a abrangência
  efetivamente aplicada (RF13 do refinamento funcional). **Decisão
  proposta**: não devolver mais do que isso — nem a lista de ocorrências
  restantes, nem os campos editáveis da despesa — porque, nos dois
  caminhos, o dado relevante para a tela não é "o que ficou da despesa" e
  sim "a linha da competência atual deve sair da lista", que o frontend
  resolve recarregando o Painel Mensal (ver refinamento de frontend), não
  reconstruindo estado a partir da resposta deste endpoint. Devolver um
  payload mínimo evita um `DataResponse` com campos que só fariam sentido
  em um dos dois ramos (ex.: campos da despesa, que não existem mais após
  "todo o histórico").

## Camada API

O novo endpoint entra no `RecurringExpensesController` já existente
(`backend/Api/Controllers/RecurringExpensesController.cs`), como uma quarta
ação.

### `DELETE /api/v1/recurring-expenses/{id}?scope={all|pending-only}`
- **Abrangência via query string, não via corpo da requisição.** Decisão
  por consistência com o único outro `DELETE` já implementado no projeto
  (`OccurrencesController.UndoPayment`, `DELETE
  /api/v1/occurrences/{occurrenceId}/payment`), que não usa corpo algum —
  parâmetros de um `DELETE` neste projeto vão na rota/query, nunca no body.
  Os dois valores aceitos (nomes de trabalho): `all` (RF06) e
  `pending-only` (RF04/RF05).
- Validação de presença/valor da query string (RF12 do refinamento
  funcional) acontece na API antes de chegar ao UseCase, com mensagem
  PT-BR — mesmo padrão de "validação de forma na API, validação de regra
  no Domain/Application" (Princípio XII) já usado pelos `DataRequest`
  existentes; como não há corpo, não há um `DataRequest` dedicado para
  esta ação — mesmo caso já existente do `GetById` (`GET`), que também não
  tem `DataRequest` por não receber corpo.
- Resposta de sucesso: `200 OK` (não `204 No Content` — ver "Referências",
  precedente de `UndoPayment`) com um novo `DeleteRecurringExpenseDataResponse`
  (`/Responses`), contendo o identificador e a abrangência aplicada (RF13).
- Resposta `400`: abrangência ausente/inválida (RF12), no mesmo formato de
  erro por campo já usado pelos demais endpoints.
- Resposta `404`: id inexistente ou já excluído (RF11/RF14), mapeada pelo
  mesmo middleware global de exceções.
- `ProducesResponseType` deve declarar `200`, `400`, `404` e `500`.
- Mapeamento dedicado (`/Mappings`, Princípio XII): um mapeamento
  `DeleteRecurringExpenseUseCaseOutput` → `DeleteRecurringExpenseDataResponse`.

### Nenhuma lógica de negócio no controller (Princípio XII)
O controller só recebe `id` e `scope`, delega ao UseCase e traduz o
resultado (`IsSuccess`/`404`, via exceção) para a resposta HTTP — a decisão
de qual dos dois caminhos seguir, e o que cada um faz, fica inteiramente no
UseCase, no aggregate e no repositório.

## Fora de escopo deste refinamento
- Qualquer mudança em `OccurrencesController` ou nos UseCases de pagamento.
- Autenticação/autorização/CORS (exceção temporária da constituição,
  Princípio IV).
- Testes (`Domain.Tests`/`Application.Tests`/`Api.Tests`/`Infrastructure.Tests`)
  — apenas a exigência herdada do Princípio II de um teste por cenário de
  resposta declarado, incluindo o teste de integração citado em
  "Infrastructure" para confirmar o cascade delete.
- Mecanismo de "desfazer exclusão" ou auditoria.

## Pontos em aberto
- **Nome de trabalho dos métodos/campos** (`DeleteKeepingPaidHistory`,
  `_deletedAt`, valores `all`/`pending-only` da query string) — todos são
  nomes de trabalho deste documento, não uma decisão final de nomenclatura.
- **Novo campo vs. reaproveitar o enum de status** (ver "Novo campo de
  estado"): este documento recomenda um campo novo e ortogonal ao status
  Ativa/Pausada, mas a alternativa (um terceiro valor no enum) não é
  impossível — só foi descartada por criar mais superfície a validar na
  tela de edição já existente.
- **Cascade delete automático do EF Core para a exclusão total**: este
  documento assume que o comportamento padrão já configurado
  (`IsRequired()` numa relação 1:N com o principal carregado via
  `Include`) é suficiente, sem nenhuma migração nova só para isso — mas
  isso precisa ser confirmado por um teste de integração real
  (`Infrastructure.Tests`, com o mesmo `PostgreSqlContainerFixture` já
  usado por outros testes daquele projeto), não assumido apenas pela
  leitura do código.
- **Herda os pontos em aberto do refinamento funcional**, especialmente:
  qual abrangência vem pré-selecionada por padrão na tela (isso não afeta
  este documento, já que a API sempre exige uma escolha explícita, RF10),
  e a interação com uma edição de pagamento em andamento na mesma
  ocorrência (EC06 daquele documento).
