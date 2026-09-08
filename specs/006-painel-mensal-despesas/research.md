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
hoje fixado em `app.html`. As setas de navegação de mês e o botão "Nova
despesa" do design (FR-020/FR-021 — ver "Clarifications" de `spec.md`,
sessão 2026-09-07) ganham comportamento real: cada seta chama um método do
componente (`mesAnterior()`/`proximoMes()`) que calcula a competência de
destino a partir do `referencePeriod` atual, atualiza o signal e recarrega
via `painel-mensal-despesas.service.ts` (`GET /api/v1/occurrences` com o
novo `year`/`month`, já suportado pelo endpoint desde o desenho original —
FR-002/RF02); o botão "Nova despesa" usa `routerLink="/despesas/nova"` do
próprio `@angular/router` já introduzido nesta seção.

**Rationale**: Hoje `App` monta `<app-cadastro-despesa-recorrente />`
diretamente (única tela existente até esta feature). Introduzir a tela de
painel como uma segunda tela exige alguma forma de as duas coexistirem sem
regredir o cadastro já entregue; `@angular/router` já é uma dependência
declarada em `package.json` (apenas ainda não utilizada), então não há
nenhuma dependência npm nova a justificar perante o AI Agent Guardrails da
constituição. É o mínimo necessário — duas rotas, sem guards, resolvers ou
lazy loading, já que nenhum deles tem justificativa concreta nesta etapa
(Princípio V). Uma versão anterior deste documento (antes da clarificação
de 2026-09-07) assumia que as setas e o botão "Nova despesa" ficariam sem
`onClick`, por a spec então tratá-los apenas como uma Assumption implícita;
a spec agora os formaliza como FR-020/FR-021, então implementá-los é a
única leitura consistente com o Technical Context desta feature — nenhuma
rota, endpoint ou dependência nova é necessária para isso, apenas dois
handlers de clique no componente já planejado e um `routerLink` no mesmo
router já introduzido por esta seção.

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

---

# Adendo — Requisitos de UX acrescentados em 2026-09-08 (FR-022 a FR-030)

As seções abaixo resolvem as decisões técnicas para os novos requisitos
funcionais/UX incorporados a `spec.md` na sessão de `/speckit-specify` e
`/speckit-clarify` de 2026-09-08: cursor de mão (FR-022), navegação/
confirmação da tela de cadastro e ações da tela de sucesso (FR-023–FR-025,
que — conforme a clarificação de 2026-09-08 — são implementadas dentro
desta mesma feature, alterando o componente `cadastro-despesa-recorrente`
já entregue pela feature 002), responsividade do painel (FR-026/FR-027),
máscara de moeda (FR-028), seletor de data nativo (FR-029) e exibição em
Euro (FR-030). Nenhuma dessas decisões introduz endpoint, rota de API ou
dependência npm nova — são mudanças de frontend (template/componente/CSS)
sobre os dois componentes já existentes (`painel-mensal-despesas`,
`cadastro-despesa-recorrente`).

## 9. Cursor de mão em elementos clicáveis, excluindo desabilitados (FR-022)

**Decision**: Cada elemento clicável/interativo de `painel-mensal-despesas`
e `cadastro-despesa-recorrente` (botões, links-estilizados-como-botão,
setas de navegação, ícones de ação, o input de data) ganha a classe
utilitária Tailwind `cursor-pointer`; todo `<button>` que pode ficar
desabilitado ganha adicionalmente `disabled:cursor-default` (variante
nativa do Tailwind, que já reage ao atributo `disabled` do elemento sem
nenhum binding condicional extra no componente).

**Rationale**: O Princípio VIII exige "Tailwind CSS utility classes para
estilo global e de componente"; uma regra CSS global solta em
`frontend/src/styles.css` (fora do padrão de utility classes já usado no
projeto) seria a alternativa mais simples de aplicar, mas foge do que a
constituição autoriza sem justificativa (a exceção documentada nela é
apenas para SCSS encapsulado por componente, não para CSS global solto).
Aplicar `cursor-pointer` por elemento já é o padrão em uso neste mesmo
componente (o botão "Desfazer" já recebeu `cursor-pointer` na Fase 5/T064
original) — esta decisão apenas generaliza esse padrão já estabelecido
para todos os demais elementos clicáveis das duas telas. `disabled:` é uma
variante nativa do Tailwind (não uma dependência nova), reagindo ao mesmo
atributo HTML `disabled` que já existe nos botões que hoje ficam
desabilitados durante carregamento/envio (ex.: "Confirmar" no formulário
de cadastro durante `isLoading()`).

**Alternatives considered**: Regra CSS global em `styles.css` (`button
{ cursor: pointer } button:disabled { cursor: default }`, espelhando
literalmente o `<style>` do design) — descartada por não usar utility
classes Tailwind, sem uma necessidade concreta que justifique o desvio do
Princípio VIII (diferente do caso já aceito de SCSS por componente, que
exige insuficiência demonstrada das utility classes).

## 10. Máscara de moeda compartilhada entre as duas telas (FR-028)

**Decision**: Novo módulo utilitário `frontend/src/app/shared/currency-mask.util.ts`,
exportando `maskCurrencyDigits(raw: string): string` — porta direta do
algoritmo já usado no design (`maskMoneyDigits` em `Main.dc.html`/
`Cadastro.dc.html`: remove tudo que não é dígito, trata os dois últimos
dígitos como centavos, agrupa milhares com `.` e usa `,` como separador
decimal, sem símbolo de moeda embutido). `painel-mensal-despesas.component.ts`
(`onDraftValorInput`) e `cadastro-despesa-recorrente.component.ts`
(`onValorInput`) passam a aplicar essa função ao valor bruto do evento de
input antes de gravar no signal correspondente (`draftValor`/`valor`), e o
próprio `<input>` reflete o valor mascarado via `[value]` (padrão
Angular/signal já em uso nos dois componentes — sem `ngModel`/two-way
binding novo). O parsing existente de "vírgula como decimal" no backend e
nas funções `parseValor`/`onSalvar`/`confirmarPagamento` não muda — a
máscara só formata o que o usuário vê digitando, sem alterar o formato já
enviado ao backend.

**Rationale**: FR-028 exige a mesma máscara nos dois campos monetários da
spec (valor pago no painel, valor previsto no cadastro); duplicar o
algoritmo em dois arquivos violaria o Princípio V (simplicidade —
preferir reuso a duplicação, mesmo padrão já seguido por
`CATEGORY_COLORS`/`CATEGORY_OPTIONS`, reaproveitados de
`despesa-recorrente.model.ts` por `painel-mensal-despesas.model.ts`). Uma
pasta `shared/` nova é o mínimo necessário para essa reutilização sem
introduzir uma dependência ou projeto novo — organizada por
responsabilidade (utilitário puro, sem UI), não por camada técnica
genérica, então não conflita com a organização por feature exigida pelo
Princípio VIII.

**Alternatives considered**: Duplicar a função em cada componente —
descartada por violar Princípio V. Colocar o utilitário dentro de
`features/despesa-recorrente/` e importá-lo de `painel-mensal-despesas`
— descartada por criar uma dependência de uma feature sobre outra,
quebrando o isolamento por feature que `CATEGORY_COLORS` já contorna hoje
apenas porque a spec de cadastro é anterior; um utilitário sem relação de
domínio com nenhuma das duas features pertence a um local neutro
(`shared/`), não a uma das duas.

## 11. Formatação de valores em Euro, compartilhada (FR-030)

**Decision**: Novo utilitário `frontend/src/app/shared/currency-format.util.ts`,
exportando `formatEUR(value: number): string` — porta direta do
`formatEUR` já existente no design (`Main.dc.html`/`Cadastro.dc.html`:
milhar agrupado com `.`, decimal com `,`, sufixo `" €"`), em vez de
`Intl.NumberFormat` com uma locale/moeda escolhida por adivinhação (ex.:
`Intl.NumberFormat('pt-BR', {currency:'EUR'})` produziria `"€ 1.234,56"`,
com o símbolo antes do valor e possivelmente um espaço diferente do
protótipo — inconsistente com o design de referência que esta feature deve
seguir fielmente). Substitui a função local `formatBRL` de
`painel-mensal-despesas.component.ts` e o `Intl.NumberFormat(...BRL...)`
usado em `cadastro-despesa-recorrente.component.ts` (`valorFmt`) por esse
único utilitário compartilhado.

**Rationale**: Mesma razão de reuso do item 10 — os dois componentes
formatam valores monetários e ambos devem, a partir de FR-030, exibir
Euro; duplicar a lógica de formatação romperia o Princípio V. Reproduzir
o algoritmo exato do protótipo (em vez de `Intl.NumberFormat`) garante
fidelidade pixel-a-pixel ao design de referência (mandato original desta
feature) sem depender do comportamento de formatação específico de uma
locale ICU que pode variar por ambiente/navegador.

**Alternatives considered**: `Intl.NumberFormat('de-DE', {style:'currency',
currency:'EUR'})` (produz `"1.234,56 €"`, mais próximo do design) —
descartada por depender indiretamente de uma locale alemã em uma aplicação
inteiramente em PT-BR, e por não garantir determinismo entre ambientes
Node (testes) e navegador da mesma forma que uma função pura testada
unitariamente garante.

## 12. Seletor de data nativo (FR-029) — impacto no formato de valor trocado com o backend

**Decision**: Os dois campos de data afetados (`painel-mensal-despesas`:
`draftData`; `cadastro-despesa-recorrente`: `dataInicio`) passam de
`type="text"` para `type="date"`. Como o valor nativo de um
`<input type="date">` é sempre ISO (`yyyy-MM-dd`) ou vazio:
- Em `cadastro-despesa-recorrente`, isso **simplifica** o componente: o
  signal `dataInicio` passa a guardar diretamente o valor ISO do input;
  `parseDataInicio`/`toIsoDate` deixam de ser necessários (o payload
  `startDate` já é ISO hoje — nenhuma mudança de contrato de API), e
  `dataInicioError` passa a checar apenas presença (`type="date"` já
  impede o navegador de submeter uma data inválida/incompleta).
- Em `painel-mensal-despesas`, o backend continua esperando
  `paymentDate` como string `dd/MM/yyyy` (`MarkOccurrenceAsPaidUseCase`,
  `data-model.md`) — nenhuma mudança de contrato de API é feita por esta
  adição de UX. O componente converte o valor ISO do `<input type="date">`
  para `dd/MM/yyyy` apenas no momento de montar a chamada
  `markOccurrenceAsPaid(...)` (nova função utilitária pura, ex.
  `isoToBrDate`, colocada junto de `formatIsoDateAsBR`/`formatDateAsBR` já
  existentes no próprio arquivo do componente — sem necessidade de
  compartilhar com `cadastro-despesa-recorrente`, que não precisa dessa
  conversão).

**Rationale**: Trocar o mecanismo de entrada de data (nativo vs. texto
livre) é uma mudança de UX, não uma mudança de contrato de API — manter o
formato já trafegado (`dd/MM/yyyy` no painel, ISO no cadastro) evita
qualquer alteração em `backend/Application`/`backend/Api` para este
requisito puramente de frontend, consistente com o Princípio V
(simplicidade — não alterar o que já funciona sem necessidade concreta) e
com o Princípio I (o contrato de API é a fonte da verdade; um requisito de
UX do cliente não deveria forçar uma renegociação de contrato sem
necessidade).

**Alternatives considered**: Mudar o backend para aceitar `paymentDate`
em ISO, alinhando os dois campos ao mesmo formato de trafego — descartada
por ser uma mudança de contrato de API não pedida por nenhum requisito
desta spec (FR-029 fala apenas do comportamento do seletor no cliente) e
por exigir reabrir `MarkOccurrenceAsPaidUseCase`/testes já entregues
(Fase 4, T034/T042) sem necessidade concreta.

## 13. Responsividade do painel — breakpoints não sobrepostos (FR-026/FR-027)

**Decision**: A grade dos três cartões de resumo usa três classes
Tailwind não sobrepostas — base (`grid-cols-3`, >720px), uma faixa média
explícita (`min-[481px]:max-[720px]:grid-cols-2`, 481–720px) e a faixa
estreita (`max-[480px]:grid-cols-1`, ≤480px) — em vez de duas classes
`max-[720px]:grid-cols-2`/`max-[480px]:grid-cols-1` que se sobrepõem em
qualquer largura ≤480px (ambas as condições seriam verdadeiras
simultaneamente ali, tornando o resultado final dependente da ordem de
emissão das variantes arbitrárias no CSS gerado pelo Tailwind, que não é
uma garantia de contrato da ferramenta). Cada coluna da linha de ocorrência
(`nome`/`status`/`valor`/`dia`/`ações`) ganha `order-{1..5}` condicional
via `max-[720px]:order-{n}` (mesma ordem de FR-026: nome, status, valor,
dia, ações) — aqui não há sobreposição de faixas (é uma única condição
`max-[720px]`, sem uma segunda regra `max-[480px]` conflitante para
`order`), então o padrão simples de variante única é suficiente.

**Rationale**: Faixas não sobrepostas eliminam qualquer dependência da
ordem de emissão do CSS gerado (a mesma preocupação seria irrelevante em
CSS escrito à mão, como no design de referência, porque lá a ordem das
regras no arquivo é explícita e controlada manualmente — em utility
classes Tailwind isso não é garantido da mesma forma). Nenhuma dependência
nova ou arquivo de configuração (`tailwind.config.*`) é necessária —
Tailwind CSS 4 (já em uso, configurado via `@theme` em
`frontend/src/styles.css`) suporta variantes arbitrárias de breakpoint
(`max-[720px]:`, `min-[481px]:`) nativamente.

**Alternatives considered**: Declarar breakpoints nomeados novos no tema
Tailwind (`@theme { --breakpoint-panel-md: 720px; ... }`) — descartada por
introduzir um novo conceito de configuração de tema para um requisito que
afeta apenas dois grupos de elementos em um único componente; variantes
arbitrárias inline resolvem o mesmo requisito com menos uma camada de
indireção, consistente com o Princípio V.

## 14. "Voltar ao painel" com confirmação condicional e ação dupla na tela de sucesso (FR-023–FR-025)

**Decision**: `cadastro-despesa-recorrente.component.ts` importa
`Router`/`RouterLink` (já uma dependência do projeto, introduzida em
`research.md` §7 para o roteamento do painel) e ganha:
- Um novo `computed` `hasUnsavedData`, `true` quando qualquer um dos
  signals do formulário difere de seu valor inicial (`nome`, `valor`,
  `dia`, `dataInicio`, `observacao` não vazios, ou `categoria`/`status`
  diferentes de seus padrões `'Housing'`/`'ativa'`) — a mesma definição
  usada pela clarificação de `spec.md` (edge case: reverter manualmente
  para o estado inicial volta a contar como "sem dados não salvos", pois é
  uma comparação de valor, não uma flag "tocou o campo").
- Um novo signal `showExitConfirmDialog`, controlando a exibição do modal
  de confirmação (mesmo texto/estrutura de `design/Cadastro.dc.html`,
  bloco `onCancelExit`/`onConfirmExit`).
- `onClickVoltar()`: se `hasUnsavedData()` for `false`, navega direto
  (`router.navigateByUrl('/')`); caso contrário, abre o modal
  (`showExitConfirmDialog.set(true)`) em vez de navegar.
- `onCancelExit()`: fecha o modal sem navegar (`showExitConfirmDialog.set(false)`).
- `onConfirmExit()`: fecha o modal e navega para `/` — sem tentativa de
  salvamento parcial (FR-024/edge case).
- No estado de sucesso (`isSuccess()`), o botão novo "Voltar ao painel"
  usa `routerLink="/"` diretamente (sem verificação de dados não salvos —
  não há formulário para perder nesse estado; a única saída sem
  confirmação condicional é aqui e no caso "formulário vazio" acima), ao
  lado do botão já existente "Cadastrar outra despesa" (`onNovaDespesa()`,
  já implementado desde `002-cadastro-despesa-recorrente`).
- Um botão/link "Voltar ao painel" equivalente é adicionado também ao
  cabeçalho do formulário (estado não-sucesso), reproduzindo a posição de
  `design/Cadastro.dc.html` (topo, com ícone de seta), chamando
  `onClickVoltar()`.

**Rationale**: Reaproveita o `Router` já introduzido por esta mesma
feature (`research.md` §7) — nenhuma dependência nova. A definição de
"dados não salvos" como comparação de valor (não como flag "tocou") é a
única leitura consistente com o edge case já adicionado a `spec.md` em
2026-09-08 ("reverter manualmente para o estado inicial... tratado como
sem dados não salvos"). Não reabrir `onNovaDespesa()` (já testado desde a
feature 002) minimiza a superfície de mudança sobre um componente já
entregue, alinhado ao Princípio V.

**Alternatives considered**: Rastrear "sujeira" do formulário com uma flag
booleana setada no primeiro evento de `input`/`change` de qualquer campo
(padrão comum de "formulário tocado") — descartada por contradizer
diretamente o edge case clarificado (um campo preenchido e depois revertido
manualmente para vazio deve contar como "sem dados não salvos", o que uma
flag de "já foi tocado" nunca reverteria automaticamente sem lógica extra
equivalente à própria comparação de valor, tornando a flag redundante ou
incorreta).
