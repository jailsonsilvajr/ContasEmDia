# Implementation Plan: Excluir Despesa Recorrente (Tela)

**Branch**: `011-delete-recurring-expense-ui` | **Date**: 2026-10-31 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-delete-recurring-expense-ui/spec.md`

## Summary

Adicionar, a cada item da listagem do Painel Mensal de Despesas, um botão
de exclusão (ícone de lixeira) que abre um modal de confirmação com duas
abrangências mutuamente exclusivas — "apenas pendentes/vencidas"
(pré-selecionada) e "excluir todo o histórico" — cada uma com uma frase
explicando o efeito sobre ocorrências já pagas (FR-001–FR-005). Confirmar
chama `DELETE /api/v1/recurring-expenses/{id}?scope=...`, já implementado
e consumível pelo backend (`010-delete-recurring-expense`); sucesso fecha
o modal e busca novamente o painel da competência exibida (FR-010), erro
de rede/servidor mantém o modal aberto com mensagem e abrangência
preservadas (FR-011), e um `404` (já excluída por outra aba) fecha o modal
e atualiza a lista do mesmo jeito que um sucesso (FR-012).

**Abordagem técnica**: nenhum componente novo — o botão, o modal e todo o
estado de exclusão vivem dentro do `PainelMensalDespesasComponent` já
existente (mesmo padrão já usado ali para o fluxo de pagamento, e mesmo
padrão de modal/overlay já usado por `EditarDespesaRecorrenteComponent`
para "Sair sem salvar"). Um novo método `delete(id, scope)` em
`DespesaRecorrenteService`, ao lado de `getById`/`update` já existentes
(mesmo agregado, mesma rota base). Diferente da feature `008` (edição),
`recurringExpenseId` já está presente no payload do painel mensal desde
aquela correção — **nenhuma dependência cross-feature bloqueante nesta
etapa**.

## Technical Context

**Language/Version**: TypeScript ~6.0.2 (Angular 22.1, `strict: true`)

**Primary Dependencies**: Angular 22 (standalone components, Signals,
`HttpClient`), RxJS 7.8, Tailwind CSS 4 — todas já presentes em
`frontend/package.json`; nenhuma dependência nova é necessária.

**Storage**: N/A — feature é somente frontend; consome
`DELETE /api/v1/recurring-expenses/{id}?scope={pending-only|all}`, já
implementado pelo backend (`010-delete-recurring-expense`,
`DeleteRecurringExpenseUseCase`/`RecurringExpensesController.Delete`).

**Testing**: Vitest 4 (`@vitest/coverage-v8`, `jsdom`), no mesmo padrão já
usado por `painel-mensal-despesas.component.spec.ts` e
`painel-mensal-despesas.service.spec.ts` (component harness +
`HttpTestingController` via `provideHttpClientTesting()`).

**Target Platform**: Navegador web (desktop e mobile responsivo), servido
pela SPA Angular já existente.

**Project Type**: Web application (frontend Angular + backend .NET já
existentes) — esta feature toca somente `frontend/`; o backend
(`010-delete-recurring-expense`) já está implementado e fora de escopo
desta spec.

**Performance Goals**: Padrão de responsividade de SPA já estabelecido
pelo projeto; nenhuma meta numérica específica (spec não define nenhuma).

**Constraints**: WCAG 2.1 AA no novo modal (Princípio II) — foco, `role`,
rótulos, operabilidade por teclado, seguindo o mesmo padrão
`role="alertdialog" aria-modal="true"` já usado pelo diálogo de "Sair sem
salvar"; TS estrito sem `any` (Princípio III); nenhuma nova dependência
npm sem justificativa em plano (AI Agent Guardrails).

**Scale/Scope**: Um botão novo por item de lista, um modal novo (inline no
componente já existente), um método novo num serviço já existente, dois
tipos novos de modelo — sem novos pacotes, sem componente novo, sem mudança
de arquitetura.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. API-First Backend/Frontend Separation | PASS — a tela consome exclusivamente `HttpClient` contra `DELETE /api/v1/recurring-expenses/{id}?scope=...`, endpoint já contratado por `specs/010-delete-recurring-expense/contracts/api-contract.md`; nenhum acesso direto a dados. |
| II. Test-First Development | PASS (a garantir em `/speckit-tasks`) — testes Vitest para o novo método de serviço (`HttpTestingController`, sucesso/`400`/`404`/falha de rede para as duas abrangências) e para cada estado do modal (fechado, aberto com cada abrangência, carregando, erro, sucesso), seguindo o padrão já usado por `painel-mensal-despesas.component.spec.ts`; acessibilidade WCAG 2.1 AA verificada no modal (foco, `aria-modal`, rótulos dos dois rádios). |
| III. Type Safety & Static Analysis | PASS — TypeScript estrito, sem `any`; nenhuma supressão de lint prevista. |
| IV. Secure Handling of Financial Data | PASS — herda a exceção de fase já vigente (sem autenticação/CORS ainda); nenhum dado sensível novo introduzido. |
| V. Simplicity & Incremental Delivery | PASS — reaproveita o padrão de modal/overlay já existente (não cria um componente de modal genérico sem segundo uso real); estado de exclusão vive no componente que já tem o botão que o aciona, mesmo padrão já usado ali para o fluxo de pagamento inline — nenhuma abstração nova especulativa. |
| VI. DDD no Domain Layer | N/A — nenhuma mudança de domínio nesta spec (já entregue por `010-delete-recurring-expense`). |
| VII. Infrastructure Layer | N/A — nenhuma mudança de persistência nesta spec. |
| VIII. Angular Standalone Architecture & Estrutura | PASS — altera apenas um componente standalone já existente (`PainelMensalDespesasComponent`), organizado em `features/painel-mensal-despesas/`; Tailwind para estilo do modal, seguindo o padrão visual já usado por "Sair sem salvar". |
| IX. Signal-Based Reactivity & HTTP Access | PASS — estado do modal em `signal`; `HttpClient` injetado via `inject()` em `DespesaRecorrenteService` (`providedIn: 'root'`), nunca instanciado no componente. |
| X. Dependency Injection & Frontend Coding Standards | PASS — `inject()` em vez de injeção por construtor; nomes `*.component.ts`/`*.service.ts`; nenhuma mutação em `input()`/estado de sinal fora de `set`/`update`. |
| XI. Application Layer (backend) | N/A — fora de escopo; `DeleteRecurringExpenseUseCase` já entregue por `010-delete-recurring-expense`. |
| XII. API Layer (backend) | N/A — fora de escopo; `DELETE /api/v1/recurring-expenses/{id}` já entregue e documentado por `010-delete-recurring-expense`, apenas consumido aqui. |

**Nenhuma violação identificada.** Nenhuma dependência cross-feature
bloqueante (diferente de `008`): `recurringExpenseId` já está presente em
`PanelOccurrenceResponse`, e o endpoint de exclusão já existe e está
implementado.

## Project Structure

### Documentation (this feature)

```text
specs/011-delete-recurring-expense-ui/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── delete-screen-contract.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
# Option 2: Web application (frontend Angular + backend .NET já existentes)

frontend/src/app/features/painel-mensal-despesas/
├── painel-mensal-despesas.component.ts      # ALTERADO: + estado/handlers de exclusão (botão, modal, abrangência, loading, erro)
├── painel-mensal-despesas.component.html    # ALTERADO: + botão de lixeira por item, + modal de confirmação com as duas abrangências
├── painel-mensal-despesas.component.spec.ts # ALTERADO: + testes dos novos estados/handlers
└── painel-mensal-despesas.model.ts          # ALTERADO: + DeleteRecurringExpenseResponse, DeleteScope (reexportados de despesa-recorrente.model.ts, mesmo padrão já usado para ApiEnvelope/CategoryValue)

frontend/src/app/features/despesa-recorrente/
├── despesa-recorrente.service.ts            # ALTERADO: + delete(id, scope)
├── despesa-recorrente.service.spec.ts       # ALTERADO: + testes de delete() (sucesso x2 abrangências, 400, 404, falha de rede)
└── despesa-recorrente.model.ts              # ALTERADO: + DeleteRecurringExpenseResponse, DeleteScope

design/Main.dc.html                          # ALTERADO (fora do código de produção): protótipo precisa trocar a confirmação inline hoje presente (ver Assumptions do spec.md) por um modal com as duas abrangências, para servir de referência visual fiel antes da implementação do HTML Angular final
```

**Structure Decision**: Reaproveita a estrutura Option 2 (web application)
já existente; nenhuma pasta nova, nenhum componente novo, nenhum endpoint
novo. Toda a mudança de produção fica dentro de `frontend/src/app/`, em
dois arquivos de feature já existentes mais o serviço/modelo compartilhado
já usado por eles — consistente com o Princípio V (nenhuma abstração nova
sem necessidade concreta e presente).

## Complexity Tracking

Nenhuma violação de princípio foi identificada nesta etapa — tabela
omitida intencionalmente (nada a justificar).
