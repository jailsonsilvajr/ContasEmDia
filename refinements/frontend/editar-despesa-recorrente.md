# Refinamento — Frontend: Editar Despesa Recorrente

## Origem
Refinamento funcional: [`editar-despesa-recorrente.md`](../editar-despesa-recorrente.md).
**Não há tela de design para esta feature** (nenhum `.dc.html` equivalente
a `Cadastro.dc.html`/`Main.dc.html`). Diferente de
[`cadastro-despesa-recorrente.md`](cadastro-despesa-recorrente.md), que
transcreve um protótipo já pronto, este documento **propõe** a estrutura
visual e de componentes por consistência com as duas telas já implementadas
(`CadastroDespesaRecorrenteComponent` e `PainelMensalDespesasComponent`,
ambas em `frontend/src/app/features/`). Todo o desenho de interação abaixo
deve ser tratado como proposta a validar contra um design real antes da
implementação final — não como especificação fechada, ao contrário do que
`cadastro-despesa-recorrente.md` pôde ser por já partir de um protótipo.

## Feature
Refinamento técnico da implementação **Angular** da edição de uma despesa
recorrente já cadastrada: ponto de entrada a partir do painel mensal, tela
de edição (reaproveitando ao máximo a tela de cadastro já implementada),
pré-carregamento dos dados via `GET`, envio da atualização via `PUT`, e os
estados visuais correspondentes.

## Escopo
Cobre exclusivamente o lado frontend da edição: onde o usuário aciona
"editar", a rota e o componente da tela de edição, o estado local
necessário para pré-carregar e depois salvar os dados, os contratos de
`GET`/`PUT` necessários (no mesmo nível de detalhe que
`cadastro-despesa-recorrente.md` já documentou para o `POST`), e os
estados visuais da tela (carregando, erro ao carregar, não encontrado,
preenchimento, salvando, sucesso, erro ao salvar).

Não cobre: exclusão de despesa recorrente, qualquer alteração na tela do
painel mensal além do necessário para abrir a edição (RF01 abaixo), e
qualquer decisão que dependa de uma tela de design real ainda não
produzida (ver "Pontos em aberto").

## Referências
- Refinamento funcional (RF/EC citados abaixo são os dele, salvo indicação
  contrária): [`editar-despesa-recorrente.md`](../editar-despesa-recorrente.md).
- Refinamento backend correspondente (contratos de `GET`/`PUT`):
  [`editar-despesa-recorrente.md`](../backend/editar-despesa-recorrente.md).
- Refinamento e código já implementados da tela de cadastro, base de toda
  a reutilização proposta aqui:
  [`cadastro-despesa-recorrente.md`](cadastro-despesa-recorrente.md) e
  `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/`.
- Código já implementado do painel mensal (ponto de entrada da edição):
  `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.html`.

## Ponto de entrada (RF01 do refinamento funcional)
**Proposta, sem design de referência**: um botão/ícone "Editar" por
ocorrência na lista do painel mensal (`painel-mensal-despesas.component.html`,
mesmo bloco de ações à direita de cada `occurrence-row`, hoje ocupado só
por "Marcar como paga"/dados de pagamento), navegando para a tela de
edição da despesa recorrente **dona** daquela ocorrência — não da
ocorrência em si (a edição é da despesa recorrente, RF01 do refinamento
funcional). Alternativa não escolhida: um botão de editar dentro do modo
de edição de pagamento (`edicao-pagamento`) — descartada por misturar
"editar pagamento de uma ocorrência" com "editar a despesa recorrente",
dois conceitos que o próprio refinamento funcional trata como
independentes (RF09 daquele documento). **Ponto em aberto**: confirmar
esse local exato quando houver um design real.

## Rota
Nova rota `despesas/:id/editar`, ao lado da já existente `despesas/nova`
(`app.routes.ts`), apontando para o componente de edição (ver "Estrutura
de componentes").

## Estrutura de componentes (Angular, Princípio VIII)

```
frontend/src/app/features/despesa-recorrente/
  cadastro-despesa-recorrente/            (já existe, reaproveitado)
    cadastro-despesa-recorrente.component.ts
    cadastro-despesa-recorrente.component.html
  editar-despesa-recorrente/              (novo)
    editar-despesa-recorrente.component.ts
    editar-despesa-recorrente.component.html
  despesa-preview/                        (já existe, reaproveitado sem mudanças)
  despesa-recorrente.service.ts           (ganha get()/update())
  despesa-recorrente.model.ts             (ganha tipos de update)
```

### Reaproveitar o componente de cadastro ou criar um novo?
**Decisão proposta**: criar `EditarDespesaRecorrenteComponent` como
componente próprio, e não reaproveitar `CadastroDespesaRecorrenteComponent`
com uma flag de modo (`isEdicao`). Razão: o componente de cadastro já tem
lógica própria de "nova despesa" pós-sucesso (`onNovaDespesa()`, botão
"Cadastrar outra despesa") que não faz sentido no fluxo de edição, e
misturar os dois pelo meio de condicionais contraria o Princípio V
(simplicidade — preferir dois componentes pequenos e diretos a um só
componente ramificado por modo). Em compensação, para não duplicar toda a
estrutura de estado/validação, `EditarDespesaRecorrenteComponent` reaproveita:
- `DespesaPreviewComponent` (sem nenhuma mudança).
- As mesmas funções puras de máscara/parse já existentes
  (`maskCurrencyDigits`, `formatEUR`, `parseValor`/`parseDia` — hoje
  privadas dentro do componente de cadastro; **ponto em aberto**: extraí-las
  para um utilitário compartilhado em vez de duplicá-las no novo
  componente, já que as duas telas passam a precisar exatamente das
  mesmas regras de parsing/validação por campo).
- As mesmas mensagens de erro por campo e a mesma estrutura de
  `touched`/`submitAttempted` para revelar erros.

## Estado do componente (signals, Princípio IX)

Estado equivalente ao de `CadastroDespesaRecorrenteComponent`, com as
seguintes diferenças:

| Signal | Diferença em relação ao cadastro |
|---|---|
| `nome`, `categoria`, `valor`, `dia`, `dataInicio`, `status`, `observacao` | idênticos, mas inicializados **vazios** até o `GET` de pré-carregamento responder (ver "Carregamento inicial"), não com os valores padrão fixos do cadastro (`Housing`/`ativa`) |
| `loadStatus` | **novo**: `signal<'loading' \| 'loaded' \| 'not-found' \| 'error'>`, controla o pré-carregamento — não existe equivalente no cadastro, que não carrega nada de antemão |
| `formStatus` | mesmo papel do cadastro (`idle \| loading \| success \| error`), mas cobre o **salvar**, não o carregar |
| `touched`, `submitAttempted`, `apiFieldErrors` | idênticos ao cadastro |
| `initialSnapshot` | **novo**: guarda os valores recebidos do `GET`, usado para `hasUnsavedData` (ver abaixo) e para permitir o UseCase backend aplicar apenas os campos que mudaram (`editar-despesa-recorrente.md` do backend, seção "Chamar apenas os métodos dos campos que mudaram") |

Computeds idênticos aos do cadastro (`nomePreview`, `catColor`, `valorFmt`,
`diaLabel`, `statusHelperLabel`, `isAtiva`/`isPausada`, `isLoading`/
`isSuccess`/`isError`, `nomeError`/`valorError`/`diaError`/`dataInicioError`,
`isFormValid`, `showXxxError`), com uma mudança:

- `hasUnsavedData`: em vez de comparar contra os valores padrão fixos do
  cadastro (`DEFAULT_CATEGORIA`/`DEFAULT_STATUS`), compara cada signal
  contra o valor correspondente em `initialSnapshot` — sair da tela de
  edição sem ter mudado nada não deveria disparar o diálogo de confirmação
  de saída (mesmo comportamento de "no-op", RF10 do refinamento
  funcional, espelhado no frontend).

## Carregamento inicial (novo em relação ao cadastro)

**RF-F01 — Buscar dados ao entrar na tela.** Ao ativar a rota
`despesas/:id/editar`, o componente lê `id` da rota e chama
`DespesaRecorrenteService.getById(id)`; enquanto a resposta não chega,
`loadStatus() === 'loading'`.

**RF-F02 — Preencher o formulário com a resposta.** Em sucesso, cada
signal (`nome`, `categoria`, etc.) é inicializado com o valor recebido,
`initialSnapshot` é gravado com a mesma cópia, e `loadStatus.set('loaded')`
— só a partir daqui o formulário fica editável e a pré-visualização
(`DespesaPreviewComponent`) passa a refletir os dados carregados.

**RF-F03 — Despesa não encontrada.** Se o `GET` responder `404`,
`loadStatus.set('not-found')`; a tela mostra uma mensagem de "despesa não
encontrada" e não renderiza o formulário (não há como editar algo que não
existe).

**RF-F04 — Falha ao carregar (rede/5xx).** `loadStatus.set('error')`; a
tela mostra um estado de erro genérico com um botão "Tentar novamente" que
repete a chamada de RF-F01 — mesmo padrão visual já usado no cadastro para
falha ao salvar (`isError`/"Tentar novamente" em
`cadastro-despesa-recorrente.component.html`), reaproveitado aqui para uma
falha de leitura em vez de escrita.

## Campos do formulário
Idênticos aos do cadastro (`cadastro-despesa-recorrente.md`, seção "Campos
do formulário"), com a mesma validação client-side por campo — nome
(RF02), categoria (RF03), valor previsto mensal (RF04), dia de vencimento
(RF05), data de início (RF06), status (RF08), observação opcional (RF09).
A única diferença: **frequência continua fixa em "Mensal" e não
editável**, exatamente como no cadastro (RF01 do refinamento funcional de
edição confirma que frequência não está entre os campos editáveis).

## Interações / eventos
Idênticos aos já descritos em `cadastro-despesa-recorrente.md` para
`onChangeXxx`/`onBlurXxx`/`onSetAtiva`/`onSetPausada`, com estas
diferenças:

| Evento | Diferença em relação ao cadastro |
|---|---|
| `onSalvar` | chama `DespesaRecorrenteService.update(id, payload)` em vez de `create(payload)`; em sucesso, `formStatus.set('success')` e `initialSnapshot` é atualizado para o novo estado salvo (para que um segundo salvamento consecutivo sem mudanças volte a ser tratado como no-op) |
| `onTentarNovamente` | reexecuta `onSalvar()`, igual ao cadastro |
| `onNovaDespesa` | **não existe** nesta tela — não há "editar outra despesa" a partir daqui; o estado de sucesso (ver abaixo) oferece apenas voltar ao painel mensal |
| `onClickVoltar` | mesmo comportamento do cadastro (confirma saída via `hasUnsavedData`, ver acima) |

## Estados visuais da tela

1. **Carregando dados** (`loadStatus() === 'loading'`) — tela mostra um
   indicador de carregamento no lugar do formulário; nenhum campo é
   renderizado ainda (evita mostrar campos vazios por uma fração de
   segundo antes do preenchimento).
2. **Não encontrada** (`loadStatus() === 'not-found'`) — mensagem
   dedicada, sem formulário, com um link/botão para voltar ao painel
   mensal.
3. **Erro ao carregar** (`loadStatus() === 'error'`) — banner de erro com
   "Tentar novamente" (RF-F04).
4. **Preenchimento** (`loadStatus() === 'loaded'` e `formStatus() === 'idle'`)
   — idêntico ao cadastro: formulário editável, pré-visualização em tempo
   real, erros inline por campo após `touched`/`submitAttempted`.
5. **Salvando** (`formStatus() === 'loading'`) — idêntico ao cadastro:
   botão com spinner, rótulo "Salvando alterações…".
6. **Sucesso ao salvar** (`formStatus() === 'success'`) — banner de
   confirmação; diferente do cadastro, **não** oferece "Cadastrar outra
   despesa" (não existe `onNovaDespesa` aqui) — oferece apenas voltar ao
   painel mensal.
7. **Erro ao salvar** (`formStatus() === 'error'`) — idêntico ao cadastro:
   banner vermelho com "Tentar novamente", e mapeamento de erros `400` por
   campo (`apiFieldErrors`) igual ao já descrito em
   `cadastro-despesa-recorrente.md`.

## Contrato de API necessário

Reaproveita o envelope `ApiResponse<TData>`/`ApiError` e o formato de erro
por campo já fixados pelo `POST` de cadastro
(`cadastro-despesa-recorrente.md`, seção "Contrato de API necessário"). O
recorte funcional completo de cada endpoint (campos, regras de validação
por campo, formato de `404`) já está descrito no refinamento backend
correspondente — esta seção só resume o que o `DespesaRecorrenteService`
precisa consumir.

### `GET /api/v1/recurring-expenses/{id}`
Usado por RF-F01/RF-F02/RF-F03. Devolve os mesmos campos editáveis do
formulário (nome, categoria, valor previsto mensal, dia de vencimento,
data de início, status, observação); `404` quando o id não existe (ver
[`editar-despesa-recorrente.md`](../backend/editar-despesa-recorrente.md),
seção "Camada API").

### `PUT /api/v1/recurring-expenses/{id}`
Usado por `onSalvar`. Mesmo corpo de request do `POST` de cadastro, sem o
campo `frequency` (não editável); resposta `200` com os dados atualizados,
`400` com erros por campo no mesmo formato do cadastro, `404` quando o id
não existe.

`despesa-recorrente.service.ts` ganha dois métodos novos
(`getById(id)`/`update(id, payload)`), ao lado do `create(payload)` já
existente — sem alterar a assinatura ou o comportamento do que já existe.

## Pontos em aberto

- **Ausência de tela de design**, já registrada na "Origem": todo o
  desenho de interação deste documento (ponto de entrada, estados de
  carregamento/não-encontrado, ausência de "editar outra despesa" no
  sucesso) é proposta, não transcrição de um protótipo — precisa de
  validação visual antes da implementação final, do mesmo jeito que
  `cadastro-despesa-recorrente.md` pôde nascer direto de
  `Cadastro.dc.html`.
- **Local exato do botão/ícone "Editar"** no painel mensal — depende do
  design (ver "Ponto de entrada").
- **Extração das funções de máscara/parse/validação para um utilitário
  compartilhado** entre cadastro e edição, para não duplicá-las — decisão
  de implementação, não bloqueia este refinamento.
- **Herda os pontos em aberto do refinamento funcional**, em especial: se
  a reativação (Pausada → Ativa) deve mesmo gerar a ocorrência da
  competência atual (o que, no frontend, significa que o
  `statusHelperLabel` ao trocar para "Ativa" durante a edição deveria
  avisar sobre isso, assim como já avisa no cadastro) — e se há alguma
  restrição a impor no client ao editar a data de início.
- **Botão "Cancelar"** (equivalente ao `onClickVoltar` do cadastro): assume-
  se o mesmo comportamento (confirmação de saída se houver alteração não
  salva) — não há, no design inexistente desta tela, nada que sugira o
  contrário.
