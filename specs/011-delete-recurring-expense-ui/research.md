# Research: Excluir Despesa Recorrente (Tela)

Nenhum item do "Technical Context" ficou marcado como `NEEDS
CLARIFICATION` — todas as escolhas técnicas já são convenções vigentes do
projeto (Angular 22 standalone, Signals, Vitest, Tailwind). As decisões
abaixo cobrem as escolhas de desenho que o refinamento de frontend e a
spec 011 (já clarificada) deixavam abertas para esta etapa de
planejamento.

## Decisão 1 — Modal inline no componente do painel, não um componente de modal genérico

**Decision**: Implementar o modal de confirmação de exclusão como um
bloco adicional no template/estado de `PainelMensalDespesasComponent` já
existente, em vez de extrair um `ConfirmModalComponent` reutilizável.

**Rationale**: Hoje existe exatamente um outro modal no projeto — "Sair
sem salvar?", em `EditarDespesaRecorrenteComponent` — e ele também está
implementado inline no componente que o usa, não como um componente
compartilhado. Extrair uma abstração de modal genérica agora serviria
apenas dois usos com formas de conteúdo bem diferentes (texto simples vs.
duas opções de abrangência com rádio), sem uma terceira necessidade
concreta hoje — violaria o Princípio V (YAGNI). Repete a mesma decisão já
tomada implicitamente pelo padrão existente.

**Alternatives considered**: Um `ConfirmModalComponent` compartilhado,
parametrizado por conteúdo via `ng-content`/`@Input` — rejeitado por ser
uma generalização especulativa; os dois modais existentes diferem o
suficiente (um é puramente textual, o outro precisa de um grupo de rádio
com estado próprio) que a abstração ganharia pouco em reuso e perderia em
simplicidade de leitura de cada componente isoladamente.

## Decisão 2 — Estado de exclusão vive no `PainelMensalDespesasComponent`, não num serviço/store dedicado

**Decision**: `deleteTargetId`, `deleteScope`, `isDeleting` e
`deleteErrorMessage` são `signal`s locais do componente do painel, no
mesmo nível dos já existentes `editingOccurrenceId`/`draftValor` do fluxo
de pagamento.

**Rationale**: Mesmo padrão já estabelecido no próprio componente para o
fluxo de "marcar como paga" (estado local por item ativo, sem store
global). A exclusão é acionada e encerrada na mesma tela, sem necessidade
de compartilhar esse estado com nenhum outro componente.

**Alternatives considered**: Nenhuma considerada seriamente — introduzir
um serviço de estado só para isto duplicaria, sem motivo, um padrão que o
próprio componente já resolve localmente para um caso análogo.

## Decisão 3 — `delete()` entra em `DespesaRecorrenteService`, não em `PainelMensalDespesasService`

**Decision**: O novo método `delete(id, scope)` é adicionado a
`DespesaRecorrenteService` (onde já vivem `getById`/`update`, ambos sobre
`/api/v1/recurring-expenses/{id}`), não a `PainelMensalDespesasService`
(que chama `/api/v1/occurrences` e `/api/v1/occurrences/{id}/payment`).

**Rationale**: O endpoint (`/api/v1/recurring-expenses/{id}`) e o recurso
que ele afeta (a despesa recorrente, não uma ocorrência — FR-002) são
exatamente os mesmos que `getById`/`update` já tratam neste serviço;
agrupar por recurso consumido, como o projeto já faz, evita duplicar a
URL base em dois serviços diferentes. `PainelMensalDespesasComponent`
simplesmente injeta `DespesaRecorrenteService` além do
`PainelMensalDespesasService` que já injeta — nenhuma duplicação cruzada,
já que a UI que aciona a exclusão (painel) não é o mesmo limite que o
recurso que ela afeta (despesa recorrente).

**Alternatives considered**: Adicionar `delete()` a
`PainelMensalDespesasService` por ser o serviço já injetado na tela que
aciona a ação — rejeitado, pois agruparia chamadas por *tela que chama*
em vez de por *recurso afetado*, divergindo do critério já usado para
`getById`/`update` (que vivem em `DespesaRecorrenteService` mesmo sendo
chamados por `EditarDespesaRecorrenteComponent`, uma tela diferente de
onde o recurso foi originalmente criado).

## Decisão 4 — Após sucesso, buscar novamente o painel da competência exibida (sem remoção local otimista)

**Decision**: Em qualquer exclusão bem-sucedida (ambas as abrangências) ou
em um `404` (já excluída por outra aba), chamar de novo
`PainelMensalDespesasService.getMonthlyPanel()` para a competência
atualmente exibida, em vez de filtrar `occurrences()` localmente.

**Rationale**: Já fixado nas Assumptions do `spec.md` — a regra de
preservação de ocorrências pagas na abrangência "apenas
pendentes/vencidas" (FR-007) é avaliada pelo backend; buscar de novo
garante que o Painel reflita exatamente o que o backend decidiu manter,
sem duplicar essa regra no frontend (ex.: "quais ocorrências desta
despesa, na competência atual, eram pendentes vs. pagas" já é uma decisão
do backend, não algo que o frontend deveria re-derivar para decidir o que
remover da lista local).

**Alternatives considered**: Remoção local otimista (filtrar
`occurrences()` pelo `recurringExpenseId` segundo a abrangência escolhida)
— rejeitada explicitamente pelas Assumptions do spec, pelo motivo acima;
também introduziria um segundo lugar (frontend) decidindo o efeito de
FR-007, com risco real de a UI mostrar algo diferente do que o backend
persistiu caso as regras um dia divirjam.

## Decisão 5 — Tratamento de `404` na exclusão: mesmo caminho do sucesso, não do erro

**Decision**: Um `404` do `DELETE` (FR-012 — despesa já excluída por outra
aba/sessão) fecha o modal e recarrega o painel, exatamente como um
sucesso — não ativa o banner de erro usado para falha de rede/5xx
(FR-011).

**Rationale**: Já definido explicitamente pela spec (FR-012 vs. FR-011
são requisitos distintos com efeitos diferentes) e pelo contrato do
backend (`010-delete-recurring-expense`, que documenta `404` como "já não
existe", nunca como uma falha transitória). Diferente do `PUT` consumido
por `008` (onde um `404` nesse ponto era tratado como a mesma classe de
erro genérico, por ser um caso que a User Story daquela tela não
distinguia visualmente), aqui a própria spec exige um tratamento visual
diferente para os dois casos (FR-011 vs. FR-012).

**Alternatives considered**: Tratar `404` como qualquer outro erro
(banner + manter modal aberto) — rejeitado, pois FR-012 exige
explicitamente fechar o modal e atualizar a lista, não manter uma
confirmação pendindo sobre algo que já não existe mais no servidor.

## Decisão 6 — Nenhuma dependência cross-feature bloqueante (diferente de `008`)

**Decision**: Nenhuma alteração de backend é necessária antes da tarefa de
integração do botão de exclusão.

**Rationale**: Confirmado lendo o estado atual do código:
`PanelOccurrenceResponse` (`painel-mensal-despesas.model.ts`) já expõe
`recurringExpenseId` — a lacuna que bloqueou `008` já foi corrigida em
etapa anterior. O endpoint `DELETE /api/v1/recurring-expenses/{id}` já
está implementado (`RecurringExpensesController.Delete`,
`DeleteRecurringExpenseUseCase`, `DeleteRecurringExpenseDataResponse`),
conforme `specs/010-delete-recurring-expense/contracts/api-contract.md`.

**Alternatives considered**: N/A — apenas uma verificação, não uma
escolha de design.
