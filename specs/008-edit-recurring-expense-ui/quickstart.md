# Quickstart: Editar Despesa Recorrente (Tela)

Guia de validação end-to-end desta feature. Assume que as tarefas de
`/speckit-tasks` já foram implementadas, incluindo o pré-requisito
cross-feature descrito em `plan.md` (`recurringExpenseId` exposto pelo
painel mensal).

## Pré-requisitos

- Backend `007-edit-recurring-expense` implementado: `GET`/`PUT
  /api/v1/recurring-expenses/{id}` respondendo conforme
  [`contracts/edit-screen-contract.md`](./contracts/edit-screen-contract.md).
- Correção cross-feature aplicada: `GET /api/v1/occurrences` (painel
  mensal) devolvendo `recurringExpenseId` por item.
- Ao menos uma despesa recorrente cadastrada com uma ocorrência na
  competência atual (para aparecer no painel mensal).
- `frontend/`: `npm install` já executado.

## Rodando a aplicação

```bash
# backend (a partir de backend/Api)
dotnet run

# frontend (a partir de frontend/)
npm start
```

Abrir o painel mensal (`/`).

## Cenário 1 — Editar com sucesso (User Story 1, P1)

1. No painel mensal, clicar no ícone de editar de um item da lista.
2. **Esperado**: a tela `despesas/:id/editar` abre mostrando um estado de
   carregamento e, em seguida, o formulário preenchido com os valores
   atuais da despesa (FR-003).
3. Alterar o valor previsto mensal para um número válido diferente do
   atual.
4. Clicar em "Salvar alterações".
5. **Esperado**: banner de confirmação de sucesso com o nome da despesa;
   nenhum botão de "cadastrar outra despesa" aparece (FR-010).
6. Voltar ao painel mensal e reabrir a edição da mesma despesa.
7. **Esperado**: o novo valor previsto mensal aparece pré-carregado,
   confirmando que a alteração foi persistida (SC-003).

## Cenário 2 — Validação client-side bloqueia o envio (User Story 1)

1. Abrir a edição de uma despesa existente.
2. Apagar o campo "Nome" (deixar em branco) e tentar salvar.
3. **Esperado**: envio bloqueado, erro "Informe um nome..." aparece junto
   ao campo, nenhuma chamada de rede é feita (FR-007/FR-008/SC-002).

## Cenário 3 — Despesa não encontrada (User Story 4)

1. Acessar diretamente `despesas/<id-inexistente>/editar`.
2. **Esperado**: mensagem dedicada de "despesa não encontrada", sem
   formulário, com um link/botão para voltar ao painel mensal (FR-005).

## Cenário 4 — Confirmação de saída com alterações não salvas (User Story 3)

1. Abrir a edição de uma despesa existente.
2. Alterar o campo "Observação".
3. Clicar em "Cancelar" (ou tentar navegar para fora da tela).
4. **Esperado**: diálogo de confirmação "Sair sem salvar?" aparece
   (FR-013).
5. Clicar em "Continuar editando", apagar a alteração feita no passo 2
   (deixar o campo exatamente como estava antes), tentar sair novamente.
6. **Esperado**: nenhuma confirmação aparece desta vez — o valor do campo
   voltou a ser igual ao carregado originalmente (FR-014, comparação por
   valor).

## Cenário 5 — Reativação avisa sobre geração de ocorrência (Edge case / FR-016)

1. Abrir a edição de uma despesa recorrente com status "Pausada".
2. **Esperado**: texto auxiliar junto ao controle de status já indica o
   comportamento de "Pausada".
3. Trocar o status para "Ativa".
4. **Esperado**: o texto auxiliar muda imediatamente para indicar que uma
   ocorrência do mês corrente pode ser gerada ao salvar — visível o tempo
   todo enquanto "Ativa" estiver selecionado, não só num instante
   pontual (FR-016, clarificado).

## Cenário 6 — Falha ao salvar preserva os dados digitados (User Story 4)

1. Abrir a edição de uma despesa existente.
2. Simular uma falha de rede/servidor no `PUT` (ex.: desligar o backend
   momentaneamente, ou usar o tweak de demonstração do design de
   referência como guia do comportamento esperado).
3. Alterar um campo válido e clicar em "Salvar alterações".
4. **Esperado**: banner de erro com "Tentar novamente"; o campo alterado
   continua com o valor digitado, nada é perdido (FR-011).

## Verificação automatizada

- Testes de componente (Vitest) cobrindo cada estado de `loadStatus` e
  `formStatus` listado em `data-model.md`, seguindo o padrão de
  `cadastro-despesa-recorrente.component.spec.ts`.
- Testes de serviço (Vitest + `HttpTestingController`) para
  `getById`/`update`, seguindo o padrão de
  `despesa-recorrente.service.spec.ts` — casos de sucesso, `400`, `404` e
  falha de rede para cada método.
- Teste de navegação em `painel-mensal-despesas.component.spec.ts`
  confirmando que o ícone de editar aponta para
  `despesas/{recurringExpenseId}/editar` (não `despesas/{occurrenceId}/editar`).
