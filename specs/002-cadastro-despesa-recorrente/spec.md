# Feature Specification: Cadastro de Despesa Recorrente

**Feature Branch**: `002-cadastro-despesa-recorrente`

**Created**: 2026-09-02

**Status**: Draft

**Input**: User description: "Tela de cadastro de despesas refinada em @refinements/frontend/cadastro-despesa-recorrente.md"

## Clarifications

### Session 2026-09-02

- Q: Existe um limite máximo de caracteres para o campo "Nome" da despesa? → A: 100 caracteres.
- Q: Como devemos definir "sem atraso perceptível" na atualização da pré-visualização (SC-003), para que seja um critério testável? → A: Atualização síncrona, sem debounce — a pré-visualização é um valor derivado local que reflete a mudança no mesmo ciclo de renderização da digitação, sem depender de um teto em milissegundos.
- Q: Deve existir um tempo máximo de espera (timeout) para a chamada de salvamento, após o qual o sistema exibe o erro automaticamente mesmo sem resposta do servidor? → A: Sem timeout explícito no cliente; o estado de erro é disparado apenas pela resposta real de falha (rede ou servidor).
- Q: A regra de no máximo 2 casas decimais para o "Valor previsto mensal" (FR-004) deve continuar valendo, mesmo o design de referência (Cadastro.dc.html) não implementar essa checagem em seu script de protótipo? → A: Sim — mantém-se a regra de 2 casas decimais; o script do design é um protótipo simplificado de interação, não a regra de negócio final.
- Q: Como o limite de 100 caracteres do campo "Nome" (FR-002) deve ser aplicado na interface, já que o design de referência não usa `maxlength` nem checa tamanho na validação? → A: O campo DEVE impedir, via atributo de limite do input, que o usuário digite ou cole texto além de 100 caracteres.
- Q: O design de referência não modela nenhum estado de erro específico por campo vindo do backend (FR-016), exibindo apenas um aviso genérico de sucesso/erro. Esse requisito de erro inline por campo deve continuar no escopo desta tela? → A: Sim, permanece em escopo; o design cobre apenas os fluxos comuns, e o tratamento visual do erro por campo do backend será detalhado na fase de planejamento.
- Q: O HTML/CSS final deve reproduzir o design de referência com fidelidade visual exata (cores, espaçamentos, tipografia), ou é aceitável usar os tokens padrão do design system Tailwind do projeto mesmo com pequenas variações visuais? → A: Fidelidade estrita via Tailwind — reproduzir cores exatas, tipografia (Sora/Public Sans), espaçamentos e border-radius do design usando classes utilitárias/tokens Tailwind, nunca copiando estilos inline literalmente.
- Q: O cabeçalho do arquivo de design (logo "ContasEmDia" + ícone, no topo da página) faz parte do escopo desta tela, ou pertence a um layout/shell compartilhado fora desta feature? → A: Fora do escopo — o cabeçalho pertence a um shell/layout compartilhado da aplicação; esta feature implementa apenas o conteúdo do formulário, a partir do título "Nova despesa recorrente".
- Q: O layout desta tela precisa suportar telas estreitas (mobile), além do comportamento de quebra de linha (flex-wrap) mostrado no design para telas largas? → A: Sim — deve ser responsivo e utilizável em telas estreitas (respeitando WCAG 2.1 AA), mesmo o design de referência não detalhar breakpoints mobile explícitos.
- Q: A cor de destaque (accent, ex.: #2E6FF2, usada em botões e ícones) deve vir de um token de tema compartilhado da aplicação, ou deve ser um valor fixo no código desta tela? → A: Token de tema compartilhado — usar a cor de marca definida centralmente (tema/tokens Tailwind da aplicação), não um valor fixo duplicado nesta tela.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar uma nova despesa recorrente (Priority: P1)

Como usuário do ContasEmDia, quero preencher um formulário com os dados de
uma despesa que se repete todo mês (nome, categoria, valor previsto, dia de
vencimento, data de início, status) e salvá-la, para que ela passe a ser
acompanhada automaticamente pelo aplicativo.

**Why this priority**: É a razão de existir da tela — sem essa capacidade
nenhuma despesa recorrente pode ser criada e nenhum outro comportamento da
tela (preview, validação, feedback) tem propósito.

**Independent Test**: Preencher todos os campos obrigatórios com valores
válidos, clicar em "Salvar despesa" e verificar que o sistema confirma a
criação da despesa exibindo o nome cadastrado.

**Acceptance Scenarios**:

1. **Given** o formulário vazio, **When** o usuário preenche nome, categoria,
   valor previsto mensal, dia de vencimento, data de início e status com
   valores válidos e clica em "Salvar despesa", **Then** o sistema envia os
   dados para cadastro e, ao concluir com sucesso, exibe uma confirmação com
   o nome da despesa cadastrada.
2. **Given** uma despesa foi cadastrada com sucesso, **When** o usuário
   escolhe cadastrar outra despesa, **Then** o formulário volta ao estado
   inicial, vazio e pronto para um novo cadastro.
3. **Given** o usuário não altera o campo de frequência, **When** ele salva a
   despesa, **Then** ela é cadastrada com frequência mensal, única opção
   disponível nesta versão.
4. **Given** o usuário não altera o campo de status, **When** ele salva a
   despesa, **Then** ela é cadastrada como "Ativa" por padrão.

---

### User Story 2 - Acompanhar uma pré-visualização em tempo real (Priority: P2)

Como usuário preenchendo o formulário, quero ver um cartão de
pré-visualização que reflete imediatamente o que estou digitando (nome,
categoria, valor formatado, dia de vencimento, status), para conferir como a
despesa vai aparecer antes de salvá-la.

**Why this priority**: Reduz erros de cadastro e dá confiança ao usuário,
mas o cadastro em si (User Story 1) já entrega valor sem essa
funcionalidade.

**Independent Test**: Preencher/alterar cada campo do formulário e verificar
que o cartão de pré-visualização atualiza o valor correspondente
imediatamente, sem exigir uma ação separada de "atualizar".

**Acceptance Scenarios**:

1. **Given** o campo "Nome" está vazio, **When** o usuário digita um nome,
   **Then** o cartão de pré-visualização passa a exibir esse nome no lugar
   do texto padrão ("Nome da despesa").
2. **Given** o usuário seleciona uma categoria, **When** a seleção muda,
   **Then** o cartão de pré-visualização atualiza a cor/identificação visual
   associada àquela categoria.
3. **Given** o usuário digita um valor monetário, **When** o valor é
   digitado, **Then** o cartão exibe esse valor formatado como moeda
   brasileira (R$).
4. **Given** o usuário digita um dia de vencimento, **When** o dia é
   preenchido, **Then** o cartão exibe "Dia X"; enquanto o campo estiver
   vazio ou inválido, o cartão exibe um marcador neutro ("Dia --").
5. **Given** o usuário alterna o status entre Ativa e Pausada, **When** a
   alternância ocorre, **Then** o texto auxiliar do cartão reflete o status
   selecionado.

---

### User Story 3 - Ser impedido de salvar dados inválidos (Priority: P2)

Como usuário preenchendo o formulário, quero ser avisado claramente quando
um campo obrigatório está vazio ou inválido, para corrigir o problema antes
de tentar salvar novamente.

**Why this priority**: Protege a integridade dos dados cadastrados e evita
chamadas desnecessárias ao backend, mas depende da existência do formulário
da User Story 1.

**Independent Test**: Deixar um campo obrigatório vazio ou com valor
inválido, tentar salvar, e verificar que o sistema não conclui o cadastro e
sinaliza visualmente o(s) campo(s) problemático(s).

**Acceptance Scenarios**:

1. **Given** o campo "Nome" está vazio, **When** o usuário sai do campo (sem
   preenchê-lo) ou tenta salvar, **Then** o campo é destacado com uma
   mensagem de erro indicando que o nome é obrigatório.
2. **Given** o usuário está digitando ou colando texto no campo "Nome",
   **When** o conteúdo alcança 100 caracteres, **Then** o campo impede a
   entrada de caracteres adicionais, de forma que o valor armazenado nunca
   exceda esse limite.
3. **Given** o "Valor previsto mensal" é zero, negativo ou tem mais de duas
   casas decimais, **When** o usuário sai do campo ou tenta salvar, **Then**
   uma mensagem de erro específica é exibida junto ao campo.
4. **Given** o "Dia de vencimento" está fora do intervalo de 1 a 31, **When**
   o usuário sai do campo ou tenta salvar, **Then** uma mensagem de erro
   específica é exibida junto ao campo.
5. **Given** a "Data de início" não é uma data válida, **When** o usuário
   sai do campo ou tenta salvar, **Then** uma mensagem de erro específica é
   exibida junto ao campo.
6. **Given** um ou mais campos obrigatórios estão inválidos, **When** o
   usuário clica em "Salvar despesa", **Then** o sistema revela as
   mensagens de erro em todos os campos inválidos de uma vez, exibe um aviso
   geral pedindo para corrigir os campos destacados, e **não** envia os
   dados para o backend.

---

### User Story 4 - Recuperar-se de uma falha ao salvar (Priority: P3)

Como usuário que preencheu o formulário corretamente, quero saber quando o
salvamento falha por um motivo fora do meu controle (rede, servidor) e
poder tentar novamente sem perder o que já preenchi, para não ter que
redigitar tudo.

**Why this priority**: Cobre um cenário de exceção; a maioria dos cadastros
segue o caminho feliz da User Story 1, mas essa recuperação evita
frustração e perda de dados quando algo dá errado.

**Independent Test**: Simular uma falha no envio de um formulário
válido e verificar que o sistema exibe um aviso de erro, mantém os dados
preenchidos, e permite tentar salvar novamente com um único clique.

**Acceptance Scenarios**:

1. **Given** o formulário é válido e o usuário clica em "Salvar despesa",
   **When** o envio falha por um problema de conexão ou do servidor,
   **Then** o sistema exibe um aviso de erro genérico e mantém todos os
   dados preenchidos no formulário.
2. **Given** o sistema está exibindo o aviso de erro de envio, **When** o
   usuário clica em "Tentar novamente", **Then** o sistema tenta salvar a
   despesa novamente com os mesmos dados, sem exigir que o usuário os
   redigite.
3. **Given** o backend rejeita os dados por uma regra de negócio (ex.:
   categoria inválida) associada a um campo conhecido, **When** a resposta
   chega, **Then** o sistema deve, sempre que possível, exibir essa
   informação como uma mensagem inline no campo correspondente, em vez de
   apenas um aviso genérico.

---

### Edge Cases

- O usuário tenta salvar sem alterar nenhum campo: todos os campos
  obrigatórios são marcados como inválidos e nenhuma chamada é enviada.
- O usuário preenche o valor previsto mensal com um número muito grande ou
  com formatação inesperada: o sistema deve tratá-lo como inválido caso não
  seja um valor monetário positivo com no máximo duas casas decimais, em vez
  de travar ou aceitar silenciosamente um valor incorreto.
- O usuário tenta selecionar uma frequência diferente de "Mensal": a opção
  não está disponível para seleção nesta versão.
- O usuário cadastra a despesa com status "Pausada": a despesa é criada, mas
  nenhuma ocorrência do mês corrente é gerada automaticamente para ela.
- O usuário clica em "Salvar despesa" múltiplas vezes seguidas enquanto o
  salvamento anterior ainda está em andamento: o sistema deve impedir envios
  duplicados enquanto o salvamento estiver em andamento.
- O botão "Cancelar" é clicado: nenhum comportamento é definido para esse
  botão nesta versão (fora de escopo).
- O cabeçalho de aplicação (logo/ícone "ContasEmDia") visto no arquivo de
  design: não faz parte desta tela; é fornecido por um layout/shell
  compartilhado da aplicação, fora do escopo desta especificação.
- O usuário acessa a tela em uma janela estreita (mobile): o layout se
  adapta para permanecer utilizável, em vez de manter apenas o
  comportamento de quebra de linha (flex-wrap) pensado para telas largas
  no design de referência.
- O salvamento demora para responder: não há timeout explícito no cliente;
  o formulário permanece no estado "processando" até que o backend
  efetivamente responda (sucesso ou falha de rede/servidor), sem um limite
  de tempo artificial imposto pelo frontend.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE permitir que o usuário informe nome, categoria,
  valor previsto mensal, dia de vencimento, data de início, frequência,
  status e uma observação opcional para uma nova despesa recorrente.
- **FR-002**: O sistema DEVE exigir que o nome não esteja vazio (após
  remover espaços em branco nas extremidades) para permitir o cadastro, e
  DEVE impedir, diretamente no campo, a digitação ou colagem de texto que
  ultrapasse 100 caracteres, de modo que o valor armazenado nunca exceda
  esse limite.
- **FR-003**: O sistema DEVE restringir a categoria a um conjunto fixo de 5
  opções predefinidas (Moradia, Serviços, Transporte, Assinaturas, Outra).
- **FR-004**: O sistema DEVE exigir que o valor previsto mensal seja um
  número maior que zero, com no máximo duas casas decimais.
- **FR-005**: O sistema DEVE exigir que o dia de vencimento seja um número
  inteiro entre 1 e 31.
- **FR-006**: O sistema DEVE exigir que a data de início seja uma data
  válida no calendário.
- **FR-007**: O sistema DEVE restringir a frequência a "Mensal" nesta
  versão, sem permitir a seleção de outras frequências.
- **FR-008**: O sistema DEVE exigir um status para a despesa, com "Ativa"
  como valor padrão, permitindo alternar entre "Ativa" e "Pausada".
- **FR-009**: O sistema DEVE permitir uma observação em texto livre,
  opcional, sem validação de conteúdo.
- **FR-010**: O sistema DEVE atualizar uma pré-visualização da despesa (com
  nome, categoria, valor formatado como moeda, dia de vencimento e status)
  imediatamente a cada alteração feita pelo usuário no formulário, sem exigir
  uma ação separada de atualização.
- **FR-011**: O sistema DEVE impedir o envio do cadastro caso qualquer campo
  obrigatório esteja inválido, e DEVE, nesse caso, sinalizar visualmente
  todos os campos inválidos de uma só vez junto com uma mensagem orientando
  o usuário a corrigi-los.
- **FR-012**: O sistema DEVE exibir a mensagem de erro de um campo assim
  que o usuário sair dele (mesmo antes de tentar salvar) ou, o mais tardar,
  ao tentar salvar o formulário.
- **FR-013**: O sistema DEVE indicar visualmente que o cadastro está sendo
  processado enquanto aguarda a resposta do salvamento, e DEVE impedir que
  o usuário dispare múltiplos envios simultâneos do mesmo cadastro.
- **FR-014**: Ao concluir o cadastro com sucesso, o sistema DEVE exibir uma
  confirmação identificando a despesa pelo nome cadastrado, e DEVE oferecer
  uma ação para iniciar o cadastro de uma nova despesa, retornando o
  formulário ao estado inicial vazio.
- **FR-015**: Caso o cadastro falhe, o sistema DEVE exibir um aviso de erro
  e manter os dados já preenchidos pelo usuário, sem exigir que ele os
  redigite.
- **FR-016**: Quando a falha do cadastro identificar um campo específico
  inválido, o sistema DEVE, sempre que possível, exibir essa informação
  como mensagem inline junto ao campo correspondente; quando não for
  possível identificar um campo específico (ex.: falha de rede), o sistema
  DEVE exibir um aviso de erro genérico.
- **FR-017**: Após uma falha no cadastro, o sistema DEVE permitir que o
  usuário tente salvar novamente com um único clique, sem perder os dados já
  preenchidos.
- **FR-018**: O sistema DEVE reproduzir o layout, os campos, os estados de
  erro, a pré-visualização e os badges de status definidos em
  `design/Cadastro.dc.html` com fidelidade visual (cores, tipografia,
  espaçamentos), usando os padrões de estilização definidos pela
  arquitetura do projeto — nunca copiando os estilos inline do arquivo de
  design literalmente. A cor de destaque (accent) DEVE vir de um token de
  tema compartilhado da aplicação, não de um valor fixo nesta tela. O
  cabeçalho de aplicação (logo/ícone) mostrado no arquivo de design está
  fora do escopo desta tela.
- **FR-019**: O sistema DEVE manter o formulário utilizável em telas
  estreitas (mobile), adaptando o layout responsivamente além do
  comportamento de quebra de linha (flex-wrap) mostrado no design de
  referência apenas para telas largas.

### Key Entities

- **Despesa Recorrente**: representa uma despesa que se repete
  periodicamente. Atributos relevantes para esta tela: nome, categoria (uma
  de 5 categorias fixas), valor previsto mensal, dia de vencimento, data de
  início, frequência (apenas mensal nesta versão), status (ativa ou
  pausada) e observação opcional.
- **Categoria**: classificação fixa e predefinida atribuída a uma despesa
  recorrente, usada tanto na seleção do formulário quanto na identificação
  visual da pré-visualização.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um usuário com todos os dados em mãos consegue preencher e
  concluir o cadastro de uma despesa recorrente válida em menos de 2
  minutos.
- **SC-002**: 100% das tentativas de salvar um formulário com pelo menos um
  campo obrigatório inválido são bloqueadas antes de qualquer envio, com o(s)
  campo(s) problemático(s) claramente identificado(s) para o usuário.
- **SC-003**: A pré-visualização reflete qualquer alteração feita em um
  campo do formulário de forma síncrona, no mesmo ciclo de renderização da
  digitação, sem debounce nem atraso perceptível pelo usuário.
- **SC-004**: Em caso de falha ao salvar, 100% dos dados preenchidos pelo
  usuário permanecem no formulário, permitindo nova tentativa sem
  redigitação.
- **SC-005**: Um usuário consegue identificar, sem ajuda externa, qual
  campo precisa corrigir quando o cadastro é rejeitado por dado inválido.
- **SC-006**: A tela é utilizável (todos os campos, mensagens de erro e
  ações acessíveis, legíveis e sem sobreposição) em larguras de viewport a
  partir de 360px, sem exigir rolagem horizontal.

## Assumptions

- O conjunto de categorias disponíveis (Moradia, Serviços, Transporte,
  Assinaturas, Outra) é fixo e estável o suficiente para não exigir consulta
  a uma lista dinâmica nesta versão; qualquer mudança futura nesse conjunto
  exigirá uma atualização manual da tela.
- Apenas a frequência mensal é suportada nesta versão; demais frequências
  ficam visíveis, porém desabilitadas, sem impacto funcional.
- O comportamento do botão "Cancelar" (ex.: navegação de volta a outra
  tela) está fora do escopo desta especificação.
- A autenticação/autorização necessária para o cadastro já é resolvida pela
  infraestrutura existente do aplicativo, fora do escopo desta tela.
- Quando o backend rejeita o cadastro por uma regra de negócio, ele indica
  a qual campo o erro se refere sempre que possível; quando isso não é
  possível (ou a falha é de rede/servidor), apenas um aviso genérico é
  mostrado.
- A listagem, edição, pausa/reativação, exclusão de despesas recorrentes já
  existentes, assim como o painel mensal de ocorrências, estão fora do
  escopo desta especificação, que cobre exclusivamente o cadastro de uma
  nova despesa recorrente.
- O arquivo `design/Cadastro.dc.html` define a referência visual e de
  interação (layout, campos, estados de erro, pré-visualização, badges de
  status) para esta tela. O script de demonstração embutido nesse arquivo é
  um protótipo simplificado de interatividade e não substitui as regras de
  negócio desta especificação (ex.: a checagem de no máximo duas casas
  decimais do valor e o limite de 100 caracteres do nome, ausentes no
  protótipo, permanecem exigidos — ver FR-002, FR-004 e Clarifications).
  O tratamento visual do erro inline por campo vindo do backend (FR-016),
  não representado no protótipo, será definido durante a fase de
  planejamento.
- A fidelidade ao design de referência (FR-018) é exigida em cores,
  tipografia (Sora/Public Sans) e espaçamentos, mas a implementação DEVE
  usar os padrões de estilização e tokens já definidos pela arquitetura do
  projeto, nunca copiar literalmente os estilos inline do arquivo
  `.dc.html`. O cabeçalho de aplicação (logo/ícone "ContasEmDia") mostrado
  no design pertence a um layout/shell compartilhado e está fora do escopo
  desta tela.
- A cor de destaque (accent) usada em botões, ícones e no badge "Ativa"
  DEVE vir de um token de tema compartilhado da aplicação (cor de marca
  definida centralmente), e não de um valor fixo duplicado nesta tela,
  ainda que o protótipo de design exponha essa cor como uma propriedade
  configurável isolada (`accent`).
