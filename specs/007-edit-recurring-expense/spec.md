# Feature Specification: Editar Despesa Recorrente (Backend)

**Feature Branch**: `007-edit-recurring-expense`

**Created**: 2026-09-10

**Status**: Draft

**Input**: User description: "Editar despesas recorrentes baseando-se no refinamento @refinements/backend/editar-despesa-recorrente.md"

## Clarifications

### Session 2026-09-10

- Q: Ao reativar uma despesa recorrente pausada (status Pausada → Ativa), o
  sistema deve gerar automaticamente uma ocorrência Pendente para a
  competência atual (quando ainda não existir uma e a data de início já
  tiver começado), ou a reativação deve apenas mudar o status, sem gerar
  nenhuma ocorrência automaticamente? → A: Sim, gera automaticamente —
  espelha o comportamento já existente no cadastro (RF10/RF11 do domínio);
  confirma User Story 3 e FR-005–FR-007/SC-005 como estão especificados.
- Q: Deveria haver alguma restrição ao editar a data de início de uma
  despesa recorrente que já tem ocorrências geradas (ex.: impedir mover a
  data de início para depois de uma competência que já tem ocorrência), ou
  qualquer data de início válida deve ser aceita sem essa checagem extra?
  → A: Sem restrição adicional — qualquer data de início válida é aceita,
  mesmo que crie uma inconsistência lógica com ocorrências já geradas;
  confirma FR-008 como está especificado.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Editar os dados de uma despesa recorrente com sucesso (Priority: P1)

Quando um pedido de edição contém valores válidos para nome, categoria,
valor previsto mensal, dia de vencimento, data de início, status
(Ativa/Pausada) e observação de uma despesa recorrente já cadastrada, o
sistema precisa aplicar as alterações e devolver os dados já atualizados,
sem reescrever nenhum dado já gravado em ocorrências geradas anteriormente.

**Why this priority**: É a ação central desta feature; sem ela, nenhuma
despesa recorrente já cadastrada pode ser corrigida ou atualizada depois do
cadastro inicial.

**Independent Test**: Pode ser testado isoladamente enviando um pedido de
edição com todos os campos válidos para uma despesa recorrente existente e
verificando que a resposta contém os dados atualizados, e que uma ocorrência
já gerada antes da edição continua com os valores antigos de nome, categoria,
valor e vencimento.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente existente com ao menos uma ocorrência já
   gerada, **When** seu nome, categoria, valor previsto mensal ou dia de
   vencimento são editados com valores válidos, **Then** a edição é
   confirmada com os dados atualizados e a ocorrência já existente continua
   exibindo os valores antigos inalterados.
2. **Given** uma despesa recorrente existente, **When** ela é editada sem
   nenhuma mudança de valor em nenhum campo, **Then** a operação é
   bem-sucedida e nenhuma ocorrência é criada, alterada ou removida.
3. **Given** uma despesa recorrente que já possui ocorrências marcadas como
   pagas, **When** ela é editada, **Then** a edição é aplicada normalmente e
   nenhum dado de pagamento (valor pago, data de pagamento, status da
   ocorrência) é lido ou alterado.

---

### User Story 2 - Consultar os dados atuais de uma despesa recorrente para editar (Priority: P2)

Antes de enviar uma edição, é preciso consultar os dados atuais de uma
despesa recorrente pelo seu identificador, para que os valores existentes
possam ser pré-carregados e apresentados antes de qualquer alteração.

**Why this priority**: Sem uma forma de consultar os dados atuais, não é
possível pré-carregar os valores existentes nem confirmar o que foi
efetivamente salvo após uma edição — a User Story 1 depende desta consulta
para ser utilizável de ponta a ponta.

**Independent Test**: Pode ser testado isoladamente consultando o
identificador de uma despesa recorrente existente e verificando que a
resposta contém todos os campos editáveis com seus valores atuais, sem a
lista de ocorrências.

**Acceptance Scenarios**:

1. **Given** o identificador de uma despesa recorrente existente, **When** a
   consulta é feita, **Then** a resposta contém nome, categoria, valor
   previsto mensal, dia de vencimento, data de início, status e observação
   com os valores atuais.
2. **Given** um identificador que não corresponde a nenhuma despesa
   recorrente cadastrada, **When** a consulta é feita, **Then** a resposta
   indica "não encontrado", nunca uma resposta de sucesso com dados vazios.

---

### User Story 3 - Reativar uma despesa recorrente pausada durante a edição (Priority: P3)

Quando a edição muda o status de uma despesa recorrente de Pausada para
Ativa, o sistema precisa, na mesma operação, avaliar se uma ocorrência da
competência atual deve ser gerada, sem exigir uma segunda ação do
solicitante.

**Why this priority**: É um efeito colateral importante de uma edição comum
(troca de status), mas só se aplica ao subconjunto de edições que envolvem
reativação — a edição dos demais campos (User Story 1) já entrega valor sem
depender deste comportamento.

**Independent Test**: Pode ser testado isoladamente editando o status de uma
despesa recorrente Pausada para Ativa e verificando o comportamento de
geração (ou não) da ocorrência da competência atual, conforme as condições
de data e de não duplicidade.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente Pausada cuja data de início já começou e
   que não possui ocorrência para a competência atual, **When** seu status é
   editado para Ativa, **Then** uma ocorrência Pendente para a competência
   atual é criada com o valor previsto mensal vigente.
2. **Given** uma despesa recorrente Pausada que, por alguma inconsistência,
   já possui uma ocorrência para a competência atual, **When** seu status é
   editado para Ativa, **Then** nenhuma ocorrência adicional é criada.
3. **Given** uma despesa recorrente Pausada cuja data de início ainda não
   chegou, **When** seu status é editado para Ativa, **Then** o status muda
   para Ativa mas nenhuma ocorrência da competência atual é criada.

---

### User Story 4 - Receber erros claros ao editar com dados inválidos ou identificador inexistente (Priority: P4)

Quando um pedido de edição contém um campo com valor inválido, ou aponta
para um identificador que não corresponde a nenhuma despesa recorrente
cadastrada, o sistema precisa recusar a operação por completo e informar
exatamente o que impediu a edição.

**Why this priority**: Sem um retorno de erro claro e completo, quem está
editando não sabe o que corrigir, ou pode acreditar erroneamente que uma
edição foi aplicada quando não foi.

**Independent Test**: Pode ser testado isoladamente enviando um pedido de
edição com um campo inválido, e separadamente um pedido de edição para um
identificador inexistente, verificando que nenhum dado é alterado em nenhum
dos dois casos.

**Acceptance Scenarios**:

1. **Given** um pedido de edição com um ou mais campos violando uma regra de
   validação, **When** a edição é solicitada, **Then** a operação é
   recusada por completo, identificando cada campo inválido, e nenhum campo
   da despesa recorrente é alterado.
2. **Given** um pedido de edição (ou de consulta) para um identificador que
   não corresponde a nenhuma despesa recorrente cadastrada, **When** a
   operação é solicitada, **Then** o sistema responde como "não encontrado",
   sem criar uma nova despesa nem retornar sucesso.

---

### Edge Cases

- Trocar dia de vencimento para um valor fora de 1–31, apagar o nome
  (vazio/só espaços), ou trocar o valor previsto mensal para zero, negativo
  ou com mais de 2 casas decimais: rejeitado pela mesma validação já usada no
  cadastro; nenhum dado é salvo.
- Trocar categoria, nome ou valor previsto mensal de uma despesa que já tem
  ocorrência na competência atual: a ocorrência já existente mantém os
  valores antigos para sempre; só ocorrências futuras usam os novos valores.
- Editar a data de início: não cria nem apaga retroativamente nenhuma
  ocorrência para competências entre a data antiga e a nova.
- Duas edições concorrentes da mesma despesa recorrente (ex.: duas abas do
  navegador): nenhum mecanismo de concorrência otimista é assumido — a
  última edição salva prevalece, sem aviso.
- Editar uma despesa recorrente enquanto uma de suas ocorrências está sendo
  editada (pagamento) em outra tela: os dois fluxos são independentes;
  nenhuma interação entre eles é definida por esta feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir alterar nome, categoria, valor
  previsto mensal, dia de vencimento, data de início, status (Ativa/Pausada)
  e observação de uma despesa recorrente já cadastrada. A frequência
  permanece fixa em "Mensal" e MUST NOT ser editável.
- **FR-002**: Cada campo editado MUST seguir exatamente a mesma regra de
  validação já aplicada no cadastro (nome não vazio, categoria dentre as
  suportadas, valor previsto mensal maior que zero com até 2 casas decimais,
  dia de vencimento entre 1 e 31, data de início válida, status Ativa ou
  Pausada); nenhuma regra adicional ou mais permissiva MUST ser aplicada só
  por se tratar de uma edição.
- **FR-003**: Editar nome, categoria, valor previsto mensal ou dia de
  vencimento MUST NOT alterar nenhum dado já gravado em nenhuma ocorrência
  existente (passada, da competência atual, ou já paga) — apenas ocorrências
  geradas após a edição MUST refletir os novos valores.
- **FR-004**: Quando o status é alterado de Ativa para Pausada, o sistema
  MUST impedir a geração de novas ocorrências a partir desse momento, e
  MUST NOT alterar ou remover nenhuma ocorrência já existente.
- **FR-005**: Quando o status é alterado de Pausada para Ativa, e a data de
  início da despesa já começou (é igual ou anterior à competência atual) e
  não existe ainda ocorrência dela para a competência atual, o sistema MUST
  gerar uma ocorrência Pendente para a competência atual com o valor
  previsto mensal vigente após a edição.
- **FR-006**: Quando o status é alterado de Pausada para Ativa mas já existe
  uma ocorrência da despesa para a competência atual, o sistema MUST NOT
  criar nenhuma ocorrência adicional (não duplicidade).
- **FR-007**: Quando o status é alterado de Pausada para Ativa mas a data de
  início da despesa (já editada ou não) é posterior à competência atual, o
  sistema MUST mudar o status para Ativa sem gerar nenhuma ocorrência da
  competência atual.
- **FR-008**: Editar a data de início MUST NOT criar nem remover
  retroativamente nenhuma ocorrência para competências entre a data antiga e
  a nova.
- **FR-009**: O sistema MUST permitir editar uma despesa recorrente
  independentemente de ela já possuir ocorrências marcadas como pagas, e a
  edição MUST NOT ler nem alterar valor pago, data de pagamento ou status de
  pagamento de nenhuma ocorrência.
- **FR-010**: Solicitar uma edição sem alterar o valor de nenhum campo MUST
  ser uma operação válida e bem-sucedida, e MUST NOT criar, alterar ou
  remover nenhuma ocorrência.
- **FR-011**: O sistema MUST expor uma forma de consultar uma despesa
  recorrente pelo seu identificador, devolvendo todos os campos editáveis
  (FR-001) com os valores atuais, para pré-carregar uma edição.
- **FR-012**: Consultar um identificador que não corresponde a nenhuma
  despesa recorrente cadastrada MUST ser tratado como "não encontrado",
  nunca como uma resposta de sucesso com dados vazios.
- **FR-013**: A consulta por identificador MUST NOT precisar devolver a
  lista de ocorrências da despesa recorrente.
- **FR-014**: O sistema MUST expor uma forma de solicitar a edição de uma
  despesa recorrente, aceitando os mesmos campos de FR-001 e aplicando as
  mesmas validações de FR-002, sem reimplementar nenhuma regra de negócio já
  estabelecida pelo cadastro.
- **FR-015**: Solicitar a edição de um identificador que não corresponde a
  nenhuma despesa recorrente cadastrada MUST ser tratado como "não
  encontrado", nunca criando uma nova despesa nem retornando sucesso.
- **FR-016**: Uma solicitação de edição que viole qualquer regra de FR-002
  MUST ser recusada por completo (nenhum campo salvo parcialmente),
  identificando cada campo inválido, no mesmo formato de erro já usado pelo
  cadastro.
- **FR-017**: Uma edição bem-sucedida MUST devolver os dados já atualizados
  da despesa recorrente (os mesmos campos de FR-001), para confirmar
  exatamente o que foi salvo.
- **FR-018**: Quando a edição envolve trocar o status de Pausada para Ativa,
  a avaliação e eventual geração da ocorrência da competência atual
  (FR-005–FR-007) MUST acontecer como parte da mesma operação de salvar a
  edição, sem exigir uma segunda solicitação.
- **FR-019**: Toda resposta das duas operações desta feature (consulta e
  edição) — sucesso ou erro — MUST usar o mesmo envelope de resposta padrão
  já estabelecido pela API de cadastro existente.
- **FR-020**: Uma solicitação de edição com um campo obrigatório ausente ou
  malformado MUST ser recusada no mesmo formato de erro usado para violação
  de regra de negócio (FR-016), com mensagens em português do Brasil.

### Key Entities *(include if feature involves data)*

- **Despesa recorrente**: Entidade já cadastrada cujos campos editáveis são
  nome, categoria, valor previsto mensal, dia de vencimento, data de início,
  status (Ativa/Pausada) e observação; a frequência é fixa e não editável.
- **Ocorrência**: Registro gerado a partir de uma despesa recorrente para uma
  competência específica; seus dados (nome, categoria, valor, vencimento) são
  fixados no momento em que foi gerada e nunca são reescritos por uma edição
  posterior da despesa recorrente. Não faz parte do escopo de leitura ou
  escrita desta feature, exceto como efeito colateral da reativação
  (FR-005–FR-007).
- **Competência atual**: Mês/ano de referência corrente, usado para decidir
  se a reativação de uma despesa recorrente deve gerar uma nova ocorrência.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das edições com todos os campos válidos resultam em uma
  resposta de sucesso cujo conteúdo reflete exatamente os dados persistidos.
- **SC-002**: 100% das edições que violam uma regra de validação, ou que
  têm um campo obrigatório ausente/malformado, são recusadas por completo,
  identificando todos os campos inválidos, sem nenhum dado salvo
  parcialmente.
- **SC-003**: 100% das consultas ou edições para um identificador
  inexistente retornam "não encontrado", nunca sucesso.
- **SC-004**: 100% das ocorrências já geradas antes de uma edição continuam
  com os mesmos dados de nome, categoria, valor e vencimento depois da
  edição ser salva.
- **SC-005**: 100% das reativações (Pausada → Ativa) que atendem às
  condições de data e não duplicidade geram exatamente uma ocorrência da
  competência atual; 100% das que não atendem não geram nenhuma.
- **SC-006**: 100% das edições salvas sem nenhuma mudança de valor não
  criam, alteram ou removem nenhuma ocorrência.

## Assumptions

- Domain, Application e Infrastructure para o cadastro de despesa recorrente
  já existem e suas regras de validação por campo estão fechadas; esta
  feature reaproveita exatamente as mesmas regras, sem criar nem relaxar
  nenhuma validação nova.
- A infraestrutura de persistência atual não requer nenhuma mudança
  estrutural para suportar a leitura e a atualização de uma despesa
  recorrente já existente.
- Exclusão de despesa recorrente é uma capacidade distinta e permanece fora
  do escopo desta feature.
- Qualquer alteração em ocorrências individuais (marcar como paga, desfazer
  pagamento) não é afetada por esta feature e continua exclusiva das ações já
  existentes para essa finalidade.
- Autenticação, autorização e política de CORS reais permanecem fora de
  escopo nesta etapa, pela mesma exceção de fase já vigente para os demais
  endpoints existentes.
- Nenhum mecanismo de concorrência otimista (ex.: comparação de versão) é
  necessário nesta etapa; duas edições simultâneas da mesma despesa aplicam
  "a última a salvar vence", sem aviso ao solicitante.
- Não há interação definida entre a edição de uma despesa recorrente e uma
  ocorrência dela que esteja, no mesmo instante, em edição de pagamento em
  outra tela — os dois fluxos são independentes.
- O ponto de entrada (tela/navegação) de onde a edição é acionada é definido
  por uma feature de frontend separada; esta especificação cobre apenas o
  comportamento do sistema ao processar uma consulta ou uma edição, não onde
  o usuário aciona essas operações.
