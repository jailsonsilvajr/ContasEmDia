# Research: Editar Despesa Recorrente (Tela)

Nenhum item do "Technical Context" ficou marcado como `NEEDS
CLARIFICATION` — todas as escolhas técnicas já são as convenções vigentes
do projeto (Angular 22 standalone, Signals, Vitest, Tailwind). As decisões
abaixo cobrem, em vez disso, as escolhas de desenho de componente que o
refinamento de frontend deixava como proposta e que a spec 008
(clarificada) já resolveu, mais a única lacuna de dados encontrada nesta
etapa.

## Decisão 1 — Componente próprio em vez de flag de modo no cadastro

**Decision**: Criar `EditarDespesaRecorrenteComponent` como componente
standalone independente, não reaproveitar `CadastroDespesaRecorrenteComponent`
com uma flag `isEdicao`.

**Rationale**: O componente de cadastro já tem lógica própria do fluxo
pós-sucesso ("Cadastrar outra despesa", `onNovaDespesa()`) que a spec 008
explicitamente exclui da tela de edição (FR-010). Ramificar um único
componente por modo violaria o Princípio V (simplicidade — preferir dois
componentes pequenos e diretos a um componente com lógica condicional por
modo). Decisão já proposta em `refinements/frontend/editar-despesa-recorrente.md`
e mantida aqui.

**Alternatives considered**: Um único componente com `@Input() mode:
'create' | 'edit'` — rejeitado por espalhar `if (mode === ...)` por
validação, submit e estados de sucesso, tornando os testes de ambos os
modos mais frágeis e o componente mais difícil de ler.

## Decisão 2 — Extrair máscara/parse/validação para um utilitário compartilhado

**Decision**: Mover `maskMoneyDigits`-equivalente (já extraído como
`maskCurrencyDigits`), `formatEUR`-equivalente (já `formatEUR` em
`currency-format.util.ts`) — que já são compartilhados — e as regras de
`validate()`/`parseValor`/`parseDia` hoje privadas em
`CadastroDespesaRecorrenteComponent` para um novo arquivo
`frontend/src/app/shared/recurring-expense-form.util.ts`, consumido pelos
dois componentes (cadastro e edição).

**Rationale**: Ponto em aberto já registrado no refinamento de frontend
("extraí-las para um utilitário compartilhado em vez de duplicá-las").
Duplicar essas ~4 funções puras no novo componente violaria o Princípio V
(DRY não é uma abstração especulativa aqui — as duas telas já precisam
exatamente das mesmas regras hoje, não uma necessidade futura hipotética).

**Alternatives considered**: Duplicar as funções dentro de
`EditarDespesaRecorrenteComponent` — rejeitado, gera duas fontes de
verdade para a mesma regra de validação de campo (risco real: um ajuste
de regra futuro esquecido em uma das duas cópias).

## Decisão 3 — Ponto de entrada: ícone de ação por item no painel mensal

**Decision**: Um botão/ícone de ação dedicado (lápis) por item da lista do
painel mensal, ao lado das ações já existentes (marcar como
paga/desfazer), navegando para `despesas/:id/editar` com o `id` da despesa
recorrente dona do item.

**Rationale**: Já resolvido por clarificação na spec 008 (FR-002) e já
implementado no design de referência (`design/Main.dc.html`). Mantém a
ação de editar separada das ações de pagamento (que são sobre a
ocorrência, não sobre a despesa recorrente) — RF09 do refinamento
funcional trata os dois conceitos como independentes.

**Alternatives considered**: Link de texto junto ao nome; linha inteira
clicável — ambos descartados pela clarificação da spec 008 (opção A
escolhida explicitamente).

## Decisão 4 — Comparação de "alterado" por valor, não por interação

**Decision**: `hasUnsavedData`/`isDirty` compara cada campo atual contra o
valor capturado no momento do carregamento (`initialSnapshot`), não uma
flag "algum campo já foi tocado alguma vez".

**Rationale**: Clarificado na spec 008 (FR-013/FR-014) e já implementado
no design de referência (`design/Editar.dc.html`, método `isDirty`). Evita
que reverter manualmente um campo ao valor original ainda dispare a
confirmação de saída — menos atrito percebido, sem custo de implementação
adicional (a comparação por valor já é necessária de qualquer forma para
alimentar a pré-visualização em tempo real).

**Alternatives considered**: Uma flag booleana "algum campo foi editado
nesta sessão" — rejeitada por gerar falsos positivos (usuário digita e
desfaz, ainda assim veria a confirmação de saída).

## Decisão 5 (bloqueante) — Identificador da despesa recorrente ausente no payload do painel mensal

**Decision**: Antes da tarefa de integração do ícone de edição
(`/speckit-tasks`), `GetMonthlyPanelUseCase`/`PanelOccurrenceData`/
`PanelOccurrenceDataResponse` precisam passar a expor o identificador da
despesa recorrente dona de cada ocorrência (ex.: campo
`RecurringExpenseId` / `recurringExpenseId`), além do `Id` da própria
ocorrência que já existe.

**Rationale**: Confirmado lendo o código atual
(`backend/Application/UseCases/GetMonthlyPanel/GetMonthlyPanelUseCase.cs`,
linhas 54-65): a projeção usa `expense.GetOccurrencesForPeriod(...)` mas
descarta `expense.GetId()` no caminho. Sem esse dado, o ícone de edição
por item (FR-002) não tem para qual despesa recorrente navegar — é uma
dependência bloqueante de dados, não uma escolha de UX.

**Alternatives considered**:
- Deduzir a despesa recorrente a partir de nome+categoria — rejeitado,
  nada garante unicidade e é uma gambiarra que reintroduz a mesma regra
  de negócio (correspondência de identidade) que a API já deveria
  resolver.
- Fazer o frontend chamar um novo endpoint de busca por
  nome/competência para descobrir o id — rejeitado, mais complexo e mais
  lento que simplesmente incluir um campo já disponível no domínio
  (`RecurringExpense.GetId()`) na mesma resposta que já existe.
- Resolvida como: pequena adição de campo na projeção já existente
  (`GetMonthlyPanelUseCase`), sem novo UseCase nem novo endpoint —
  correção mínima, não uma mudança arquitetural.

**Status**: Não resolvida nesta feature (fora de escopo — pertence à
camada de API/Application do backend, spec `006-painel-mensal-despesas`
ou um ajuste pontual antes de `007`); registrada aqui para que
`/speckit-tasks` inclua essa correção como pré-requisito da tarefa do
ícone de edição, e para que a spec 008 não seja implementada como se essa
lacuna já estivesse fechada.
