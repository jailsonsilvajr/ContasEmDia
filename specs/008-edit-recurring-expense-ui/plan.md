# Implementation Plan: Editar Despesa Recorrente (Tela)

**Branch**: `008-edit-recurring-expense-ui` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-edit-recurring-expense-ui/spec.md`

## Summary

Adicionar uma tela Angular dedicada para editar os dados de uma despesa
recorrente já cadastrada (nome, categoria, valor previsto mensal, dia de
vencimento, data de início, status, observação), acessível por um ícone de
ação por item na listagem do painel mensal (FR-002, clarificado). A tela
carrega os dados atuais via `GET`, valida e salva alterações via `PUT`,
com estados de carregamento/não-encontrada/erro/sucesso, e pede confirmação
antes de descartar alterações não salvas (comparação por valor, conforme
clarificado).

A implementação **segue fielmente o design já produzido nesta iniciativa**
(`design/Editar.dc.html` e o ícone de edição já adicionado a
`design/Main.dc.html`): estrutura visual, textos, estados e ordem dos
campos do HTML final devem corresponder ao protótipo — cores, tipografia,
rótulos de botão ("Salvar alterações", "Salvando…"), textos de estado
(não encontrada, erro ao carregar/salvar, aviso de reativação persistente)
e o leiaute de duas colunas (formulário + pré-visualização) vêm direto
dele, adaptados de HTML+CSS inline para o padrão Tailwind + template
Angular já usado por `CadastroDespesaRecorrenteComponent`.

**Abordagem técnica**: novo componente standalone
`EditarDespesaRecorrenteComponent`, reaproveitando `DespesaPreviewComponent`
sem alterações e extraindo as funções de máscara/parse/validação hoje
privadas em `CadastroDespesaRecorrenteComponent` para um utilitário
compartilhado (para não duplicá-las, conforme ponto em aberto do
refinamento de frontend). Duas novas rotas de serviço
(`getById`/`update`) em `DespesaRecorrenteService`, uma nova rota
`despesas/:id/editar`, e um novo ícone de ação por linha em
`PainelMensalDespesasComponent` que navega para essa rota.

**Dependência bloqueante identificada nesta etapa de planejamento** (ver
"Cross-Feature Dependency" abaixo): o payload atual do painel mensal
(`PanelOccurrenceData`/`PanelOccurrenceDataResponse`) não expõe o
identificador da despesa recorrente dona de cada ocorrência — apenas o da
ocorrência. Sem esse dado, o ícone de edição (FR-002) não tem para qual
`id` navegar. Esta é uma lacuna pequena e localizada no backend
(`GetMonthlyPanelUseCase`), fora do escopo desta spec (frontend), mas que
bloqueia a User Story 2 de ponta a ponta até ser corrigida.

## Technical Context

**Language/Version**: TypeScript ~6.0.2 (Angular 22.1, `strict: true`)

**Primary Dependencies**: Angular 22 (standalone components, Signals,
Router, `HttpClient`), RxJS 7.8, Tailwind CSS 4 — todas já presentes em
`frontend/package.json`; nenhuma dependência nova é necessária.

**Storage**: N/A — feature é somente frontend; consome a API REST do
backend (`ContasEmDia.Api`).

**Testing**: Vitest 4 (`@vitest/coverage-v8`, `jsdom`), no mesmo padrão já
usado por `cadastro-despesa-recorrente.component.spec.ts` e
`despesa-recorrente.service.spec.ts` (component harness + `HttpTestingController`
via `provideHttpClientTesting()`).

**Target Platform**: Navegador web (desktop e mobile responsivo), servido
pela SPA Angular já existente.

**Project Type**: Web application (frontend Angular + backend .NET já
existentes) — esta feature toca somente o lado `frontend/`, mais uma
alteração mínima cross-feature no backend (ver "Cross-Feature Dependency").

**Performance Goals**: Padrão de responsividade de SPA já estabelecido
pelo projeto; nenhuma meta numérica específica desta feature (spec não
define nenhuma, por ser uma tela de formulário de baixo volume).

**Constraints**: WCAG 2.1 AA em todo componente novo (Princípio II); TS
estrito sem `any` (Princípio III); nenhuma nova dependência npm sem
justificativa em plano (AI Agent Guardrails); reaproveitar o envelope de
resposta e o formato de erro por campo já fixados pelo endpoint de
cadastro.

**Scale/Scope**: Uma tela nova, uma rota nova, um ícone novo numa tela já
existente, dois métodos novos num serviço já existente, um utilitário
compartilhado novo — sem novos pacotes, sem mudança de arquitetura.

## Cross-Feature Dependency (bloqueia FR-002 de ponta a ponta)

`GetMonthlyPanelUseCase` (`backend/Application/UseCases/GetMonthlyPanel/GetMonthlyPanelUseCase.cs`,
linhas 54-65) constrói cada `PanelOccurrenceData` a partir de
`expense.GetOccurrencesForPeriod(...)`, mas descarta `expense.GetId()` no
caminho — nem `PanelOccurrenceData` nem `PanelOccurrenceDataResponse`
(`backend/Api/Responses/PanelOccurrenceDataResponse.cs`) têm um campo para
o identificador da despesa recorrente dona da ocorrência, só o `Id` da
própria ocorrência.

Sem esse campo, o ícone de edição por item (FR-002/User Story 2) não tem
para qual despesa recorrente navegar. É uma correção pequena e localizada
(adicionar `RecurringExpenseId` aos dois tipos e passar `expense.GetId()`
na projeção), mas pertence à camada de API/Application do backend — fora
do escopo desta spec, que é só a tela (frontend). **Esta lacuna deve ser
resolvida antes da tarefa de integração do ícone de edição em
`/speckit-tasks`**, e fica registrada aqui em vez de silenciosamente
assumida como já resolvida.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. API-First Backend/Frontend Separation | PASS — a tela consome exclusivamente `HttpClient` contra `GET`/`PUT /api/v1/recurring-expenses/{id}`, endpoints já contratados por `refinements/backend/editar-despesa-recorrente.md` e pela spec `007-edit-recurring-expense`; nenhum acesso direto a dados. A dependência cross-feature acima é sobre o **payload de um endpoint já existente** (painel mensal), não uma violação da separação. |
| II. Test-First Development | PASS (a garantir em `/speckit-tasks`) — cada novo método de serviço e cada estado visual do componente precisa de teste Vitest escrito antes da implementação, seguindo o padrão já usado por `cadastro-despesa-recorrente.component.spec.ts`; acessibilidade WCAG 2.1 AA verificada por componente (rótulos, foco, contraste — já usado no design de referência). |
| III. Type Safety & Static Analysis | PASS — TypeScript estrito, sem `any`; nenhuma supressão de lint prevista. |
| IV. Secure Handling of Financial Data | PASS — herda a exceção de fase já vigente (sem autenticação/CORS ainda); nenhum dado sensível novo introduzido. |
| V. Simplicity & Incremental Delivery | PASS — reaproveita `DespesaPreviewComponent` sem mudanças; extrai utilitário compartilhado em vez de duplicar; cria um componente novo em vez de ramificar o de cadastro por flag (decisão já justificada no refinamento de frontend). |
| VI. DDD no Domain Layer | N/A — nenhuma mudança de domínio nesta spec (pertence a `007-edit-recurring-expense`). |
| VII. Infrastructure Layer | N/A — nenhuma mudança de persistência nesta spec. |
| VIII. Angular Standalone Architecture & Estrutura | PASS — `EditarDespesaRecorrenteComponent` standalone, organizado em `features/despesa-recorrente/editar-despesa-recorrente/`; Tailwind para estilo, seguindo `design/Editar.dc.html`. |
| IX. Signal-Based Reactivity & HTTP Access | PASS — estado local em `signal`/`computed`; `HttpClient` injetado via `inject()` em `DespesaRecorrenteService` (`providedIn: 'root'`), nunca instanciado no componente. |
| X. Dependency Injection & Frontend Coding Standards | PASS — `inject()` em vez de injeção por construtor; nomes `*.component.ts`/`*.service.ts`; nenhuma mutação em `input()`/estado de sinal fora de `set`/`update`. |
| XI. Application Layer (backend) | N/A — fora de escopo (pertence a `007`); a lacuna cross-feature é uma correção de projeção já existente, não um novo UseCase. |
| XII. API Layer (backend) | N/A — fora de escopo direto; os dois endpoints consumidos (`GET`/`PUT` por id) já são especificados por `007-edit-recurring-expense`, não reimplementados aqui. |

**Nenhuma violação identificada.** A única pendência é a dependência
cross-feature documentada acima, que não é uma violação de princípio, mas
um pré-requisito de dados a resolver antes da tarefa de integração do
ponto de entrada (FR-002).

## Project Structure

### Documentation (this feature)

```text
specs/008-edit-recurring-expense-ui/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── edit-screen-contract.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
# Option 2: Web application (frontend Angular + backend .NET já existentes)

frontend/src/app/
├── features/
│   ├── despesa-recorrente/
│   │   ├── cadastro-despesa-recorrente/            # existente, não alterado
│   │   ├── editar-despesa-recorrente/              # NOVO
│   │   │   ├── editar-despesa-recorrente.component.ts
│   │   │   ├── editar-despesa-recorrente.component.html
│   │   │   └── editar-despesa-recorrente.component.spec.ts
│   │   ├── despesa-preview/                        # existente, reaproveitado sem mudanças
│   │   ├── despesa-recorrente.service.ts           # ALTERADO: + getById(), + update()
│   │   ├── despesa-recorrente.service.spec.ts       # ALTERADO: + testes dos 2 métodos novos
│   │   └── despesa-recorrente.model.ts             # ALTERADO: + tipos de leitura/atualização
│   └── painel-mensal-despesas/
│       ├── painel-mensal-despesas.component.html   # ALTERADO: + ícone de editar por item
│       ├── painel-mensal-despesas.component.ts     # ALTERADO: + navegação do ícone
│       └── painel-mensal-despesas.component.spec.ts # ALTERADO: + teste do ícone/rota
├── shared/
│   └── recurring-expense-form.util.ts              # NOVO: máscara/parse/validação extraídos
└── app.routes.ts                                    # ALTERADO: + rota despesas/:id/editar

backend/Application/UseCases/GetMonthlyPanel/
└── GetMonthlyPanelUseCase.cs                        # ALTERADO (cross-feature, ver acima):
                                                       #   + RecurringExpenseId em PanelOccurrenceData
backend/Api/Responses/
└── PanelOccurrenceDataResponse.cs                   # ALTERADO (cross-feature): + RecurringExpenseId
```

**Structure Decision**: Reaproveita a estrutura Option 2 (web application)
já existente no repositório (`backend/` + `frontend/`); nenhuma nova
pasta de nível superior é criada. A única alteração fora de
`frontend/src/app/` é a correção mínima e localizada em
`GetMonthlyPanelUseCase`/`PanelOccurrenceDataResponse`, necessária para
que o ícone de edição (FR-002) tenha o identificador correto para
navegar — não introduz nenhum UseCase, endpoint ou camada nova.

## Complexity Tracking

Nenhuma violação de princípio foi identificada nesta etapa — tabela
omitida intencionalmente (nada a justificar).
