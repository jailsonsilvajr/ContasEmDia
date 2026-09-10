# Phase 0 Research: Editar Despesa Recorrente (Backend)

**Feature**: `007-edit-recurring-expense` | **Date**: 2026-09-10

A maior parte das decisões técnicas desta feature já vem fechada pelo
refinamento
([`refinements/backend/editar-despesa-recorrente.md`](../../refinements/backend/editar-despesa-recorrente.md)).
Este documento resolve os pontos que aquele refinamento deixou explicitamente
em aberto ("Pontos em aberto") e confirma, contra o código atual, as
suposições de Infrastructure e de mecanismo de erro que o refinamento pediu
para verificar antes da implementação. Não há nenhum item
"NEEDS CLARIFICATION" de contexto técnico pendente.

## 1. Granularidade e nomes dos métodos de negócio do aggregate

**Decision**: Um método por campo editável, com nomes de intenção de
negócio em inglês (consistente com o idioma já usado por todo identificador
do Domain: `MarkOccurrenceAsPaid`, `UndoOccurrencePayment`,
`GetOccurrencesForPeriod`):

| Campo | Método |
|---|---|
| Nome | `Rename(ExpenseName newName)` |
| Categoria | `ChangeCategory(ExpenseCategory newCategory)` |
| Valor previsto mensal | `ChangeMonthlyAmount(Money newMonthlyAmount)` |
| Dia de vencimento | `ChangeDueDay(DueDay newDueDay)` |
| Data de início | `ChangeStartDate(CalendarDate newStartDate)` |
| Observação | `ChangeNote(Note newNote)` |
| Pausar | `Pause()` |
| Reativar | `Reactivate(ReferencePeriod currentReferencePeriod)` |

**Rationale**: O refinamento levantou como alternativa um único método
`Update(...)` recebendo todos os campos de uma vez, mas rejeitou essa opção
por se afastar do estilo de nomes de intenção de negócio já estabelecido por
`MarkOccurrenceAsPaid`/`UndoOccurrencePayment` — o mesmo precedente já usado
para pagamento de ocorrência. Um método por campo também é o que permite ao
`UpdateRecurringExpenseUseCase` invocar apenas os métodos dos campos que
realmente mudaram (ver §3), preservando RF10 (edição sem alterações é um
no-op real).

**Alternatives considered**: Método único `Update(ExpenseName, ExpenseCategory, Money, DueDay, CalendarDate, RecurringExpenseStatus, Note, ReferencePeriod)`
— descartado por (a) violar o estilo de nomes de intenção de negócio já
estabelecido no aggregate, e (b) obrigar o próprio aggregate a decidir
internamente "o que mudou", que é uma decisão de orquestração melhor
resolvida no UseCase (Princípio XI).

## 2. Extração da lógica de geração de ocorrência compartilhada

**Decision**: Extrair a lógica hoje inline no construtor (linhas 40–49 de
`RecurringExpense.cs`) para um método privado,
`GenerateOccurrenceForCurrentPeriodIfDue(ReferencePeriod currentReferencePeriod)`,
que verifica a condição de início (`currentReferencePeriod >= startPeriod`)
e de não duplicidade (`GetOccurrencesForPeriod(currentReferencePeriod).Count == 0`,
já existente) antes de gerar e adicionar a ocorrência Pendente. O construtor
passa a chamá-lo quando `status == Active`; `Reactivate` o chama
incondicionalmente logo após trocar `_status` para `Active`.

**Rationale**: Recomendado explicitamente pelo refinamento, para que a
regra de geração de ocorrência (condição de data + não duplicidade) exista
em um único lugar, nunca duplicada entre o construtor e `Reactivate`
(RF06/RF07 do refinamento funcional).

**Alternatives considered**: Duplicar a lógica dentro de `Reactivate` —
descartado por violar DRY dentro do próprio aggregate e arriscar as duas
cópias divergirem em uma mudança futura.

## 3. Invocar métodos de negócio apenas para campos que mudaram

**Decision**: `UpdateRecurringExpenseUseCase` compara o valor de cada Value
Object recém-construído (via `GetValue()`) com o valor atual correspondente
do aggregate, e só invoca o método de negócio daquele campo quando os
valores diferem. Para o status: `Pause()` é chamado apenas quando o status
atual é `Active` e o novo é `Paused`; `Reactivate(currentReferencePeriod)`
apenas quando o atual é `Paused` e o novo é `Active`.

**Rationale**: Confirmado na sessão de `/speckit-clarify` da spec (ver
`spec.md`, "Clarifications") — chamar `Reactivate` sobre um status que já é
`Active` geraria uma ocorrência duplicada indevidamente; comparar antes de
invocar é a única forma de RF10 (no-op real) valer para todos os campos,
não só para o status.

**Alternatives considered**: Sempre invocar todos os oito métodos
incondicionalmente — descartado porque isso quebraria RF10 diretamente para
`Pause`/`Reactivate` (seriam invocados mesmo sem mudança de status,
arriscando gerar ocorrência duplicada) e não teria efeito visível, mas
incorreto, para os demais métodos (uma reatribuição do mesmo valor não é um
"no-op" verdadeiro se o método tivesse, no futuro, algum efeito colateral
além de reatribuir o campo).

## 4. Mecanismo de "não encontrado" para os dois novos UseCases

**Decision**: Ambos os UseCases (`GetRecurringExpenseById` e
`UpdateRecurringExpense`) lançam `KeyNotFoundException("Despesa recorrente não encontrada.")`
quando `IRepositoryManager.RecurringExpenseRepository.GetByIdAsync` retorna
`null`, exatamente como `MarkOccurrenceAsPaidUseCase`/`UndoOccurrencePaymentUseCase`
já fazem hoje para ocorrência não encontrada. Nenhuma mudança no
`ExceptionHandlingMiddleware` (`backend/Api/Middlewares/ExceptionHandlingMiddleware.cs`)
é necessária — ele já mapeia `KeyNotFoundException` para `404`.

**Rationale**: Reaproveita um mecanismo já implementado e já testado,
evitando introduzir um segundo padrão de "não encontrado" (ex.: um
`IsSuccess`/`NotFound` no Output) só para estes dois UseCases.

**Alternatives considered**: Um campo `IsNotFound`/`IsSuccess` no Output de
cada UseCase, traduzido pelo controller — descartado por duplicar um
mecanismo (mapeamento exceção → status HTTP) que o middleware global já
centraliza (Princípio XII: "esse mapeamento MUST live only in the global
middleware").

## 5. Confirmação: Infrastructure não precisa de nenhuma mudança

**Decision confirmada**: Nenhuma mudança em `backend/Infrastructure`.

**Evidência levantada nesta sessão**:
- `IRecurringExpenseRepository.GetByIdAsync`/`UpdateAsync` já existem
  (`backend/Domain/Repositories/IRecurringExpenseRepository.cs`); a
  implementação de `UpdateAsync`
  (`backend/Infrastructure/Repositories/RecurringExpenseRepository.cs`) já é
  um wrapper direto de `SaveChangesAsync()`, suficiente para persistir
  qualquer mudança de estado feita pelos novos métodos do aggregate.
- `RecurringExpenseConfigurations`
  (`backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs`) já
  mapeia cada campo privado do aggregate (`_name`, `_category`,
  `_monthlyAmount`, `_dueDay`, `_startDate`, `_status`, `_note`) via
  `HasConversion` diretamente sobre o backing field — não há nenhuma
  configuração de "somente leitura" ou "somente na criação"; uma
  reatribuição de campo feita por um método de negócio é detectada
  normalmente pelo change tracker do EF Core no próximo `SaveChangesAsync()`,
  o mesmo mecanismo que já persiste as mutações de `Occurrence` feitas por
  `MarkOccurrenceAsPaid`/`UndoOccurrencePayment` em produção.

**Rationale**: O refinamento pediu para confirmar isso "não como suposição";
esta seção registra a confirmação com base na leitura direta do código de
Infrastructure já implementado.

**Atualização pós-implementação (T041, validação manual contra PostgreSQL
real)**: a confirmação acima — nenhuma mudança de *assinatura* na
Infrastructure — permanece correta, mas a validação manual do `quickstart.md`
contra uma instância real do PostgreSQL (não os duplos de teste usados em
`Application.Tests`/`Api.Tests`) encontrou dois defeitos de comportamento
pré-existentes nunca antes exercitados por nenhum endpoint ou teste deste
repositório, ambos exigindo uma correção pequena e cirúrgica:

1. **Conversão de `_note` ignorada para coluna `NULL`.** O conversor EF Core
   configurado em `RecurringExpenseConfigurations` (`vo => vo.GetValue(),
   value => new Note(value)`) nunca é invocado pelo provider quando a coluna
   `Note` é `NULL` — comportamento padrão do EF Core, que atribui o valor CLR
   default (`null`) diretamente à propriedade em vez de chamar o conversor
   com `null`. Como `Note` é uma classe (Value Object), isso deixava `_note`
   como `null` "cru" em vez de `new Note(null)`, quebrando qualquer chamada a
   `GetNote().GetValue()` em uma despesa recarregada do banco sem
   observação — exatamente o que `GetRecurringExpenseByIdUseCase` e
   `UpdateRecurringExpenseUseCase` fazem. **Corrigido no Domain, não na
   Infrastructure**: o construtor privado de reidratação do EF em
   `RecurringExpense` agora normaliza `note ?? new Note(null)`, restaurando a
   mesma invariante ("nunca nulo") que o construtor público já garante — não
   é uma mudança de comportamento de negócio, apenas a correção de uma
   violação de invariante introduzida silenciosamente pela plumbing do EF.
2. **`UPDATE` gerado em vez de `INSERT` para uma nova `Occurrence` em um
   agregado já rastreado.** `Reactivate()` adiciona uma nova `Occurrence` à
   lista `_occurrences` de um `RecurringExpense` que já veio de
   `GetByIdAsync` (já rastreado pelo `DbContext`). Como `Occurrence.Id` é um
   GUID gerado pela aplicação (já não-default), a detecção automática de
   mudanças do EF Core assume que a entidade já existe no banco (heurística
   padrão do EF para chaves geradas pelo cliente) e a marca como `Modified`
   em vez de `Added`, gerando um `UPDATE` que afeta 0 linhas →
   `DbUpdateConcurrencyException`. Nenhum código existente antes desta
   feature jamais adicionava uma ocorrência a um agregado *já persistido e
   rastreado* (só no construtor, sempre via `AddAsync`/`context.Add(...)`
   explícito no grafo inteiro). **Corrigido na Infrastructure**:
   `RecurringExpenseRepository.UpdateAsync` agora marca explicitamente
   `EntityState.Added` para qualquer `Occurrence` cujo `_context.Entry(...)`
   ainda esteja `Detached`, antes de `SaveChangesAsync()`.

Nenhum dos dois defeitos era visível em `Application.Tests` (repositório
falso escrito à mão, sem EF Core) nem em `Api.Tests` (provider EF Core
InMemory, que nunca chega a exercitar semântica real de coluna `NULL` nem a
diferença `INSERT`/`UPDATE`) — só a validação manual contra PostgreSQL real
(T041) os revelou. Testes de regressão para ambos foram adicionados a
`Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`.

## 6. Estrutura de testes

**Decision**: Nenhum projeto de teste novo. Os testes desta feature entram
nos projetos/arquivos já existentes:
- `Domain.Tests/Aggregates/RecurringExpenseTests.cs` — um teste por método
  novo do aggregate, incluindo os três cenários de `Reactivate` (gera
  ocorrência / não gera por já existir uma / não gera por a data de início
  ainda não ter começado).
- `Application.Tests/UseCases/GetRecurringExpenseById/` e
  `.../UpdateRecurringExpense/` — novas pastas, seguindo a mesma convenção
  já usada por `.../CreateRecurringExpense/` e `.../MarkOccurrenceAsPaid/`.
- `Api.Tests/Controllers/RecurringExpensesControllerTests.cs` (já existente)
  — novos métodos de teste para os dois novos endpoints, via
  `WebApplicationFactory`, um por cenário de resposta declarado
  (`200`/`404`/`500` no `GET`; `200`/`400`/`404`/`500` no `PUT`).

**Rationale**: Mesma lógica de simplicidade já registrada no refinamento e
em `research.md` da feature 004 — uma estrutura de pastas dedicada por
endpoint/controller não é justificada enquanto um único controller
concentra todas as ações deste recurso (Princípio V).

**Alternatives considered**: Nenhuma — segue diretamente a convenção já
estabelecida pelas features 001–006.
