# Feature Specification: Excluir Despesa Recorrente (Tela)

**Feature Branch**: `011-delete-recurring-expense-ui`

**Created**: 2026-10-31

**Status**: Draft

**Input**: User description: "@refinements/frontend/deletar-despesa-recorrente.md"

## Clarifications

### Session 2026-10-31

- Q: O refinamento de frontend propõe substituir a confirmação inline (já
  no design `Main.dc.html`) por um modal, para caber a segunda pergunta de
  abrangência. Qual superfície a especificação deve assumir? → A: Modal
  (overlay) — reaproveita o padrão já existente de confirmação para ações
  destrutivas (o mesmo usado para "sair sem salvar" no cadastro/edição).
- Q: Qual abrangência de exclusão deve vir pré-selecionada por padrão na
  confirmação? → A: "Apenas pendentes/vencidas" — a opção mais segura, que
  preserva o histórico de pagamentos por padrão.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Excluir uma despesa recorrente preservando o histórico pago (Priority: P1)

Uma pessoa percebe, no Painel Mensal de Despesas, que uma despesa
recorrente não deveria mais existir (ex.: assinatura cancelada, conta
encerrada). Ela aciona a exclusão a partir de qualquer ocorrência dessa
despesa, confirma que quer excluir apenas o que ainda não foi pago, e vê a
despesa parar de gerar novas contas — sem perder o registro do que já foi
pago.

**Why this priority**: É o caminho mais seguro e, por isso, a opção
pré-selecionada por padrão (ver "Clarifications"); sem ele, a única forma
de "cancelar uma assinatura" seria apagar também o histórico financeiro já
registrado, o que a maioria das pessoas não espera ao excluir algo.

**Independent Test**: Pode ser testado isoladamente acionando a exclusão de
uma despesa recorrente que tem ao menos uma ocorrência paga e uma
pendente, confirmando com a abrangência "apenas pendentes/vencidas", e
verificando que a ocorrência pendente desaparece da lista enquanto a
ocorrência paga permanece visível, inalterada, na competência em que foi
paga.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente com uma ocorrência paga e uma
   pendente no Painel Mensal, **When** a pessoa aciona a exclusão, confirma
   com a abrangência "apenas pendentes/vencidas", **Then** a linha da
   ocorrência pendente desaparece da lista e os totais do topo (previsto,
   pago, pendente) e os alertas de vencidas/vence-em-breve são atualizados
   sem a ocorrência removida.
2. **Given** a mesma despesa recorrente já excluída dessa forma, **When** a
   pessoa navega até a competência em que a ocorrência paga foi registrada,
   **Then** essa ocorrência continua aparecendo normalmente, marcada como
   paga.
3. **Given** a mesma despesa recorrente já excluída dessa forma, **When** a
   pessoa navega até uma competência futura, **Then** nenhuma ocorrência
   nova dessa despesa aparece.

---

### User Story 2 - Excluir uma despesa recorrente e todo o seu histórico (Priority: P2)

Uma pessoa quer remover completamente uma despesa recorrente cadastrada
por erro, incluindo qualquer ocorrência já paga dela — não apenas
interrompê-la a partir de agora.

**Why this priority**: É um caminho igualmente necessário (RF06/RF07 do
refinamento funcional não permitem um meio-termo), mas é a opção mais
arriscada e por isso exige um passo deliberado (trocar a seleção padrão),
o que a torna secundária à opção segura da User Story 1.

**Independent Test**: Pode ser testado isoladamente acionando a exclusão
de uma despesa recorrente que tem ao menos uma ocorrência paga,
selecionando deliberadamente a abrangência "excluir todo o histórico" (ao
invés da opção pré-selecionada), confirmando, e verificando que nenhuma
ocorrência dessa despesa — paga ou não — continua acessível em nenhuma
competência.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente com ocorrências pagas e pendentes,
   **When** a pessoa aciona a exclusão, troca a seleção para "excluir todo
   o histórico" e confirma, **Then** nenhuma ocorrência dessa despesa
   continua acessível em nenhuma competência do Painel Mensal, incluindo as
   que já estavam pagas.
2. **Given** a despesa recorrente excluída com essa abrangência, **When** a
   pessoa tenta abrir a tela de edição dela (ex.: por um link salvo ou
   histórico do navegador), **Then** o sistema trata como "não encontrada",
   do mesmo jeito que trataria um identificador que nunca existiu.

---

### User Story 3 - Confirmar antes de excluir, com a abrangência sempre visível (Priority: P1)

Antes que qualquer exclusão aconteça de fato, a pessoa precisa ver
claramente as duas opções de abrangência, entender a diferença entre elas,
e poder cancelar sem que nada seja alterado.

**Why this priority**: É o que torna as duas abrangências (User Stories 1
e 2) possíveis e seguras de usar — sem essa confirmação explícita, um
clique isolado no botão de exclusão apagaria dados irreversivelmente, sem
chance de revisão.

**Independent Test**: Pode ser testado isoladamente acionando o botão de
exclusão e verificando que nenhuma despesa é excluída até uma confirmação
explícita; e, separadamente, cancelando a confirmação e verificando que
nada foi alterado.

**Acceptance Scenarios**:

1. **Given** a pessoa está vendo o Painel Mensal, **When** ela aciona o
   botão de exclusão de uma ocorrência, **Then** aparece uma confirmação
   explícita com as duas abrangências de exclusão, cada uma com uma frase
   explicando o que vai acontecer, antes que qualquer dado seja alterado.
2. **Given** a confirmação está visível, **When** a pessoa a cancela sem
   escolher excluir, **Then** nenhuma despesa é excluída e nenhum dado é
   alterado.
3. **Given** a confirmação está visível, **When** a pessoa ainda não
   trocou a seleção, **Then** a abrangência "apenas pendentes/vencidas"
   já aparece selecionada por padrão.

---

### User Story 4 - Ser informado com clareza quando a exclusão falha (Priority: P3)

A chamada de exclusão pode falhar por problema de rede, por a despesa já
ter sido excluída em outra aba, ou por outro erro do servidor. A pessoa
precisa entender o que aconteceu e poder tentar novamente sem perder a
escolha que já tinha feito.

**Why this priority**: É um desvio do caminho feliz; melhora a robustez
percebida das User Stories 1 e 2, mas não é necessário para que o valor
principal da exclusão já funcione.

**Independent Test**: Pode ser testado isoladamente simulando uma falha na
chamada de exclusão (ex.: rede indisponível) e verificando que a
confirmação permanece visível com uma mensagem de erro e a abrangência
escolhida continua selecionada, permitindo tentar novamente ou cancelar.

**Acceptance Scenarios**:

1. **Given** a pessoa confirmou a exclusão, **When** a chamada ao servidor
   falha (rede ou erro do servidor), **Then** a confirmação permanece
   visível com uma mensagem de erro, a abrangência escolhida continua
   selecionada, e a pessoa pode tentar novamente ou cancelar.
2. **Given** a pessoa confirmou a exclusão de uma despesa que já havia sido
   excluída por outra aba/sessão nesse intervalo, **When** a chamada ao
   servidor retorna "não encontrada", **Then** a confirmação é fechada e a
   lista do Painel Mensal é atualizada (a linha já não existe mais no
   servidor).

---

### Edge Cases

- O que acontece se a pessoa excluir (abrangência "apenas
  pendentes/vencidas") uma despesa recorrente que só tem ocorrências
  pagas? Nenhuma ocorrência desaparece da lista, mas a despesa deixa de
  gerar qualquer ocorrência nova a partir de agora.
- O que acontece se a ocorrência da competência exibida ainda não foi
  paga e a pessoa exclui com "apenas pendentes/vencidas"? Essa ocorrência
  específica é removida e a linha desaparece do Painel na competência
  atual.
- O que acontece se a ocorrência da competência exibida já foi paga e a
  pessoa exclui com "apenas pendentes/vencidas"? Essa linha continua
  aparecendo exatamente como antes; só deixam de existir ocorrências
  futuras.
- O que acontece se a pessoa estiver, ao mesmo tempo, editando o
  pagamento de uma ocorrência da mesma despesa (ou de outra linha) quando
  aciona a exclusão? Os dois fluxos são independentes; nenhuma interação
  entre eles é definida por esta especificação.
- O que acontece se a exclusão de todo o histórico (User Story 2) for
  confirmada e, em seguida, a pessoa tentar excluir a mesma despesa de
  novo (ex.: duplo clique, duas abas)? A segunda tentativa é tratada como
  "não encontrada", do mesmo jeito que acessar uma despesa que nunca
  existiu.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer, em cada linha de ocorrência do
  Painel Mensal, um botão de ação dedicado (ícone de lixeira) ao lado do
  botão de editar já existente, para acionar a exclusão da despesa
  recorrente dona daquela ocorrência.
- **FR-002**: Acionar a exclusão a partir de qualquer ocorrência de uma
  despesa recorrente MUST referir-se à despesa recorrente inteira (todas
  as suas ocorrências, sujeitas à abrangência escolhida), nunca apenas à
  ocorrência da competência exibida.
- **FR-003**: Clicar no botão de exclusão MUST NOT excluir nada
  imediatamente; o sistema MUST exigir, antes, uma confirmação explícita
  apresentada como um modal (sobreposição centralizada), reaproveitando o
  mesmo padrão visual já usado pelo projeto para confirmações de ações
  destrutivas (ex.: "sair sem salvar" no cadastro/edição).
- **FR-004**: A confirmação MUST apresentar duas opções de abrangência,
  mutuamente exclusivas, cada uma acompanhada de uma frase explicando o
  que acontece com as ocorrências já pagas: "excluir apenas as
  pendentes/vencidas" (preserva o histórico pago) e "excluir todo o
  histórico" (remove também as ocorrências pagas, de forma irreversível).
- **FR-005**: Ao abrir a confirmação, a opção "excluir apenas as
  pendentes/vencidas" MUST já aparecer selecionada por padrão; a pessoa
  MUST poder trocar a seleção para "excluir todo o histórico" antes de
  confirmar.
- **FR-006**: A confirmação MUST oferecer uma forma de cancelar que, ao
  ser acionada, MUST NOT alterar nenhum dado e MUST fechar a confirmação
  sem nenhum efeito.
- **FR-007**: Confirmar a exclusão com a abrangência "apenas
  pendentes/vencidas" MUST resultar em: toda ocorrência não paga dessa
  despesa deixa de aparecer no Painel Mensal, toda ocorrência já paga
  continua aparecendo normalmente na competência em que foi paga, e a
  despesa para de gerar qualquer ocorrência nova a partir de então.
- **FR-008**: Confirmar a exclusão com a abrangência "excluir todo o
  histórico" MUST resultar em nenhuma ocorrência dessa despesa — paga ou
  não — continuar acessível em nenhuma competência do Painel Mensal.
- **FR-009**: Enquanto a exclusão confirmada está em andamento, o sistema
  MUST exibir um estado de carregamento distinto na confirmação (ex.:
  botão de confirmar desabilitado com rótulo de carregamento), impedindo
  um segundo envio da mesma exclusão.
- **FR-010**: Ao concluir uma exclusão com sucesso (qualquer abrangência),
  o sistema MUST fechar a confirmação e MUST atualizar a lista do Painel
  Mensal da competência exibida, de forma que: a linha correspondente à
  ocorrência excluída (quando aplicável) deixe de aparecer, e os totais do
  topo (previsto, pago, pendente) e os alertas de vencidas/vence-em-breve
  reflitam a remoção sem exigir um recarregamento manual da página.
- **FR-011**: Caso a chamada de exclusão falhe por erro de rede ou do
  servidor, o sistema MUST manter a confirmação visível, com uma mensagem
  de erro e a abrangência previamente selecionada preservada, permitindo
  tentar novamente ou cancelar sem perder a escolha feita.
- **FR-012**: Caso a chamada de exclusão retorne que a despesa recorrente
  já não existe (por já ter sido excluída antes, por qualquer abrangência,
  em outra aba/sessão), o sistema MUST fechar a confirmação e MUST
  atualizar a lista do Painel Mensal, refletindo que a despesa já não está
  mais presente.
- **FR-013**: O sistema MUST NOT oferecer, nesta tela, uma terceira opção
  de abrangência além das duas definidas em FR-004.

### Key Entities *(include if feature involves data)*

- **Despesa recorrente**: Entidade existente que é o alvo real de toda
  exclusão acionada por esta tela; a exclusão nunca é de uma ocorrência
  isolada, mesmo quando acionada a partir de uma linha específica do
  Painel Mensal.
- **Item do Painel Mensal**: Representação, na listagem mensal, de uma
  ocorrência de uma despesa recorrente; é o ponto a partir do qual a
  exclusão da despesa recorrente dona daquele item é acionada (FR-001),
  mas o efeito da exclusão se estende a todas as ocorrências da despesa,
  conforme a abrangência escolhida (FR-002, FR-007, FR-008).
- **Abrangência de exclusão**: Escolha binária feita na confirmação —
  "apenas pendentes/vencidas" ou "todo o histórico" — que determina se as
  ocorrências já pagas da despesa são preservadas ou removidas junto com
  as demais.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Nenhuma despesa recorrente é excluída sem uma confirmação
  explícita que exiba as duas abrangências antes de qualquer exclusão
  efetiva.
- **SC-002**: 100% das exclusões confirmadas com "apenas
  pendentes/vencidas" preservam todas as ocorrências já pagas da despesa,
  verificável navegando até a competência em que cada uma foi paga.
- **SC-003**: 100% das exclusões confirmadas com "excluir todo o
  histórico" removem completamente a despesa, sem nenhuma ocorrência
  dela — paga ou não — permanecendo acessível em qualquer competência.
- **SC-004**: Toda exclusão bem-sucedida reflete-se nos totais e na lista
  do Painel Mensal sem exigir um recarregamento manual da página.
- **SC-005**: Toda falha na exclusão apresenta uma ação clara de
  continuar (tentar novamente ou cancelar), nunca uma confirmação sem
  saída nem uma perda silenciosa da escolha já feita.

## Assumptions

- Esta especificação cobre apenas a tela e a experiência de exclusão
  (frontend); as regras de negócio sobre o efeito de cada abrangência
  sobre as ocorrências e sobre a geração futura já estão especificadas e
  resolvidas pela feature de backend correspondente
  (`010-delete-recurring-expense`) — esta tela apenas aciona esse
  comportamento e o comunica à pessoa usuária.
- A confirmação é apresentada como um modal (overlay), e não como a
  confirmação inline hoje presente no design de referência
  (`design/Main.dc.html`) — decisão tomada nesta especificação (ver
  "Clarifications") por não caber, de forma legível, a explicação das duas
  abrangências dentro do espaço de uma única linha da lista, especialmente
  no layout mobile já definido para o Painel Mensal. Isso diverge do
  design atual e requer uma atualização visual antes da implementação
  final; o botão de exclusão por linha em si (gatilho) permanece o mesmo
  já desenhado.
- A opção "apenas pendentes/vencidas" vem pré-selecionada por padrão na
  confirmação (ver "Clarifications"); excluir todo o histórico exige uma
  troca deliberada de seleção.
- A redação final dos rótulos e frases explicativas das duas abrangências
  na tela permanece um detalhe de texto a refinar na implementação; esta
  especificação fixa apenas o conteúdo mínimo que cada uma precisa
  comunicar (FR-004).
- Depois de uma exclusão bem-sucedida, a lista do Painel Mensal é
  atualizada buscando novamente os dados da competência exibida, em vez
  de removê-los apenas localmente — garante que a regra de preservação de
  ocorrências pagas (FR-007) seja sempre aplicada pela mesma fonte que já
  a implementa (o backend), sem duplicá-la no frontend.
- Não há, nesta etapa, nenhuma interação definida entre excluir uma
  despesa recorrente e uma de suas ocorrências estar, no mesmo instante,
  em edição de pagamento em outra linha do Painel Mensal.
- Não existe nenhuma operação de "desfazer exclusão" em nenhuma das duas
  abrangências; ambas são definitivas assim que confirmadas.
- Exclusão de despesa recorrente é uma capacidade distinta e não altera
  nada nas telas de cadastro ou de edição, exceto pelo efeito colateral já
  esperado de uma despesa excluída se tornar "não encontrada" para a
  tela de edição (comportamento definido pela feature de backend).
