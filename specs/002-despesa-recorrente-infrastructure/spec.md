# Feature Specification: Infrastructure de Despesa Recorrente (Persistência)

**Feature Branch**: `002-despesa-recorrente-infrastructure`

**Created**: 2026-08-31

**Status**: Draft

**Input**: User description: "@refinements/infrastructure-despesa-recorrente.md" — Criar, no backend, o projeto Infrastructure, responsável por implementar a persistência (Entity Framework Core 10) do Domain já existente (aggregate `RecurringExpense`, entidade `Occurrence`), suportando salvar uma nova despesa recorrente (com a ocorrência do mês corrente gerada automaticamente) e recuperar despesas recorrentes persistidas.

## Clarifications

### Session 2026-08-31

- Q: O Domain (`RecurringExpense`, `Occurrence`) hoje não possui o construtor privado de reconstrução exigido pela constituição (Princípio VI) para uso do EF Core — sem ele, FR-009 não pode ser cumprido. Adicionar esse construtor ao Domain deve fazer parte do escopo desta feature? → A: Sim — dentro do escopo: esta feature adiciona o construtor privado de reconstrução mínimo necessário a `RecurringExpense`, `Occurrence` e Value Objects que precisarem, exclusivamente para satisfazer a regra constitucional já existente, sem alterar nenhuma regra de negócio.
- Q: O Acceptance Scenario 3 (User Story 1) exige comprovar que uma falha na escrita não deixa nem a despesa nem a ocorrência persistidas parcialmente — o que exige transações e constraints reais, que o provedor InMemory do EF Core não reproduz fielmente. Contra o que os testes de persistência devem rodar para validar isso? → A: SQL Server real via Testcontainers — os testes sobem um container SQL Server descartável por execução, exercitando o provedor real (FKs, transações), com paridade total com produção e sem depender de uma instância compartilhada/manual.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Salvar despesa recorrente cadastrada, com a ocorrência do mês corrente (Priority: P1)

Quando alguém cadastra uma nova despesa recorrente pela tela "Nova despesa recorrente" e aciona "Salvar despesa", o sistema precisa gravar de forma definitiva, em uma única operação, a despesa recorrente e — quando o Domain tiver gerado uma no momento da criação — a ocorrência do mês corrente associada a ela, para que os dados não se percam e fiquem disponíveis para consultas futuras.

**Why this priority**: É a única operação efetivamente disparada pela tela de origem hoje; sem persistência funcionando, nenhuma despesa recorrente cadastrada sobrevive além da sessão em memória, tornando o restante do sistema inutilizável.

**Independent Test**: Pode ser testado isoladamente criando, via Domain, uma despesa recorrente Ativa com data de início dentro da competência do mês corrente (gerando portanto uma ocorrência), salvando-a através da Infrastructure e verificando que tanto a despesa quanto a ocorrência aparecem persistidas de forma consistente (ou nenhuma das duas, em caso de falha).

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente Ativa recém-criada pelo Domain com uma ocorrência do mês corrente já gerada, **When** a operação de salvar é executada, **Then** tanto a despesa quanto a ocorrência são persistidas com sucesso em uma única confirmação de escrita.
2. **Given** uma despesa recorrente Pausada (ou com início futuro) recém-criada pelo Domain sem nenhuma ocorrência gerada, **When** a operação de salvar é executada, **Then** apenas a despesa é persistida, sem nenhuma ocorrência associada.
3. **Given** uma falha durante a operação de salvar (ex.: violação de restrição no banco), **When** a escrita é confirmada, **Then** nem a despesa nem a ocorrência ficam persistidas parcialmente — a operação falha por completo.

---

### User Story 2 - Recuperar despesa recorrente por identificador (Priority: P2)

O sistema precisa localizar uma despesa recorrente já persistida a partir do seu identificador único, retornando-a junto com todas as suas ocorrências, para suportar funcionalidades futuras (ex.: exibir detalhes de uma despesa, editar, consultar histórico de ocorrências).

**Why this priority**: Já exigida pelo contrato de repositório definido no Domain; não é acionada pela tela atual, mas precisa estar implementada por completo para que o contrato não fique com métodos pendentes.

**Independent Test**: Pode ser testado isoladamente salvando uma despesa recorrente com uma ocorrência e, em seguida, recuperando-a pelo identificador, verificando que os dados da despesa e todas as suas ocorrências retornam intactos; e verificando que um identificador inexistente retorna ausência de resultado.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente persistida com ocorrências associadas, **When** ela é buscada pelo seu identificador, **Then** a despesa é retornada junto de todas as suas ocorrências já persistidas.
2. **Given** um identificador que não corresponde a nenhuma despesa recorrente persistida, **When** a busca por esse identificador é executada, **Then** o sistema indica ausência de resultado, sem lançar erro.

---

### User Story 3 - Listar despesas recorrentes ativas (Priority: P3)

O sistema precisa localizar o conjunto de todas as despesas recorrentes com status Ativa, cada uma com suas ocorrências, para suportar funcionalidades futuras como a geração mensal automática de novas ocorrências.

**Why this priority**: Também parte do contrato já definido no Domain, reservada para uso por uma feature futura; não é acionada pela tela atual, mas fecha o contrato de repositório por completo.

**Independent Test**: Pode ser testado isoladamente salvando despesas recorrentes com status misto (Ativa/Pausada) e verificando que a listagem retorna exclusivamente as despesas com status Ativa, cada uma com suas ocorrências.

**Acceptance Scenarios**:

1. **Given** um conjunto de despesas recorrentes persistidas com status misto, **When** a listagem de despesas ativas é executada, **Then** apenas as despesas com status Ativa são retornadas, cada uma junto de suas ocorrências já persistidas.
2. **Given** nenhuma despesa recorrente ativa persistida, **When** a listagem de despesas ativas é executada, **Then** o sistema retorna uma lista vazia, sem lançar erro.

---

### Edge Cases

- Despesa recorrente sem nenhuma ocorrência gerada no momento do cadastro (Pausada, ou Ativa com início futuro): a operação de salvar persiste a despesa normalmente, sem exigir nenhuma ocorrência associada.
- Falha na confirmação da escrita (ex.: violação de restrição do banco, indisponibilidade momentânea): nem a despesa nem a ocorrência do mês corrente ficam persistidas — a operação inteira é revertida.
- Busca por identificador inexistente, ou listagem de ativas quando não há nenhuma despesa ativa persistida: retorno de ausência/lista vazia, nunca uma exceção não tratada.
- Reconstrução de uma despesa recorrente e de suas ocorrências a partir dos dados persistidos: deve reproduzir fielmente os mesmos dados e estado validados originalmente pelo Domain, sem reaplicar nem contornar as validações de criação.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST persistir uma despesa recorrente e, quando existente, a ocorrência do mês corrente gerada junto com ela, em uma única operação atômica de confirmação de escrita.
- **FR-002**: O sistema MUST garantir que, em caso de falha na operação de salvar, nenhuma parte dos dados (despesa ou ocorrência) fique persistida de forma parcial.
- **FR-003**: O sistema MUST persistir uma despesa recorrente sem nenhuma ocorrência associada quando o Domain não tiver gerado nenhuma no momento da criação.
- **FR-004**: O sistema MUST permitir recuperar uma despesa recorrente persistida a partir do seu identificador único, retornando-a junto de todas as suas ocorrências já persistidas.
- **FR-005**: O sistema MUST indicar ausência de resultado (sem lançar erro) quando o identificador buscado não corresponder a nenhuma despesa recorrente persistida.
- **FR-006**: O sistema MUST permitir listar todas as despesas recorrentes persistidas cujo status seja Ativa, cada uma retornada junto de suas ocorrências já persistidas.
- **FR-007**: O sistema MUST retornar uma lista vazia (sem lançar erro) quando não existir nenhuma despesa recorrente com status Ativa persistida.
- **FR-008**: O sistema MUST garantir, na persistência, que toda ocorrência esteja sempre associada a exatamente uma despesa recorrente, nunca existindo de forma isolada.
- **FR-009**: O sistema MUST reconstruir despesas recorrentes e ocorrências a partir dos dados persistidos preservando fielmente os mesmos dados e estado originalmente validados pelo Domain, sem reaplicar nem contornar as validações executadas na criação. Quando o Domain ainda não expuser o construtor privado de reconstrução exigido pela constituição para esse fim, esta feature MUST adicioná-lo a `RecurringExpense`, `Occurrence` e Value Objects necessários, como compliance estrutural mínima — sem introduzir nem alterar nenhuma regra de negócio existente.
- **FR-010**: O sistema MUST NOT implementar, na camada de persistência, nenhuma regra de negócio ou decisão já resolvida pelo Domain (ex.: decidir se uma ocorrência deve ou não ser gerada).
- **FR-011**: O sistema MUST obter a string de conexão e demais configurações sensíveis de acesso ao banco de dados a partir de configuração externa, nunca de valores fixos no código-fonte.
- **FR-012**: O acesso às operações de persistência de despesa recorrente pela camada de aplicação MUST ocorrer exclusivamente através de um ponto único de acesso aos repositórios, que disponibiliza o repositório de despesas recorrentes.
- **FR-013**: O sistema MUST utilizar, para o cadastro inicial do schema de persistência de despesa recorrente e ocorrência, uma migration inicial, sem alterar, renomear ou remover qualquer migration de outra feature já existente.

### Key Entities *(include if feature involves data)*

- **Despesa Recorrente (persistida)**: Representação em banco de dados do aggregate de domínio já existente — mesmos dados (nome, categoria, valor previsto mensal, dia de vencimento, data de início, frequência, status, observação) mais a coleção de suas ocorrências, reconstruída a partir do banco sem contornar as validações do Domain.
- **Ocorrência (persistida)**: Representação em banco de dados da entidade de domínio já existente, sempre associada a exatamente uma despesa recorrente dona, nunca existindo de forma independente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das operações de salvar uma despesa recorrente com ocorrência do mês corrente gerada resultam em ambas — despesa e ocorrência — persistidas de forma consistente, ou em nenhuma das duas persistida em caso de falha.
- **SC-002**: 100% das despesas recorrentes recuperadas por identificador, ou listadas como ativas, retornam com os mesmos dados e ocorrências originalmente persistidos, sem perda ou alteração.
- **SC-003**: 0% das buscas por identificador inexistente ou listagens sem despesas ativas resultam em erro não tratado — sempre retornam ausência de resultado ou lista vazia.
- **SC-004**: 0% de ocorrências persistidas sem uma despesa recorrente dona associada, em qualquer cenário testado.
- **SC-005**: 100% das configurações de acesso ao banco de dados (string de conexão e afins) são carregadas de fora do código-fonte, sem nenhum segredo versionado no repositório.

## Assumptions

- O Domain (aggregate `RecurringExpense`, entidade `Occurrence`, Value Objects e a interface `IRecurringExpenseRepository`) já existe e suas regras de negócio estão fechadas; este trabalho não altera nem adiciona regra de negócio alguma. Excepcionalmente, esta feature PODE adicionar ao Domain o construtor privado de reconstrução (exigido pela constituição, Princípio VI, e hoje ausente em `RecurringExpense`/`Occurrence`) estritamente para viabilizar FR-009 — essa adição é compliance estrutural, não regra de negócio nova.
- O provedor de banco de dados é SQL Server, e a versão de Entity Framework Core utilizada é a 10, conforme já decidido no refinamento de origem.
- A relação entre despesa recorrente e suas ocorrências é persistida como relação de posse (1:N) com chave estrangeira explícita, não como owned entity — decisão já confirmada no refinamento de origem.
- Este trabalho cobre exclusivamente a camada de persistência (Infrastructure) para o aggregate `RecurringExpense`; não inclui camada de aplicação/API (controllers, endpoints HTTP, DTOs) que exponha essas operações ao frontend, autenticação/autorização, nem qualquer outro aggregate ou repositório.
- Atualização (edição) e exclusão de despesas recorrentes, e qualquer operação sobre ocorrências (marcar como paga, desfazer pagamento), não são cobertas nesta etapa — o contrato de repositório do Domain ainda não define esses métodos.
- Nenhum mecanismo adicional de Unit of Work é introduzido; a confirmação de escrita do próprio mecanismo de persistência (`SaveChangesAsync` do `DbContext`) é o único ponto de confirmação, chamado diretamente por quem invoca o repositório.
- Os testes de persistência (incluindo a comprovação de rollback completo em caso de falha, Acceptance Scenario 3 da User Story 1) rodam contra uma instância real de SQL Server provisionada via Testcontainers, descartável por execução — não contra o provedor InMemory do EF Core nem outro banco relacional substituto.
