# Phase 0 Research: Painel Mensal de Despesas

**Feature**: `006-painel-mensal-despesas` | **Date**: 2026-09-07

O refinamento funcional (`refinements/painel-mensal-despesas.md`) já fechou
todas as regras de comportamento observáveis (RF01–RF24, EC01–EC16,
CA01–CA14) e registrou explicitamente, na seção "Pré-requisito", três
lacunas de domínio que bloqueiam qualquer implementação real desta tela.
Este documento resolve essas lacunas e todas as demais decisões técnicas
necessárias para fechar o `Technical Context` de `plan.md`, sem nenhum item
"NEEDS CLARIFICATION" pendente.

## 1. Rastreamento de pagamento em `Occurrence`

**Decision**: `Occurrence` deixa de ser imutável após construção. Ganha
dois campos privados novos (`_paidAmount: Money?`, `_paymentDate:
CalendarDate?`), dois métodos de intenção de negócio —
`MarkAsPaid(Money paidAmount, CalendarDate paymentDate)` e
`UndoPayment()` — e dois getters nulos por natureza (`GetPaidAmount()`,
`GetPaymentDate()`), retornando `null` enquanto a ocorrência está
`Pending`. `MarkAsPaid` lança `DomainRuleViolationException` com a mensagem
"Esta ocorrência já está paga." se `_status` já for `Paid`; `UndoPayment`
lança "Esta ocorrência ainda não foi paga." se `_status` for `Pending`.

**Rationale**: FR-013/FR-015/FR-017/FR-018 exigem que o valor pago e a data
de pagamento sejam estado real da ocorrência (não apenas do protótipo de
UI), e que marcar/desfazer sejam operações de negócio, não apenas troca de
um campo de status. Manter a regra "não pode pagar de novo uma ocorrência
já paga" / "não pode desfazer o pagamento de uma ocorrência não paga" nos
próprios métodos preserva o modelo rico exigido pelo Princípio VI — nenhum
outro lugar do sistema pode colocar `Occurrence` em um estado inconsistente
(paga sem valor/data, ou com valor/data sem estar paga).

**Alternatives considered**: Guardar valor/data pagos como campos "soltos"
sempre presentes (com `0`/data mínima como padrão em vez de `null`) —
descartado por violar a distinção clara entre "nunca foi paga" e "paga com
valor previsto", além de exigir mágica adicional para exibir/ocultar o
bloco "pago em {data}" no frontend.

## 2. Status derivado (RF11/RF22, EC03–EC06)

**Decision**: Novo Value Object `OccurrenceDerivedStatus`, envolvendo o
enum `OccurrenceDerivedStatusType { Paid, Overdue, DueSoon, Pending }`
(mesmo padrão de validação `Enum.IsDefined` de todo VO-enum já existente no
Domain). Novo método `Occurrence.GetDerivedStatus(DateOnly referenceDate)`,
puramente computado (nunca persistido), aplicando exatamente a prioridade
de FR-005: `Paid` se `_status == Paid`; senão `Overdue` se
`_dueDate < referenceDate`; senão `DueSoon` se
`_dueDate - referenceDate <= 7 dias` (0 a 7 inclusive); senão `Pending`.

**Rationale**: RF22 exige que "Vencida"/"Vence em breve" sejam calculados
no servidor usando a data completa de vencimento, evitando exatamente o bug
de virada de mês do protótipo (EC06) — o cálculo não pode viver no
frontend nem duplicar a regra de `OccurrenceStatusType` (status
*persistido*, apenas paga/não paga). Um Value Object novo, e não um método
retornando o enum "cru", mantém a mesma convenção uniforme já usada por
`OccurrenceStatus`/`ExpenseCategory`/`RecurringExpenseStatus`/`Frequency` —
sem exceção aberta para este caso.

**Alternatives considered**: Reaproveitar `OccurrenceStatusType` acrescentando
`Overdue`/`DueSoon` como valores adicionais — descartado porque misturaria
estado persistido (pago/não pago, decidido pelo usuário) com estado
derivado (calculado a cada consulta, dependente de "hoje"), quebrando a
regra "o status exibido... é derivado... não é um valor armazenado à
parte" (spec, "Key Entities").

## 3. Consulta de ocorrências por competência, entre todas as despesas recorrentes (RF19–RF20, EC12, EC15)

**Decision**: `IRecurringExpenseRepository` ganha
`Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod)`,
retornando todo agregado `RecurringExpense` (ativo ou pausado) que tenha ao
menos uma ocorrência na competência pedida. Novo método de leitura no
agregado, `RecurringExpense.GetOccurrencesForPeriod(ReferencePeriod
referencePeriod)`, filtra apenas as ocorrências daquela competência dentro
de cada agregado retornado.

**Rationale**: Repositórios só podem ser definidos para Agregados
(Princípio VI) — `Occurrence` não pode ter repositório próprio. Retornar o
agregado completo e filtrar suas ocorrências via um método do próprio
agregado mantém a regra de acesso "Entidades só são alcançáveis através do
Agregado dono" e satisfaz RF20 (despesas pausadas continuam aparecendo) sem
duplicar a regra de filtragem fora do Domain.

**Alternatives considered**: Projeção SQL direta para um DTO plano de
ocorrências (bypassando o agregado) — descartada por romper a fronteira de
Repository-só-para-Agregados do Princípio VI, mesmo sendo tecnicamente mais
simples de traduzir para SQL. A tradução exata da consulta EF Core (dado
que `ReferencePeriod` é mapeado como `ComplexProperty` de `Occurrence`, não
como coluna simples) é um detalhe de implementação; se a tradução
server-side direta se mostrar impraticável, a implementação pode carregar
os agregados com `Include("_occurrences")` e filtrar em memória — aceitável
dada a escala pessoal/pequena do sistema (Princípio V), sem paginação nesta
iteração (Assumptions da spec).

## 4. Localizar o agregado dono de uma ocorrência, para marcar/desfazer pagamento

**Decision**: `IRecurringExpenseRepository` ganha também
`Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId)` e
`Task UpdateAsync(RecurringExpense recurringExpense)` (este último apenas
chama `_context.SaveChangesAsync()` — o agregado já está *tracked* pelo
mesmo `DbContext` com escopo de requisição que o carregou). No agregado,
`RecurringExpense.FindOccurrence(Guid occurrenceId)` localiza a ocorrência
filha; `MarkOccurrenceAsPaid`/`UndoOccurrencePayment` delegam a ela,
lançando `KeyNotFoundException("Ocorrência não encontrada.")` quando o id
não existe no agregado.

**Rationale**: Do ponto de vista do cliente (design/refinamento), cada
ocorrência na tela é identificada só pelo seu próprio `id` — nenhuma tela
do design expõe o id da despesa recorrente de origem. Buscar o agregado
pelo id da ocorrência filha evita vazar esse conceito para o contrato de
API das ações de pagamento (rotas `.../occurrences/{occurrenceId}/payment`,
sem precisar de um id de despesa recorrente na URL nem na resposta do
painel). `UpdateAsync` segue exatamente o mesmo padrão já usado por
`AddAsync` (repositório chama `SaveChangesAsync` diretamente) — não
introduz nenhum Unit of Work novo, permanecendo dentro do que o Princípio
VII já permite.

**Alternatives considered**: Expor `recurringExpenseId` no contrato do
painel e exigi-lo nas rotas de pagamento (`.../recurring-expenses/{id}/occurrences/{occurrenceId}/payment`)
— descartado por vazar um conceito de modelagem interna que a tela nunca
precisa conhecer, tornando o contrato mais verboso sem nenhum ganho
funcional.

## 5. Diferenciação de erros no middleware global (400 regra de negócio × 404 não encontrado × 500 inesperado)

**Decision**: Novo tipo `DomainRuleViolationException` (herda de
`Exception`, mensagem PT-BR obrigatória no construtor), definido na raiz do
projeto `Domain`. É o único tipo lançado por `Occurrence.MarkAsPaid`/
`UndoPayment` para violações de invariante. `ExceptionHandlingMiddleware`
ganha dois `catch` novos, antes do `catch (Exception)` genérico já
existente: `DomainRuleViolationException` → `400 Bad Request`;
`KeyNotFoundException` → `404 Not Found` — ambos reaproveitando o mesmo
formato de envelope (`ApiResponse<object>.Failure([...])`) já usado pelo
`catch` de `500`, com a mensagem da exceção relayed as-is (Princípio VI).

**Rationale**: É a primeira feature do repositório em que um método de
negócio de Agregado/Entidade (não um construtor de Value Object) precisa
sinalizar uma violação de invariante, e a primeira em que uma condição de
"não encontrado" existe — por isso o middleware, que hoje só mapeia
qualquer exceção para `500`, precisa ser estendido para cumprir o que o
Princípio XII já exige desde sempre ("um erro de negócio deve mapear para
400/422; não encontrado para 404"). Um tipo dedicado (em vez de reaproveitar
`InvalidOperationException` do BCL) é necessário porque o teste já existente
`ExceptionHandlingMiddlewareTests.Post_WhenPersistenceThrows_Returns500...`
depende de uma `InvalidOperationException` de falha de persistência
continuar mapeando para `500` — se o novo `catch` de `400` capturasse
`InvalidOperationException` genericamente, esse teste (e qualquer outra
falha inesperada que hoje acidentalmente seja uma `InvalidOperationException`)
passaria a responder `400` incorretamente. `KeyNotFoundException` do BCL é
reaproveitada sem ambiguidade porque nenhum outro lugar do código já a usa
com outro significado.

**Alternatives considered**: Continuar tratando toda violação de invariante
dentro do próprio UseCase (capturando a exceção ali e convertendo para
`FieldError`/`400`, no mesmo estilo já usado por `CreateRecurringExpenseUseCase`
para erros de Value Object) — descartado para "já está paga"/"ainda não foi
paga" porque essas não são falhas de validação de um campo específico de
entrada (não há `field` correspondente no request), e porque o Princípio
XII já presume, desde a primeira versão da constituição, que violações de
regra de negócio devem chegar ao middleware global como exceções reais —
este é o primeiro caso em que essa via passa a ser exercida.

## 6. Generalização de `ApiBehaviorOptions.InvalidModelStateResponseFactory`

**Decision**: A fábrica de resposta de `ModelState` inválido, hoje
hardcoded em `Program.cs` para `ApiResponse<CreateRecurringExpenseDataResponse>`,
passa a usar `ApiResponse<object>` — mesmo tipo genérico já usado pelo
`catch` de `500` do middleware.

**Rationale**: Correção necessária, não [opcional/cosmética. O JSON
resultante é idêntico (o campo `Data` é sempre `null` quando `Success ==
false`, independentemente do parâmetro de tipo genérico em tempo de
compilação), mas manter o tipo acoplado a um único endpoint antigo deixa
de fazer sentido a partir do momento em que outra ação (`GET
/api/v1/occurrences`, sem `DataRequest` de corpo, mas ainda sujeita a esta
fábrica caso um filtro de rota/framework produza um erro de `ModelState`)
compartilha a mesma composição.

**Alternatives considered**: Nenhuma — é a correção mínima, sem introduzir
nenhum tipo ou abstração nova.

## 7. Roteamento no frontend (painel + cadastro coexistindo)

**Decision**: Introduzir `app.routes.ts` com duas rotas —
`{ path: '', component: PainelMensalDespesasComponent }` e
`{ path: 'despesas/nova', component: CadastroDespesaRecorrenteComponent }`
— e trocar `app.config.ts`/`app.html`/`app.ts` para usar
`provideRouter(routes)` + `<router-outlet />`, em vez do componente único
hoje fixado em `app.html`.

**Rationale**: Hoje `App` monta `<app-cadastro-despesa-recorrente />`
diretamente (única tela existente até esta feature). Introduzir a tela de
painel como uma segunda tela exige alguma forma de as duas coexistirem sem
regredir o cadastro já entregue; `@angular/router` já é uma dependência
declarada em `package.json` (apenas ainda não utilizada), então não há
nenhuma dependência npm nova a justificar perante o AI Agent Guardrails da
constituição. É o mínimo necessário — duas rotas, sem guards, resolvers ou
lazy loading, já que nenhum deles tem justificativa concreta nesta etapa
(Princípio V). O botão "Nova despesa" e as setas de navegação de mês do
design permanecem sem `onClick`, exatamente como no protótipo (RF02/RF03 do
refinamento os declaram fora de escopo) — a rota `despesas/nova` fica
alcançável por URL, mas nada dentro da tela do painel ainda link a ela.

**Alternatives considered**: Substituir simplesmente o conteúdo de
`app.html` pelo painel, descartando a tela de cadastro do ponto de
montagem da aplicação — rejeitada por regredir silenciosamente uma tela já
entregue e testada (`CadastroDespesaRecorrenteComponent`,
`002-cadastro-despesa-recorrente`) sem que nenhum requisito desta feature
peça sua remoção.

## 8. Operabilidade via teclado do link "Desfazer" (Princípio II — WCAG 2.1 AA)

**Decision**: No design (`design/Main.dc.html`), a ação "Desfazer" é um
`<span class="cd-link" onClick="...">`. Na implementação Angular real, o
mesmo elemento é um `<button type="button">` com a aparência visual
idêntica (mesmo texto, cor, sublinhado, tamanho), em vez de um `<span>`
clicável.

**Rationale**: A constituição exige, sem exceção, que todo componente novo
seja operável via teclado (Princípio II) — um `<span>` com `onClick` não
recebe foco nem responde a Enter/Espaço nativamente. "Seguir fielmente o
design" é interpretado, aqui e em toda a tela, como fidelidade *visual*
(cores, espaçamento, tipografia, layout), não como reprodução literal de
uma escolha de tag HTML do protótipo estático que conflita com um
requisito não-negociável da constituição. As demais ações interativas do
design ("Marcar como paga", "Confirmar", "Cancelar", as setas de navegação,
"Nova despesa") já são `<button>` no protótipo — sem ajuste necessário.

**Alternatives considered**: Manter um `<span>`/`<a>` com `role="button"`,
`tabindex="0"` e um `keydown` handler manual para Enter/Espaço — descartado
por reintroduzir manualmente semântica que um `<button>` nativo já oferece
de graça, sem nenhum ganho visual (o CSS já usado por `cd-link` se aplica
igualmente a um `<button>` sem estilo nativo de botão).
