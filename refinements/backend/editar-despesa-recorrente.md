# Refinamento — Backend: Editar Despesa Recorrente

## Origem
Refinamento funcional: [`editar-despesa-recorrente.md`](../editar-despesa-recorrente.md).
Não há tela de design para esta feature (ver "Origem" daquele documento) —
este refinamento técnico depende inteiramente das regras funcionais já
levantadas ali, não de um protótipo visual.

## Feature
Refinamento técnico (sem código, sem assinaturas de método completas — só
conceitos e contratos) das quatro camadas do backend necessárias para
implementar a edição de uma despesa recorrente já cadastrada: novos
métodos de negócio no aggregate `RecurringExpense` (Domain), dois novos
UseCases (Application), confirmação de que a Infrastructure já é
suficiente, e dois novos endpoints — leitura por id e atualização — na
API.

Este documento parte do **código já implementado**, não dos refinamentos
antigos de domínio/API (`domain-despesa-recorrente.md`,
`api-despesa-recorrente.md`), que descrevem apenas o estado da primeira
entrega (cadastro) e já estão desatualizados frente ao que existe hoje —
por exemplo, `RecurringExpense` já expõe `MarkOccurrenceAsPaid`,
`UndoOccurrencePayment`, `GetOccurrencesForPeriod` e `FindOccurrence`,
nenhum dos quais existia quando aqueles documentos foram escritos.

## Escopo
Cobre:
- Novos métodos de negócio no aggregate `RecurringExpense` (`Domain`),
  necessários para alterar cada campo editável sem violar o Princípio VI
  da constituição (nunca expor propriedades diretamente configuráveis).
- Dois novos UseCases na `Application`: buscar uma despesa recorrente por
  id e atualizar seus dados.
- Confirmação de que a `Infrastructure` (repositório, `DbContext`,
  configurações EF Core) não precisa de nenhuma mudança estrutural.
- Dois novos endpoints na `Api`: `GET` por id e atualização (`PUT`),
  seguindo o mesmo envelope de resposta (`ApiResponse<TData>`/`ApiError`),
  mapeamentos dedicados e middleware de exceções já estabelecidos por
  `RecurringExpensesController`/`OccurrencesController`.

Não cobre:
- Exclusão de despesa recorrente (feature separada, ver "Pontos em
  aberto" do refinamento funcional).
- Qualquer alteração em ocorrências individuais — já implementado
  (`MarkOccurrenceAsPaid`/`UndoOccurrencePayment` em `OccurrencesController`).
- Autenticação/autorização e CORS — mesma exceção temporária já registrada
  na constituição (Princípio IV) e usada pelos endpoints existentes.
- Testes (nomes de classes/métodos de teste) — apenas a exigência,
  herdada do Princípio II, de que cada cenário de resposta declarado
  tenha um teste correspondente.

## Referências
- Refinamento funcional (RF/EC/CA citados abaixo são os dele):
  [`editar-despesa-recorrente.md`](../editar-despesa-recorrente.md).
- Código atual do aggregate: `backend/Domain/Aggregates/RecurringExpense.cs`.
- Código atual dos controllers: `backend/Api/Controllers/RecurringExpensesController.cs`
  e `backend/Api/Controllers/OccurrencesController.cs` (precedente de como
  um segundo endpoint foi adicionado a um controller já existente, e de
  como `404`/`400` já convivem no mesmo controller).
- `backend/Application/Ports/ICurrentDateProvider.cs` (já implementado,
  usado pelo cadastro para obter a competência atual — reaproveitado por
  este refinamento, ver RF06 do refinamento funcional).
- Constituição do projeto: Princípios VI (Domain), VII (Infrastructure),
  XI (Application) e XII (API).

## Camada Domain

### Novos métodos de negócio no aggregate `RecurringExpense`
Pelo Princípio VI, nenhuma propriedade pode virar um setter direto; cada
alteração precisa de um método com nome de intenção de negócio, no mesmo
espírito dos métodos já existentes `MarkOccurrenceAsPaid`/`UndoOccurrencePayment`
(que já provam o padrão de "aggregate ganha um método novo para um
comportamento novo" usado nesta base de código):

| Método (conceitual) | Parâmetro | Regra reaproveitada (RF do domínio original) |
|---|---|---|
| Renomear | novo `ExpenseName` | RF02 |
| Trocar categoria | novo `ExpenseCategory` | RF03 |
| Trocar valor previsto mensal | novo `Money` | RF04 |
| Trocar dia de vencimento | novo `DueDay` | RF05 |
| Trocar data de início | novo `CalendarDate` | RF06 |
| Trocar observação | novo `Note` | RF09 |
| Pausar | nenhum | RF15 (domínio) / RF05 (funcional deste refinamento) |
| Reativar | `ReferencePeriod` da competência atual | RF10/RF11 (domínio) / RF06–RF07 (funcional deste refinamento) |

Cada método substitui a referência interna do campo por uma nova
instância do Value Object correspondente (ex.: `_name = newName;`) — isso
não viola a imutabilidade dos Value Objects (Princípio VI: "nenhum estado
interno pode mudar após a construção"), porque o VO em si nunca é
mutado; o aggregate apenas passa a apontar para um VO novo, já validado
pelo próprio construtor do VO (a mesma validação que já roda no cadastro,
sem duplicação de regra — RF02 do refinamento funcional).

### `Renomear`/`Trocar categoria`/`Trocar valor`/`Trocar dia`/`Trocar data de início`/`Trocar observação`
Cada método recebe já um Value Object construído (a validação de forma
individual do campo já aconteceu na própria construção desse VO, igual ao
que já acontece hoje em `CreateRecurringExpenseUseCase`) e apenas
substitui o campo interno correspondente. Nenhuma dessas alterações toca
em `_occurrences` — consistente com RF03/RF04 do refinamento funcional
(ocorrências já geradas nunca são reescritas).

### `Pausar`
Sem parâmetros. Apenas substitui `_status` pelo valor `Paused`. Não
remove nem altera nenhuma ocorrência já existente em `_occurrences`
(RF05 do refinamento funcional). Simétrico ao que a própria ausência de
geração automática já garante para despesas pausadas desde o cadastro
(regra de domínio original RF15).

### `Reativar`
Recebe a competência atual (`ReferencePeriod`, a mesma obtida via
`ICurrentDateProvider.GetCurrentDate()` já usado no cadastro). Substitui
`_status` pelo valor `Active` e, **apenas se** `GetOccurrencesForPeriod`
(já existente no aggregate) para a competência atual estiver vazio **e**
a competência atual for igual ou posterior à competência da data de
início (`_startDate`), gera e adiciona a `_occurrences` uma nova
ocorrência Pendente com o valor previsto mensal vigente — reaproveitando
exatamente a mesma lógica de geração que já existe no construtor do
aggregate (linhas 40–49 de `RecurringExpense.cs` hoje) e a mesma condição
de não duplicidade que `GetOccurrencesForPeriod` já existe para servir
(RF06/RF07 do refinamento funcional). Recomenda-se extrair essa lógica de
geração para um método privado compartilhado entre o construtor e
`Reativar`, para não duplicar a regra dentro do próprio aggregate.

### Validação de campo obrigatório e presença continua fora do Domain
Igual ao cadastro: o Domain só valida a regra de negócio de cada VO já
construído; validação de forma/presença (campo ausente no request) segue
sendo responsabilidade da API (Princípio XII), nunca duplicada aqui.

## Camada Application

Dois novos UseCases, cada um em sua própria pasta
(`Application/UseCases/<Nome>/`), seguindo exatamente a convenção já usada
por `CreateRecurringExpense` e `GetMonthlyPanel` (Princípio XI: interface
`I<Nome>UseCase`, implementação `<Nome>UseCase`, `Input`/`Output` próprios
e não compartilhados).

### `GetRecurringExpenseById`
- **Input**: identificador da despesa recorrente.
- Orquestração: busca a despesa via `IRepositoryManager.RecurringExpenseRepository.GetByIdAsync`
  (já existe, nenhuma mudança de repositório necessária).
- **Output — não encontrado**: quando `GetByIdAsync` devolve `null`, o
  UseCase deve sinalizar "não encontrado" de forma que o controller
  consiga mapear para `404` (RF12 do refinamento funcional) — mesmo
  padrão de "não encontrado" que `MarkOccurrenceAsPaidUseCase`/
  `UndoOccurrencePaymentUseCase` já resolvem hoje ao lançar
  `KeyNotFoundException` a partir de `FindOccurrence` no aggregate (ver
  `RecurringExpense.MarkOccurrenceAsPaid`/`UndoOccurrencePayment`,
  linhas 104–116) — o mesmo mecanismo (`KeyNotFoundException` capturada
  pelo middleware global) se aplica aqui para "despesa recorrente não
  encontrada".
- **Output — sucesso**: todos os campos editáveis (RF01 do refinamento
  funcional) — nome, categoria, valor previsto mensal, dia de vencimento,
  data de início, status, observação. Frequência também é devolvida
  (sempre `Monthly`), por completude, ainda que não seja editável. Não
  inclui a lista de ocorrências (RF13 do refinamento funcional: a tela de
  edição não precisa delas).

### `UpdateRecurringExpense`
- **Input**: identificador da despesa recorrente + todos os campos
  editáveis (RF01 do refinamento funcional) em sua forma "crua" (strings
  para nome/categoria/data, número para valor, etc.), no mesmo padrão que
  `CreateRecurringExpenseUseCaseInput` já usa hoje.
- Orquestração:
  1. Busca a despesa via `GetByIdAsync`; se não encontrada, sinaliza "não
     encontrado" (RF15 do refinamento funcional) do mesmo jeito que
     `GetRecurringExpenseById` acima.
  2. Constrói cada Value Object a partir do input, coletando erros de
     campo exatamente como `CreateRecurringExpenseUseCase` já faz hoje
     (mesmo padrão de "constrói cada VO num try/catch, acumula
     `FieldError`, só prossegue se a lista de erros estiver vazia") — RF14
     do refinamento funcional: nenhuma validação de negócio é
     reimplementada aqui, apenas delegada à construção de cada VO.
  3. Se algum VO falhar a construção, devolve falha com a lista de erros
     de campo, sem chamar nenhum método do aggregate (RF16 do refinamento
     funcional: nada é salvo parcialmente).
  4. Se todos os VOs forem válidos, invoca no aggregate, em sequência, os
     métodos de negócio correspondentes a cada campo que efetivamente
     mudou (comparado ao valor atual) — inclusive `Pausar`/`Reativar`
     quando o status mudou, usando `ICurrentDateProvider.GetCurrentDate()`
     para obter a competência atual a passar para `Reativar` (RF06 do
     refinamento funcional).
  5. Chama `SaveChangesAsync()` (via `IRepositoryManager`, Unit of Work
     do Princípio VII) para persistir.
- **Output — sucesso**: os mesmos campos devolvidos por
  `GetRecurringExpenseById`, já atualizados (RF17 do refinamento
  funcional).

### Chamar apenas os métodos dos campos que mudaram, ou sempre todos?
**Assunção**: o UseCase deve invocar cada método de negócio (Renomear,
Trocar categoria, etc.) **apenas quando o valor recebido for diferente do
valor atual**, para que RF10 do refinamento funcional (edição sem
alterações é um no-op real, nenhuma ocorrência é tocada) valha de fato —
isso é particularmente importante para `Pausar`/`Reativar`: chamar
`Reativar` num status que já está Ativo não deve gerar uma ocorrência
duplicada, então o UseCase só deve chamar `Pausar`/`Reativar` quando o
status efetivamente mudou de valor.

## Camada Infrastructure

**Nenhuma mudança estrutural é necessária.** `IRecurringExpenseRepository`
já expõe `GetByIdAsync`, suficiente para os dois novos UseCases; não é
necessário nenhum método de "atualizar" no repositório, porque o
aggregate já vem rastreado pelo `DbContext` desde o `GetByIdAsync`, e
`SaveChangesAsync()` (Unit of Work, Princípio VII) já persiste qualquer
mudança de estado feita nos métodos de negócio do aggregate — mesmo
padrão que `MarkOccurrenceAsPaidUseCase`/`UndoOccurrencePaymentUseCase`
já usam hoje, sem repositório dedicado a "atualizar".

Único ponto a confirmar no refinamento técnico da Infrastructure (fora
deste documento): se o mapeamento EF Core atual (`Configs`) para os
campos privados do aggregate já suporta a EF Core reidratar e re-salvar
um `RecurringExpense` alterado, ou se algum ajuste de configuração é
necessário — não deveria ser necessário, já que `MarkOccurrenceAsPaid`/
`UndoOccurrencePayment` (que também mutam estado de uma entidade já
carregada) já funcionam em produção, mas fica como confirmação, não como
suposição.

## Camada API

Ambos os novos endpoints entram no `RecurringExpensesController` já
existente (`backend/Api/Controllers/RecurringExpensesController.cs`),
como um segundo e terceiro método — mesmo padrão que
`OccurrencesController` já usa hoje (um controller, múltiplas ações REST
sobre o mesmo recurso).

### `GET /api/v1/recurring-expenses/{id}`
- **RF11/RF12 do refinamento funcional**: devolve os campos editáveis da
  despesa; `404` (via o mesmo middleware global de exceções, Princípio
  XII) quando o id não existe.
- Resposta de sucesso: novo `DataResponse` dedicado (`/Responses`), com os
  mesmos campos de `CreateRecurringExpenseDataResponse` exceto
  `occurrences` (RF13 do refinamento funcional).
- Envelope `ApiResponse<TData>`/`ApiError` idêntico ao já usado por todos
  os outros endpoints (Princípio XII) — nenhum endpoint tem envelope
  próprio.
- `ProducesResponseType` deve declarar `200`, `404` e `500` (não há corpo
  de request nesta ação, logo não há `400` de forma/presença; poderia
  haver um `400` se o próprio `id` da rota vier malformado, o que o
  binding de `Guid` da rota já rejeita antes de chegar ao UseCase).

### `PUT /api/v1/recurring-expenses/{id}`
- **RF14–RF18 do refinamento funcional**: `DataRequest` dedicado
  (`/Requests`) com os mesmos campos de `CreateRecurringExpenseDataRequest`
  (nome, categoria, valor, dia, data de início, status, observação — sem
  `frequency`, que não é editável, RF01 do refinamento funcional).
- Validação de forma/presença (campos obrigatórios ausentes) via data
  annotations no `DataRequest`, cada uma com `ErrorMessage` em PT-BR
  (Princípio XII), igual ao que já existe em
  `CreateRecurringExpenseDataRequest`.
- Resposta de sucesso: `200 OK` (não `201`, pois não cria recurso) com o
  mesmo `DataResponse` do `GET` acima, já atualizado.
- Resposta `400`: mesmo envelope de erro por campo já usado pelo `POST`
  de cadastro (`api-despesa-recorrente.md`) — um item por regra violada.
- Resposta `404`: id inexistente (RF15 do refinamento funcional), mapeada
  pelo mesmo middleware global de exceções que já trata isso para
  `MarkAsPaid`/`UndoPayment` em `OccurrencesController`.
- `ProducesResponseType` deve declarar `200`, `400`, `404` e `500`.
- Mapeamentos dedicados (`/Mappings`, um arquivo por
  DataRequest/DataResponse mapeado, Princípio XII): um mapeamento
  `DataRequest` → `UpdateRecurringExpenseUseCaseInput` e um mapeamento
  `UpdateRecurringExpenseUseCaseOutput`/`GetRecurringExpenseByIdUseCaseOutput`
  → `DataResponse` (pode ser o mesmo `DataResponse` para os dois
  endpoints, já que devolvem exatamente os mesmos campos).

### Nenhuma lógica de negócio no controller (Princípio XII)
Igual aos controllers já existentes: o controller só recebe o request,
delega ao UseCase e traduz o resultado (`IsSuccess`/`404`) para a
resposta HTTP — toda decisão sobre o que muda em cada campo, e sobre
gerar ou não a ocorrência da reativação, fica inteiramente no UseCase e
no aggregate.

## Fora de escopo deste refinamento
- Exclusão de despesa recorrente.
- Qualquer mudança em `OccurrencesController` ou nos UseCases de
  pagamento (`MarkOccurrenceAsPaid`/`UndoOccurrencePayment`) — não são
  afetados por esta feature (RF09 do refinamento funcional).
- Autenticação/autorização/CORS (exceção temporária da constituição,
  Princípio IV, já em vigor para todos os endpoints existentes).
- Testes (`Domain.Tests`/`Application.Tests`/`Api.Tests`) — apenas a
  exigência herdada do Princípio II de um teste por cenário de resposta
  declarado.

## Pontos em aberto
- **Granularidade dos métodos de negócio do aggregate.** Este documento
  propõe um método por campo (RF da tabela em "Domain"), pelo precedente
  de `MarkOccurrenceAsPaid`/`UndoOccurrencePayment` serem métodos
  separados e pequenos. Uma alternativa (um único método `Update(...)`
  recebendo todos os campos de uma vez) violaria menos a superfície
  pública do aggregate, mas se afasta do estilo de nomes de intenção de
  negócio já estabelecido — decisão a confirmar antes da implementação.
- **Extração da lógica de geração de ocorrência compartilhada entre o
  construtor e `Reativar`.** Recomendado nesta seção, mas a forma exata
  fica para a implementação.
- **Chamar métodos de negócio apenas para campos que mudaram** (ver
  "Chamar apenas os métodos dos campos que mudaram, ou sempre todos?") —
  assunção deste documento, a confirmar.
- **Herda todos os pontos em aberto do refinamento funcional**,
  especialmente: se a reativação deve mesmo gerar a ocorrência da
  competência atual (RF06 daquele documento) e se deveria haver alguma
  restrição ao editar a data de início (RF08 daquele documento) — ambos
  afetam diretamente o comportamento de `Reativar`/`Trocar data de
  início` descrito aqui.
