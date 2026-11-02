# Feature Specification: Deletar Despesa Recorrente (Backend)

**Feature Branch**: `010-delete-recurring-expense`

**Created**: 2026-10-31

**Status**: Draft

**Input**: User description: "@refinements/backend/deletar-despesa-recorrente.md"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Excluir preservando o histórico pago (Priority: P1)

Quando um pedido de exclusão de despesa recorrente escolhe a abrangência
"apenas pendentes/vencidas", o sistema remove toda ocorrência ainda não
paga desta despesa, preserva intactas todas as ocorrências já pagas, e
impede que essa despesa gere qualquer ocorrência nova a partir de agora —
de forma definitiva, sem possibilidade de reativação.

**Why this priority**: É a abrangência que preserva dado financeiro já
registrado (pagamentos); é o caminho mais seguro e, por isso, o núcleo
funcional desta feature — sem ele, "excluir" só poderia significar "apagar
tudo", o que RF04/RF05 do refinamento funcional explicitamente rejeita como
única opção.

**Independent Test**: Pode ser testado isoladamente excluindo, com esta
abrangência, uma despesa recorrente que tem ao menos uma ocorrência paga e
uma pendente, e verificando que a ocorrência paga continua acessível na sua
competência enquanto a pendente desaparece, e que nenhuma ocorrência nova é
gerada ao avançar para uma competência futura.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente com uma ocorrência paga e uma
   ocorrência pendente, **When** ela é excluída com a abrangência "apenas
   pendentes/vencidas", **Then** a ocorrência pendente é removida e a
   ocorrência paga permanece acessível, inalterada, na competência em que
   foi paga.
2. **Given** a mesma despesa já excluída com essa abrangência, **When** uma
   nova competência é alcançada, **Then** nenhuma ocorrência nova é gerada
   para essa despesa.
3. **Given** uma despesa recorrente que só possui ocorrências pagas,
   **When** ela é excluída com a abrangência "apenas pendentes/vencidas",
   **Then** nenhuma ocorrência é removida, mas a despesa deixa de gerar
   qualquer ocorrência nova a partir de agora.
4. **Given** uma despesa recorrente cuja ocorrência da competência atual
   ainda não foi paga, **When** ela é excluída com essa abrangência,
   **Then** essa ocorrência específica é removida e deixa de aparecer na
   competência atual.
5. **Given** uma despesa recorrente cuja ocorrência da competência atual já
   foi paga, **When** ela é excluída com essa abrangência, **Then** essa
   ocorrência específica permanece visível, marcada como paga, exatamente
   como antes da exclusão.

---

### User Story 2 - Excluir todo o histórico (Priority: P1)

Quando um pedido de exclusão escolhe a abrangência "todo o histórico", o
sistema remove absolutamente todas as ocorrências desta despesa recorrente
— pagas ou não — e a própria despesa recorrente deixa de existir, não
ficando mais acessível em nenhuma consulta.

**Why this priority**: É a outra abrangência obrigatória definida pelo
refinamento funcional (RF03/RF06); sem ela, não há como o usuário remover
completamente uma despesa cadastrada por engano ou que não deve deixar
nenhum rastro.

**Independent Test**: Pode ser testado isoladamente excluindo, com esta
abrangência, uma despesa recorrente que tem ocorrências pagas e pendentes,
e verificando que nenhuma delas continua acessível em nenhuma consulta, e
que uma nova consulta pela própria despesa recorrente responde "não
encontrado".

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente com ocorrências pagas e pendentes,
   **When** ela é excluída com a abrangência "todo o histórico", **Then**
   nenhuma dessas ocorrências continua acessível em nenhuma competência.
2. **Given** a mesma despesa, **When** a exclusão é concluída, **Then** uma
   nova consulta por essa despesa recorrente responde "não encontrado".
3. **Given** uma despesa recorrente que nunca teve nenhuma ocorrência paga,
   **When** ela é excluída com qualquer uma das duas abrangências, **Then**
   o resultado sobre as ocorrências é o mesmo, mas apenas a abrangência
   "todo o histórico" faz a despesa recorrente em si deixar de existir.

---

### User Story 3 - Rejeitar uma exclusão sem abrangência válida (Priority: P2)

Quando um pedido de exclusão não informa a abrangência, ou informa um valor
que não corresponde a nenhuma das duas abrangências suportadas, o sistema
recusa o pedido por completo, sem remover nenhuma ocorrência e sem alterar
a despesa recorrente.

**Why this priority**: Evita que uma ambiguidade de entrada resulte em uma
exclusão com abrangência não intencional; é a mesma prioridade de proteção
de dado dada a validações equivalentes nas features de cadastro e edição já
entregues.

**Independent Test**: Pode ser testado isoladamente enviando um pedido de
exclusão sem o parâmetro de abrangência, e separadamente com um valor que
não seja nenhum dos dois suportados, e verificando que nenhuma ocorrência
ou despesa é afetada em nenhum dos dois casos.

**Acceptance Scenarios**:

1. **Given** um pedido de exclusão sem o parâmetro de abrangência, **When**
   o pedido é processado, **Then** ele é recusado por completo e nenhum
   dado é alterado.
2. **Given** um pedido de exclusão com um valor de abrangência que não seja
   nenhuma das duas abrangências suportadas, **When** o pedido é
   processado, **Then** ele é recusado por completo e nenhum dado é
   alterado.

---

### User Story 4 - Tratar identificador inexistente ou já excluído como "não encontrado" (Priority: P3)

Quando um pedido de exclusão, consulta ou edição aponta para um
identificador que nunca correspondeu a uma despesa recorrente, ou que
correspondia a uma despesa já excluída anteriormente (em qualquer
abrangência), o sistema responde "não encontrado" — nunca cria nada, nunca
tenta uma segunda exclusão, e nunca retorna sucesso.

**Why this priority**: Garante que uma exclusão seja de fato definitiva
(RF14/EC07 do refinamento funcional: nada de "ressuscitar" ou editar uma
despesa já excluída) e reaproveita, sem lógica nova, o mesmo tratamento de
"não encontrado" já usado pela consulta e pela edição existentes.

**Independent Test**: Pode ser testado isoladamente tentando excluir um
identificador que nunca existiu, depois excluindo uma despesa real com
sucesso e tentando excluí-la (ou editá-la, ou consultá-la) uma segunda vez,
verificando que todas essas tentativas respondem "não encontrado".

**Acceptance Scenarios**:

1. **Given** um identificador que nunca correspondeu a nenhuma despesa
   recorrente, **When** uma exclusão é solicitada para ele, **Then** a
   resposta é "não encontrado".
2. **Given** uma despesa recorrente já excluída com a abrangência "todo o
   histórico", **When** qualquer nova exclusão, consulta ou edição é
   solicitada para o mesmo identificador, **Then** a resposta é "não
   encontrado".
3. **Given** uma despesa recorrente já excluída com a abrangência "apenas
   pendentes/vencidas" (que ainda existe internamente por preservar
   histórico pago), **When** qualquer nova exclusão, consulta ou edição é
   solicitada para o mesmo identificador, **Then** a resposta é "não
   encontrado", exatamente como se a despesa não existisse mais.

---

### Edge Cases

- Duas exclusões concorrentes do mesmo identificador (ex.: duas abas do
  navegador): nenhum mecanismo de concorrência otimista é assumido; a
  segunda a ser processada encontra a despesa já excluída pela primeira e
  recebe "não encontrado" (User Story 4).
- Excluir (qualquer abrangência) uma despesa recorrente que nunca teve
  nenhuma ocorrência gerada: a exclusão é bem-sucedida normalmente, sem
  nenhuma ocorrência para remover ou preservar.
- Consultar o histórico de pagamentos (ex.: painel mensal, por competência)
  de uma despesa excluída com "apenas pendentes/vencidas": as ocorrências
  já pagas continuam aparecendo normalmente nessas consultas, mesmo que a
  consulta direta pela despesa recorrente (User Story 4) responda "não
  encontrado".
- Interação entre excluir uma despesa recorrente e uma de suas ocorrências
  estar, no mesmo instante, em edição de pagamento em outra tela: nenhuma
  interação entre as duas ações é definida por esta feature (mesmo ponto em
  aberto já registrado para a edição de despesa recorrente).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST expor uma forma de excluir uma despesa
  recorrente pelo seu identificador, exigindo como entrada também a
  abrangência da exclusão (uma das duas definidas em FR-003/FR-004), sem
  nenhum valor padrão assumido pelo sistema quando a abrangência está
  ausente.
- **FR-002**: Uma solicitação de exclusão sem abrangência informada, ou com
  um valor de abrangência que não seja nenhuma das duas suportadas, MUST
  ser recusada por completo, identificando o campo inválido, sem remover
  nenhuma ocorrência nem alterar a despesa recorrente.
- **FR-003**: Na abrangência "apenas pendentes/vencidas", o sistema MUST
  remover toda ocorrência desta despesa que não esteja paga, MUST manter
  inalterada toda ocorrência já paga, e MUST impedir, de forma definitiva e
  sem possibilidade de reativação, que essa despesa gere qualquer
  ocorrência nova a partir de então.
- **FR-004**: Na abrangência "todo o histórico", o sistema MUST remover
  absolutamente todas as ocorrências desta despesa — pagas ou não — e a
  própria despesa recorrente MUST deixar de existir e de ser encontrável
  por qualquer consulta subsequente.
- **FR-005**: A exclusão (em qualquer abrangência) MUST ser permitida
  independentemente de a despesa recorrente estar, no momento do pedido,
  com status Ativa ou Pausada.
- **FR-006**: Excluir um identificador que não corresponde a nenhuma
  despesa recorrente cadastrada MUST ser tratado como "não encontrado",
  sem criar nada e sem retornar sucesso.
- **FR-007**: Excluir, consultar ou editar uma despesa recorrente que já
  foi excluída anteriormente (em qualquer uma das duas abrangências) MUST
  ser tratado como "não encontrado", exatamente como se essa despesa nunca
  tivesse existido — mesmo quando a abrangência anterior foi "apenas
  pendentes/vencidas" e a despesa ainda existe internamente para preservar
  histórico pago.
- **FR-008**: O efeito de FR-007 sobre consultas MUST NOT se estender a
  consultas que servem o painel mensal por competência ou o fluxo de
  pagamento de uma ocorrência específica — essas consultas MUST continuar
  encontrando normalmente uma ocorrência já paga de uma despesa excluída
  com "apenas pendentes/vencidas", preservando o histórico que essa
  abrangência existe para proteger.
- **FR-009**: Uma exclusão bem-sucedida (qualquer abrangência) MUST
  devolver uma confirmação identificando ao menos a despesa recorrente
  afetada e a abrangência efetivamente aplicada.
- **FR-010**: Toda resposta desta operação de exclusão — sucesso ou erro —
  MUST usar o mesmo envelope de resposta padrão já estabelecido pelas
  demais operações existentes sobre despesa recorrente, incluindo o mesmo
  tratamento de erro por campo (para abrangência inválida/ausente) e o
  mesmo tratamento de "não encontrado" (para identificador inexistente ou
  já excluído) já usados pela consulta e pela edição existentes.

### Key Entities *(include if feature involves data)*

- **Despesa recorrente**: Entidade já cadastrada que, a partir desta
  feature, passa a admitir um estado de exclusão definitivo e sem volta,
  ortogonal ao status Ativa/Pausada já existente. Quando excluída com
  "apenas pendentes/vencidas", continua existindo internamente (para
  preservar histórico pago) mas se torna não encontrável por qualquer
  consulta direta, edição ou nova exclusão. Quando excluída com "todo o
  histórico", deixa de existir.
- **Ocorrência**: Materialização mensal de uma despesa recorrente, com
  status Pendente ou Paga. A abrangência "apenas pendentes/vencidas" remove
  toda ocorrência não paga e preserva toda ocorrência paga; a abrangência
  "todo o histórico" remove ambas sem exceção.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das exclusões com a abrangência "apenas
  pendentes/vencidas" preservam, inalteradas e acessíveis, todas as
  ocorrências já pagas da despesa excluída.
- **SC-002**: 100% das exclusões com a abrangência "todo o histórico"
  resultam em zero ocorrências acessíveis e na despesa recorrente deixando
  de ser encontrável por qualquer consulta.
- **SC-003**: 100% dos pedidos de exclusão sem abrangência válida são
  recusados antes de qualquer remoção, sem exceção.
- **SC-004**: 100% das tentativas de excluir, consultar ou editar um
  identificador inexistente ou já excluído recebem "não encontrado".
- **SC-005**: 100% das despesas excluídas com "apenas pendentes/vencidas"
  nunca geram nenhuma ocorrência nova em nenhuma competência futura.

## Assumptions

- Domain, Application, Infrastructure e API do cadastro e da edição de
  despesa recorrente já existem e permanecem como estão, exceto pelo ajuste
  pontual necessário para que uma despesa excluída se torne "não
  encontrada" para consulta e edição (FR-007).
- Não há nenhum mecanismo de "desfazer exclusão" ou auditoria de quem
  excluiu e quando — ambos permanecem fora de escopo, assim como no
  refinamento funcional de origem.
- Nenhum mecanismo de concorrência otimista é necessário; duas exclusões
  concorrentes do mesmo identificador resultam na segunda recebendo "não
  encontrado", sem aviso especial.
- Não há interação definida entre excluir uma despesa recorrente e uma de
  suas ocorrências estar, no mesmo instante, em edição de pagamento em
  outra tela.
- Autenticação, autorização e política de CORS reais permanecem fora de
  escopo nesta etapa, pela mesma exceção de fase já vigente para os demais
  endpoints existentes.
- O ponto de entrada (tela/navegação) de onde a exclusão é acionada, e qual
  abrangência vem pré-selecionada por padrão na interface, são definidos
  por uma feature de frontend separada; esta especificação cobre apenas o
  comportamento do sistema ao processar uma exclusão, não onde ou como o
  usuário a aciona.
