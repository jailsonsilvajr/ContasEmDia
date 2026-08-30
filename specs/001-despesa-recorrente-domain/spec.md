# Feature Specification: Domínio de Despesa Recorrente

**Feature Branch**: `001-despesa-recorrente-domain`

**Created**: 2026-08-29

**Status**: Draft

**Input**: User description: "@refinements/domain-despesa-recorrente.md" — Criar, no backend, o projeto Domain com Aggregates, Entities, Value Objects e Repositories (apenas interfaces), modelando o domínio necessário para suportar o cadastro de uma despesa recorrente e a geração automática da sua primeira ocorrência, com base na tela "Nova despesa recorrente".

## Clarifications

### Session 2026-08-29

- Q: Does the "Despesa Recorrente" aggregate need an actual status-transition capability (e.g., a `Pausar()`/`Reativar()` method) in this feature, given User Story 2's Scenario 3 describes a despesa whose status "changes to Pausada" after occurrences already exist? → A: No transition method now — despesas are only ever created with a fixed status (Ativa ou Pausada); the existing-occurrences-unaffected guarantee is an invariant for a future status-transition feature, not a capability built in this feature.
- Q: How should the domain determine "today" / the "competência do mês corrente" used to decide whether an occurrence gets generated at creation time — is the reference date passed in explicitly, or does the domain read the system clock internally? → A: Passed in explicitly as an input to the creation operation; the domain has no internal notion of system time.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar despesa recorrente ativa com ocorrência automática (Priority: P1)

Um usuário cadastra uma nova despesa recorrente (ex.: aluguel, internet) informando nome, categoria, valor previsto mensal, dia de vencimento, data de início e frequência. Como a despesa é criada com status Ativa e a competência do mês corrente já está dentro do período de vigência da despesa, o sistema gera automaticamente, no mesmo momento do cadastro, a ocorrência de cobrança referente ao mês corrente, pendente de pagamento.

**Why this priority**: É o fluxo central da tela "Nova despesa recorrente" e a razão de existir do domínio: sem cadastro válido e geração da primeira ocorrência, nenhuma outra funcionalidade do painel mensal (fora de escopo aqui) tem dados para operar.

**Independent Test**: Pode ser testado isoladamente criando uma despesa recorrente com dados válidos e status Ativa, com data de início igual ou anterior à competência do mês corrente, e verificando que (a) a despesa é persistida com os dados informados e (b) exatamente uma ocorrência pendente é criada para a competência do mês corrente, com valor previsto igual ao valor mensal da despesa.

**Acceptance Scenarios**:

1. **Given** dados válidos de nome, categoria, valor previsto mensal, dia de vencimento e data de início igual ou anterior ao mês corrente, **When** a despesa recorrente é cadastrada com status Ativa (padrão), **Then** a despesa é criada e uma ocorrência Pendente da competência do mês corrente é gerada automaticamente com o mesmo valor previsto da despesa.
2. **Given** uma despesa recorrente com dia de vencimento 31, **When** a ocorrência do mês corrente é gerada em uma competência cujo mês tem menos de 31 dias (ex.: fevereiro), **Then** a data de vencimento da ocorrência é ajustada para o último dia daquele mês.
3. **Given** uma despesa recorrente já cadastrada, **When** os dados da ocorrência gerada são consultados, **Then** nome, categoria e valor previsto exibidos na ocorrência refletem os dados da despesa recorrente no momento em que a ocorrência foi gerada.

---

### User Story 2 - Cadastrar despesa recorrente pausada ou com início futuro, sem gerar ocorrência (Priority: P2)

Um usuário cadastra uma despesa recorrente com status Pausada, ou com status Ativa mas com data de início em uma competência futura em relação ao mês corrente. Em ambos os casos, o sistema não deve gerar nenhuma ocorrência automaticamente no momento do cadastro, pois a despesa ainda não deve cobrar no mês corrente.

**Why this priority**: Evita a criação indevida de cobranças (ocorrências) para despesas que o usuário explicitamente não quer ativas ainda, ou que ainda não começaram a valer — um erro aqui gera dados financeiros incorretos no painel do usuário.

**Independent Test**: Pode ser testado isoladamente cadastrando (a) uma despesa com status Pausada e dados válidos, e (b) uma despesa Ativa com data de início em uma competência posterior à do mês corrente, e verificando em ambos os casos que nenhuma ocorrência é criada.

**Acceptance Scenarios**:

1. **Given** dados válidos de uma despesa recorrente, **When** ela é cadastrada com status Pausada, **Then** a despesa é criada com esse status e nenhuma ocorrência é gerada automaticamente.
2. **Given** dados válidos de uma despesa recorrente com status Ativa e data de início em uma competência futura (posterior ao mês corrente), **When** a despesa é cadastrada, **Then** a despesa é criada e nenhuma ocorrência é gerada no momento do cadastro, pois a competência do mês corrente é anterior à data de início.

---

### User Story 3 - Recuperar despesas recorrentes para uso posterior (Priority: P3)

O sistema precisa localizar despesas recorrentes já cadastradas — por identificador único ou filtrando apenas as ativas — para suportar funcionalidades futuras, como a geração mensal de novas ocorrências e a exibição do painel de contas.

**Why this priority**: Não faz parte do fluxo de cadastro em si, mas é a capacidade mínima de persistência sem a qual nenhuma despesa recorrente cadastrada poderia ser reutilizada por outras features.

**Independent Test**: Pode ser testado isoladamente salvando uma despesa recorrente e em seguida recuperando-a por identificador, e também cadastrando despesas com status misto (Ativa/Pausada) e verificando que a busca por despesas ativas retorna apenas as despesas com status Ativa.

**Acceptance Scenarios**:

1. **Given** uma despesa recorrente cadastrada, **When** ela é buscada pelo seu identificador, **Then** os mesmos dados cadastrados (nome, categoria, valor previsto, dia de vencimento, data de início, frequência, status, observação) são retornados.
2. **Given** um conjunto de despesas recorrentes com status misto (algumas Ativas, outras Pausadas), **When** as despesas ativas são localizadas, **Then** apenas as despesas com status Ativa são retornadas.

---

### Edge Cases

- Dia de vencimento configurado como 31 em uma competência cujo mês tem menos de 31 dias (ex.: fevereiro, abril, junho, setembro, novembro): a data de vencimento da ocorrência daquela competência cai no último dia do mês.
- Despesa cadastrada como Ativa com data de início em competência futura: nenhuma ocorrência é gerada no cadastro; a geração da primeira ocorrência fica para quando a competência do mês corrente alcançar a competência de início (responsabilidade de feature futura de geração mensal).
- Tentativa de existir mais de uma ocorrência para a mesma despesa recorrente na mesma competência (mês/ano): não é permitida.
- Nome da despesa vazio ou composto somente por espaços em branco: rejeitado no cadastro.
- Categoria informada fora do conjunto suportado (Moradia, Serviços, Transporte, Assinaturas, Outra): rejeitada no cadastro.
- Valor previsto mensal igual a zero, negativo, ou com mais de duas casas decimais: rejeitado no cadastro.
- Dia de vencimento fora da faixa 1–31: rejeitado no cadastro.
- Frequência diferente de "mensal" informada: rejeitada no cadastro, pois nenhuma outra frequência é suportada nesta etapa.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir criar uma despesa recorrente com os seguintes dados: nome, categoria, valor previsto mensal, dia de vencimento, data de início, frequência e observação opcional.
- **FR-002**: O sistema MUST exigir que o nome da despesa seja não vazio e não composto apenas por espaços em branco, rejeitando o cadastro caso contrário.
- **FR-003**: O sistema MUST exigir que a categoria pertença ao conjunto fechado de categorias suportadas (Moradia, Serviços, Transporte, Assinaturas, Outra), rejeitando qualquer outro valor.
- **FR-004**: O sistema MUST exigir que o valor previsto mensal seja uma quantia em reais (BRL) maior que zero, com no máximo duas casas decimais, rejeitando o cadastro caso contrário.
- **FR-005**: O sistema MUST exigir que o dia de vencimento seja um valor entre 1 e 31, rejeitando o cadastro caso contrário.
- **FR-006**: Quando a competência (mês/ano) de uma ocorrência tiver menos dias no mês do que o dia de vencimento configurado, o sistema MUST usar o último dia daquele mês como data de vencimento da ocorrência correspondente.
- **FR-007**: O sistema MUST exigir uma data de início válida para a despesa, que determina a partir de qual competência (mês/ano) a despesa passa a gerar ocorrências.
- **FR-008**: O sistema MUST permitir registrar a frequência da despesa, aceitando apenas o valor "mensal" nesta etapa e rejeitando qualquer outro valor de frequência.
- **FR-009**: O sistema MUST criar toda despesa recorrente com status Ativa ou Pausada, usando Ativa como padrão quando o status não for explicitamente alterado no cadastro.
- **FR-010**: O sistema MUST aceitar uma observação em texto livre, opcional, sem validação de formato ou obrigatoriedade.
- **FR-011**: Quando uma despesa recorrente for cadastrada com status Ativa e a competência do mês corrente for igual ou posterior à competência da data de início, o sistema MUST gerar automaticamente, no momento do cadastro, uma ocorrência da competência do mês corrente, com status Pendente e valor previsto igual ao valor previsto mensal da despesa.
- **FR-012**: Quando uma despesa recorrente for cadastrada com status Pausada, o sistema MUST NOT gerar nenhuma ocorrência automaticamente no momento do cadastro.
- **FR-013**: Quando uma despesa recorrente for cadastrada com status Ativa mas a competência do mês corrente for anterior à competência da data de início, o sistema MUST NOT gerar nenhuma ocorrência automaticamente no momento do cadastro.
- **FR-014**: O sistema MUST garantir que toda ocorrência pertença a exatamente uma despesa recorrente e não possa existir de forma independente dela.
- **FR-015**: Os dados de exibição de uma ocorrência (nome, categoria, valor previsto) MUST refletir os dados da despesa recorrente no momento em que a ocorrência foi gerada.
- **FR-016**: O sistema MUST impedir que uma mesma despesa recorrente tenha mais de uma ocorrência para a mesma competência (mês/ano).
- **FR-017**: O sistema MUST ser capaz de armazenar uma nova despesa recorrente, recuperá-la por identificador, e localizar o conjunto de despesas recorrentes com status Ativa.
- **FR-018**: Uma despesa recorrente com status Pausada MUST NOT gerar novas ocorrências enquanto permanecer pausada. Este refinamento não implementa nenhuma operação de transição de status (ex.: Pausar/Reativar uma despesa já existente); toda despesa recorrente é criada com o status definido no momento do cadastro (Ativa ou Pausada) e esse status não muda dentro do escopo desta feature.
- **FR-019**: Toda ocorrência gerada automaticamente no cadastro MUST nascer com status Pendente, nunca já como paga.
- **FR-020**: O sistema MUST tratar o dia de vencimento como um atributo da despesa recorrente (não da ocorrência individual); a data de vencimento de cada ocorrência MUST ser derivada do dia de vencimento da despesa combinado com a competência daquela ocorrência, aplicando a regra do FR-006 quando necessário.
- **FR-021**: A "competência do mês corrente" usada para decidir a geração da ocorrência automática (FR-011, FR-013) MUST ser recebida como um dado de entrada explícito da operação de cadastro (fornecido por quem chama o domínio); o domínio MUST NOT ler a data/hora do sistema internamente.

### Key Entities *(include if feature involves data)*

- **Despesa Recorrente**: Representa uma conta que se repete mensalmente. Concentra nome, categoria, valor previsto mensal, dia de vencimento, data de início, frequência, status (Ativa/Pausada) e observação opcional. É a raiz de consistência: toda criação/alteração de dados da despesa e geração de novas ocorrências passam por ela.
- **Ocorrência**: Representa a instância de cobrança de uma despesa recorrente em uma competência (mês/ano) específica. Pertence exclusivamente a uma Despesa Recorrente, possui status próprio (Pendente/Paga) e carrega os dados de exibição (nome, categoria, valor previsto, data de vencimento) capturados no momento em que foi gerada.
- **Valor Monetário**: Quantia em reais (BRL), não negativa, com precisão de centavos, usada tanto no valor previsto mensal da despesa quanto no valor previsto de cada ocorrência.
- **Categoria**: Classificação da despesa, restrita a um conjunto fechado (Moradia, Serviços, Transporte, Assinaturas, Outra).
- **Dia de Vencimento**: Dia do mês (1–31) em que a despesa vence, com a regra de compatibilidade para meses mais curtos descrita no FR-006.
- **Competência**: Referência de mês/ano à qual uma ocorrência pertence.
- **Status da Despesa Recorrente**: Ativa ou Pausada.
- **Status da Ocorrência**: Pendente ou Paga.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das despesas recorrentes cadastradas com dados válidos e status Ativa (com competência do mês corrente dentro do período de vigência) resultam em exatamente uma ocorrência pendente do mês corrente criada no mesmo momento do cadastro.
- **SC-002**: 100% das tentativas de cadastro com nome vazio, categoria inválida, valor previsto inválido, dia de vencimento inválido ou frequência não suportada são rejeitadas antes de qualquer persistência.
- **SC-003**: 0% das despesas recorrentes cadastradas como Pausada, ou com data de início futura, geram ocorrência automática no momento do cadastro.
- **SC-004**: 0% de duplicidade de ocorrências para a mesma despesa recorrente na mesma competência, em qualquer cenário testado.
- **SC-005**: 100% das despesas recorrentes com dia de vencimento 31 cadastradas produzem, em competências de mês mais curto, uma ocorrência com data de vencimento no último dia daquele mês.
- **SC-006**: Despesas recorrentes cadastradas podem ser recuperadas por identificador e o conjunto de despesas ativas pode ser localizado, sem perda ou alteração dos dados originalmente cadastrados.

## Assumptions

- "Ocorrência" é modelada como uma entidade interna ao aggregate "Despesa Recorrente" (não como um aggregate independente), pois seu ciclo de vida depende totalmente da despesa que a originou; a Ocorrência é sempre acessada através do aggregate ao qual pertence.
- Não há restrição de unicidade de nome entre despesas recorrentes distintas nesta etapa — a tela de origem não define essa regra.
- Quando o dia de vencimento configurado não existir em uma competência (ex.: dia 31 em fevereiro), a data de vencimento da ocorrência daquela competência é ajustada para o último dia do mês.
- Quando a data de início de uma despesa Ativa estiver em uma competência futura em relação ao mês corrente, nenhuma ocorrência é gerada no cadastro; a geração da primeira ocorrência dessa despesa fica a cargo de uma feature futura de geração mensal recorrente (fora de escopo aqui).
- Este refinamento cobre exclusivamente o modelo de domínio (Aggregates, Entities, Value Objects, interfaces de Repository) necessário para o cadastro de despesa recorrente e a geração da ocorrência inicial; não inclui edição/exclusão de despesas, pagamento ou desfazimento de ocorrências, cálculo dos status derivados "Vencida"/"Vence em breve", suporte a frequências diferentes de mensal, nem qualquer camada de aplicação, API ou persistência concreta.
- Nenhuma operação de transição de status (ex.: Pausar/Reativar) é implementada nesta feature; o status da despesa é fixado no momento do cadastro. Quando uma feature futura implementar essa transição, ela MUST preservar a invariante de que ocorrências já geradas permanecem inalteradas pela mudança de status.
