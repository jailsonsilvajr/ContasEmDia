# Feature Specification: Application de Despesa Recorrente (Cadastro)

**Feature Branch**: `003-despesa-recorrente-application`

**Created**: 2026-09-04

**Status**: Draft

**Input**: User description: "@refinements/backend/application-despesa-recorrente.md" — Criar, no backend, o projeto Application, contendo o Use Case que orquestra a única ação que a tela "Nova despesa recorrente" dispara contra o backend: cadastrar uma despesa recorrente. O Use Case recebe dados primitivos (equivalentes ao corpo do `POST /api/recurring-expenses` já documentado no contrato de API), converte-os para os Value Objects do Domain, determina a competência do mês corrente, cria o aggregate `RecurringExpense` pelo seu construtor público, persiste através do contrato de repositório já existente e devolve um resultado — pronto para uma futura camada de API traduzir em `201` ou `400` — sem nenhuma camada de transporte HTTP envolvida.

## Clarifications

### Session 2026-09-04

- Q: How should the Use Case obtain the recurring-expense repository — directly via the Domain's existing `IRecurringExpenseRepository`, or through a new `IRepositoryManager` interface extracted from the currently concrete-only `RepositoryManager` in Infrastructure? → A: New `IRepositoryManager` interface, declared in Domain and implemented by Infrastructure's existing `RepositoryManager`, created as part of this work.
- Q: Should revising Domain Value Object/aggregate error messages to PT-BR (currently English, e.g. "Money value must be greater than zero.") be done as part of this feature's implementation, given FR-006/SC-003 depend on it? → A: Yes, in scope of this feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar despesa recorrente com dados válidos (Priority: P1)

Quando os dados equivalentes ao corpo de "Nova despesa recorrente" chegam
corretos, o sistema precisa converter cada campo para o conceito de negócio
correspondente, determinar a competência do mês corrente, criar a despesa
recorrente através das regras já fechadas no Domain, persisti-la e devolver
os dados da despesa criada — e da ocorrência do mês corrente, quando o Domain
tiver decidido gerá-la — para que uma futura camada de API possa confirmar o
cadastro ao usuário.

**Why this priority**: É a única ação que a tela de origem dispara contra o
backend hoje; sem ela funcionando corretamente, nenhum cadastro é possível.

**Independent Test**: Pode ser testado isoladamente executando o Use Case com
dados equivalentes a uma despesa Ativa com início na competência corrente,
substituindo o repositório e o port de data atual por dublês de teste, e
verificando que o resultado devolvido reflete fielmente os dados criados pelo
Domain (incluindo a ocorrência gerada) e que a persistência foi acionada.

**Acceptance Scenarios**:

1. **Given** dados de entrada válidos para uma despesa Ativa com início dentro
   da competência do mês corrente, **When** o Use Case é executado, **Then**
   a despesa recorrente é criada, persistida, e o resultado de sucesso traz os
   dados da despesa e da ocorrência do mês corrente gerada.
2. **Given** dados de entrada válidos para uma despesa Pausada, ou Ativa com
   início em uma competência futura, **When** o Use Case é executado, **Then**
   a despesa recorrente é criada e persistida, e o resultado de sucesso traz a
   despesa com uma lista de ocorrências vazia.
3. **Given** dados de entrada válidos, **When** o Use Case monta o resultado
   de sucesso, **Then** todos os dados devolvidos vêm exclusivamente dos
   métodos de leitura já expostos pela despesa recorrente e pela ocorrência
   criadas, nunca de acesso direto a estado interno.

---

### User Story 2 - Rejeitar cadastro com dados inválidos, agregando todos os erros (Priority: P2)

Quando um ou mais campos dos dados de entrada violam uma regra de negócio já
fechada no Domain (ex.: nome vazio, categoria desconhecida, valor previsto
mensal zerado, data de início malformada), o sistema precisa reportar, em uma
única resposta, o erro de cada campo inválido — sem interromper a validação
no primeiro erro encontrado — usando a mesma mensagem em PT-BR já fornecida
pelo Domain, para que uma futura camada de API destaque todos os campos
inválidos de uma vez, como já ocorre na validação client-side da tela.

**Why this priority**: É o outro desfecho possível da única ação da tela;
sem agregação correta de erros, o usuário precisaria corrigir e reenviar um
campo por vez.

**Independent Test**: Pode ser testado isoladamente executando o Use Case com
dados de entrada contendo mais de um campo inválido simultaneamente (ex.:
nome vazio e categoria desconhecida) e verificando que o resultado de falha
traz um erro para cada campo inválido, cada um com a mensagem em PT-BR
fornecida pelo Domain, e que nenhuma despesa foi persistida.

**Acceptance Scenarios**:

1. **Given** dados de entrada com exatamente um campo inválido, **When** o Use
   Case é executado, **Then** o resultado de falha traz um único erro, com o
   nome do campo e a mensagem em PT-BR fornecida pelo Domain, e nada é
   persistido.
2. **Given** dados de entrada com mais de um campo inválido simultaneamente,
   **When** o Use Case é executado, **Then** o resultado de falha traz um
   erro para cada campo inválido em uma única lista, e nada é persistido.
3. **Given** um valor de categoria que não corresponde a nenhuma das cinco
   categorias suportadas, **When** o Use Case é executado, **Then** um erro do
   campo de categoria é reportado, mesmo sem tentar construir o Value Object
   de categoria.
4. **Given** um texto de data de início malformado ou correspondente a uma
   data inexistente no calendário, **When** o Use Case é executado, **Then**
   um erro do campo de data de início é reportado.

---

### User Story 3 - Distinguir falha de validação de falha inesperada (Priority: P3)

Quando ocorre uma falha que não é uma violação de regra de negócio (ex.:
indisponibilidade do banco de dados durante a persistência), o sistema
precisa deixar essa falha propagar como exceção, em vez de reportá-la como um
erro de campo, para que uma futura camada de API possa diferenciá-la de uma
requisição inválida e respondê-la de forma apropriada (ex.: `5xx` em vez de
`400`).

**Why this priority**: Não faz parte do fluxo principal de uso, mas é
necessário para que uma futura camada de API consiga distinguir corretamente
os dois tipos de falha; sem essa distinção, falhas de sistema seriam
mascaradas como erro do usuário.

**Independent Test**: Pode ser testado isoladamente substituindo o
repositório por um dublê que lança uma exceção ao persistir, executando o Use
Case com dados de entrada válidos, e verificando que a exceção propaga sem
ser convertida em um resultado de falha de validação.

**Acceptance Scenarios**:

1. **Given** dados de entrada válidos, **When** a persistência falha por um
   motivo que não é uma violação de regra de negócio, **Then** a falha
   propaga como exceção, e o Use Case não devolve um resultado de falha de
   validação para ela.

---

### Edge Cases

- Texto de data de início malformado, ou correspondente a uma data
  inexistente no calendário (ex.: 31 de fevereiro): reportado como erro do
  campo de data de início, não como exceção não tratada.
- Valor de categoria fora das cinco categorias suportadas: reportado como
  erro do campo de categoria, mesmo antes de tentar construir o Value Object
  correspondente.
- Mais de um campo inválido ao mesmo tempo: todos os erros são agregados em
  uma única resposta, sem interromper a validação no primeiro encontrado.
- Um único campo que viola mais de uma regra ao mesmo tempo: apenas a
  mensagem da primeira regra violada, checada pelo próprio Value Object do
  Domain, fica disponível — comportamento herdado do Domain, não uma decisão
  desta camada.
- Despesa criada como Pausada, ou Ativa com início em competência futura à
  competência corrente: resultado de sucesso com lista de ocorrências vazia,
  espelhando fielmente a decisão já tomada pelo Domain.
- Falha de persistência não relacionada a validação de negócio (ex.: banco de
  dados indisponível): propaga como exceção, nunca como erro de campo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST prover um único Use Case responsável por
  cadastrar uma despesa recorrente a partir de dados primitivos equivalentes
  ao corpo já documentado no contrato de API de cadastro.
- **FR-002**: O sistema MUST converter cada campo de entrada (nome,
  categoria, valor previsto mensal, dia de vencimento, data de início,
  frequência, status e observação) para o respectivo Value Object do Domain
  antes de criar a despesa recorrente, sem reimplementar nenhuma das regras
  de validação já expressas nesses Value Objects.
- **FR-003**: O sistema MUST interpretar a data de início recebida como texto
  (`yyyy-MM-dd`) e reportar como erro do campo de data de início tanto um
  texto malformado quanto uma data inexistente no calendário.
- **FR-004**: O sistema MUST reportar como erro do campo de categoria
  qualquer valor de categoria que não corresponda a nenhuma das cinco
  categorias suportadas, mesmo antes de tentar construir o Value Object de
  categoria correspondente.
- **FR-005**: Quando mais de um campo de entrada for inválido, o sistema
  MUST tentar converter todos os campos — sem interromper no primeiro erro
  encontrado — e retornar todos os erros de campo já identificados em uma
  única lista.
- **FR-006**: Cada erro de campo retornado MUST conter a mensagem de
  validação em PT-BR voltada ao usuário final já fornecida pelo Value Object
  (ou aggregate) do Domain correspondente, repassada tal como recebida, sem
  reescrevê-la, traduzi-la ou duplicá-la.
- **FR-007**: O sistema MUST obter a data atual através de uma abstração
  dedicada (independente do relógio do runtime) e derivar dela a competência
  (mês/ano) corrente usada na criação da despesa recorrente.
- **FR-008**: O sistema MUST criar a despesa recorrente exclusivamente
  através do construtor público do aggregate, nunca através do construtor de
  reconstrução reservado à persistência nem por qualquer meio que contorne as
  validações do construtor público.
- **FR-009**: Após a criação bem-sucedida, o sistema MUST persistir a despesa
  recorrente através de uma interface de acesso a repositórios declarada no
  Domain, nunca através de uma implementação concreta de Infrastructure
  acessada diretamente.
- **FR-010**: Em caso de sucesso, o sistema MUST montar o resultado usando
  exclusivamente os dados expostos pelos métodos de leitura já existentes na
  despesa recorrente e na ocorrência criadas, nunca por acesso direto a
  estado interno.
- **FR-011**: Quando a despesa recorrente for criada como Pausada, ou quando
  a competência corrente for anterior à competência da data de início, o
  resultado de sucesso MUST trazer a lista de ocorrências vazia, espelhando
  fielmente o que o Domain decidiu, sem o Use Case reavaliar essa decisão.
- **FR-012**: Falhas que não sejam violação de regra de negócio (ex.:
  indisponibilidade do banco de dados) MUST NOT ser convertidas em erro de
  campo — MUST propagar como exceção.
- **FR-013**: O Use Case MUST NOT depender de nenhum detalhe de transporte
  HTTP (tipos de framework web, atributos de serialização), operando apenas
  com tipos primitivos e tipos da própria camada de aplicação/Domain.
- **FR-014**: O Use Case MUST NOT implementar nenhuma regra de negócio já
  coberta por uma regra do Domain — sua responsabilidade MUST se limitar a
  orquestração e tradução de erros.
- **FR-015**: O sistema MUST declarar, no Domain, uma nova interface para o
  ponto único de acesso a repositórios (hoje exposto apenas pela classe
  concreta `RepositoryManager` na Infrastructure), implementada pela
  Infrastructure, de modo que o Use Case dependa exclusivamente dessa
  interface — revisando a decisão anterior que expunha esse ponto único
  apenas como classe concreta.
- **FR-016**: O sistema MUST revisar, como parte deste trabalho, as
  mensagens de erro por campo hoje em inglês nos Value Objects e no
  aggregate do Domain, substituindo-as por mensagens em PT-BR voltadas ao
  usuário final, das quais o Use Case apenas as repassa (FR-006).

### Key Entities *(include if feature involves data)*

- **Entrada do cadastro**: Espelha 1:1 os campos primitivos do corpo de
  cadastro já documentado (nome, categoria, valor previsto mensal, dia de
  vencimento, data de início como texto, frequência, status, observação
  opcional).
- **Resultado de sucesso**: Dados da despesa recorrente criada e da
  ocorrência gerada (se houver), na mesma forma já documentada para o
  cadastro bem-sucedido.
- **Resultado de falha de validação**: Lista de erros de campo, cada um com
  o nome do campo e a mensagem em PT-BR fornecida pelo Domain.
- **Abstração de data atual**: Ponto de obtenção da data corrente usado para
  derivar a competência do mês corrente exigida pela criação da despesa
  recorrente.
- **Interface de acesso a repositórios**: Novo contrato, declarado no
  Domain e implementado pela Infrastructure, que substitui o acesso direto
  à classe concreta `RepositoryManager`, usado pelo Use Case para obter o
  repositório de despesas recorrentes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cadastros com dados válidos resultam em uma despesa
  recorrente persistida cujo resultado devolvido reflete exatamente os dados
  produzidos pelo Domain, incluindo a ocorrência do mês corrente quando
  gerada.
- **SC-002**: 100% dos cadastros com um ou mais campos inválidos retornam o
  erro de cada campo inválido em uma única resposta, sem nenhum erro
  descartado.
- **SC-003**: 100% das mensagens de erro de campo retornadas correspondem
  exatamente à mensagem em PT-BR fornecida pelo Domain para a regra violada,
  sem texto reescrito ou duplicado.
- **SC-004**: 0% das despesas recorrentes com dados inválidos ficam
  persistidas, mesmo parcialmente.
- **SC-005**: 0% das falhas inesperadas (não relacionadas a validação de
  negócio) são reportadas como erro de campo.
- **SC-006**: 100% dos cadastros de despesas Pausadas, ou Ativas com início
  em competência futura, retornam com lista de ocorrências vazia no
  resultado.

## Assumptions

- O Domain (aggregate `RecurringExpense`, entidade `Occurrence`, Value
  Objects e a interface `IRecurringExpenseRepository`) e a Infrastructure
  (persistência via EF Core) já existem e suas regras de negócio estão
  fechadas; este trabalho não altera nem adiciona regra de negócio alguma.
  As duas exceções estruturais a isso são a nova interface de acesso
  a repositórios (FR-015) e a revisão textual das mensagens de erro para
  PT-BR (FR-016) — nenhuma das duas introduz ou altera uma regra de negócio.
- O acesso ao repositório de despesas recorrentes passa a ocorrer através de
  uma nova interface do ponto único de acesso a repositórios, criada como
  parte deste trabalho, declarada no Domain e implementada na
  Infrastructure — revisando a decisão anterior que expunha esse ponto único
  apenas como classe concreta.
- As mensagens de erro em PT-BR por campo, hoje em inglês nos Value Objects e
  no aggregate do Domain, MUST ser revisadas para PT-BR como parte deste
  trabalho, já que o Use Case apenas repassa essa mensagem tal como recebida
  (FR-006), sem manter texto duplicado na Application.
- A abstração de data atual (necessária apenas ao Use Case, não ao aggregate)
  é declarada nesta camada e implementada pela Infrastructure; sua
  implementação concreta não é coberta por este trabalho.
- Nenhuma camada de transporte HTTP (controllers, rotas, model binding,
  serialização JSON, códigos de status, autenticação/autorização) é coberta
  por este trabalho — pertence a uma futura camada de API/Apresentação.
  Consequentemente, erros de desserialização de tipos primitivos malformados
  (ex.: um valor não numérico onde um número é esperado) não são cobertos: a
  entrada do Use Case assume que os tipos primitivos já chegaram corretamente
  tipados.
- Nenhum outro Use Case além do cadastro é coberto — a tela de origem não
  dispara nenhuma outra ação contra o backend nesta etapa.
