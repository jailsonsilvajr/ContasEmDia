# Refinamento — Frontend: Tela de Cadastro de Despesa Recorrente

## Origem
Tela de design: **"Nova despesa recorrente"** (`design/Cadastro.dc.html`).

## Feature
Refinamento técnico da implementação **Angular** da tela de cadastro de uma
despesa recorrente: formulário de criação, pré-visualização em tempo real do
cartão que a despesa vai gerar no painel mensal, e tela de confirmação
pós-cadastro.

## Escopo
Cobre exclusivamente a tela "Nova despesa recorrente": estrutura de
componentes Angular, estado local, validações de formulário, valores
derivados da pré-visualização, estados visuais da tela e o contrato de
integração com o backend necessário para o botão "Salvar despesa" funcionar
de fato.

Não cobre: implementação real do backend (apenas a documentação do contrato
esperado, na seção final), a tela/painel mensal de listagem de ocorrências
(`design/Main.dc.html`), edição/pagamento de ocorrências, o comportamento do
botão "Cancelar" (sem `onClick` no design — navegação não definida) e
frequências diferentes de mensal (desabilitadas no próprio design).

Este documento **não contém código Angular**, apenas a estrutura conceitual
de componentes, estado e contratos, em conformidade com os Princípios I, V,
VII, VIII e IX da constituição do projeto (`.specify/memory/constitution.md`).
Nenhum arquivo de backend é criado ou alterado por este refinamento.

## Referências
- Modelo de domínio já refinado: [`refinements/domain-despesa-recorrente.md`](../domain-despesa-recorrente.md).
- Contrato público do domínio: [`specs/001-despesa-recorrente-domain/contracts/domain-public-api.md`](../../specs/001-despesa-recorrente-domain/contracts/domain-public-api.md).
- Modelo de dados do domínio: [`specs/001-despesa-recorrente-domain/data-model.md`](../../specs/001-despesa-recorrente-domain/data-model.md).

As regras funcionais (RFxx) citadas abaixo são as mesmas numeradas no
refinamento de domínio, garantindo que a validação client-side e o contrato
de API propostos não divirjam do modelo já definido para o backend.

## Estrutura proposta de componentes (Angular, Princípio VII)

Organização por feature, não por camada técnica:

```
frontend/src/app/features/despesa-recorrente/
  cadastro-despesa-recorrente/
    cadastro-despesa-recorrente.component.ts   (standalone, smart component)
    cadastro-despesa-recorrente.component.html
  despesa-preview/
    despesa-preview.component.ts               (standalone, presentational)
    despesa-preview.component.html
  despesa-recorrente.service.ts                (HTTP, injetado via inject())
  despesa-recorrente.model.ts                  (tipos/DTOs da feature)
```

- `CadastroDespesaRecorrenteComponent`: componente "smart", dono do estado do
  formulário (via `signal`) e da orquestração de submit. Corresponde à classe
  `Component`/`DCLogic` do protótipo.
- `DespesaPreviewComponent`: componente "presentational", recebe os valores
  já formatados via `input()` e apenas renderiza o cartão de pré-visualização
  (bloco `Pré-visualização` do design). Extraído como componente próprio por
  ser uma unidade de UI autocontida e reaproveitável (ex.: o mesmo cartão
  poderá ser usado no painel mensal).
- `DespesaRecorrenteService`: encapsula a chamada HTTP de criação da despesa
  (Princípio VIII — `HttpClient` injetado via `inject()`, nunca instanciado
  direto no componente). Não expõe leitura de categorias: o conjunto é
  fechado no domínio (`ExpenseCategoryType`) e foi assumido novamente como
  estável o suficiente para ser hardcodado no cliente (ver
  `despesa-recorrente.model.ts`), sem endpoint dedicado de listagem.
- `despesa-recorrente.model.ts`: interfaces TypeScript do payload de
  requisição/resposta, o union type `CategoryValue` (espelhando o enum
  `ExpenseCategoryType` do domínio) e a constante `CATEGORY_OPTIONS:
  CategoryOption[]` (par `value`/`label` hardcodado, ver "Contrato de API
  necessário") usada para popular o `<select>`. `categoria`, `status` e
  `frequency` são todos union types locais fixos, pois a UI restringe os
  três via controles fixos (select com as 5 opções do design, toggle, opção
  única "Mensal") — nenhuma validação de negócio é duplicada aqui (ela
  continua definitiva no backend — Princípio IV).

## Estado do componente (signals, Princípio VIII)

Estado local equivalente ao `state` do protótipo, migrado para `signal`:

| Signal | Tipo | Correspondência no protótipo |
|---|---|---|
| `nome` | `signal<string>` | `state.nome` |
| `categoria` | `signal<CategoryValue>` | `state.categoria` (union type fechado, ver "Estrutura proposta de componentes") |
| `valor` | `signal<string>` | `state.valor` (texto bruto digitado) |
| `dia` | `signal<string>` | `state.dia` (texto bruto digitado) |
| `dataInicio` | `signal<string>` | `state.dataInicio` (texto bruto digitado) |
| `status` | `signal<'ativa' \| 'pausada'>` | `state.status` |
| `observacao` | `signal<string>` | `state.observacao` |
| `touched` | `signal<{nome, valor, dia, dataInicio: boolean}>` | `state.touched` (atualizado agora no design, via `onBlurXxx`) |
| `submitAttempted` | `signal<boolean>` | `state.submitAttempted` (atualizado agora no design) |
| `formStatus` | `signal<'idle' \| 'loading' \| 'success' \| 'error'>` | `state.formStatus`, substitui o antigo `state.saved: boolean` (atualizado agora no design) |

Valores derivados do cartão de pré-visualização, migrados de `renderVals()`
para `computed()`:

| Computed | Origem no protótipo | Regra |
|---|---|---|
| `nomePreview` | `nomePreview` | `nome() \|\| 'Nome da despesa'` |
| `catColor` | `catColor` | mapa fixo categoria → cor (`CATEGORY_COLORS`, idêntico ao protótipo), com fallback para cor neutra (`#667085`) caso algum valor inesperado chegue |
| `valorFmt` | `valorFmt` | parse de `valor()` para número e formatação BRL (`Intl.NumberFormat` ou `CurrencyPipe`, não a função manual `formatBRL`) |
| `diaLabel` | `diaLabel` | `dia()` válido → `"Dia " + dia`; caso contrário `"Dia --"` |
| `statusHelperLabel` | `statusHelperLabel` | texto conforme `status()` |
| `isAtiva` / `isPausada` | `isAtiva` / `isPausada` | derivados de `status()` |
| `isLoading` / `isSuccess` / `isError` | `isLoading` / `isSuccess` / `isError` (**novo no design**) | derivados de `formStatus()` |
| `showXxxError` / `xxxErrorMsg` / `xxxBorderColor` (por campo: nome, valor, dia, dataInicio) | idem (**novo no design**) | `validate()` roda a cada mudança; erro só é exibido se o campo estiver `touched` ou `submitAttempted` for `true` (idêntico à função `reveal()` do protótipo) |

## Campos do formulário

| Campo (label na tela) | Signal | Tipo/entrada | Obrigatório | Validação client-side (espelha RF do domínio) |
|---|---|---|---|---|
| Nome | `nome` | texto livre | Sim | não vazio após trim (RF02) |
| Categoria | `categoria` | select com as 5 opções fixas hardcodadas em `CATEGORY_OPTIONS` (Moradia, Serviços, Transporte, Assinaturas, Outra) | Sim | valor deve ser um dos 5 valores fixos (RF03); a validação definitiva do enum continua no backend no momento do `POST` |
| Valor previsto mensal | `valor` | texto com máscara/parse monetário BRL | Sim | > 0, no máximo 2 casas decimais após parse (RF04) |
| Dia de vencimento | `dia` | texto numérico | Sim | inteiro entre 1 e 31 (RF05) |
| Data de início | `dataInicio` | texto `dd/mm/aaaa` | Sim | data válida no calendário (RF06) |
| Frequência | — (fixo) | somente "Mensal" selecionável na UI; demais opções desabilitadas | Sim | fixo em `Monthly` — nenhuma interação (RF07) |
| Status | `status` | toggle segmentado Ativa/Pausada | Sim | um dos dois valores; padrão "Ativa" (RF08) |
| Observação | `observacao` | textarea livre | Não | nenhuma (RF09) |

O botão "Salvar despesa" só deve disparar a submissão se todos os campos
obrigatórios passarem na validação client-side acima. O design atual não
desabilita o botão em campos inválidos — ele permanece clicável, e um clique
com o formulário inválido revela as mensagens de erro em todos os campos de
uma vez (`submitAttempted = true`) sem chamar a API, além de exibir o aviso
"Corrija os campos destacados para continuar." ao lado do botão.

## Interações / eventos

| Evento no design (`onXxx`) | Ação Angular equivalente |
|---|---|
| `onChangeNome`, `onChangeCategoria`, `onChangeValor`, `onChangeDia`, `onChangeDataInicio`, `onChangeObservacao` | `(input)`/`(change)` no template chamando `signal.set(...)` |
| `onBlurNome`, `onBlurValor`, `onBlurDia`, `onBlurDataInicio` | `(blur)` no template marcando `touched.set({ ...touched(), campo: true })` — controla a partir de quando o erro do campo passa a ser exibido (design atual, seção "Estados visuais da tela") |
| `onSetAtiva` / `onSetPausada` | clique no botão do toggle segmentado chamando `status.set('ativa' \| 'pausada')` |
| `onSalvar` | roda a validação client-side; se houver erro, marca todos os campos como `touched` e `submitAttempted = true` e **não** chama a API (idêntico ao design atual); se válida, seta `formStatus = 'loading'`, chama `DespesaRecorrenteService.create(payload)`, e em sucesso seta `formStatus = 'success'`; em falha (rede ou 400) seta `formStatus = 'error'`. No design, o botão "Tentar novamente" do estado de erro também dispara `onSalvar` |
| `onNovaDespesa` | reseta todos os signals do formulário para os valores iniciais, incluindo `touched`, `submitAttempted` e `formStatus = 'idle'` (idêntico ao protótipo) |
| Botão "Cancelar" | sem handler no design (`cursor: default`); nenhum comportamento definido nesta feature |

## Estados visuais da tela

1. **Preenchimento** — formulário editável, pré-visualização atualizada em
   tempo real a cada mudança de signal (estado padrão, `formStatus() === 'idle'`).
   Campos com erro (após `touched`/`submitAttempted`) ganham borda vermelha e
   mensagem inline abaixo do campo — já modelado no design.
2. **Enviando** — botão "Salvar despesa" com spinner, rótulo "Salvando…",
   opacidade reduzida e `cursor: default` enquanto `isLoading()` é verdadeiro
   (`formStatus() === 'loading'`) — já modelado no design (antes era uma
   lacuna do protótipo, que salvava de forma síncrona; agora o design tem um
   `setTimeout` simulando a latência da chamada real).
3. **Sucesso** — banner de confirmação verde com o nome da despesa e botão
   "Cadastrar outra despesa" (`formStatus() === 'success'`), conforme já
   modelado no design.
4. **Erro ao salvar** — banner vermelho com botão "Tentar novamente" que
   rechama `onSalvar` (`formStatus() === 'error'`) — já modelado no design.
   O texto do design é genérico ("falha de conexão com o servidor"); ver
   "Pontos em aberto" sobre como diferenciar isso de um `400` de validação.
Não há estado de carregamento/erro para o `<select>` de categoria: o
`<select>` é hardcodado e sempre populado, igual ao protótipo — ver "Pontos
em aberto".

## Pontos em aberto (frontend)

### Resolvidos pela atualização do design
- ~~Estado de carregamento/erro no envio do formulário~~ — o design agora
  modela os três estados (`idle`/`loading`/`success`/`error` via
  `formStatus`), incluindo spinner no botão e banner de erro com "Tentar
  novamente". Passa a ser especificação, não mais suposição.
- ~~Mensagens de validação inline nos campos~~ — o design agora modela
  borda vermelha + texto de erro por campo (nome, valor, dia, data de
  início), revelados em `blur` ou ao tentar submeter com o formulário
  inválido (`touched` / `submitAttempted`). As regras client-side já
  descritas na tabela "Campos do formulário" continuam válidas e batem com
  a função `validate()` do protótipo atualizado.

### Resolvidos nesta iteração (assunções confirmadas pelo usuário)
- **Categorias voltam a ser assumidas como fixas no cliente** (reversão da
  correção da iteração anterior): `ExpenseCategoryType` é um enum fechado de
  5 valores no domínio, e o usuário confirmou que o frontend pode hardcodar
  esses valores e seus rótulos em PT-BR em `despesa-recorrente.model.ts`
  (`CATEGORY_OPTIONS`), sem endpoint dedicado de listagem. Isso implica:
  - `GET /api/expense-categories` deixa de ser necessário e foi removido do
    "Contrato de API necessário" — a tela passa a depender de um único
    endpoint, `POST /api/recurring-expenses`.
  - O `<select>` de categoria permanece exatamente como no protótipo/design
    (`Cadastro.dc.html`): 5 opções fixas, sempre populado, sem estado de
    carregamento/erro — por isso o antigo item 5 de "Estados visuais da
    tela" (que só existia para o carregamento assíncrono) foi removido.
  - O mapa `catColor` volta a ser um mapa fixo e completo (`CATEGORY_COLORS`,
    idêntico ao protótipo), com fallback para cor neutra apenas como defesa
    contra um valor inesperado, não como caminho esperado.
  - Risco aceito explicitamente pelo usuário: se o backend adicionar ou
    renomear uma categoria no futuro, isso exigirá uma alteração manual em
    `despesa-recorrente.model.ts` (e um novo deploy do frontend).
- O banner de erro de submit no design é genérico ("falha de conexão com o
  servidor") e não diferencia falha de rede de um `400` de validação de
  negócio (RF02–RF08) retornado pela API. O design não modela como um `400`
  com erros por campo deveria aparecer — **assunção aceita pelo usuário**: a
  implementação real deve, quando possível, mapear os erros do envelope
  `400` para as mesmas mensagens inline por campo já usadas na validação
  client-side, caindo no banner genérico apenas para erros que não apontam
  um campo conhecido ou para falha de rede/5xx.
- O botão "Cancelar" não tem comportamento definido no design; navegação
  real (ex.: voltar ao painel mensal) **confirmado fora de escopo** deste
  refinamento.
- Autenticação/autorização da chamada ao endpoint de criação (Princípio IV)
  **confirmado que não é tratada aqui**; assume-se que a infraestrutura de
  autenticação do frontend (fora do escopo desta tela) já anexa as
  credenciais necessárias.

### Ainda em aberto
Nenhum ponto em aberto no momento.

## Contrato de API necessário (documentação apenas — sem implementação)

A tela depende de **um único endpoint**, ainda não implementado no backend
(apenas o projeto `Domain` existe hoje): o do botão "Salvar despesa". Nenhum
outro endpoint é necessário para esta tela (a lista de categorias é fixa no
cliente — ver "Pontos em aberto" — e não há edição ou exclusão aqui). A
forma dos dados abaixo segue o modelo de domínio já refinado
(`RecurringExpense`/`Occurrence`/`ExpenseCategory`), para que o futuro time
de backend implemente a camada de Aplicação/API sem divergir do domínio já
especificado.

`despesa-recorrente.model.ts` hardcoda a constante local usada para popular
o `<select>` e para o mapeamento de rótulo no cartão de pré-visualização
(mesmos 5 pares que o design já usa em `Cadastro.dc.html`):

```ts
const CATEGORY_OPTIONS: CategoryOption[] = [
  { value: 'Housing', label: 'Moradia' },
  { value: 'Services', label: 'Serviços' },
  { value: 'Transportation', label: 'Transporte' },
  { value: 'Subscriptions', label: 'Assinaturas' },
  { value: 'Other', label: 'Outra' },
];
```

`value` é o literal do enum `ExpenseCategoryType`, o mesmo que a tela envia
em `category` no `POST` abaixo; `label` é o rótulo em PT-BR exibido no
`<select>` e no cartão de pré-visualização.

### `POST /api/recurring-expenses`

Cria uma despesa recorrente e, se aplicável, gera automaticamente a
ocorrência do mês corrente (RF10/RF11).

**Request body**

| Campo | Tipo | Obrigatório | Regra (RF) |
|---|---|---|---|
| `name` | `string` | Sim | não vazio após trim (RF02) |
| `category` | `string` (union fechado hardcodado no cliente: `"Housing" \| "Services" \| "Transportation" \| "Subscriptions" \| "Other"`, ver `CATEGORY_OPTIONS`) | Sim | um dos 5 valores fixos (RF03) |
| `monthlyAmount` | `number` (decimal) | Sim | > 0, até 2 casas decimais (RF04) |
| `dueDay` | `integer` | Sim | 1–31 (RF05) |
| `startDate` | `string` (ISO 8601, `yyyy-MM-dd`) | Sim | data válida (RF06) |
| `frequency` | `string` (enum: `"Monthly"`) | Sim | apenas `"Monthly"` aceito nesta etapa (RF07) |
| `status` | `string` (enum: `"Active" \| "Paused"`) | Sim | padrão `"Active"` se omitido (RF08) |
| `note` | `string \| null` | Não | texto livre (RF09) |

**Response — `201 Created`**

```json
{
  "id": "guid",
  "name": "string",
  "category": "Housing | Services | Transportation | Subscriptions | Other",
  "monthlyAmount": 0.0,
  "dueDay": 1,
  "startDate": "yyyy-MM-dd",
  "frequency": "Monthly",
  "status": "Active | Paused",
  "note": "string | null",
  "occurrences": [
    {
      "id": "guid",
      "referencePeriod": { "year": 0, "month": 1 },
      "dueDate": "yyyy-MM-dd",
      "status": "Pending",
      "expectedAmount": 0.0,
      "name": "string",
      "category": "Housing | Services | Transportation | Subscriptions | Other"
    }
  ]
}
```

`occurrences` deve conter exatamente 0 ou 1 item: 1 item se `status` enviado
for `"Active"` (RF10), 0 itens se `"Paused"` (RF11) — espelhando
`GetOccurrences()` do agregado `RecurringExpense`.

**Response — `400 Bad Request`** (falha de validação de negócio)

```json
{
  "errors": [
    { "field": "name", "message": "string" }
  ]
}
```

Um item por regra violada (RF02–RF08), permitindo à tela destacar o campo
correspondente. O formato exato (nomes de campo, estrutura do envelope de
erro) deve ser alinhado com o padrão de erro já adotado pela API do projeto,
caso já exista; nenhum padrão de erro de API foi encontrado neste repositório
no momento deste refinamento.

**Fora do escopo destes endpoints / desta tela**
- Listagem de despesas recorrentes ou ocorrências (painel mensal).
- Edição, pausa/reativação ou exclusão de uma despesa recorrente existente.
- Marcar ocorrência como paga ou desfazer pagamento.
- Qualquer endpoint de suporte a frequências diferentes de mensal.
- Criação, edição, remoção ou listagem dinâmica de categorias — o conjunto é
  fixo no cliente nesta iteração (ver "Pontos em aberto"); gestão do
  catálogo de categorias via API (se um dia existir) é uma feature à parte.

Este contrato é uma especificação para orientar uma futura feature de
backend (camada de Aplicação/API) e **não implica nenhuma criação de
controller, DTO ou rota nesta etapa** — apenas o projeto `Domain` (já
refinado separadamente) existe hoje no backend.
