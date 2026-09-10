# Feature Specification: Editar Despesa Recorrente (Tela)

**Feature Branch**: `008-edit-recurring-expense-ui`

**Created**: 2026-09-10

**Status**: Draft

**Input**: User description: "Cria tela para editar despesas recorrentes conforme o refinamento @../refinements/frontend/editar-despesa-recorrente.md"

## Clarifications

### Session 2026-09-10

- Q: Quando o status é trocado para "Ativa" durante a edição (FR-016), o
  aviso sobre a possível geração de ocorrência deve ficar sempre visível
  enquanto "Ativa" estiver selecionado, ou deve aparecer só no instante em
  que o usuário troca de Pausada para Ativa? → A: Texto persistente,
  sempre visível junto ao controle de status enquanto "Ativa" estiver
  selecionado (mesmo padrão do cadastro).
- Q: Se a pessoa altera um campo e depois digita de volta exatamente o
  valor original antes de tentar sair da tela, isso ainda deve contar como
  "alteração não salva" (FR-013/014), ou o sistema deve considerar que não
  há nada para perder? → A: Comparação por valor — reverter um campo ao
  valor original remove esse campo da contagem de "alterado", mesmo que
  tenha sido editado e desfeito manualmente.
- Q: Na listagem do painel mensal, a ação de editar (FR-002) deve ser um
  ícone/botão dedicado por item, um link de texto, ou o item inteiro deve
  ser clicável? → A: Ícone de ação dedicado (ex.: lápis) por item, ao lado
  das demais ações da linha.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Editar os dados de uma despesa recorrente numa tela dedicada (Priority: P1)

Uma pessoa que já cadastrou uma despesa recorrente (ex.: aluguel, internet)
precisa corrigir ou atualizar seus dados sem precisar excluir e recadastrar
a despesa do zero. Ela abre uma tela dedicada, encontra os dados atuais já
preenchidos, altera o que for necessário e salva.

**Why this priority**: É a ação central da feature; sem ela, nenhuma
despesa recorrente já cadastrada pode ser corrigida depois do cadastro
inicial — o valor de negócio inteiro da feature está aqui.

**Independent Test**: Pode ser testado isoladamente abrindo a tela de
edição de uma despesa recorrente existente, alterando um ou mais campos
para valores válidos, salvando, e verificando que a confirmação mostra
exatamente os dados que foram salvos.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente já cadastrada, **When** a pessoa abre a
   tela de edição dela, **Then** todos os campos editáveis aparecem
   preenchidos com os valores atuais, antes de qualquer alteração.
2. **Given** a tela de edição carregada, **When** a pessoa altera um ou
   mais campos para valores válidos e confirma o salvamento, **Then** a
   tela mostra uma confirmação de sucesso com os dados já atualizados.
3. **Given** a tela de edição carregada, **When** a pessoa tenta salvar com
   um ou mais campos inválidos, **Then** a tela recusa o envio e mostra a
   mensagem de erro de cada campo inválido, sem enviar nada.

---

### User Story 2 - Acessar a edição a partir do painel mensal (Priority: P2)

Ao visualizar a lista de contas do mês no painel mensal, a pessoa percebe
que uma delas está com um dado errado (valor, dia de vencimento, etc.) e
quer corrigi-la sem sair do fluxo em que já está.

**Why this priority**: Sem um ponto de entrada claro a partir de onde a
pessoa já está (o painel mensal), a tela de edição da User Story 1 fica
inacessível na prática, ainda que exista.

**Independent Test**: Pode ser testado isoladamente acionando a ação de
editar em um item da lista do painel mensal e verificando que a tela de
edição aberta corresponde à despesa recorrente correta (não apenas à
ocorrência daquele mês).

**Acceptance Scenarios**:

1. **Given** a pessoa está vendo o painel mensal, **When** ela aciona a
   ação de editar em um item da lista, **Then** a tela de edição é aberta
   já carregando os dados da despesa recorrente dona daquele item.

---

### User Story 3 - Ser avisado antes de perder alterações não salvas (Priority: P3)

Depois de alterar um ou mais campos na tela de edição, a pessoa tenta sair
sem salvar (por engano ou de propósito). O sistema precisa avisar antes de
descartar o que foi digitado.

**Why this priority**: Evita perda de trabalho por engano; é um cuidado de
qualidade sobre o fluxo principal (User Story 1), não uma capacidade nova.

**Independent Test**: Pode ser testado isoladamente alterando um campo e
tentando sair da tela, verificando que aparece uma confirmação antes de
realmente sair; e, separadamente, saindo sem ter alterado nada e
verificando que nenhuma confirmação aparece.

**Acceptance Scenarios**:

1. **Given** a pessoa alterou ao menos um campo sem salvar, **When** ela
   tenta sair da tela de edição, **Then** aparece uma confirmação
   perguntando se ela quer mesmo sair sem salvar.
2. **Given** a pessoa não alterou nenhum campo (ou já salvou com sucesso),
   **When** ela sai da tela, **Then** nenhuma confirmação aparece.

---

### User Story 4 - Ser informado com clareza quando algo impede a edição (Priority: P4)

A despesa recorrente pode ter sido removida entre o momento em que a pessoa
clicou em editar e o momento em que a tela carrega, ou a conexão pode falhar
ao carregar os dados ou ao salvar. Em todos esses casos, a pessoa precisa
entender o que aconteceu e ter uma forma clara de seguir em frente.

**Why this priority**: São desvios do caminho feliz; melhoram a robustez
percebida da feature, mas a User Story 1 já entrega o valor principal sem
depender destes casos acontecerem.

**Independent Test**: Pode ser testado isoladamente simulando cada uma das
três situações (despesa inexistente, falha ao carregar, falha ao salvar) e
verificando que a tela mostra uma mensagem específica para cada uma, com
uma ação de continuar (tentar novamente ou voltar).

**Acceptance Scenarios**:

1. **Given** a despesa recorrente apontada pela tela não existe (mais),
   **When** a tela tenta carregá-la, **Then** aparece uma mensagem de
   "não encontrada", sem exibir nenhum formulário, com uma forma de voltar
   ao painel mensal.
2. **Given** falha a tentativa de carregar os dados da despesa, **When** a
   tela tenta carregá-la, **Then** aparece uma mensagem de erro com uma
   ação para tentar novamente.
3. **Given** falha a tentativa de salvar uma edição válida, **When** a
   pessoa confirma o salvamento, **Then** aparece uma mensagem de erro com
   uma ação para tentar novamente, e os dados digitados continuam visíveis
   e editáveis (nada é perdido).

---

### Edge Cases

- O que acontece se a pessoa reativa (Pausada → Ativa) uma despesa
  recorrente durante a edição? A tela deve deixar claro que uma ocorrência
  do mês corrente pode ser gerada automaticamente ao salvar (mesmo
  comportamento já definido para o backend desta feature).
- O que acontece se a pessoa salvar sem ter alterado nenhum campo? A
  operação deve ser aceita normalmente como bem-sucedida, sem qualquer
  aviso ou bloqueio.
- O que acontece se a pessoa editar uma despesa recorrente que já tem
  ocorrências pagas? A edição deve prosseguir normalmente — dados de
  pagamento não são exibidos nem afetados por esta tela.
- O que acontece se a pessoa estiver, ao mesmo tempo, editando o pagamento
  de uma ocorrência dessa mesma despesa em outra parte da tela do painel
  mensal? Os dois fluxos são independentes; nenhuma interação entre eles é
  definida.
- O que acontece se duas edições da mesma despesa acontecerem ao mesmo
  tempo (ex.: duas abas abertas)? Não há aviso de conflito; a última a ser
  salva prevalece.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer uma tela dedicada para editar os
  dados de uma despesa recorrente já cadastrada: nome, categoria, valor
  previsto mensal, dia de vencimento, data de início, status
  (Ativa/Pausada) e observação. A frequência permanece fixa em "Mensal" e
  MUST NOT ser editável nesta tela.
- **FR-002**: O sistema MUST oferecer, em cada item da listagem do painel
  mensal, um ícone/botão de ação dedicado (ex.: lápis), ao lado das demais
  ações da linha, para abrir a tela de edição da despesa recorrente dona
  daquele item — não um link de texto nem a linha inteira clicável.
- **FR-003**: Ao abrir a tela de edição, o sistema MUST carregar e
  preencher todos os campos editáveis com os valores atuais da despesa
  recorrente antes de permitir qualquer alteração.
- **FR-004**: Enquanto os dados estão sendo carregados, o sistema MUST
  exibir uma indicação de carregamento e MUST NOT exibir os campos do
  formulário vazios ou incompletos nesse meio-tempo.
- **FR-005**: Caso a despesa recorrente não seja encontrada, o sistema
  MUST exibir uma mensagem dedicada de "não encontrada" em vez de um
  formulário, com uma forma de voltar ao painel mensal.
- **FR-006**: Caso o carregamento dos dados falhe, o sistema MUST exibir
  um estado de erro com uma ação para tentar novamente.
- **FR-007**: O sistema MUST validar cada campo editado com exatamente as
  mesmas regras já usadas no cadastro (nome não vazio; categoria dentre as
  suportadas; valor previsto mensal maior que zero, com até 2 casas
  decimais; dia de vencimento entre 1 e 31; data de início válida; status
  Ativa ou Pausada), exibindo o erro de cada campo junto a ele.
- **FR-008**: O sistema MUST impedir o envio do salvamento enquanto houver
  qualquer campo inválido, revelando de uma vez os erros de todos os
  campos inválidos quando a pessoa tentar salvar mesmo assim.
- **FR-009**: Enquanto o salvamento está em andamento, o sistema MUST
  exibir um estado de "salvando" distinto dos estados de carregamento
  inicial e de preenchimento normal.
- **FR-010**: Ao salvar com sucesso, o sistema MUST exibir uma confirmação
  com os dados já atualizados e MUST oferecer uma forma de voltar ao
  painel mensal; MUST NOT oferecer, nesta tela, uma forma de cadastrar uma
  despesa recorrente separada.
- **FR-011**: Caso o salvamento falhe, o sistema MUST exibir um estado de
  erro com uma ação para tentar novamente, preservando os dados já
  digitados pela pessoa (nada do que foi digitado é perdido).
- **FR-012**: Caso o salvamento falhe por um ou mais campos inválidos
  segundo a validação de negócio, o sistema MUST exibir o erro
  correspondente junto a cada campo apontado, e não apenas uma mensagem
  genérica.
- **FR-013**: Caso a pessoa tente sair da tela de edição com pelo menos um
  campo cujo valor atual seja diferente do valor originalmente carregado,
  o sistema MUST pedir confirmação antes de descartar essas alterações.
  Essa comparação MUST ser feita por valor: um campo editado e depois
  revertido manualmente para o valor original MUST NOT contar como
  alteração pendente.
- **FR-014**: Caso a pessoa tente sair da tela de edição sem que nenhum
  campo tenha valor diferente do originalmente carregado (inclusive após
  reverter manualmente todas as edições, ou já tendo salvado com sucesso),
  o sistema MUST NOT pedir essa confirmação.
- **FR-015**: Salvar a edição sem ter alterado o valor de nenhum campo
  MUST ser tratado como uma operação válida e bem-sucedida.
- **FR-016**: Enquanto o status estiver definido como "Ativa" na tela de
  edição, o sistema MUST exibir, de forma persistente junto ao controle de
  status (não apenas num instante pontual da troca), um texto informando
  que uma ocorrência do mês corrente pode ser gerada automaticamente ao
  salvar — mesmo padrão de texto auxiliar já usado na tela de cadastro.

### Key Entities *(include if feature involves data)*

- **Despesa recorrente**: Entidade já cadastrada cujos campos editáveis
  nesta tela são nome, categoria, valor previsto mensal, dia de
  vencimento, data de início, status (Ativa/Pausada) e observação; a
  frequência é fixa e não aparece como editável.
- **Item do painel mensal**: Representação, na listagem mensal, de uma
  ocorrência de uma despesa recorrente; é o ponto a partir do qual a
  edição da despesa recorrente dona daquele item é aberta (FR-002) — a
  edição em si nunca é da ocorrência, apenas da despesa recorrente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma pessoa consegue abrir a edição de qualquer despesa
  recorrente existente e ver todos os dados atuais já preenchidos, sem
  precisar redigitar nada que não vá mudar.
- **SC-002**: 100% das tentativas de salvar com algum campo inválido são
  bloqueadas antes do envio, com o campo problemático claramente indicado.
- **SC-003**: 100% das edições salvas com sucesso mostram uma confirmação
  cujo conteúdo corresponde exatamente aos dados salvos.
- **SC-004**: Nenhuma alteração não salva é perdida sem aviso prévio,
  exceto quando a pessoa não alterou nenhum campo.
- **SC-005**: Toda falha ao carregar ou ao salvar apresenta uma ação clara
  de continuar (tentar novamente ou voltar), nunca uma tela sem saída.

## Assumptions

- Esta especificação cobre apenas a tela e a experiência de edição
  (frontend); as regras de negócio, validações de campo e o comportamento
  de reativação (FR-016) já estão especificados e resolvidos pela feature
  de backend correspondente (`007-edit-recurring-expense`) — esta tela
  apenas consome esse comportamento e o comunica à pessoa usuária.
- Um design de referência para esta tela (`design/Editar.dc.html`) já foi
  produzido nesta mesma iniciativa e serve de base visual; nenhuma decisão
  de leiaute específica é fixada nesta especificação, que permanece restrita
  a comportamento observável, não a detalhes de implementação.
- A tela de edição é um formulário e um fluxo próprios, distintos da tela
  de cadastro de nova despesa — não existe, a partir da tela de edição,
  nenhuma forma de cadastrar uma despesa recorrente adicional (FR-010).
- Exclusão de despesa recorrente é uma capacidade distinta e permanece
  fora do escopo desta tela.
- Qualquer interação com o pagamento de uma ocorrência (marcar como paga,
  desfazer pagamento) permanece exclusiva das ações já existentes para
  essa finalidade e não é afetada por esta tela.
- Não há, nesta etapa, nenhum mecanismo de aviso de edição concorrente
  (duas pessoas editando a mesma despesa ao mesmo tempo); a última edição
  salva prevalece, sem aviso.
