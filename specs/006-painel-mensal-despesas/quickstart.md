# Quickstart: Painel Mensal de Despesas

**Feature**: `006-painel-mensal-despesas` | **Date**: 2026-09-07

Guia de validação de ponta a ponta da tela "Painel mensal" e dos três
endpoints novos sob `api/v1/occurrences`. Ver
[`contracts/api-contract.md`](./contracts/api-contract.md) para a forma
completa de request/response e [`data-model.md`](./data-model.md) para os
tipos citados.

## Pré-requisitos

- .NET 10 SDK e Node.js (versão exigida por `frontend/package.json`)
  instalados.
- Uma instância PostgreSQL acessível para desenvolvimento, com a connection
  string configurada em `backend/Api/appsettings.Development.json` (nunca
  hardcoded — Princípio IV).
- Migrations do EF Core aplicadas, incluindo a nova migration desta
  feature (`dotnet ef database update`, a partir de `backend/Infrastructure`,
  apontando para `backend/Api` como projeto de inicialização) — ou deixe o
  próprio `Program.cs` aplicá-las automaticamente na subida (já configurado
  para PostgreSQL).
- Ao menos uma despesa recorrente já cadastrada (via
  `POST /api/v1/recurring-expenses`, ver
  `specs/004-api-despesa-recorrente/quickstart.md`) com `status: "Active"` e
  `startDate` na competência atual, para que exista uma ocorrência visível
  no painel.

## Executando o backend localmente

```bash
cd backend/Api
dotnet run
```

`https://localhost:<porta>/swagger` expõe os três endpoints novos junto
dos já existentes.

## Executando o frontend localmente

```bash
cd frontend
npm start
```

Com o backend em execução, `http://localhost:4200/` deve carregar
diretamente a tela do painel mensal (nova rota padrão — ver
`research.md` §7); `http://localhost:4200/despesas/nova` continua servindo
a tela de cadastro já existente.

## Cenário 1 — Visualizar o painel da competência atual (US1, cenário 1)

```bash
curl -s https://localhost:<porta>/api/v1/occurrences | jq
```

**Resultado esperado**: `200 OK`, `data.referencePeriod` igual ao mês/ano
atuais, `data.occurrences` contendo cada ocorrência já gerada, cada uma com
`status` derivado (`Pending`/`DueSoon`/`Overdue`/`Paid`) coerente com sua
`dueDate` e a data atual. No navegador, o cabeçalho da lista mostra
"Contas de {mês} {ano}" e o contador bate com `occurrences.length`.

## Cenário 2 — Competência sem nenhuma ocorrência (EC01/SC-005)

```bash
curl -s "https://localhost:<porta>/api/v1/occurrences?year=2020&month=1" | jq
```

**Resultado esperado**: `200 OK`, `data.occurrences: []`. Na tela, lista
vazia, contador "0 contas", os três cartões de resumo em € 0,00 e nenhum
banner.

## Cenário 3 — Período inválido (EC16/CA14)

```bash
curl -i "https://localhost:<porta>/api/v1/occurrences?year=2026&month=13"
curl -i "https://localhost:<porta>/api/v1/occurrences?month=8"
```

**Resultado esperado**: `400 Bad Request` em ambos, `success: false`,
`errors` com um item de campo `"period"`, mensagem em PT-BR — nunca dados
de uma competência diferente da pedida.

## Cenário 4 — Marcar uma ocorrência como paga, com valor/data válidos (US2, cenário 3)

```bash
curl -i -X PATCH https://localhost:<porta>/api/v1/occurrences/<occurrenceId>/payment \
  -H "Content-Type: application/json" \
  -d '{ "paidAmount": "1500,00", "paymentDate": "18/08/2026" }'
```

**Resultado esperado**: `200 OK`, `data.occurrence.status: "Paid"`,
`paidAmount: 1500.00`, `paymentDate: "2026-08-18"`. Na tela, a mesma ação
via UI: clicar "Marcar como paga" abre a edição pré-preenchida, "Confirmar"
sai da edição e mostra "pago em 18/08/2026".

## Cenário 5 — Confirmar pagamento com valor/data em branco (US2, cenário 4/5 — EC08/EC09)

```bash
curl -i -X PATCH https://localhost:<porta>/api/v1/occurrences/<occurrenceId>/payment \
  -H "Content-Type: application/json" \
  -d '{ "paidAmount": "", "paymentDate": "" }'
```

**Resultado esperado**: `200 OK`, `data.occurrence.paidAmount` igual ao
`expectedAmount` da ocorrência, `paymentDate` igual à data atual do
servidor.

## Cenário 6 — Marcar novamente uma ocorrência já paga (regra de negócio)

Repita o Cenário 4 sobre o mesmo `occurrenceId` já pago.

**Resultado esperado**: `400 Bad Request`, `errors[0].message`: "Esta
ocorrência já está paga."

## Cenário 7 — `occurrenceId` inexistente

```bash
curl -i -X PATCH https://localhost:<porta>/api/v1/occurrences/00000000-0000-0000-0000-000000000000/payment \
  -H "Content-Type: application/json" -d '{}'
```

**Resultado esperado**: `404 Not Found`, `errors[0].message`: "Ocorrência
não encontrada."

## Cenário 8 — Desfazer pagamento (US3, cenário 1/2 — EC11/CA10)

```bash
curl -i -X DELETE https://localhost:<porta>/api/v1/occurrences/<occurrenceId>/payment
```

**Resultado esperado**: `200 OK`, `data.occurrence.status` recalculado a
partir de `dueDate` (`Pending`/`DueSoon`/`Overdue`), `paidAmount`/
`paymentDate: null`. Repetir a chamada sobre a mesma ocorrência agora não
paga → `400 Bad Request`, "Esta ocorrência ainda não foi paga."

## Cenário 9 — Edição exclusiva no cliente (US2, cenário 2 — RF14/EC10)

Somente via UI (estado puramente client-side, sem chamada de rede
associada): com o painel carregado e ao menos duas ocorrências não pagas,
clicar "Marcar como paga" em uma delas e, sem confirmar/cancelar, clicar
"Marcar como paga" em outra. **Resultado esperado**: a primeira volta ao
estado normal (rascunho descartado, sem chamada `PATCH` disparada para
ela), a segunda entra em edição.

## Cenário 10 — Navegação de mês e botão "Nova despesa" (FR-020/FR-021)

Somente via UI: com o painel carregado, clicar na seta "próximo mês" e
depois na seta "mês anterior". **Resultado esperado**: o cabeçalho e a
lista recarregam para a competência de destino a cada clique (mesma
chamada `GET /api/v1/occurrences?year=&month=` do Cenário 1, agora com os
parâmetros da competência vizinha), voltando exatamente à competência
original após o segundo clique. Em seguida, clicar em "Nova despesa".
**Resultado esperado**: a URL do navegador muda para
`http://localhost:4200/despesas/nova` e a tela de cadastro já existente é
exibida — o comportamento do formulário de cadastro em si permanece fora
do escopo desta feature.

## Cenário 11 — Cursor de mão e exclusão de elementos desabilitados (FR-022)

Somente via UI: passe o mouse sobre qualquer botão, link, seta de
navegação, o ícone "Desfazer" e o campo de data, no painel e na tela de
cadastro. **Resultado esperado**: cursor de mão (pointer) em todos eles.
Em seguida, dispare um estado com um botão desabilitado (ex.: "Salvar"
durante o envio do cadastro) e passe o mouse sobre ele. **Resultado
esperado**: cursor padrão do sistema (não pointer).

## Cenário 12 — "Voltar ao painel" na tela de cadastro, com e sem dados não salvos (FR-023/FR-024)

Somente via UI: abra `/despesas/nova` sem preencher nenhum campo e clique
em "Voltar ao painel". **Resultado esperado**: navega direto para `/`, sem
diálogo de confirmação. Repita preenchendo ao menos um campo (ex.: "Nome")
antes de clicar em "Voltar ao painel". **Resultado esperado**: um diálogo
de confirmação aparece, oferecendo "Continuar editando" (fecha o diálogo,
mantém os dados) e "Sair sem salvar" (navega para `/`, descartando os
dados). Repita uma terceira vez preenchendo o campo "Nome" e depois
apagando manualmente até ficar vazio novamente, então clique em "Voltar ao
painel". **Resultado esperado**: navega direto, sem diálogo (formulário de
volta ao estado inicial conta como "sem dados não salvos").

## Cenário 13 — Duas ações na tela de sucesso do cadastro (FR-025)

Somente via UI: cadastre uma despesa com sucesso em `/despesas/nova`.
**Resultado esperado**: a tela de sucesso mostra dois botões — "Voltar ao
painel" (navega para `/`) e "Cadastrar outra despesa" (limpa o formulário
e volta ao estado de edição, na mesma rota `/despesas/nova`, sem
navegação).

## Cenário 14 — Responsividade do painel em 720px e 480px (FR-026/FR-027)

Somente via UI: com o painel carregado em uma largura de janela ampla,
redimensione (ou use as ferramentas de dispositivo do navegador) para
719px. **Resultado esperado**: a grade dos três cartões de resumo passa a
exibir 2 colunas, e cada linha da lista de ocorrências reorganiza seus
campos em blocos empilhados (nome, depois status, valor, dia, ações).
Redimensione para 479px. **Resultado esperado**: a grade dos três cartões
passa a exibir 1 coluna, e os banners de alerta (quando presentes) ocupam
a largura total.

## Cenário 15 — Máscara de moeda ao digitar (FR-028)

Somente via UI: no painel, inicie o registro de pagamento de uma
ocorrência e digite `150050` no campo de valor. **Resultado esperado**: o
campo exibe `1.500,50` conforme os dígitos são digitados, sem exigir que
o usuário digite o ponto ou a vírgula manualmente. Repita em
`/despesas/nova`, campo "Valor previsto mensal".

## Cenário 16 — Seletor de data nativo (FR-029)

Somente via UI: clique no campo de data (data de pagamento no painel;
data de início no cadastro). **Resultado esperado**: o seletor de
calendário nativo do navegador abre; selecionar uma data preenche o campo
sem exigir digitação manual do texto da data.

## Cenário 17 — Valores monetários em Euro (FR-030)

Repita o Cenário 1 (painel) e observe qualquer valor cadastrado via
`/despesas/nova`. **Resultado esperado**: todos os valores monetários
exibidos (cartões de resumo, banners, itens da lista, pré-visualização do
cadastro) usam o formato `1.234,56 €` (símbolo após o valor), nunca
`R$`.

## Rodando os testes automatizados

```bash
# Backend
cd backend
dotnet test

# Frontend
cd frontend
npm test
```

Backend: cobre, no mínimo, um teste por cenário de resposta declarado por
`ProducesResponseType` em `OccurrencesController` (Princípio II), mais os
dois cenários novos de `ExceptionHandlingMiddlewareTests` (`400`/`404`), os
testes de `Occurrence`/`RecurringExpense` (Domain.Tests) para as regras de
`MarkAsPaid`/`UndoPayment`/`GetDerivedStatus`, e os testes de repositório
(Infrastructure.Tests) para os três métodos novos.

Frontend: cobre o cálculo de status derivado exibido, os banners
condicionais, os três totais, o fluxo de marcar/desfazer pagamento
(incluindo os casos de substituição silenciosa e edição exclusiva),
navegação de mês (`mesAnterior()`/`proximoMes()` recarregando a competência
correta — FR-020) e o `routerLink` de "Nova despesa" para `/despesas/nova`
(FR-021), e acessibilidade básica (operabilidade via teclado do botão
"Desfazer", labels dos campos de edição). Cobre também, a partir da
adição de 2026-09-08: os utilitários `maskCurrencyDigits`/`formatEUR`
(`frontend/src/app/shared/`), `hasUnsavedData`/`onClickVoltar`/
`onCancelExit`/`onConfirmExit` de `cadastro-despesa-recorrente.component.ts`
(FR-023/FR-024), as duas ações da tela de sucesso (FR-025), e a conversão
ISO↔`dd/MM/yyyy` de `confirmarPagamento` no painel (FR-029, `research.md`
§12). Responsividade (FR-026/FR-027) e cursor de mão (FR-022) são
validados manualmente conforme os Cenários 11/14 acima — não há
ferramenta de teste automatizado de layout responsivo ou de estilo de
cursor já em uso neste projeto (ver `plan.md`, "Constraints").
