---

description: "Task list template for feature implementation"
---

# Tasks: Excluir Despesa Recorrente (Tela)

**Input**: Design documents from `/specs/011-delete-recurring-expense-ui/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/delete-screen-contract.md, quickstart.md (all present)

**Tests**: Incluídos — a spec (Princípio II, Constitution Check) e o `plan.md` exigem testes Vitest para o novo método de serviço e para cada estado do modal, seguindo o padrão já usado por `painel-mensal-despesas.component.spec.ts`/`despesa-recorrente.service.spec.ts`.

**Organization**: Tarefas agrupadas pelas user stories de `spec.md` (US1 P1, US2 P2, US3 P1, US4 P3), para permitir implementação e teste independentes de cada uma.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivo diferente, sem dependência de tarefa incompleta)
- **[Story]**: US1/US2/US3/US4, conforme `spec.md`
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (Option 2 do `plan.md`) — toda mudança de produção fica em
`frontend/src/app/features/`; nenhuma pasta nova.

---

## Phase 1: Setup

**Purpose**: Atualizar o protótipo visual de referência antes da implementação Angular final (ver Assumptions do `spec.md` e Project Structure do `plan.md`).

- [X] T001 [P] Em `design/Main.dc.html`, substituir a confirmação inline de exclusão hoje presente por um modal (overlay) com as duas abrangências mutuamente exclusivas ("apenas pendentes/vencidas", pré-selecionada, e "excluir todo o histórico"), cada uma com a frase explicando o efeito sobre ocorrências pagas (FR-004, FR-005), para servir de referência visual fiel à implementação Angular das fases seguintes. Arquivo fora do código de produção; não bloqueia nenhuma tarefa abaixo.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Tipos e método de serviço que todas as user stories consomem. Nenhuma tarefa de UI (Fase 3+) pode ser concluída sem isto.

**⚠️ CRITICAL**: Bloqueia todas as user stories.

- [X] T002 [P] Adicionar `DeleteScope` (`'pending-only' | 'all'`) e `DeleteRecurringExpenseResponse` (`{ recurringExpenseId: string; scope: DeleteScope }`) em `frontend/src/app/features/despesa-recorrente/despesa-recorrente.model.ts`
- [X] T003 [P] Reexportar `DeleteScope` e `DeleteRecurringExpenseResponse` de `despesa-recorrente.model.ts` em `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.model.ts` (mesmo padrão já usado para `ApiEnvelope`/`CategoryValue`)
- [X] T004 Adicionar `delete(id: string, scope: DeleteScope): Observable<ApiEnvelope<DeleteRecurringExpenseResponse>>` em `frontend/src/app/features/despesa-recorrente/despesa-recorrente.service.ts`, chamando `DELETE /api/v1/recurring-expenses/{id}` com `scope` via `HttpParams` na query string (depende de T002)
- [X] T005 Em `frontend/src/app/features/despesa-recorrente/despesa-recorrente.service.spec.ts`, adicionar testes de `delete()`: sucesso com `scope=pending-only` e com `scope=all` (verificando `req.request.params.get('scope')` e `req.request.method === 'DELETE'`), `400` e `404` e falha de rede surfaced as-is ao chamador (mesmo padrão já usado pelos testes de `update()`/`getById()`) (depende de T004)

**Checkpoint**: Serviço e tipos prontos — a UI de qualquer user story pode começar.

---

## Phase 3: User Story 3 - Confirmar antes de excluir, com a abrangência sempre visível (Priority: P1)

**Goal**: Botão de lixeira por item abre um modal com as duas abrangências visíveis (pendentes/vencidas pré-selecionada), e nenhuma exclusão ocorre sem confirmação explícita; cancelar não altera nada.

**Independent Test**: Clicar no ícone de lixeira de um item e verificar que o modal abre com a abrangência "apenas pendentes/vencidas" já selecionada e nenhuma chamada HTTP é feita; clicar em cancelar e verificar que o modal fecha sem nenhuma chamada e sem nenhuma mudança na lista.

### Implementation for User Story 3

- [X] T006 [P] [US3] Em `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.html`, adicionar um botão de ação (ícone de lixeira, `data-testid="excluir-btn"`, `aria-label`/`title` "Excluir despesa recorrente") ao lado do `editar-btn` já existente em cada `occurrence-row`, usando `item.recurringExpenseId` (FR-001, FR-002)
- [X] T007 [P] [US3] Em `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.ts`, adicionar os signals `deleteTargetId = signal<string | null>(null)` e `deleteScope = signal<DeleteScope>('pending-only')`, o computed `isDeleteModalOpen = computed(() => this.deleteTargetId() !== null)`, e os métodos `onExcluirClick(recurringExpenseId: string)` (seta `deleteTargetId` e reinicia `deleteScope` para `'pending-only'`, FR-005) e `onCancelarExclusao()` (seta `deleteTargetId` para `null`, FR-006), injetando `DespesaRecorrenteService`
- [X] T008 [US3] Em `painel-mensal-despesas.component.html`, adicionar o modal de confirmação (`@if (isDeleteModalOpen())`, overlay `fixed inset-0`, `role="alertdialog" aria-modal="true" aria-labelledby`, mesmo padrão visual do diálogo "Sair sem salvar" de `editar-despesa-recorrente.component.html`), com um grupo de dois rádios mutuamente exclusivos ("apenas pendentes/vencidas" e "excluir todo o histórico"), cada um com a frase explicando o efeito sobre ocorrências pagas (FR-004), e um botão de cancelar ligado a `onCancelarExclusao()` (depende de T006, T007)
- [X] T009 [US3] Em `painel-mensal-despesas.component.ts`, adicionar `onDeleteScopeChange(scope: DeleteScope)` (seta `deleteScope`) e ligar os dois rádios do modal (em `painel-mensal-despesas.component.html`) a esse handler, permitindo trocar a seleção antes de confirmar (FR-005) (depende de T007, T008)
- [X] T010 [US3] Em `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.spec.ts`, adicionar testes: clicar no `excluir-btn` de um item abre o modal com "apenas pendentes/vencidas" pré-selecionada e usa `item.recurringExpenseId` (não `item.id`) como alvo; trocar a seleção para "excluir todo o histórico" reflete no estado; clicar em cancelar fecha o modal sem nenhuma requisição HTTP adicional e sem alterar a lista (depende de T006-T009)

**Checkpoint**: Botão + modal funcionam de ponta a ponta para abrir, trocar abrangência e cancelar — nenhuma exclusão real ainda ocorre.

---

## Phase 4: User Story 1 - Excluir uma despesa recorrente preservando o histórico pago (Priority: P1) 🎯 MVP (junto com US3)

**Goal**: Confirmar com a abrangência padrão ("apenas pendentes/vencidas") chama o `DELETE` com esse `scope`, fecha o modal em caso de sucesso e atualiza a lista/totais do painel sem recarregar a página, preservando ocorrências já pagas.

**Independent Test**: Excluir uma despesa recorrente com uma ocorrência paga e uma pendente, confirmando com a abrangência padrão, e verificar que a ocorrência pendente desaparece da lista enquanto a paga permanece.

### Implementation for User Story 1

- [X] T011 [US1] Em `painel-mensal-despesas.component.ts`, adicionar os signals `isDeleting = signal(false)` e `deleteErrorMessage = signal<string | null>(null)`, e o computed `canConfirmDelete = computed(() => !this.isDeleting())` (FR-009) (depende de T007)
- [X] T012 [US1] Em `painel-mensal-despesas.component.ts`, extrair um método privado `recarregarPainelAtual()` que refaz `PainelMensalDespesasService.getMonthlyPanel()` para a competência atualmente em `referencePeriod()` (reaproveitando a lógica já usada por `loadPeriod`/`load`), para ser chamado após uma exclusão bem-sucedida (Decisão 4 de `research.md` — sem remoção local otimista)
- [X] T013 [US1] Em `painel-mensal-despesas.component.ts`, implementar `onConfirmarExcluir()`: se `!canConfirmDelete()` retorna; senão seta `isDeleting.set(true)`, chama `despesaRecorrenteService.delete(deleteTargetId()!, deleteScope())`, e no sucesso (`200`) seta `isDeleting.set(false)`, `deleteTargetId.set(null)` e chama `recarregarPainelAtual()` (FR-007, FR-008, FR-010) (depende de T011, T012)
- [X] T014 [US1] Em `painel-mensal-despesas.component.html`, adicionar o botão de confirmar no modal (`data-testid="confirmar-excluir-btn"`), ligado a `onConfirmarExcluir()`, desabilitado com rótulo de carregamento (ex.: "Excluindo…") enquanto `isDeleting()` (FR-009) (depende de T008, T013)
- [X] T015 [US1] [P] Em `painel-mensal-despesas.component.spec.ts`, adicionar testes: confirmar com a abrangência padrão envia `DELETE /api/v1/recurring-expenses/{recurringExpenseId}?scope=pending-only`; enquanto a resposta não chega, o botão de confirmar fica desabilitado com rótulo de carregamento; na resposta de sucesso, o modal fecha e uma nova requisição a `/api/v1/occurrences` é disparada (recarregamento), refletindo a lista/totais atualizados (depende de T011-T014)

**Checkpoint**: Exclusão com a abrangência segura funciona de ponta a ponta (User Stories 1 e 3 completas).

---

## Phase 5: User Story 2 - Excluir uma despesa recorrente e todo o seu histórico (Priority: P2)

**Goal**: Confirmar com a abrangência "excluir todo o histórico" (após trocar deliberadamente a seleção) resulta em nenhuma ocorrência dessa despesa continuar acessível em nenhuma competência.

**Independent Test**: Trocar a seleção para "excluir todo o histórico", confirmar, e verificar que o `DELETE` foi enviado com `scope=all` e que o painel foi recarregado.

### Implementation for User Story 2

> Nenhuma lógica nova é necessária em `onConfirmarExcluir()` — ele já lê `deleteScope()` (T013) e já foi possível trocar a seleção para `'all'` desde T009; o efeito de preservar/remover ocorrências pagas é decidido pelo backend (`010-delete-recurring-expense`, Decisão 4 de `research.md`), não pelo frontend.

- [X] T016 [US2] [P] Em `painel-mensal-despesas.component.spec.ts`, adicionar teste: trocar a seleção para "excluir todo o histórico" e confirmar envia `DELETE /api/v1/recurring-expenses/{recurringExpenseId}?scope=all`; na resposta de sucesso, o modal fecha e o painel é recarregado, do mesmo jeito que com `pending-only` (depende de T009, T013)

**Checkpoint**: As duas abrangências (User Stories 1 e 2) funcionam de ponta a ponta.

---

## Phase 6: User Story 4 - Ser informado com clareza quando a exclusão falha (Priority: P3)

**Goal**: Falha de rede/servidor mantém o modal aberto com mensagem de erro e a abrangência escolhida preservada (FR-011); um `404` (já excluída por outra aba) é tratado como sucesso — fecha o modal e recarrega a lista (FR-012).

**Independent Test**: Simular uma falha de rede no `DELETE` e verificar que o modal permanece aberto com mensagem de erro e a abrangência preservada; separadamente, simular um `404` e verificar que o modal fecha e a lista é recarregada sem mensagem de erro.

### Implementation for User Story 4

- [X] T017 [US4] Em `painel-mensal-despesas.component.ts`, tratar o `error` de `onConfirmarExcluir()`: se o erro for `HttpErrorResponse` com `status === 404`, aplicar o mesmo caminho do sucesso (`isDeleting.set(false)`, `deleteTargetId.set(null)`, `recarregarPainelAtual()`, sem preencher `deleteErrorMessage`) (FR-012, Decisão 5 de `research.md`); para qualquer outro erro, `isDeleting.set(false)` e `deleteErrorMessage.set('<mensagem genérica>')`, preservando `deleteTargetId`/`deleteScope` (FR-011) (depende de T013)
- [X] T018 [US4] Em `painel-mensal-despesas.component.ts`, limpar `deleteErrorMessage.set(null)` em `onExcluirClick()`, `onCancelarExclusao()` e `onDeleteScopeChange()`, para que uma mensagem de erro anterior nunca apareça num novo ciclo de confirmação (depende de T009, T011, T017)
- [X] T019 [US4] Em `painel-mensal-despesas.component.html`, adicionar um banner de erro dentro do modal (`@if (deleteErrorMessage())`, `role="alert"`, mesmo padrão visual de `erro-banner` já usado em `editar-despesa-recorrente.component.html`), exibindo `deleteErrorMessage()` e permitindo tentar novamente (botão de confirmar já existente) ou cancelar (depende de T008, T017)
- [X] T020 [US4] [P] Em `painel-mensal-despesas.component.spec.ts`, adicionar testes: falha de rede/`5xx` no `DELETE` mantém o modal aberto com uma mensagem de erro visível e a abrangência selecionada preservada, permitindo confirmar de novo; um `404` fecha o modal e dispara o recarregamento do painel sem exibir mensagem de erro (depende de T017-T019)

**Checkpoint**: Todas as quatro user stories funcionam de ponta a ponta, incluindo os caminhos de erro.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verificação final de acessibilidade e validação manual end-to-end.

- [X] T021 [P] Verificar acessibilidade WCAG 2.1 AA do modal de exclusão (Princípio II): foco movido para o modal ao abrir, `role="alertdialog"`/`aria-modal="true"`/`aria-labelledby` presentes, os dois rádios têm rótulos associados e são operáveis por teclado (Tab/Space/Enter), em `painel-mensal-despesas.component.html`/`.spec.ts`
- [X] T022 Executar manualmente os 5 cenários de `specs/011-delete-recurring-expense-ui/quickstart.md` (`dotnet run` no backend + `npm start` no frontend) para validar o fluxo de ponta a ponta antes de considerar a feature concluída

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Fase 1)**: Sem dependências — `design/Main.dc.html` é um arquivo isolado, pode ser feito em qualquer momento.
- **Foundational (Fase 2)**: Sem dependência do Setup — BLOQUEIA todas as user stories (Fases 3-6).
- **User Stories (Fases 3-6)**: Todas dependem da Fase 2 completa.
  - US3 (Fase 3) é pré-requisito prático de US1/US2/US4 (botão + modal precisam existir antes de confirmar/errar) — implementar nesta ordem: US3 → US1 → US2 → US4.
- **Polish (Fase 7)**: Depende de todas as user stories desejadas estarem completas.

### User Story Dependencies

- **US3 (P1)**: Depende apenas da Fase 2 (Foundational). Estabelece o botão e o modal que US1/US2/US4 estendem.
- **US1 (P1)**: Depende de US3 (botão/modal já existentes) + Fase 2.
- **US2 (P2)**: Depende de US3 (troca de abrangência) e US1 (`onConfirmarExcluir()` já implementado) — não adiciona lógica nova, só exercita o caminho já genérico com `scope='all'`.
- **US4 (P3)**: Depende de US1 (`onConfirmarExcluir()` já implementado, para adicionar o tratamento de erro/404 a ele).

### Parallel Opportunities

- T002 e T003 (Fase 2) podem rodar em paralelo (arquivos diferentes).
- T006 e T007 (Fase 3) podem rodar em paralelo (arquivos diferentes: `.html` vs `.ts`).
- T001 (Setup) pode rodar em paralelo com qualquer outra fase — não compartilha arquivo com nenhuma tarefa de produção.
- Dentro de cada fase, a tarefa de teste `[P]` pode ser escrita em paralelo com a leitura das tarefas de implementação anteriores, mas só passa depois que elas estiverem prontas.

---

## Parallel Example: Foundational

```bash
Task: "Adicionar DeleteScope e DeleteRecurringExpenseResponse em despesa-recorrente.model.ts"
Task: "Reexportar DeleteScope e DeleteRecurringExpenseResponse em painel-mensal-despesas.model.ts"
```

## Parallel Example: User Story 3

```bash
Task: "Adicionar botão de lixeira por item em painel-mensal-despesas.component.html"
Task: "Adicionar signals deleteTargetId/deleteScope e handlers de abrir/cancelar em painel-mensal-despesas.component.ts"
```

---

## Implementation Strategy

### MVP First (User Stories 3 + 1 — ambas P1)

1. Completar Fase 1: Setup (pode ser feito em paralelo com o resto)
2. Completar Fase 2: Foundational (CRITICAL — bloqueia tudo)
3. Completar Fase 3: US3 (botão + modal + cancelar)
4. Completar Fase 4: US1 (confirmar com abrangência padrão, sucesso)
5. **PARAR e VALIDAR**: testar manualmente o Cenário 1 do `quickstart.md`
6. Deploy/demo se pronto — já é um MVP seguro e completo (exclusão com preservação de histórico)

### Incremental Delivery

1. Setup + Foundational → base pronta
2. US3 → botão/modal funcionam, nada é excluído ainda sem confirmação → já testável isoladamente
3. US1 → exclusão segura (padrão) funciona de ponta a ponta → **MVP**
4. US2 → exclusão completa (todo o histórico) funciona → feature funcionalmente completa (FR-001–FR-008, FR-013)
5. US4 → robustez a falhas de rede/404 → feature completa (todos os FRs)
6. Polish → acessibilidade + validação manual end-to-end

---

## Notes

- Nenhum componente novo é criado (Decisão 1 de `research.md`) — todo o HTML/TS fica em `painel-mensal-despesas.component.{ts,html}` já existente.
- `delete()` entra em `DespesaRecorrenteService`, não em `PainelMensalDespesasService` (Decisão 3 de `research.md`) — agrupamento por recurso afetado (`/recurring-expenses/{id}`), não por tela que chama.
- Nenhuma remoção local otimista: toda atualização pós-exclusão busca de novo o painel da competência exibida (Decisão 4 de `research.md`).
- `404` na exclusão segue o caminho do sucesso, não do erro (Decisão 5 de `research.md`, FR-012 vs. FR-011).
- Commit após cada tarefa ou grupo lógico; parar em cada checkpoint para validar a user story isoladamente antes de seguir.
