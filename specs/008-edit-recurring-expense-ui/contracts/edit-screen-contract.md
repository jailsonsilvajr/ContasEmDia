# Contract: Tela de Edição (consumo do frontend)

Esta feature é o lado **consumidor** de um contrato já especificado pelo
backend (`refinements/backend/editar-despesa-recorrente.md` e
`specs/007-edit-recurring-expense`). Este documento não redefine esse
contrato — apenas fixa exatamente o recorte que
`DespesaRecorrenteService` e `EditarDespesaRecorrenteComponent` dependem
dele, para que os testes de frontend (com `HttpTestingController`) tenham
uma referência única.

## `GET /api/v1/recurring-expenses/{id}`

**Consumido por**: `DespesaRecorrenteService.getById(id)`, chamado ao
ativar a rota `despesas/:id/editar` (FR-003).

| Cenário | Resposta esperada pelo frontend |
|---|---|
| Despesa existe | `200`, envelope `{ success: true, data: RecurringExpenseDetailResponse, errors: null }` |
| Despesa não existe | `404` → `loadStatus.set('not-found')` (FR-005) |
| Falha de rede/5xx | Erro HTTP capturado → `loadStatus.set('error')` (FR-006) |

`RecurringExpenseDetailResponse`:

```ts
interface RecurringExpenseDetailResponse {
  id: string;
  name: string;
  category: CategoryValue;       // 'Housing' | 'Services' | 'Transportation' | 'Subscriptions' | 'Other'
  monthlyAmount: number;
  dueDay: number;
  startDate: string;              // yyyy-MM-dd
  frequency: 'Monthly';           // exibido, nunca editável
  status: 'Active' | 'Paused';
  note: string | null;
}
```

## `PUT /api/v1/recurring-expenses/{id}`

**Consumido por**: `DespesaRecorrenteService.update(id, payload)`,
chamado por `onSalvar` (FR-008–FR-012, FR-015, FR-016).

**Request body** (`UpdateRecurringExpenseRequest`):

```ts
interface UpdateRecurringExpenseRequest {
  name: string;
  category: CategoryValue;
  monthlyAmount: number;
  dueDay: number;
  startDate: string;    // yyyy-MM-dd
  status: 'Active' | 'Paused';
  note: string | null;
  // sem "frequency": não editável (FR-001)
}
```

| Cenário | Resposta esperada pelo frontend |
|---|---|
| Todos os campos válidos, despesa existe | `200`, envelope `{ success: true, data: UpdateRecurringExpenseResponse, errors: null }` → `formStatus.set('success')`, `initialSnapshot` atualizado (FR-015) |
| Um ou mais campos inválidos | `400`, envelope `{ success: false, data: null, errors: FieldError[] }` → erro exibido junto a cada campo apontado (FR-012); campos sem erro correspondente mantêm o valor digitado |
| Despesa não existe | `404` → tratado como falha de salvamento (FR-011); a spec 008 não distingue visualmente este caso de outra falha de salvamento, pois a User Story 1 já garante que a tela só chega a esse ponto tendo carregado a despesa com sucesso — um `404` neste momento é a mesma classe de "algo mudou por fora" já coberta pelo estado de erro genérico |
| Falha de rede/5xx | Erro HTTP capturado → `formStatus.set('error')` (FR-011), dados digitados preservados |

`UpdateRecurringExpenseResponse`: mesmo shape de
`RecurringExpenseDetailResponse` (reaproveitado, sem duplicar a
interface).

`FieldError`/`ApiErrorResponse`: já existentes em
`despesa-recorrente.model.ts`, reaproveitados sem mudança.

## Fora deste contrato

- O endpoint `GET /api/v1/occurrences` (painel mensal) e o campo
  `recurringExpenseId` que ele precisa passar a expor — ver
  "Cross-Feature Dependency" em `plan.md`. Não é um contrato **desta**
  tela, mas da tela do painel mensal; citado aqui só para deixar claro
  que a rota de navegação do ícone de edição depende dele.
- Qualquer verbo/rota exata (`PUT` vs. `PATCH`), OpenAPI/SwaggerUI e
  formato de erro `500` — já fixados por `007-edit-recurring-expense` e
  pelas convenções gerais da API (Princípio XII); não redefinidos aqui.
