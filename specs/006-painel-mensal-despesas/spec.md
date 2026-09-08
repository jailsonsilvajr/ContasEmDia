# Feature Specification: Painel Mensal de Despesas

**Feature Branch**: `006-painel-mensal-despesas`

**Created**: 2026-09-07

**Status**: Draft

**Input**: User description: "Feature: Painel mensal de despesas seguindo o refinamento @refinements/painel-mensal-despesas.md e seguindo totalmente o desing já definido em @design/Main.dc.html"

## Clarifications

### Session 2026-09-07

- Q: As setas de navegação de mês e o botão "Nova despesa" fazem parte do
  escopo desta feature? Se o botão "Nova despesa" fizer parte do escopo,
  ele deve abrir e concluir o fluxo completo de cadastro de uma despesa
  recorrente, ou apenas navegar para uma tela de cadastro que é
  responsabilidade de outra feature? → A: Ambos os controles (setas de
  navegação de mês e botão "Nova despesa") fazem parte do escopo desta
  feature; o botão "Nova despesa" apenas navega para a rota
  `/despesas/nova` — o fluxo de cadastro em si permanece fora do escopo
  desta especificação.

### Session 2026-09-08

- Q: O comportamento "Voltar ao painel"/confirmação (FR-023–FR-024) e as
  duas ações da tela de sucesso (FR-025) devem ser implementados dentro
  desta mesma feature (006), alterando o componente de cadastro já entregue
  pela feature 002, ou esta especificação apenas documenta o requisito para
  um planejamento/implementação separados? → A: Implementar agora, como
  parte da feature 006 — alterando diretamente o componente
  `cadastro-despesa-recorrente` já existente (entregue pela feature 002)
  durante o planejamento/implementação desta feature.
- Q: Elementos clicáveis desabilitados/inativos (ex.: um botão "Confirmar"
  desabilitado) devem ficar de fora do requisito de cursor de mão (FR-022),
  seguindo o `button:disabled { cursor: default; }` já definido nos designs
  de referência, ou o cursor de mão deve aparecer mesmo sobre elementos
  desabilitados? → A: Excluir elementos desabilitados — o cursor de mão se
  aplica apenas a elementos habilitados/acionáveis; controles desabilitados
  ou inativos usam o cursor padrão (não pointer).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visualizar o painel mensal de uma competência (Priority: P1)

Um usuário abre o painel mensal e vê, para o mês/ano (competência) exibido,
todas as ocorrências de despesas recorrentes daquele período: um cabeçalho
identificando a competência, banners de alerta para contas vencidas e contas
vencendo em breve (quando aplicável), três cartões de resumo (total
previsto, total pago, total pendente) e a lista de ocorrências com seus
dados essenciais (nome, categoria, valor previsto, dia de vencimento e um
selo de status calculado).

**Why this priority**: É a funcionalidade central da tela — sem ela não há
painel algum; todas as demais ações (marcar como paga, desfazer) dependem de
primeiro conseguir visualizar a lista de ocorrências de uma competência.

**Independent Test**: Pode ser testado sozinho carregando o painel para uma
competência com um conjunto conhecido de ocorrências (algumas pagas,
algumas vencidas, algumas vencendo em breve, algumas pendentes) e
verificando que cabeçalho, banners, cartões de resumo e lista batem com os
dados esperados, sem executar nenhuma ação de pagamento.

**Acceptance Scenarios**:

1. **Given** uma competência com N ocorrências cadastradas (pagas e não
   pagas, com vencimentos variados), **When** o usuário abre o painel para
   essa competência, **Then** o título da lista mostra o mês/ano da
   competência e o contador mostra exatamente N ocorrências.
2. **Given** que existe ao menos uma ocorrência não paga cujo vencimento já
   passou, **When** o painel é exibido, **Then** o banner de contas
   vencidas aparece mostrando a contagem correta dessas ocorrências.
3. **Given** que existe ao menos uma ocorrência não paga vencendo hoje ou
   em até 7 dias, **When** o painel é exibido, **Then** o banner de contas
   vencendo em breve aparece mostrando a contagem e a soma dos valores
   previstos apenas dessas ocorrências.
4. **Given** que nenhuma ocorrência está vencida e nenhuma vence em breve,
   **When** o painel é exibido, **Then** nenhum dos dois banners aparece.
5. **Given** o conjunto de ocorrências da competência, **When** o painel é
   exibido, **Then** "Total previsto" soma o valor previsto de todas as
   ocorrências, "Total pago" soma o valor efetivamente pago das ocorrências
   pagas, e "Total pendente" soma o valor previsto apenas das ocorrências
   não pagas.
6. **Given** uma competência sem nenhuma ocorrência, **When** o painel é
   exibido, **Then** a lista aparece vazia, o contador mostra "0 contas",
   os três cartões de resumo mostram € 0,00 e nenhum banner aparece.
7. **Given** uma ocorrência não paga, **When** o painel calcula seu status,
   **Then** o selo mostra "Vencida" se o vencimento já passou, "Vence em
   breve" se o vencimento é hoje ou em até 7 dias, ou "Pendente" se faltam
   mais de 7 dias — e uma ocorrência já paga sempre mostra "Paga",
   independentemente da data de vencimento.

---

### User Story 2 - Marcar uma ocorrência como paga (Priority: P2)

A partir do painel já carregado (User Story 1), o usuário registra o
pagamento de uma ocorrência não paga: inicia a edição, ajusta (ou mantém) o
valor pago e a data de pagamento sugeridos, e confirma — a ocorrência passa
a exibir seu novo estado "paga".

**Why this priority**: É a ação mais frequente e de maior valor da tela
(registrar que uma conta foi paga), mas só faz sentido depois que a
listagem (User Story 1) já existe; por isso vem em segundo lugar.

**Independent Test**: Pode ser testado isoladamente carregando um painel
com ao menos uma ocorrência não paga, iniciando o pagamento, confirmando
com valor/data válidos (e depois repetindo com valor/data em branco ou
inválidos) e verificando que a ocorrência passa a "paga" com os dados
corretos em cada caso, sem depender de nenhuma outra ação da tela.

**Acceptance Scenarios**:

1. **Given** uma ocorrência não paga e nenhuma outra ocorrência em edição,
   **When** o usuário inicia o pagamento dela, **Then** a ocorrência entra
   em modo de edição com valor pré-preenchido igual ao valor previsto e
   data pré-preenchida igual à data de hoje.
2. **Given** uma ocorrência já em modo de edição, **When** o usuário inicia
   o pagamento de outra ocorrência sem confirmar nem cancelar a primeira,
   **Then** a primeira volta ao estado normal (não editando, não paga) e a
   segunda entra em edição — apenas uma ocorrência pode estar em edição por
   vez.
3. **Given** uma ocorrência em edição com um valor numérico válido e uma
   data preenchida, **When** o usuário confirma, **Then** a ocorrência
   passa a "paga" com exatamente esse valor e essa data, e a tela sai do
   modo de edição.
4. **Given** uma ocorrência em edição cujo campo de valor está vazio ou não
   é um número válido, **When** o usuário confirma, **Then** o valor
   previsto da ocorrência é usado como valor pago.
5. **Given** uma ocorrência em edição cujo campo de data está vazio,
   **When** o usuário confirma, **Then** a data de hoje é usada como data
   de pagamento.
6. **Given** uma ocorrência em edição, **When** o usuário clica em
   "Cancelar", **Then** a edição é encerrada sem alterar nenhum dado da
   ocorrência.
7. **Given** uma ocorrência que acabou de ser marcada como paga com valor
   diferente do valor previsto, **When** o painel exibe essa ocorrência,
   **Then** o valor pago aparece com destaque visual distinto e o aviso
   "diferente do previsto"; se o valor pago for igual ao previsto, aparece
   sem esse aviso.

---

### User Story 3 - Desfazer o pagamento de uma ocorrência (Priority: P3)

A partir de uma ocorrência já paga (resultado da User Story 2 ou de dados
pré-existentes), o usuário desfaz o pagamento, e a ocorrência volta a ficar
disponível para ser marcada como paga novamente.

**Why this priority**: É uma ação de correção, usada com menor frequência
do que registrar um pagamento; depende de existir ao menos uma ocorrência
paga para fazer sentido, por isso vem por último.

**Independent Test**: Pode ser testado isoladamente carregando um painel
com ao menos uma ocorrência já paga, clicando em "Desfazer" e verificando
que ela volta ao estado "não paga", sem valor pago nem data de pagamento, e
que a ação "Marcar como paga" volta a ficar disponível para ela.

**Acceptance Scenarios**:

1. **Given** uma ocorrência paga, **When** o usuário clica em "Desfazer",
   **Then** ela volta imediatamente a "não paga", sem confirmação
   adicional, com o valor pago e a data de pagamento apagados.
2. **Given** uma ocorrência que acabou de ter seu pagamento desfeito,
   **When** o painel é reexibido, **Then** ela aparece com o status
   derivado recalculado a partir da data de vencimento (podendo voltar a
   ser "Vencida", "Vence em breve" ou "Pendente"), e a ação "Marcar como
   paga" está disponível para ela novamente.

---

### Edge Cases

- Competência sem nenhuma ocorrência: lista vazia, contador "0 contas", os
  três cartões de resumo zerados e nenhum banner aparece.
- Todas as ocorrências da competência já pagas: "Total pendente" é € 0,00,
  e nenhuma ocorrência conta como "Vencida" ou "Vence em breve" (esses dois
  status só se aplicam a ocorrências não pagas).
- Ocorrência vencendo exatamente hoje: não é "Vencida"; é "Vence em breve"
  (0 dias está dentro da janela de 0 a 7 dias, inclusiva).
- Ocorrência vencendo em exatamente 7 dias: ainda é "Vence em breve" (limite
  superior da janela é inclusivo).
- Ocorrência vencendo em 8 dias ou mais: é "Pendente" e não entra em nenhum
  banner nem no "Total a vencer no período".
- Ocorrência com vencimento próximo da virada de mês: o cálculo de status
  deve usar a data completa de vencimento comparada à data atual, nunca
  apenas o número do dia isolado do mês, para não classificar errado uma
  ocorrência cujo vencimento cai no mês seguinte.
- Valor pago diferente do valor previsto (para mais ou para menos): a
  ocorrência exibe o aviso "diferente do previsto", mas o "Total pago" soma
  sempre o valor realmente pago, nunca o previsto.
- Confirmar pagamento com valor em branco ou não numérico: o valor previsto
  é usado no lugar, sem bloquear a confirmação.
- Confirmar pagamento com data em branco: a data de hoje (data atual do
  servidor) é usada no lugar.
- Trocar de ocorrência em edição sem confirmar nem cancelar a anterior: o
  rascunho da anterior é descartado sem aviso e ela volta ao estado normal.
- Desfazer pagamento de uma ocorrência com valor pago divergente do
  previsto: o valor pago e a data de pagamento são descartados; não existe
  histórico de pagamentos desfeitos.
- Despesa recorrente pausada depois que a ocorrência do mês já existia: a
  ocorrência permanece normalmente na listagem daquele mês; a pausa só
  impede a geração de novas ocorrências em competências futuras.
- Categoria de uma ocorrência não reconhecida entre as categorias
  conhecidas: o indicador de cor cai em um cinza neutro padrão.
- Duas ocorrências com o mesmo dia de vencimento: ambas aparecem
  normalmente na lista, sem agrupamento especial.
- Competência anterior ao início da despesa recorrente, ou despesa pausada
  desde o início: nenhuma ocorrência dessa despesa aparece nessa
  competência (mesmo efeito de uma competência sem ocorrências, para essa
  despesa específica).
- Parâmetro de período de consulta inválido (ex.: mês fora de 1–12, ano
  ausente ou não numérico): o sistema nunca deve tratar esse período como
  se fosse válido nem retornar dados de uma competência diferente da
  pedida.
- Usuário aciona "Voltar ao painel" na tela de cadastro sem ter alterado
  nenhum campo em relação ao estado inicial do formulário: navega direto
  para o painel, sem exibir confirmação.
- Usuário aciona "Voltar ao painel" na tela de cadastro após preencher ao
  menos um campo e depois desfazer manualmente essa alteração (formulário
  volta ao estado inicial vazio): tratado como "sem dados não salvos", sem
  confirmação.
- Usuário confirma "Sair sem salvar" no diálogo de confirmação: os dados
  digitados são descartados e o usuário é navegado para o painel, sem
  nenhuma tentativa de salvamento parcial.
- Usuário digita um valor monetário com caracteres não numéricos (letras,
  símbolos) em um campo com máscara de moeda: os caracteres inválidos são
  ignorados e apenas o valor numérico resultante é mantido e formatado.
- Largura da janela cruza um dos breakpoints (720px ou 480px) enquanto o
  painel já está aberto (ex.: redimensionamento de janela ou rotação de
  tela): o layout se reajusta imediatamente para o breakpoint aplicável,
  sem exigir recarregar a página.
- Usuário passa o mouse sobre um elemento clicável que está desabilitado ou
  temporariamente inativo (ex.: botão de confirmação desabilitado durante
  um estado de carregamento): o cursor exibido é o padrão do sistema, não o
  cursor de mão.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exibir, para uma única competência (mês/ano)
  por vez, todas as ocorrências de despesas recorrentes já geradas para
  essa competência, independentemente do status (ativa ou pausada) atual
  da despesa recorrente à qual cada ocorrência pertence.
- **FR-002**: O sistema MUST permitir consultar competências diferentes da
  atual; quando nenhuma competência é especificada, MUST usar a competência
  atual como padrão.
- **FR-003**: O sistema MUST rejeitar uma consulta cujo período seja
  inválido (mês fora do intervalo 1–12, ano ausente ou não numérico) sem
  retorná-la como se fosse uma competência válida.
- **FR-004**: O sistema MUST exibir, para cada ocorrência, ao menos: nome
  da despesa, categoria, indicador de cor por categoria, valor previsto
  formatado em euros (€), dia de vencimento e um selo de status.
- **FR-005**: O sistema MUST calcular o status de cada ocorrência a partir
  de seu estado de pagamento e da comparação entre sua data de vencimento
  completa e a data atual, segundo as seguintes regras, nesta ordem de
  prioridade: (1) uma ocorrência paga é sempre "Paga"; (2) uma ocorrência
  não paga cujo vencimento já passou é "Vencida"; (3) uma ocorrência não
  paga cujo vencimento é hoje ou ocorre em até 7 dias é "Vence em breve";
  (4) qualquer outra ocorrência não paga é "Pendente".
- **FR-006**: O sistema MUST exibir um alerta destacado com a contagem de
  ocorrências "Vencidas" sempre que existir ao menos uma na competência
  exibida, e MUST ocultar esse alerta quando não existir nenhuma.
- **FR-007**: O sistema MUST exibir um alerta destacado com a contagem de
  ocorrências "Vence em breve" e a soma dos valores previstos apenas dessas
  ocorrências, sempre que existir ao menos uma na competência exibida, e
  MUST ocultar esse alerta quando não existir nenhuma.
- **FR-008**: O sistema MUST exibir três totais da competência exibida: a
  soma do valor previsto de todas as ocorrências, a soma do valor
  efetivamente pago das ocorrências já pagas, e a soma do valor previsto
  apenas das ocorrências ainda não pagas.
- **FR-009**: O sistema MUST exibir, junto à lista de ocorrências, o
  período por extenso e a contagem total de ocorrências listadas.
- **FR-010**: O sistema MUST permitir ao usuário iniciar o registro de
  pagamento de qualquer ocorrência que esteja não paga e que não esteja
  atualmente em edição.
- **FR-011**: Ao iniciar o registro de pagamento de uma ocorrência, o
  sistema MUST pré-preencher o valor a pagar com o valor previsto da
  ocorrência e a data de pagamento com a data atual.
- **FR-012**: O sistema MUST permitir apenas uma ocorrência em edição de
  pagamento por vez; iniciar o registro de pagamento de outra ocorrência
  MUST cancelar automaticamente a edição em andamento, descartando
  qualquer valor ou data digitados nela, sem aviso.
- **FR-013**: Ao confirmar o registro de pagamento, o sistema MUST marcar a
  ocorrência como paga usando o valor e a data informados; se o valor
  informado estiver vazio ou não for um número válido, MUST usar o valor
  previsto da ocorrência no lugar; se a data informada estiver vazia, MUST
  usar a data atual no lugar.
- **FR-014**: O sistema MUST permitir cancelar o registro de pagamento em
  andamento sem alterar nenhum dado da ocorrência.
- **FR-015**: Para cada ocorrência paga (fora de edição), o sistema MUST
  exibir o valor pago e a data em que o pagamento foi registrado.
- **FR-016**: Quando o valor pago de uma ocorrência for diferente do valor
  previsto (mesmo por uma pequena diferença), o sistema MUST destacar esse
  valor com um aviso indicando que ele difere do valor previsto; quando os
  dois valores forem iguais, MUST exibir o valor pago sem esse aviso.
- **FR-017**: O sistema MUST permitir desfazer o pagamento de qualquer
  ocorrência paga, imediatamente e sem exigir confirmação adicional.
- **FR-018**: Ao desfazer um pagamento, o sistema MUST reverter a
  ocorrência para "não paga", removendo o valor pago e a data de pagamento
  anteriormente registrados, e MUST torná-la elegível para um novo registro
  de pagamento.
- **FR-019**: O sistema MUST tratar cada categoria de despesa como
  pertencente a um conjunto fechado e conhecido; uma ocorrência cuja
  categoria não seja reconhecida MUST ser exibida com uma indicação visual
  neutra padrão em vez de falhar ou ficar sem indicação.
- **FR-020**: O sistema MUST exibir controles de navegação de competência
  (seta "mês anterior" e seta "próximo mês") no painel; ao acionar um
  desses controles, o sistema MUST disparar uma nova consulta do painel
  para a competência de destino (mês anterior ou próximo mês em relação à
  competência atualmente exibida), conforme FR-002.
- **FR-021**: O sistema MUST exibir um botão "Nova despesa" no painel; ao
  ser acionado, o sistema MUST navegar o usuário para a rota
  `/despesas/nova`. O comportamento do fluxo de cadastro de despesa
  recorrente nessa rota está fora do escopo desta especificação, exceto
  pelos comportamentos de navegação e confirmação descritos em FR-023 a
  FR-025.
- **FR-022**: O sistema MUST exibir o cursor do tipo "mão" (pointer) ao
  passar o mouse sobre qualquer elemento clicável ou interativo que esteja
  habilitado/acionável — botões, links, setas de navegação de mês, ícones
  de ação, linhas/itens acionáveis e o seletor de data — em todas as telas
  cobertas por esta especificação (painel mensal, tela de cadastro e tela
  de sucesso do cadastro). Um elemento desabilitado ou temporariamente
  inativo (ex.: um botão de confirmação desabilitado) MUST exibir o cursor
  padrão (não pointer) em vez do cursor de mão, sinalizando que a ação não
  está disponível no momento.
- **FR-023**: A tela de cadastro de despesa MUST oferecer uma ação "Voltar
  ao painel" que navega o usuário de volta ao painel mensal de despesas.
- **FR-024**: Ao acionar "Voltar ao painel" a partir da tela de cadastro,
  se o formulário contiver dados preenchidos pelo usuário que ainda não
  foram salvos, o sistema MUST exibir uma confirmação antes de sair,
  oferecendo as opções de continuar editando (permanecendo na tela, sem
  perder os dados) ou sair sem salvar (descartando os dados e navegando
  para o painel); se o formulário não contiver nenhum dado preenchido pelo
  usuário, o sistema MUST navegar diretamente para o painel, sem exibir
  confirmação.
- **FR-025**: A tela de sucesso exibida após o cadastro de uma despesa
  recorrente MUST oferecer duas ações: "Voltar ao painel" (navega para o
  painel mensal) e "Cadastrar nova despesa" (retorna a um formulário de
  cadastro vazio para uma nova despesa).
- **FR-026**: O painel mensal MUST se adaptar a telas de largura reduzida
  usando ao menos dois breakpoints — até 720px e até 480px de largura —
  com o seguinte comportamento: em até 720px, a grade dos três cartões de
  resumo MUST passar a exibir 2 colunas em vez de 3, e cada linha da lista
  de ocorrências MUST reorganizar seus campos em blocos empilhados
  (nome em largura total, seguido por status, valor previsto, dia de
  vencimento e ações, cada um em sua própria linha) em vez do layout em
  colunas fixas usado em telas largas.
- **FR-027**: Em telas com até 480px de largura, o painel MUST exibir a
  grade dos três cartões de resumo em uma única coluna, e os banners de
  alerta (contas vencidas / vencendo em breve) MUST ocupar a largura total
  disponível.
- **FR-028**: Todo campo de entrada de valor monetário (ex.: valor pago ao
  registrar um pagamento no painel, valor previsto na tela de cadastro)
  MUST aplicar uma máscara de moeda enquanto o usuário digita, formatando o
  valor digitado no padrão monetário exibido pelo sistema (separador de
  milhar e separador decimal) em tempo real, sem exigir que o usuário digite
  esses separadores manualmente.
- **FR-029**: Todo campo de entrada de data (ex.: data de pagamento no
  painel, data de início na tela de cadastro) MUST abrir o seletor de
  calendário nativo do navegador/dispositivo ao ser clicado ou ativado, em
  vez de exigir digitação manual da data como único meio de preenchimento.
- **FR-030**: O sistema MUST exibir todos os valores monetários — nos
  cartões de resumo, nos banners de alerta, em cada ocorrência da lista e
  nos campos de entrada de valor — no formato de Euro (€), em vez de reais.

### Key Entities

- **Ocorrência de despesa recorrente**: um lançamento específico de uma
  despesa recorrente em uma competência (mês/ano). Atributos relevantes
  para esta tela: identificador, despesa de origem (nome, categoria), valor
  previsto, data de vencimento completa, status de pagamento (paga/não
  paga) e, apenas quando paga, o valor efetivamente pago e a data em que o
  pagamento foi registrado. O status exibido ao usuário ("Paga",
  "Vencida", "Vence em breve", "Pendente") é derivado a partir desses dados
  e da data atual — não é um valor armazenado à parte.
- **Despesa recorrente**: a despesa que origina as ocorrências de uma
  competência; para esta tela, apenas seu nome, categoria e status (ativa
  ou pausada) são relevantes, sendo o status usado apenas para decidir se
  novas ocorrências serão geradas em competências futuras, nunca para
  ocultar ocorrências já existentes.
- **Categoria**: um valor dentre um conjunto fechado e pré-definido,
  associado a cada despesa recorrente; usada para exibir o nome da
  categoria e escolher a cor do indicador visual de cada ocorrência.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um usuário consegue identificar, em menos de 5 segundos após
  abrir o painel de uma competência, quantas contas estão vencidas e
  quantas vencem nos próximos 7 dias, sem precisar abrir nenhuma ocorrência
  individualmente.
- **SC-002**: Para qualquer competência consultada, a soma dos três totais
  exibidos ("Total pago" + "Total pendente") e o "Total previsto" batem
  exatamente com a soma manual dos valores das ocorrências daquela
  competência, em 100% dos casos verificados.
- **SC-003**: Um usuário consegue registrar o pagamento de uma ocorrência
  (do clique inicial até a confirmação) em 3 ações ou menos (iniciar,
  ajustar se necessário, confirmar).
- **SC-004**: Desfazer o pagamento de uma ocorrência e voltar a marcá-la
  como paga produz o mesmo resultado visual de uma ocorrência paga pela
  primeira vez, sem exigir recarregar a tela.
- **SC-005**: Em 100% das competências sem nenhuma ocorrência elegível, a
  tela não exibe nenhum banner de alerta e mostra os totais zerados, sem
  erros ou estados de carregamento indefinidos.
- **SC-006**: Em uma tela de smartphone (largura até 480px), um usuário
  consegue ler os três totais e identificar o status de qualquer ocorrência
  da lista sem precisar rolar a tela horizontalmente.
- **SC-007**: Um usuário que preencheu dados no formulário de cadastro e
  tenta sair sem salvar é sempre avisado antes de perder esses dados; um
  usuário que não alterou nada consegue voltar ao painel sem nenhuma
  interrupção.

## Assumptions

- A navegação entre meses (setas "mês anterior"/"próximo mês", FR-020)
  dispara uma nova consulta de painel para a competência de destino
  (FR-002); o design de referência não define limites de quantos meses
  para trás/frente podem ser consultados, e nenhum limite é assumido nesta
  especificação.
- A confirmação de pagamento com valor ou data inválidos/em branco usa
  substituição silenciosa pelos padrões (valor previsto / data atual),
  conforme comportamento observado no design de referência (FR-013), em vez
  de bloquear a confirmação com uma mensagem de erro — essa é uma decisão
  já tomada para este escopo, não uma lacuna em aberto.
- A listagem de ocorrências de uma competência é assumida como não paginada
  nesta iteração; os totais e contagens exibidos (FR-006 a FR-009) são
  derivados a partir do conjunto completo de ocorrências recebido para a
  competência.
- Nenhuma ordenação específica é exigida para a lista de ocorrências; a
  ordem de exibição não afeta nenhum dos comportamentos descritos nesta
  especificação.
- Truncamento de nomes de despesa muito longos não é tratado nesta
  especificação, por não estar modelado no design de referência.
- O cadastro, a edição e a exclusão de despesas recorrentes, assim como
  frequências diferentes de mensal, estão fora do escopo desta
  especificação — cobrem apenas a visualização do painel mensal e as ações
  de marcar/desfazer pagamento de uma ocorrência já existente. O botão
  "Nova despesa" do painel (FR-021) está em escopo apenas quanto à sua
  presença e ao disparo da navegação para `/despesas/nova`; os campos,
  validações e regras de negócio do formulário de cadastro em si permanecem
  fora do escopo desta especificação. Ficam em escopo apenas os
  comportamentos de UX transversais definidos em FR-022 a FR-025: cursor de
  hover, ação "Voltar ao painel" (com confirmação condicional a dados não
  salvos) e as duas ações da tela de sucesso do cadastro. Conforme
  esclarecido na sessão de clarificação de 2026-09-08, a implementação de
  FR-023 a FR-025 é parte do escopo técnico desta feature (006) e envolve
  alterar diretamente o componente de cadastro já entregue pela feature 002
  (`cadastro-despesa-recorrente`); esta feature não recria esse componente
  do zero, apenas adiciona a ele os comportamentos de navegação/confirmação
  especificados aqui.
- O contrato técnico exato (rotas, formatos de requisição/resposta, códigos
  de status) da consulta de dados do painel e das ações de marcar/desfazer
  pagamento fica para a fase de planejamento técnico desta feature, não
  para esta especificação funcional.
- Os valores monetários do painel são exibidos em Euro (€), conforme
  FR-030, substituindo a formatação em reais assumida em uma versão
  anterior desta especificação; o formato exato de agrupamento de milhar e
  separador decimal segue o padrão observado no design de referência
  (vírgula decimal, símbolo "€" após o valor).
- O formato exato da máscara de moeda aplicada durante a digitação
  (FR-028) e o comportamento detalhado do seletor de data nativo (FR-029)
  seguem o padrão observado no design de referência; nenhuma biblioteca ou
  técnica de implementação específica é prescrita por esta especificação
  funcional.
- Os dois breakpoints responsivos citados em FR-026 e FR-027 (720px e
  480px) refletem os pontos de quebra já usados no design de referência;
  breakpoints adicionais não são exigidos por esta especificação.
