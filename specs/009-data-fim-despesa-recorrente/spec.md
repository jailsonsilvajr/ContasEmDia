# Feature Specification: Data de Fim da Despesa Recorrente

**Feature Branch**: `009-data-fim-despesa-recorrente`

**Created**: 2026-09-17

**Status**: Draft

**Input**: User description: "Crie a especificação do refinamento @refinements/data-fim-despesa-recorrente.md"

## Clarifications

### Session 2026-09-17

- Q: O sistema deve permitir que a "Data de fim" seja definida em uma
  competência anterior ao mês atual — seja no cadastro de uma despesa
  retroativa já "vencida", seja reduzindo a vigência de uma despesa
  existente para antes de hoje — mesmo que isso resulte em nenhuma
  ocorrência futura permanecer? → A: Não permitir — a data de fim deve ser
  sempre igual ou posterior ao mês atual, tanto no cadastro quanto na
  edição; violação bloqueada com mensagem própria.
- Q: Ao estender a vigência de uma despesa cujo teto anterior já estava no
  passado, o sistema deve gerar ocorrências apenas a partir do mês atual em
  diante, ou deve também preencher retroativamente os meses entre o antigo
  teto e hoje? → B: Gerar todos os meses entre o antigo teto (exclusive) e
  a nova data de fim (inclusive), incluindo os que já ficaram no passado —
  preenchendo retroativamente a lacuna.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar despesa recorrente com data de fim (Priority: P1)

Ao cadastrar uma despesa recorrente, o usuário informa obrigatoriamente uma
data de fim, além da data de início já existente. O sistema valida essa data
e, ao salvar, já gera de uma vez todas as ocorrências mensais previstas
entre o mês atual e o mês da data de fim (inclusive) — independentemente de
o usuário ter marcado a despesa como Ativa ou Pausada.

**Why this priority**: É o núcleo da feature. Sem a data de fim obrigatória
e sem a geração antecipada de todas as ocorrências da vigência, nenhuma das
demais histórias faz sentido — hoje o cadastro só gera a ocorrência do mês
atual, obrigando o usuário a "descobrir" as ocorrências futuras mês a mês.

**Independent Test**: Pode ser testada cadastrando uma despesa recorrente
informando data de início e data de fim válidas, e conferindo que todas as
ocorrências mensais entre o mês atual e o mês da data de fim aparecem
imediatamente no painel de despesas, sem precisar navegar mês a mês nem
reativar nada.

**Acceptance Scenarios**:

1. **Given** o formulário de cadastro de despesa recorrente, **When** o
   usuário tenta salvar sem preencher a data de fim, **Then** a submissão é
   bloqueada e o campo "Data de fim" exibe a mensagem de obrigatoriedade.
2. **Given** uma data de fim igual ou anterior à data de início, **When** o
   usuário tenta salvar, **Then** a submissão é bloqueada com a mensagem "A
   data de fim deve ser posterior à data de início."
3. **Given** uma vigência (intervalo entre início e fim) maior que 1 ano,
   **When** o usuário tenta salvar, **Then** a submissão é bloqueada com uma
   mensagem informando que a vigência máxima é de 1 ano.
4. **Given** uma despesa com data de início no mês atual e data de fim N
   meses à frente (N ≤ 12), **When** o usuário salva com status Ativa,
   **Then** são geradas exatamente N+1 ocorrências, uma por mês da vigência.
5. **Given** as mesmas datas do cenário anterior, **When** o usuário salva
   com status Pausada, **Then** o sistema gera as mesmas N+1 ocorrências,
   sem diferença de comportamento em relação ao status Ativa.
6. **Given** uma despesa cadastrada com data de fim no mês C, **When** o
   usuário navega no painel mensal até qualquer mês entre o atual e C,
   **Then** a ocorrência correspondente já está visível, sem exigir
   navegação prévia nem reativação manual.

---

### User Story 2 - Estender a vigência editando a data de fim (Priority: P2)

Ao editar uma despesa recorrente já cadastrada, o usuário pode alterar a
data de fim para uma data posterior à atual, ampliando o período coberto. O
sistema gera automaticamente as ocorrências dos meses recém-incluídos na
vigência.

**Why this priority**: Complementa o cadastro permitindo corrigir/estender
uma vigência sem precisar recadastrar a despesa; depende da User Story 1
existir primeiro.

**Independent Test**: Pode ser testada editando a data de fim de uma
despesa existente para uma data mais distante e conferindo que as
ocorrências dos meses recém-cobertos passam a existir, sem duplicar as que
já existiam.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente já cadastrada, **When** o usuário edita
   a data de fim para uma data posterior à anterior, **Then** as ocorrências
   dos meses recém-cobertos pela nova vigência são criadas.
2. **Given** o mesmo cenário, **When** a edição é salva, **Then** nenhuma
   ocorrência já existente é duplicada.
3. **Given** uma nova data de fim que também viola o teto de 1 ano em
   relação à data de início atual, **When** o usuário tenta salvar, **Then**
   a edição é bloqueada com a mesma mensagem de teto de vigência.
4. **Given** uma despesa cuja data de fim anterior já estava em um mês
   passado, **When** o usuário estende a data de fim para um mês futuro,
   **Then** as ocorrências de todos os meses entre a antiga data de fim
   (exclusive) e a nova data de fim (inclusive) são geradas, incluindo os
   meses que já haviam ficado no passado.

---

### User Story 3 - Reduzir a vigência editando a data de fim (Priority: P2)

Ao editar uma despesa recorrente já cadastrada, o usuário pode alterar a
data de fim para uma data anterior à atual, reduzindo o período coberto. O
sistema remove automaticamente as ocorrências dos meses que deixaram de
fazer parte da vigência — desde que nenhuma delas já tenha sido paga.

**Why this priority**: Simétrica à User Story 2 e igualmente dependente da
User Story 1; foi priorizada junto pois usa a mesma tela e o mesmo campo,
mas representa um caminho de teste e um risco (perda de dados já pagos)
diferente o suficiente para ser tratada como história própria.

**Independent Test**: Pode ser testada editando a data de fim de uma
despesa existente (sem ocorrências pagas no intervalo a remover) para uma
data mais próxima e conferindo que as ocorrências dos meses excluídos da
vigência desaparecem.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente já cadastrada sem nenhuma ocorrência
   paga nos meses que sairiam da vigência, **When** o usuário edita a data
   de fim para uma data anterior à atual, **Then** as ocorrências dos meses
   que deixaram de ser cobertos são excluídas.
2. **Given** a mesma despesa, **When** pelo menos uma ocorrência nos meses
   que sairiam da vigência já está paga, **Then** a edição inteira é
   rejeitada — nenhum campo da edição é salvo, nenhuma ocorrência é excluída
   ou alterada, e o erro é reportado no campo "Data de fim".
3. **Given** uma despesa recorrente já cadastrada, **When** o usuário tenta
   editar a data de fim para uma competência anterior ao mês atual,
   **Then** a edição é rejeitada com uma mensagem indicando que a data de
   fim não pode estar no passado, independentemente de haver ou não
   ocorrência paga no intervalo.

---

### User Story 4 - Despesas já cadastradas antes desta mudança continuam consistentes (Priority: P3)

Despesas recorrentes cadastradas antes desta mudança não tinham data de
fim. Após a mudança, essas despesas passam a ter uma data de fim
automaticamente definida como um ano após a data de início, para que
continuem respeitando a mesma regra de vigência máxima aplicada a despesas
novas.

**Why this priority**: É uma consequência necessária para manter os dados
existentes consistentes com a nova regra obrigatória, mas não bloqueia o
uso da feature em despesas novas nem é percebida ativamente pelo usuário no
dia a dia — por isso prioridade mais baixa.

**Independent Test**: Pode ser testada consultando uma despesa recorrente
que já existia antes da mudança e conferindo que sua data de fim é igual à
data de início mais 1 ano, sem nenhuma ocorrência nova sendo gerada
retroativamente só por causa dessa atualização de dado.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente cadastrada antes desta mudança, **When**
   ela é consultada após a mudança entrar em vigor, **Then** sua data de fim
   é igual à data de início mais 1 ano.
2. **Given** o mesmo cenário, **When** a atualização de dado ocorre,
   **Then** nenhuma ocorrência adicional é gerada retroativamente apenas por
   causa dessa atualização.

---

### Edge Cases

- Data de fim igual à data de início → rejeitada (a regra exige "posterior",
  não "igual ou posterior").
- Data de fim anterior à data de início → rejeitada.
- Data de fim no mesmo mês/ano da data de início → válida; gera exatamente
  uma ocorrência.
- Vigência maior que 1 ano → rejeitada, tanto no cadastro quanto na edição.
- Despesa cadastrada como Pausada → gera normalmente todas as ocorrências da
  vigência, sem diferença de comportamento em relação a uma despesa Ativa.
- Data de início no futuro (mês de início posterior ao mês atual) → nenhuma
  ocorrência é gerada ainda no cadastro; segue a mesma limitação já conhecida
  de despesas com início futuro.
- Data de início retroativa → a geração começa no mês atual, não no mês de
  início; meses passados entre o início e hoje não são materializados como
  ocorrência.
- Editar a data de início de forma que ultrapasse a data de fim atual, ou
  que a vigência resultante exceda 1 ano → rejeitado, nenhum campo é
  alterado.
- Editar a data de fim para um valor que também violaria o teto de 1 ano
  frente à data de início atual → rejeitado.
- Data de fim informada em uma competência anterior ao mês atual (no
  cadastro ou reduzindo a vigência na edição) → rejeitada, mesmo que
  resulte em nenhuma ocorrência restante.
- Estender a data de fim quando o teto anterior já estava no passado → gera
  as ocorrências de todos os meses entre o antigo teto (exclusive) e o novo
  teto (inclusive), inclusive os que já ficaram no passado, sem depender de
  o usuário ter feito a edição antes desses meses acontecerem.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exigir uma data de fim ao cadastrar uma
  despesa recorrente; o cadastro sem essa data MUST ser bloqueado com uma
  mensagem indicando que o campo é obrigatório.
- **FR-002**: O sistema MUST rejeitar uma data de fim que não seja
  estritamente posterior à data de início, tanto no cadastro quanto na
  edição.
- **FR-003**: O sistema MUST rejeitar uma vigência (intervalo entre data de
  início e data de fim) superior a 1 ano, tanto no cadastro quanto na
  edição.
- **FR-004**: Ao cadastrar uma despesa recorrente válida, o sistema MUST
  gerar, de uma só vez, uma ocorrência para cada mês entre o mês atual
  (piso) e o mês da data de fim (teto, inclusive) — exceto quando a data de
  início ainda estiver no futuro, caso em que nenhuma ocorrência é gerada
  ainda.
- **FR-005**: A geração de ocorrências no cadastro MUST se comportar da
  mesma forma independentemente de a despesa ter sido cadastrada com status
  Ativa ou Pausada.
- **FR-006**: O sistema MUST permitir editar a data de fim de uma despesa
  recorrente já cadastrada.
- **FR-007**: Quando a data de fim de uma despesa existente for editada
  para uma data posterior à anterior, o sistema MUST gerar uma ocorrência
  para cada mês entre a antiga data de fim (exclusive) e a nova data de fim
  (inclusive) — incluindo meses que já ficaram no passado desde a antiga
  data de fim — sem duplicar nenhuma ocorrência já existente para o mesmo
  mês.
- **FR-008**: Quando a data de fim de uma despesa existente for editada
  para uma data anterior à anterior, o sistema MUST excluir as ocorrências
  dos meses que deixaram de fazer parte da vigência, desde que nenhuma delas
  já esteja paga.
- **FR-009**: Se a redução da vigência exigir excluir pelo menos uma
  ocorrência já paga, o sistema MUST rejeitar a edição inteira — nenhum
  campo da edição é salvo, nenhuma ocorrência é excluída ou alterada — e
  MUST reportar o erro associado ao campo "Data de fim".
- **FR-010**: O sistema MUST aplicar as mesmas validações de data de fim
  (obrigatoriedade, posterioridade em relação ao início, teto de 1 ano) de
  forma consistente tanto na interface de cadastro quanto na de edição, e
  tanto do lado do usuário (feedback imediato) quanto na confirmação final
  do sistema.
- **FR-011**: Para despesas recorrentes cadastradas antes desta mudança
  (sem data de fim registrada), o sistema MUST considerar/atribuir uma data
  de fim igual à data de início mais 1 ano, sem gerar nenhuma ocorrência
  retroativa adicional apenas em função dessa atribuição.
- **FR-012**: Ao editar a data de início de uma despesa de forma que ela
  deixe de ser anterior à data de fim vigente, ou que a vigência resultante
  ultrapasse 1 ano, o sistema MUST rejeitar a edição, sem alterar nenhum
  campo.
- **FR-013**: Os textos apresentados ao usuário nas telas de cadastro e de
  confirmação MUST descrever que a geração cobre toda a vigência (até o
  teto de 1 ano), e não apenas o mês atual com meses futuros "gerados
  depois".
- **FR-014**: O sistema MUST rejeitar qualquer data de fim, no cadastro ou
  na edição, cuja competência seja anterior ao mês atual — com uma mensagem
  indicando que a data de fim não pode estar no passado — mesmo quando essa
  seria a única violação (ex.: reduzir a vigência para antes de hoje sem
  nenhuma ocorrência paga no intervalo).

### Key Entities

- **Despesa recorrente**: representa um gasto mensal previsto pelo usuário,
  com nome, categoria, valor previsto mensal, dia de vencimento, status
  (Ativa/Pausada), data de início e, a partir desta mudança, data de fim
  obrigatória — que juntas delimitam a vigência (janela de meses) em que a
  despesa gera ocorrências.
- **Ocorrência**: representa a materialização da despesa recorrente em um
  mês específico da vigência, com seu próprio vencimento, valor previsto e
  status de pagamento (Pendente/Paga). Uma despesa recorrente pode ter no
  máximo uma ocorrência por mês.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das despesas recorrentes cadastradas a partir desta
  mudança têm todas as ocorrências da vigência (até 13 meses) visíveis no
  painel mensal imediatamente após o cadastro, sem exigir navegação prévia
  mês a mês.
- **SC-002**: 100% das tentativas de cadastro ou edição com data de fim
  ausente, anterior/igual à data de início, anterior ao mês atual, ou com
  vigência acima de 1 ano são bloqueadas antes de qualquer alteração ser
  salva.
- **SC-003**: 100% das tentativas de reduzir a vigência que excluiriam uma
  ocorrência já paga são rejeitadas por completo, sem nenhuma perda de dado
  de pagamento já registrado.
- **SC-004**: 100% das despesas recorrentes cadastradas antes desta mudança
  passam a ter uma data de fim consistente com o teto de 1 ano, sem
  necessidade de ação manual do usuário.
- **SC-005**: Usuários deixam de precisar navegar mês a mês ou reativar
  manualmente uma despesa para ver ocorrências futuras já previstas dentro
  da vigência cadastrada.

## Assumptions

- O teto de vigência de 1 ano é medido a partir da data de início, e não da
  data do cadastro.
- Não há, nesta feature, nenhum processo periódico independente (ex.: job
  mensal) que gere ocorrências além do que é gerado no momento do cadastro
  ou da edição — a geração é sempre disparada por uma ação do usuário.
- O comportamento de reativação de uma despesa Pausada (mudar o status de
  volta para Ativa) não é revisado por esta feature; qualquer efeito da
  reativação sobre a geração de ocorrências permanece como definido em
  refinamentos anteriores e pode precisar de revisão à parte, já que esta
  feature faz o cadastro gerar toda a vigência independentemente do status.
- Não há, nesta feature, nenhuma confirmação prévia na tela de edição
  avisando quantas ocorrências serão criadas ou excluídas antes de salvar —
  o resultado é refletido apenas após a submissão, no mesmo padrão já usado
  para os demais campos editáveis.
- O suporte é restrito a despesas recorrentes com frequência mensal, mesma
  frequência já suportada hoje.
- A regra de FR-014 (data de fim não pode estar no passado) se aplica
  apenas a ações do usuário (cadastro/edição). O backfill de migração da
  User Story 4 pode legitimamente resultar em uma data de fim já no
  passado para despesas antigas (quando `data de início + 1 ano` já passou)
  — isso não é um erro nem bloqueia a leitura dessas despesas, só passa a
  valer quando o usuário tentar editar a data de fim dali em diante.
