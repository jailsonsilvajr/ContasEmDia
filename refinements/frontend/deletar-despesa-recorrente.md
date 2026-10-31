# Refinamento — Frontend: Deletar Despesa Recorrente

## Origem
Refinamento funcional: [`deletar-despesa-recorrente.md`](../deletar-despesa-recorrente.md).
Design: `design/Main.dc.html` — já recebeu, nesta mesma sessão, o botão de
exclusão por linha (ícone de lixeira) e uma confirmação **inline**, dentro
da própria linha da ocorrência ("Excluir esta despesa?" com os botões
"Excluir"/"Cancelar", ocupando o espaço da coluna de ações). Esse desenho
inline **não** cobre a segunda pergunta exigida pelo refinamento funcional
(RF03: escolher entre excluir todo o histórico ou preservar as ocorrências
pagas) — o protótipo, por ter sido feito antes dessa exigência, só
pergunta "excluir ou não". Este documento propõe estender esse fluxo para
um modal, pelas razões explicadas em "Confirmação com abrangência", e trata
essa mudança de forma explícita como uma proposta a validar contra uma
atualização do design — não como transcrição direta do protótipo atual,
no mesmo espírito de como `editar-despesa-recorrente.md` (frontend) tratou
decisões sem design de referência.

## Feature
Refinamento técnico da implementação **Angular** da exclusão de uma
despesa recorrente a partir do Painel Mensal de Despesas: o botão de
exclusão por linha, a confirmação com escolha de abrangência, a chamada ao
novo endpoint `DELETE`, e a atualização da lista após uma exclusão
bem-sucedida.

## Escopo
Cobre exclusivamente o lado frontend da exclusão: o botão por linha (já no
design), o componente/estado de confirmação com abrangência, o contrato do
`DELETE` necessário (no mesmo nível de detalhe que
`editar-despesa-recorrente.md` já documentou para `GET`/`PUT`), e como a
lista do Painel Mensal é atualizada depois de uma exclusão.

Não cobre: qualquer mudança na tela de cadastro ou de edição, a
implementação real do backend (apenas o contrato esperado), e qualquer
decisão que dependa de uma atualização do design ainda não produzida (ver
"Pontos em aberto").

## Referências
- Refinamento funcional (RF/EC citados abaixo são os dele, salvo indicação
  contrária): [`deletar-despesa-recorrente.md`](../deletar-despesa-recorrente.md).
- Refinamento backend correspondente (contrato do `DELETE`):
  [`deletar-despesa-recorrente.md`](../backend/deletar-despesa-recorrente.md).
- Design com o botão/confirmação inline já inserido: `design/Main.dc.html`.
- Código já implementado do Painel Mensal, base de toda a extensão proposta
  aqui: `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.ts`
  e `.component.html`.
- Padrão de modal de confirmação já existente no projeto (não inline):
  `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component.ts`
  (`showExitConfirmDialog`) e o bloco correspondente em
  `cadastro-despesa-recorrente.component.html` — mesmo padrão replicado em
  `editar-despesa-recorrente.component.ts`.

## Botão de exclusão por linha (RF01 do refinamento funcional)
Já coberto pelo design: um botão de ícone (lixeira) por `occurrence-row`,
ao lado do botão "Editar" já implementado
(`painel-mensal-despesas.component.html`, linhas 99–108). Segue o mesmo
padrão HTML/Tailwind desse botão (mesmo tamanho `h-8 w-8`, mesma borda,
`data-testid="excluir-btn"`), mas sem `routerLink` — aciona a confirmação
(ver abaixo) em vez de navegar.

## Confirmação com abrangência

### Inline (como no design) ou modal? Decisão proposta: modal
O design atual (`Main.dc.html`) resolve a confirmação **dentro da própria
linha**, trocando a coluna de ações por um texto + dois botões. Essa
solução funciona para uma pergunta binária simples ("excluir ou não"), mas
RF03 do refinamento funcional acrescenta uma segunda escolha (duas opções
de abrangência, cada uma com uma frase explicativa) — texto e controles
demais para caber ao lado das outras colunas da linha, especialmente no
layout mobile já definido em `painel-mensal-despesas.component.html` (a
coluna de ações vira `w-full` e empilha por baixo das outras colunas em
telas estreitas, RF do design responsivo).

Por isso, este documento propõe substituir a confirmação inline por um
modal — reaproveitando o padrão **já existente no projeto** para
confirmações importantes: `showExitConfirmDialog`
(`cadastro-despesa-recorrente.component.ts`/`editar-despesa-recorrente.component.ts`),
um overlay (`fixed inset-0 bg-black/40`) com um cartão central
(`role="alertdialog"`, `aria-modal="true"`). Excluir uma despesa —
sobretudo na abrangência "todo o histórico" (RF06) — é uma ação pelo menos
tão definitiva quanto "sair sem salvar" (o caso que já justificou aquele
modal), então reaproveitar o mesmo padrão visual é consistente com a
própria régua que o projeto já aplicou a esse tipo de decisão, em vez de
inventar uma segunda linguagem de confirmação (inline) só para esta tela.
**Isso diverge do desenho atual de `Main.dc.html` — ver "Pontos em
aberto"**: o botão de exclusão em si e o gatilho continuam os mesmos que já
estão no design; só a superfície da confirmação muda de "inline na linha"
para "modal".

### Conteúdo do modal
- Título: algo como "Excluir despesa recorrente?" (rótulo de trabalho; ver
  "Pontos em aberto" do refinamento funcional para a redação final).
- Duas opções mutuamente exclusivas (`radio`), cada uma com uma frase
  explicativa curta:
  1. "Excluir apenas as pendentes/vencidas" — "As ocorrências já pagas
     continuam no seu histórico." (RF04/RF05 do refinamento funcional)
  2. "Excluir todo o histórico" — "Remove também as ocorrências já pagas.
     Esta ação não pode ser desfeita." (RF06 do refinamento funcional)
  Uma delas pré-selecionada por padrão — **ponto em aberto**, ver
  refinamento funcional.
- Dois botões: "Cancelar" (fecha o modal sem excluir, EC05) e "Excluir"
  (confirma com a opção selecionada) — mesmo par de papéis visuais que
  `showExitConfirmDialog` já usa para "Continuar editando"/"Sair sem
  salvar" (botão destrutivo com `bg-danger`).
- Enquanto a chamada ao `DELETE` está em andamento, o botão "Excluir" fica
  desabilitado com um rótulo de carregamento (mesmo padrão de "Salvando
  alterações…" já usado no formulário de cadastro/edição para o botão de
  salvar).
- Se a chamada falhar (rede/5xx/404 — despesa já excluída por outra aba,
  EC08 do refinamento funcional), o modal permanece aberto com uma
  mensagem de erro inline e permite tentar novamente ou cancelar — mesmo
  padrão de erro já usado nos formulários de cadastro/edição.

## Estado do componente (`PainelMensalDespesasComponent`, signals)

Novos signals, ao lado dos já existentes (`editingOccurrenceId`,
`draftValor`, `draftData`):

| Signal | Papel |
|---|---|
| `deletingItem` | `signal<PanelDisplayItem \| null>(null)` — item cuja exclusão está em confirmação; controla a visibilidade do modal (`@if (deletingItem())`) |
| `deleteScope` | `signal<'all' \| 'pending-only'>('pending-only')` — opção selecionada no modal; valor inicial é a pré-seleção padrão (ver "Pontos em aberto" do refinamento funcional; `'pending-only'` é o valor de trabalho usado aqui) |
| `deleteStatus` | `signal<'idle' \| 'loading' \| 'error'>('idle')` — estado da chamada ao `DELETE`, análogo ao `formStatus` do cadastro/edição, mas escopado ao modal |

## Interações / eventos

**RF-F01 — Abrir a confirmação.** `onSolicitarExclusao(item: PanelDisplayItem)`
define `deletingItem.set(item)`, `deleteScope.set('pending-only')` (reseta
para o padrão a cada abertura) e `deleteStatus.set('idle')`.

**RF-F02 — Trocar a abrangência.** `onSelecionarAbrangencia(scope: 'all' |
'pending-only')` só faz `deleteScope.set(scope)` — não dispara nenhuma
chamada de rede.

**RF-F03 — Cancelar (EC05 do refinamento funcional).** `onCancelarExclusao()`
define `deletingItem.set(null)`; nenhum dado é alterado.

**RF-F04 — Confirmar exclusão.** `onConfirmarExclusao()`: com
`deletingItem()` e `deleteScope()` atuais, chama
`DespesaRecorrenteService.remove(item.recurringExpenseId, scope)`
(ver "Contrato de API necessário"); enquanto pendente,
`deleteStatus.set('loading')`.
- Em sucesso: `deletingItem.set(null)`, `deleteStatus.set('idle')`, e a
  lista é atualizada (ver "Atualização da lista" abaixo).
- Em erro: `deleteStatus.set('error')`; o modal permanece aberto (ver
  "Conteúdo do modal").

## Atualização da lista após exclusão (RF09 do refinamento funcional)

**Decisão proposta: recarregar a competência atual do Painel, não filtrar
o array localmente.** A alternativa mais óbvia seria remover
otimisticamente, no cliente, todas as linhas com o
`recurringExpenseId` excluído — mas isso exigiria replicar no frontend a
mesma regra condicional que já vive no backend (RF04 do refinamento
funcional: na abrangência "apenas pendentes/vencidas", uma ocorrência **já
paga** da despesa excluída deve **continuar** aparecendo; só as não pagas
saem — EC04 daquele documento). Duplicar essa condição no cliente é o tipo
de acoplamento que o Princípio V (simplicidade) recomenda evitar quando o
próprio backend já teria que ser a fonte da verdade de qualquer forma; a
resposta do `DELETE` (RF13 do refinamento funcional/backend) nem devolve
dados suficientes para o cliente decidir isso sozinho, de propósito (ver
"Camada Application" do refinamento de backend). Por isso,
`onConfirmarExclusao()` chama, em caso de sucesso, o mesmo método privado
que a navegação de mês já usa hoje (`loadPeriod`/`load`, reaproveitados sem
alteração), obtendo do backend a lista já correta para a competência
exibida — mesmo princípio de "o servidor decide o que aparece" que o
próprio Painel já segue para tudo o mais.

## Template (`painel-mensal-despesas.component.html`)

Na coluna de ações (`data-testid="occurrence-row"`, bloco atual nas linhas
98–182), ao lado do botão "Editar" já existente:

```html
<button
  type="button"
  data-testid="excluir-btn"
  aria-label="Excluir despesa recorrente"
  title="Excluir despesa recorrente"
  class="flex h-8 w-8 shrink-0 cursor-pointer items-center justify-center rounded-lg border border-border bg-transparent p-0 disabled:cursor-default"
  (click)="onSolicitarExclusao(item)"
>
  <!-- ícone de lixeira, mesmo já usado em design/Main.dc.html -->
</button>
```

E, fora do `@for`, ao lado do bloco `@if (showExitConfirmDialog())` já
existente (reaproveitando a mesma estrutura de overlay):

```html
@if (deletingItem()) {
  <div class="fixed inset-0 z-10 flex items-center justify-center bg-black/40 p-4" data-testid="excluir-confirm-overlay">
    <div class="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl" role="alertdialog" aria-modal="true" aria-labelledby="excluir-confirm-title">
      <p id="excluir-confirm-title" class="text-[15px] font-bold">Excluir despesa recorrente?</p>
      <!-- dois radios: 'pending-only' / 'all', ligados a deleteScope()/onSelecionarAbrangencia() -->
      <!-- mensagem de erro inline quando deleteStatus() === 'error' -->
      <div class="mt-5 flex flex-wrap justify-end gap-2.5">
        <button type="button" data-testid="cancelar-exclusao-btn" (click)="onCancelarExclusao()">Cancelar</button>
        <button type="button" data-testid="confirmar-exclusao-btn" [disabled]="deleteStatus() === 'loading'" (click)="onConfirmarExclusao()">Excluir</button>
      </div>
    </div>
  </div>
}
```

## Contrato de API necessário

Reaproveita o envelope `ApiEnvelope<T>` já usado por todas as outras
chamadas (`despesa-recorrente.model.ts`). O recorte funcional completo do
endpoint (abrangências aceitas, formato de `404`) já está descrito no
refinamento de backend correspondente — esta seção só resume o que
`DespesaRecorrenteService` precisa consumir.

### `DELETE /api/v1/recurring-expenses/{id}?scope={all|pending-only}`
Usado por `onConfirmarExclusao()`. `200` com uma confirmação mínima
(id + abrangência aplicada); `400` se a abrangência for omitida/inválida
(não deveria acontecer partindo da UI, que sempre envia um dos dois
valores fixos do `radio`); `404` quando o id não existe ou já foi excluído
antes (RF11/RF14 do refinamento funcional).

`despesa-recorrente.service.ts` ganha um método novo:

```ts
remove(id: string, scope: 'all' | 'pending-only'): Observable<ApiEnvelope<DeleteRecurringExpenseResponse>> {
  return this.http.delete<ApiEnvelope<DeleteRecurringExpenseResponse>>(
    `/api/v1/recurring-expenses/${id}?scope=${scope}`,
  );
}
```

ao lado de `create`/`getById`/`update` já existentes, sem alterar nenhum
deles. `despesa-recorrente.model.ts` ganha o tipo
`DeleteRecurringExpenseResponse` (`{ id: string; scope: 'all' |
'pending-only' }`, espelhando o `DeleteRecurringExpenseDataResponse` do
backend).

## Pontos em aberto

- **Modal em vez de confirmação inline.** Esta é a decisão técnica mais
  relevante deste documento e diverge do desenho hoje presente em
  `design/Main.dc.html` (confirmação inline, sem a pergunta de
  abrangência) — precisa de validação visual (uma atualização do design,
  ou aprovação direta desta proposta) antes da implementação final, do
  mesmo jeito que outras decisões sem design de referência já foram
  tratadas em `editar-despesa-recorrente.md` (frontend).
- **Qual abrangência vem pré-selecionada por padrão** — herdado do
  refinamento funcional; este documento usa `'pending-only'` como valor de
  trabalho, mas não decide.
- **Redação final dos rótulos e frases explicativas do modal** — nomes de
  trabalho aqui, mesma ressalva do refinamento funcional.
- **Ícone de lixeira exato** — este documento assume o mesmo SVG já
  inserido em `design/Main.dc.html` para o botão de exclusão; não foi
  redesenhado aqui.
- **Recarregar a competência atual vs. remover otimisticamente a linha
  clicada** (ver "Atualização da lista"): a decisão proposta aceita uma
  pequena perda de responsividade percebida (um novo request em vez de uma
  atualização instantânea do array local) em troca de não duplicar a
  regra de negócio de RF04 no cliente — alternativa a confirmar se a
  responsividade percebida for considerada mais importante.
- **Interação com edição de pagamento em andamento na mesma linha (EC06 do
  refinamento funcional)** — não definido se abrir a confirmação de
  exclusão deveria fechar automaticamente um `edicao-pagamento` em
  andamento na mesma linha, ou em outra linha da mesma despesa.
