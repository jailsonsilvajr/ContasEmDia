# Quickstart: Excluir Despesa Recorrente (Tela)

Guia de validação end-to-end desta feature. Assume que as tarefas de
`/speckit-tasks` já foram implementadas.

## Pré-requisitos

- Backend `010-delete-recurring-expense` implementado: `DELETE
  /api/v1/recurring-expenses/{id}?scope=...` respondendo conforme
  [`contracts/delete-screen-contract.md`](./contracts/delete-screen-contract.md)
  (já é o caso no estado atual do repositório).
- Ao menos uma despesa recorrente cadastrada com uma ocorrência paga e
  uma pendente, visível no painel mensal da competência exibida.
- `frontend/`: `npm install` já executado.

## Rodando a aplicação

```bash
# backend (a partir de backend/Api)
dotnet run

# frontend (a partir de frontend/)
npm start
```

Abrir o painel mensal (`/`).

## Cenário 1 — Excluir preservando o histórico pago (User Story 1, P1)

1. Localizar, na lista, uma despesa recorrente com uma ocorrência paga e
   uma pendente na competência atual.
2. Clicar no ícone de lixeira dessa linha.
3. **Esperado**: modal abre com as duas abrangências visíveis, "apenas
   pendentes/vencidas" já selecionada (FR-001, FR-003, FR-005).
4. Sem trocar a seleção, clicar em confirmar.
5. **Esperado**: botão de confirmar mostra estado de carregamento
   (FR-009); em seguida o modal fecha e a linha pendente desaparece da
   lista, com os totais do topo e os alertas de vencidas/vence-em-breve
   atualizados sem recarregar a página (FR-007, FR-010, SC-001, SC-004).
6. Navegar até a competência em que a ocorrência paga foi registrada.
7. **Esperado**: essa ocorrência continua aparecendo normalmente, marcada
   como paga (SC-002).
8. Navegar até uma competência futura.
9. **Esperado**: nenhuma ocorrência nova dessa despesa aparece.

## Cenário 2 — Excluir todo o histórico (User Story 2, P2)

1. Localizar outra despesa recorrente com ocorrências pagas e pendentes.
2. Clicar no ícone de lixeira, trocar deliberadamente a seleção para
   "excluir todo o histórico", confirmar.
3. **Esperado**: modal fecha, lista atualizada; nenhuma ocorrência dessa
   despesa — paga ou não — continua acessível em nenhuma competência do
   Painel Mensal (FR-008, SC-003).
4. Tentar abrir a edição dessa despesa diretamente (`despesas/<id>/editar`
   com o id removido).
5. **Esperado**: tela de edição trata como "não encontrada".

## Cenário 3 — Cancelar não altera nada (User Story 3, P1)

1. Clicar no ícone de lixeira de qualquer item.
2. **Esperado**: modal abre com a abrangência "apenas pendentes/vencidas"
   já selecionada (FR-005).
3. Clicar em cancelar, sem confirmar.
4. **Esperado**: modal fecha; nenhuma chamada de exclusão foi feita;
   nenhuma linha ou total mudou (FR-006, SC-001).

## Cenário 4 — Falha de rede preserva a escolha feita (User Story 4, P3)

1. Clicar no ícone de lixeira de um item, trocar para "excluir todo o
   histórico".
2. Simular uma falha de rede/servidor no `DELETE` (ex.: desligar o
   backend momentaneamente) e confirmar.
3. **Esperado**: modal permanece aberto, com uma mensagem de erro; a
   abrangência "excluir todo o histórico" continua selecionada; é
   possível tentar novamente ou cancelar (FR-011, SC-005).
4. Restaurar o backend e clicar em confirmar novamente.
5. **Esperado**: exclusão é concluída normalmente, como no Cenário 2.

## Cenário 5 — Exclusão já feita em outra aba (Edge case / FR-012)

1. Abrir o painel mensal em duas abas.
2. Em uma aba, excluir uma despesa recorrente (qualquer abrangência).
3. Na outra aba (sem recarregar a página), clicar no ícone de lixeira da
   mesma despesa e confirmar.
4. **Esperado**: a chamada retorna "não encontrada"; o modal fecha e a
   lista é atualizada, refletindo que a despesa já não está mais
   presente — sem mensagem de erro (FR-012).

## Verificação automatizada

- Testes de componente (Vitest) cobrindo cada estado de
  `deleteTargetId`/`deleteScope`/`isDeleting`/`deleteErrorMessage`
  listado em `data-model.md`, seguindo o padrão já usado por
  `painel-mensal-despesas.component.spec.ts`.
- Testes de serviço (Vitest + `HttpTestingController`) para `delete(id,
  scope)` em `despesa-recorrente.service.spec.ts` — casos de sucesso
  (ambas abrangências), `400`, `404` e falha de rede, incluindo a
  verificação de que `scope` é enviado como query string.
- Teste confirmando que o ícone de lixeira usa
  `item.recurringExpenseId` (não `item.id`, que é o id da ocorrência) ao
  abrir o modal (FR-002).
