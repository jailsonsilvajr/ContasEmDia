# Phase 0 Research: Data de Fim da Despesa Recorrente

**Feature**: `009-data-fim-despesa-recorrente` | **Date**: 2026-09-17

O refinamento técnico
([`refinements/data-fim-despesa-recorrente.md`](../../refinements/data-fim-despesa-recorrente.md))
já fecha a maior parte das decisões (campo novo, teto de 1 ano, geração em
lote, ajuste na edição, backfill). Este documento resolve os pontos que o
refinamento deixou como "Pontos em aberto", incorpora as duas clarificações
adicionais fechadas em `spec.md` (FR-014 — data de fim não pode estar no
passado; extensão retroativa preenche a lacuna inteira) e confirma, contra o
código atual, os pontos de Infrastructure que a feature 007 já havia
diagnosticado como frágeis. Não há nenhum item "NEEDS CLARIFICATION" de
contexto técnico pendente.

## 1. Nova capacidade em `ReferencePeriod`: avançar uma competência

**Decision**: Adicionar `ReferencePeriod.Next()`, retornando a competência
seguinte (`month == 12 ? new ReferencePeriod(year + 1, 1) : new
ReferencePeriod(year, month + 1)`).

**Rationale**: `ReferencePeriod` hoje só expõe comparação
(`CompareTo`/operadores). Gerar toda a vigência no cadastro (FR-004) e
calcular a lacuna a preencher ao estender a data de fim (FR-007) exigem
iterar mês a mês entre um piso e um teto — uma operação de avanço é o
mínimo necessário, sem introduzir aritmética de datas mais ampla
(ex.: `AddMonths(int n)`) do que o que as duas regras desta feature
realmente usam (Princípio V).

**Alternatives considered**: Expor `AddMonths(int n)` genérico —
descartado por YAGNI: nenhuma regra desta feature precisa avançar mais de
um mês por vez; um laço com `Next()` resolve os dois casos de uso
(cadastro e extensão) sem superfície extra não utilizada.

## 2. Onde vivem as duas invariantes de vigência (fim > início; ≤ 1 ano)

**Decision**: Um método privado estático no aggregate,
`ValidateVigencia(CalendarDate startDate, CalendarDate endDate)`, chamado
por três pontos: o construtor público, `ChangeStartDate` e `ChangeEndDate`.
Ele verifica, nesta ordem:
1. `endDate.GetValue() <= startDate.GetValue()` →
   `DomainRuleViolationException("A data de fim deve ser posterior à data de início.")`
2. `endDate.GetValue() > startDate.GetValue().AddYears(1)` →
   `DomainRuleViolationException("A vigência não pode ultrapassar 1 ano a partir da data de início.")`

**Rationale**: São invariantes entre dois campos do mesmo aggregate (não de
um Value Object isolado), então vivem no aggregate (decisão 5 do
refinamento). Compartilhar um único método entre os três pontos de chamada
evita que as duas regras divirjam entre criação e cada uma das duas edições
possíveis (início ou fim) — mesmo raciocínio de DRY já usado em
`007-edit-recurring-expense` para `GenerateOccurrenceForCurrentPeriodIfDue`.

**Alternatives considered**: Duplicar a checagem dentro de cada método —
descartado por risco de divergência futura (ex.: alguém corrige a mensagem
em um lugar e esquece o outro).

## 3. Onde vive a invariante "fim não pode estar no passado" (FR-014)

**Decision**: **Não** faz parte de `ValidateVigencia` (§2). É uma checagem
separada, `ValidateEndDateNotInPast(CalendarDate endDate, ReferencePeriod
currentReferencePeriod)`, chamada **apenas** pelo construtor público e por
`ChangeEndDate` — nunca por `ChangeStartDate`.

**Rationale**: A spec (`spec.md`, Assumptions) é explícita: a regra "só
passa a valer quando o usuário tentar editar a data de fim dali em diante"
— editar **outros** campos (ex.: `ChangeStartDate`, que já precisa
revalidar `ValidateVigencia` contra o `_endDate` vigente por FR-012) não
deve falhar só porque aquele `_endDate` pré-existente (possivelmente
herdado do backfill da User Story 4) já ficou no passado. Separar as duas
checagens em métodos distintos é o que permite `ChangeStartDate` reusar
`ValidateVigencia` sem herdar a checagem de "não pode estar no passado".

**Alternatives considered**: Uma única `ValidateVigencia` com um parâmetro
opcional `currentReferencePeriod` aplicado sempre — descartado porque
tornaria `ChangeStartDate` incorretamente sujeito a uma regra que a spec
associa explicitamente só a edições da data de fim.

## 4. Geração de ocorrências no cadastro: substituir o método de 1 competência por um laço

**Decision**: Extrair o corpo de `GenerateOccurrenceForCurrentPeriodIfDue`
(cálculo de dia de vencimento + inserção, já com checagem de não
duplicidade via `GetOccurrencesForPeriod`) para um método privado
reutilizável de **uma única competência**,
`GenerateOccurrenceForPeriodIfMissing(ReferencePeriod period)`. O
construtor passa a chamar um novo método,
`GenerateOccurrencesForVigencia(ReferencePeriod currentReferencePeriod)`:

```text
startPeriod = ReferencePeriod.FromDate(_startDate.GetValue())
se currentReferencePeriod < startPeriod: nada a gerar ainda (EC06)
senão:
  endPeriod = ReferencePeriod.FromDate(_endDate.GetValue())
  período = currentReferencePeriod
  enquanto período <= endPeriod:
    GenerateOccurrenceForPeriodIfMissing(período)
    período = período.Next()
```

incondicionalmente (sem checar `status == Active`) — decisão 1 do
refinamento: geração em lote independe de Ativa/Pausada.
`Reactivate` continua chamando só
`GenerateOccurrenceForPeriodIfMissing(currentReferencePeriod)` (a
competência atual), preservando seu comportamento externo atual — revisão
de `Reactivate` para esta nova realidade (vigência inteira já nasce
gerada) fica registrada como "Pontos em aberto" do refinamento e não é
resolvida aqui (fora de escopo desta feature, ver `spec.md`, Assumptions).

**Rationale**: Reaproveita a checagem de não duplicidade já existente por
competência (RF13/FR-013), sem duplicá-la entre o laço e `Reactivate`.
Mesmo padrão de extração de método privado compartilhado já usado por
`007-edit-recurring-expense` (`research.md` §2).

**Alternatives considered**: Manter `GenerateOccurrenceForCurrentPeriodIfDue`
e chamá-lo N vezes por fora (no UseCase) — descartado por vazar para a
Application uma decisão que é do domínio (quais competências pertencem à
vigência), violando Princípio XI.

## 5. Geração/exclusão de ocorrências em `ChangeEndDate`

**Decision**: `ChangeEndDate(CalendarDate newEndDate, ReferencePeriod
currentReferencePeriod)` faz, nesta ordem:
1. `ValidateVigencia(_startDate, newEndDate)` (§2).
2. `ValidateEndDateNotInPast(newEndDate, currentReferencePeriod)` (§3).
3. Compara `ReferencePeriod.FromDate(newEndDate.GetValue())` (`newPeriod`)
   com `ReferencePeriod.FromDate(_endDate.GetValue())` (`oldPeriod`):
   - **`newPeriod > oldPeriod` (estender)**: nenhuma checagem de pagamento
     necessária (só cria). Depois de atualizar `_endDate`, laço de
     `oldPeriod.Next()` até `newPeriod` (inclusive) chamando
     `GenerateOccurrenceForPeriodIfMissing` — **sem** condicionar ao mês
     atual (clarificação da spec, Q2: preenche a lacuna inteira, inclusive
     competências já no passado).
   - **`newPeriod < oldPeriod` (reduzir)**: **antes** de mudar qualquer
     estado, verifica se alguma ocorrência com competência `> newPeriod`
     já está paga (`GetStatus().GetValue() == OccurrenceStatusType.Paid`).
     Se sim: `throw new DomainRuleViolationException(...)` — nada muda
     (nem `_endDate`, nem `_occurrences`). Se não: atualiza `_endDate` e
     remove (`_occurrences.RemoveAll(...)`) as ocorrências com competência
     `> newPeriod`.
   - **`newPeriod == oldPeriod` (mesma competência, dia diferente)**:
     apenas atualiza `_endDate`; nenhuma ocorrência é tocada (EC03).

**Rationale**: A checagem de bloqueio por ocorrência paga precisa acontecer
antes de qualquer mutação para satisfazer FR-009 ("nenhum campo é salvo,
nenhuma ocorrência é excluída ou alterada") — como o aggregate em memória
só é persistido pelo UseCase ao final (via `UpdateAsync`/
`SaveChangesAsync`), lançar a exceção antes de mutar `_endDate`/
`_occurrences` garante que, mesmo que o UseCase decida não capturar a
exceção corretamente, nenhuma mutação parcial already aconteceu no
aggregate que seria salva. Comparar por `ReferencePeriod` (não pela
`CalendarDate` completa) é o que torna EC03 (mesma competência, dia
diferente) um no-op de ocorrências.

**Alternatives considered**: Deixar a Application orquestrar "quantas
ocorrências criar/excluir" a partir de getters públicos do aggregate —
descartado porque o aggregate é o único dono da invariante de não
duplicidade (RF13) e da nova invariante de cobertura completa da vigência;
delegar o cálculo para fora violaria Princípio VI/XI (mesma conclusão já
registrada pelo refinamento, seção Application/Update).

## 6. Remoção de ocorrências rastreadas pelo EF Core — verificação necessária

**Decision**: Nenhuma mudança de assinatura é esperada em
`RecurringExpenseRepository`/`IRecurringExpenseRepository` só por causa da
remoção. `_occurrences` é a mesma instância de `List<Occurrence>` que o EF
Core carregou via `Include("_occurrences")`; `HasMany<Occurrence>` está
configurado com relacionamento obrigatório (`IsRequired()`), então remover
um item da lista rastreada e chamar `SaveChangesAsync()` deve resultar em
`DELETE` (comportamento padrão do EF Core para "orphans" de um
relacionamento obrigatório, detectado por `DetectChanges()` no próximo
`SaveChangesAsync`).

**Risco identificado (mesma classe de problema já encontrado por
`007-edit-recurring-expense`, `research.md` §5, T041)**: nenhuma feature
anterior deste repositório jamais **removeu** um item de uma coleção
rastreada pelo EF Core — só adicionou. `Application.Tests` usa um
repositório falso (sem EF Core) e `Api.Tests` usa o provider InMemory
(que não exercita `DELETE` real de forma idêntica ao PostgreSQL). Por
isso, assim como a feature 007 só descobriu o defeito de `INSERT`/`UPDATE`
via validação manual contra PostgreSQL real (T041), esta feature **deve**
incluir testes de `Infrastructure.Tests` que persistem uma despesa com
ocorrências, removem uma via edição de `_endDate`, e verificam que a linha
correspondente deixa de existir em `Occurrences` — e a validação manual do
`quickstart.md` deve exercitar esse caminho contra PostgreSQL real antes de
declarar a feature concluída. Se o comportamento padrão do EF Core não se
confirmar, o ajuste cirúrgico esperado é o mesmo padrão já usado para o
caso de `INSERT` (T041): marcar explicitamente o `EntityState` correto
(`Deleted`) para ocorrências removidas da lista, dentro de
`RecurringExpenseRepository.UpdateAsync`, antes de `SaveChangesAsync()`.

**Rationale**: Registrar o risco agora (Fase 0) evita que a mesma classe de
bug de plumbing do EF Core, já vista uma vez neste projeto, seja
reintroduzida silenciosamente por uma segunda feature.

**Alternatives considered**: Assumir que "vai funcionar" sem teste
dedicado — rejeitado, dado o precedente exato já documentado pela feature
007.

## 7. Tratamento de erro na Application: `DomainRuleViolationException` → `FieldError`

**Decision**: `CreateRecurringExpenseUseCase` passa a envolver a chamada
`new RecurringExpense(...)` em `try/catch (DomainRuleViolationException
ex)`, convertendo para `new FieldError("endDate", ex.Message)` (cobre as
três regras: obrigatoriedade já é barrada antes via parse; fim > início;
vigência ≤ 1 ano; fim não pode estar no passado — refinamento, seção Api:
"erro 400 pode trazer um item com field: endDate para qualquer uma das três
regras"). `UpdateRecurringExpenseUseCase` envolve separadamente cada
chamada condicional (`ChangeStartDate` → `FieldError("startDate", ...)`;
`ChangeEndDate` → `FieldError("endDate", ...)`), acumulando ambas antes de
decidir `Failure`/`Success` — mesmo padrão de acumulação de `FieldError`
já usado para os Value Objects.

**Rationale**: Mantém o mecanismo de erro já estabelecido (`FieldError` por
campo, capturado pela Application, nunca reescrito pela API — Princípio
VI) em vez de introduzir um segundo mecanismo só para regras cruzadas.
Atribuir o erro de `ChangeStartDate` ao campo `startDate` (não `endDate`)
seque a intuição de UX de que o campo que o usuário está mudando é o que
"causou" a rejeição (FR-012).

**Alternatives considered**: Reportar toda violação cruzada sempre no campo
`endDate`, mesmo quando originada por `ChangeStartDate` — descartado por
confundir o usuário ao editar apenas a data de início.

## 8. Migração e backfill (`EndDate` não anulável + dado existente)

**Decision**: Uma única migração EF Core (PostgreSQL, mesmo provider da
feature 005) em três passos:
1. `AddColumn<DateOnly>("EndDate", "RecurringExpenses", type: "date",
   nullable: true)`.
2. `migrationBuilder.Sql("UPDATE \"RecurringExpenses\" SET \"EndDate\" = \"StartDate\" + INTERVAL '1 year' WHERE \"EndDate\" IS NULL;")`
   — backfill por linha (decisão 3 do refinamento), não um valor fixo.
3. `AlterColumn<DateOnly>("EndDate", "RecurringExpenses", type: "date",
   nullable: false, oldNullable: true)`.

Nenhuma ocorrência é gerada por esta migração (FR-011/User Story 4) — ela
só preenche a coluna nova.

**Rationale**: É o padrão já usado por EF Core para adicionar uma coluna
`NOT NULL` a uma tabela com linhas existentes sem um valor default
constante — a mesma técnica já é implícita no restante do schema (todas as
colunas obrigatórias de `RecurringExpenses` foram criadas junto da tabela
na migração inicial, então este é o primeiro caso de "adicionar coluna
obrigatória depois", exigindo o backfill explícito em SQL).

**Alternatives considered**: Coluna anulável (`EndDate` como
`DateOnly?`/`CalendarDate?`) e tratar `null` como "sem data de fim" na
Application — descartado porque a spec (FR-011) exige que despesas antigas
passem a **ter** uma data de fim de verdade (início + 1 ano), não apenas
tolerar a ausência; manter a coluna não anulável preserva a mesma
invariante de "todo agregado sempre tem uma data de fim" tanto para dados
novos quanto migrados.

## 9. Frontend: novo campo replicado em cadastro e edição

**Decision**: Mesma forma já adotada por `dataInicio` nos dois
componentes (`CadastroDespesaRecorrenteComponent`/
`EditarDespesaRecorrenteComponent`): um novo `signal('')` `dataFim`, uma
nova chave `dataFim: boolean` em `touched`, um novo par
`onDataFimInput`/`onDataFimBlur`, e uma nova função
`getDataFimError(dataInicio: string, dataFim: string): string | null` em
`recurring-expense-form.util.ts` (obrigatória; posterior à data de início;
vigência ≤ 1 ano via `addYearsIso`-equivalente em TypeScript) — mesmas três
mensagens já validadas em `design/Cadastro.dc.html`
(commit não integrado à branch principal, ver git status). `CreateRecurringExpenseRequest`/
`UpdateRecurringExpenseRequest`/`CreateRecurringExpenseResponse`/
`RecurringExpenseDetailResponse` ganham `endDate: string`. Nenhuma
validação client-side de "não pode estar no passado" (FR-014) é adicionada
nesta iteração além das três já desenhadas em `Cadastro.dc.html` — o
refinamento e o mockup não a modelam client-side; ela chega ao usuário como
erro de campo vindo do backend (`400`, campo `endDate`), consistente com o
padrão já usado pelas demais regras de negócio que hoje só existem no
backend (ex.: nenhuma regra de negócio do Domain é duplicada no
client-side além das três já explicitamente desenhadas).

**Rationale**: Reaproveita integralmente o padrão já estabelecido por
`dataInicio` nas duas telas, sem introduzir uma abstração de formulário
nova (Princípio V). `design/Cadastro.dc.html` já foi atualizado com o
campo e as três validações — a implementação Angular replica esse mesmo
comportamento; `design/Editar.dc.html` **não** foi atualizado (ver git
status), então a tela de edição ganha o campo seguindo o mesmo padrão já
usado por `dataInicio` nela (que também não tem, hoje, nenhuma mensagem
dedicada além de "obrigatória"), estendida com as mesmas três validações
do cadastro — consistente com a intenção do refinamento (seção "Frontend —
Edição": "mesma adição de campo/estado/validação do cadastro").

**Alternatives considered**: Adicionar a validação de "não pode estar no
passado" também no client — descartado por não estar no design aprovado
(`Cadastro.dc.html`) nem pedido explicitamente pela spec para o
client-side; a validação backend já cobre o requisito (FR-014/SC-002).

## 10. Estrutura de testes

**Decision**: Nenhum projeto de teste novo, backend ou frontend. Os testes
desta feature entram nos arquivos/pastas já existentes:
- `Domain.Tests/Aggregates/RecurringExpenseTests.cs` — novos testes para
  `ValidateVigencia`/`ValidateEndDateNotInPast` (via construtor,
  `ChangeStartDate`, `ChangeEndDate`), geração em lote no construtor
  (N+1 ocorrências, Ativa e Pausada), extensão (inclusive lacuna
  retroativa), redução (com e sem ocorrência paga).
- `Application.Tests/UseCases/CreateRecurringExpense/` e
  `.../UpdateRecurringExpense/` (já existentes) — novos casos para
  `endDate` obrigatório/inválido/violações cruzadas.
- `Api.Tests/Controllers/RecurringExpensesControllerTests.cs` (já
  existente) — novos casos `400` para `endDate`.
- `Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`
  (já existente) — novo teste de regressão para remoção de ocorrência
  rastreada (§6).
- Frontend: `cadastro-despesa-recorrente.component.spec.ts`,
  `editar-despesa-recorrente.component.spec.ts` e
  `recurring-expense-form.util.spec.ts` (já existentes) — novos casos para
  `dataFim`.

**Rationale**: Mesma lógica de simplicidade já registrada pelas features
004/007 — nenhuma estrutura de pastas dedicada é justificada por esta
feature (Princípio V).

**Alternatives considered**: Nenhuma — segue a convenção já estabelecida.
