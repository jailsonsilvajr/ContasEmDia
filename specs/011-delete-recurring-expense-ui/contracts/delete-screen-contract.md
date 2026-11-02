# Contract: Tela de Exclusão (consumo do frontend)

Esta feature é o lado **consumidor** de um contrato já especificado e
**já implementado** pelo backend
(`specs/010-delete-recurring-expense/contracts/api-contract.md`). Este
documento não redefine esse contrato — apenas fixa exatamente o recorte
que `DespesaRecorrenteService` e `PainelMensalDespesasComponent`
dependem dele, para que os testes de frontend (com
`HttpTestingController`) tenham uma referência única.

## `DELETE /api/v1/recurring-expenses/{id}?scope={pending-only|all}`

**Consumido por**: `DespesaRecorrenteService.delete(id, scope)`, chamado
por `onConfirmarExcluir` em `PainelMensalDespesasComponent` (FR-002,
FR-007, FR-008).

Sem corpo de requisição — `scope` é enviado via query string.

| Cenário | Resposta esperada pelo frontend |
|---|---|
| Abrangência `pending-only`, despesa existe | `200`, envelope `{ success: true, data: { recurringExpenseId, scope: 'pending-only' }, errors: null }` → modal fecha, `deleteTargetId.set(null)`, painel da competência exibida é recarregado (FR-007, FR-010) |
| Abrangência `all`, despesa existe | `200`, envelope `{ success: true, data: { recurringExpenseId, scope: 'all' }, errors: null }` → mesmo tratamento acima (FR-008, FR-010) |
| Despesa já não existe (excluída antes, por qualquer abrangência, em outra aba/sessão) | `404` → **mesmo tratamento de sucesso**: modal fecha e painel é recarregado (FR-012) — não é um erro para esta tela |
| Falha de rede/5xx | Erro HTTP capturado → modal **permanece aberto**, `deleteErrorMessage` preenchida, `deleteScope` preservada, `isDeleting.set(false)` (FR-011) |
| `scope` ausente/inválido (`400`) | Não deveria ocorrer em uso normal — o frontend sempre envia um dos dois valores fechados do `DeleteScope`; se ocorrer, tratado pelo mesmo caminho de erro de rede/servidor acima (banner genérico), sem campo específico a destacar (não há formulário nesta tela) |

`DeleteRecurringExpenseResponse`:

```ts
type DeleteScope = 'pending-only' | 'all';

interface DeleteRecurringExpenseResponse {
  recurringExpenseId: string;
  scope: DeleteScope;
}
```

## Fora deste contrato

- O efeito de cada abrangência sobre as ocorrências (quais são removidas,
  quais são preservadas) e sobre a geração futura de ocorrências — regra
  de negócio já especificada e implementada por
  `010-delete-recurring-expense` (`DeleteRecurringExpenseUseCase`), não
  redefinida aqui. Esta tela apenas aciona o endpoint e reage ao
  resultado.
- O endpoint `GET /api/v1/occurrences` (painel mensal) usado para
  recarregar a lista após a exclusão — já contratado por
  `006-painel-mensal-despesas`; não alterado por esta feature.
- Qualquer verbo/rota exata, OpenAPI/SwaggerUI e formato de erro `500` —
  já fixados por `010-delete-recurring-expense` e pelas convenções gerais
  da API (Princípio XII); não redefinidos aqui.
