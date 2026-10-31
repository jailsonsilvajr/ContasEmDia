# Data Model: Excluir Despesa Recorrente (Tela)

Esta feature não introduz nenhuma entidade de domínio nova (a exclusão em
si, e o efeito de cada abrangência, já pertencem a
`010-delete-recurring-expense`, backend). O que segue é o modelo de
**dados client-side** — estado do componente e formas de request/response
que o frontend consome — derivado da spec 011.

## Estado novo em `PainelMensalDespesasComponent`

| Campo | Tipo | Origem / regra |
|---|---|---|
| `deleteTargetId` | `signal<string \| null>` | `recurringExpenseId` da despesa cuja confirmação está aberta (FR-002); `null` quando o modal está fechado |
| `deleteScope` | `signal<DeleteScope>` | Abrangência selecionada na confirmação; reinicia em `'pending-only'` sempre que o modal é aberto (FR-005) |
| `isDeleting` | `signal<boolean>` | `true` enquanto a chamada `DELETE` está em andamento; desabilita o botão de confirmar e troca seu rótulo para carregamento (FR-009) |
| `deleteErrorMessage` | `signal<string \| null>` | Mensagem de erro exibida quando a chamada falha por rede/servidor (FR-011); `null` em qualquer outro momento |

### Computeds (derivados, sem estado próprio)

- `isDeleteModalOpen`: `deleteTargetId() !== null`.
- `canConfirmDelete`: `!isDeleting()` — usado para bloquear um segundo
  envio da mesma exclusão (FR-009).

### Transições de estado relevantes

```text
deleteTargetId: null --(clique no botão de lixeira do item)--> <recurringExpenseId>
                <recurringExpenseId> --(cancelar)--> null
                <recurringExpenseId> --(confirmar, sucesso OU 404)--> null  (+ painel recarregado)
                <recurringExpenseId> --(confirmar, erro de rede/5xx)--> <recurringExpenseId>  (modal permanece aberto)

isDeleting:     false --(confirmar)--> true
                true  --(resposta, qualquer resultado)--> false

deleteErrorMessage: null --(erro de rede/5xx)--> "<mensagem>"
                     "<mensagem>" --(tentar novamente OU cancelar OU trocar abrangência)--> null
```

Não existe transição de `deleteTargetId` para um valor diferente do item
que abriu o modal sem passar por `null` primeiro — abrir a confirmação de
uma segunda despesa enquanto a de outra está aberta não é um fluxo
previsto pela spec (cada linha tem seu próprio botão, mas só um modal
pode estar na tela por vez, mesmo padrão do diálogo "Sair sem salvar").

## Formas de request/response consumidas (contrato já definido por `010-delete-recurring-expense`)

Ver [`contracts/delete-screen-contract.md`](./contracts/delete-screen-contract.md)
para o recorte completo consumido por esta tela; resumo dos tipos
client-side novos, adicionados a `despesa-recorrente.model.ts` e
reexportados por `painel-mensal-despesas.model.ts` (mesmo padrão já usado
para `ApiEnvelope`/`CategoryValue`):

| Tipo | Uso |
|---|---|
| `DeleteScope` | `'pending-only' \| 'all'` — as duas abrangências mutuamente exclusivas (FR-004) |
| `DeleteRecurringExpenseResponse` | Resposta de sucesso do `DELETE` — `{ recurringExpenseId: string; scope: DeleteScope }` |

`FieldError`/`ApiErrorResponse`: já existentes em
`despesa-recorrente.model.ts`, reaproveitados sem mudança para o caso
`400` (abrangência ausente/inválida — ver contrato; não deveria ocorrer
em uso normal, pois o frontend sempre envia um dos dois valores fechados,
mas o tratamento de erro genérico já cobre esse caso se ocorrer).

## Item do painel mensal (dado consumido, não alterado por esta feature)

`PanelOccurrenceResponse.recurringExpenseId` (já existente desde a
correção cross-feature de `008-edit-recurring-expense-ui`) é o único dado
do item necessário para acionar a exclusão (FR-002) — nenhum campo novo é
necessário neste tipo.
