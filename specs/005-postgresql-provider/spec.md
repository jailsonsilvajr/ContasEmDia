# Feature Specification: Provider de Banco de Dados PostgreSQL

**Feature Branch**: `005-postgresql-provider`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "Atualizar o provider de banco de dados para PostgreSQL"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Aplicação funciona de ponta a ponta sobre PostgreSQL (Priority: P1)

Hoje o backend persiste todos os dados (despesas recorrentes e demais
entidades já implementadas) em SQL Server. Como responsável por evoluir a
infraestrutura do sistema, preciso que a aplicação passe a persistir e
recuperar esses mesmos dados usando PostgreSQL como banco relacional, sem
alterar nenhum comportamento observável pelas telas ou pela API já
existentes.

**Why this priority**: É o objetivo central da mudança — sem a aplicação
funcionando corretamente sobre PostgreSQL, nenhuma outra melhoria de
infraestrutura decorrente da troca (custo, portabilidade, licenciamento)
tem valor. Todas as demais histórias dependem desta troca já estar completa
e correta.

**Independent Test**: Pode ser testado subindo a aplicação apontando para
uma instância PostgreSQL, executando as migrações do schema, e confirmando
— via os endpoints de API já existentes (ex.: cadastro de despesa
recorrente) — que criar, consultar, atualizar e remover dados funciona
exatamente como funcionava sobre SQL Server.

**Acceptance Scenarios**:

1. **Given** uma instância PostgreSQL vazia e a aplicação configurada para
   usá-la, **When** as migrações do schema são aplicadas, **Then** todas as
   tabelas, colunas, tipos, chaves e restrições necessárias para as
   funcionalidades já existentes são criadas com sucesso.
2. **Given** a aplicação em execução sobre PostgreSQL, **When** uma
   requisição de cadastro de despesa recorrente é enviada ao endpoint de
   API já existente, **Then** o dado é persistido corretamente e pode ser
   recuperado com os mesmos valores e o mesmo comportamento observados
   anteriormente sobre SQL Server.
3. **Given** dados previamente persistidos via PostgreSQL, **When** eles são
   lidos, atualizados ou removidos através das operações já existentes,
   **Then** o resultado observado é idêntico ao comportamento anterior com
   SQL Server, sem perda ou corrupção de dados.

---

### User Story 2 - Ambiente de desenvolvimento local sem dependência de SQL Server (Priority: P2)

Como desenvolvedor do projeto, preciso conseguir configurar e rodar o
ambiente de desenvolvimento local usando PostgreSQL, sem precisar instalar
ou licenciar SQL Server, para reduzir o atrito e o custo de colocar o
projeto para rodar em uma máquina nova.

**Why this priority**: Reduz fricção e custo de onboarding, mas o valor só
se concretiza depois que a aplicação já funciona corretamente sobre
PostgreSQL (User Story 1).

**Independent Test**: Pode ser testado seguindo as instruções de setup do
projeto em uma máquina limpa e verificando que é possível levantar um banco
PostgreSQL local, aplicar as migrações e rodar a API sem qualquer
referência a SQL Server na configuração.

**Acceptance Scenarios**:

1. **Given** uma máquina de desenvolvimento sem SQL Server instalado,
   **When** o desenvolvedor segue as instruções de setup do projeto,
   **Then** ele consegue subir um banco PostgreSQL local, aplicar as
   migrações e rodar a aplicação com sucesso.
2. **Given** os arquivos de configuração do projeto (ex.: strings de
   conexão de exemplo), **When** o desenvolvedor os inspeciona, **Then** não
   há nenhuma referência a SQL Server como provider de banco de dados.

---

### User Story 3 - Testes automatizados validam o comportamento contra PostgreSQL real (Priority: P3)

Como responsável por manter a confiabilidade do sistema, preciso que os
testes automatizados que hoje validam a camada de persistência contra uma
instância real de SQL Server passem a validar contra uma instância real de
PostgreSQL, para que a suíte de testes reflita o banco de dados
efetivamente usado em produção.

**Why this priority**: Garante confiança contínua na troca de provider,
mas depende da aplicação e do schema já funcionarem sobre PostgreSQL (User
Story 1) para fazer sentido.

**Independent Test**: Pode ser testado rodando a suíte de testes de
infraestrutura/persistência e confirmando que ela sobe um container
PostgreSQL descartável, aplica as migrações nele e executa todos os casos
de teste existentes com sucesso, sem qualquer container ou dependência de
SQL Server.

**Acceptance Scenarios**:

1. **Given** a suíte de testes de persistência existente, **When** ela é
   executada, **Then** todos os casos de teste rodam contra uma instância
   PostgreSQL real (via container descartável) e passam com o mesmo
   resultado observado anteriormente contra SQL Server.
2. **Given** o código-fonte dos testes de persistência, **When** ele é
   inspecionado, **Then** não há nenhuma dependência de infraestrutura de
   teste específica de SQL Server.

---

### Edge Cases

- O que acontece com tipos de dados, valores padrão ou comportamentos de
  coluna que existiam no schema SQL Server e não têm equivalente direto em
  PostgreSQL? Eles devem ser mapeados para o equivalente PostgreSQL mais
  próximo, preservando o mesmo comportamento observável (ex.: precisão de
  valores monetários, tratamento de datas/horários, geração de
  identificadores).
- Como o sistema se comporta se a string de conexão configurada apontar
  para um PostgreSQL inacessível ou com credenciais inválidas? O sistema
  deve falhar de forma clara na inicialização ou na primeira tentativa de
  acesso ao banco, sem mascarar o erro como sucesso.
- O que acontece com migrações de schema já aplicadas em SQL Server quando
  a aplicação passa a rodar sobre PostgreSQL? Como não há dados de produção
  existentes até o momento desta troca (ver Assumptions), o histórico de
  migração é reiniciado do zero para o novo provider, sem necessidade de
  converter dados previamente persistidos.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE persistir e recuperar todos os dados de
  domínio hoje suportados (incluindo despesas recorrentes e suas
  ocorrências) usando PostgreSQL como banco de dados relacional, no lugar
  de SQL Server.
- **FR-002**: O sistema DEVE fornecer migrações de schema aplicáveis contra
  uma instância PostgreSQL, criando todas as tabelas, colunas, tipos,
  chaves e restrições necessárias para as funcionalidades já existentes.
- **FR-003**: Toda funcionalidade já existente que depende de persistência
  (ex.: cadastro de despesa recorrente via API) DEVE continuar se
  comportando de forma idêntica, do ponto de vista de quem consome a API,
  após a troca de provider.
- **FR-004**: O sistema DEVE permitir configurar os dados de conexão com o
  PostgreSQL de forma independente por ambiente (ex.: desenvolvimento,
  testes, produção), sem exigir alteração de código para trocar de
  ambiente.
- **FR-005**: A suíte de testes automatizados que hoje valida a camada de
  persistência contra uma instância real de SQL Server DEVE passar a
  validar contra uma instância real de PostgreSQL.
- **FR-006**: Qualquer tipo de dado, valor padrão ou comportamento de
  coluna específico de SQL Server usado no schema atual DEVE ser substituído
  por um equivalente compatível com PostgreSQL, preservando o comportamento
  observável descrito nas User Stories.
- **FR-007**: Após a troca, o sistema NÃO DEVE manter nenhuma dependência
  de execução (pacotes, configuração, infraestrutura de teste) em relação a
  SQL Server.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos testes automatizados existentes (unitários e de
  persistência) passam sem regressão após a troca de provider, agora
  executando contra PostgreSQL.
- **SC-002**: A aplicação sobe e atende a todos os endpoints de API já
  existentes sem erros, usando configuração de PostgreSQL, nos ambientes de
  desenvolvimento local, testes automatizados e produção.
- **SC-003**: Um desenvolvedor novo consegue colocar o ambiente local para
  rodar (banco de dados incluso) sem instalar nenhum software com custo de
  licença.
- **SC-004**: 100% dos dados de negócio já suportados (ex.: despesas
  recorrentes e suas ocorrências) podem ser criados, consultados,
  atualizados e removidos corretamente após a troca, com o mesmo resultado
  observado antes da troca.

## Assumptions

- O projeto ainda não está em produção com dados reais persistidos em SQL
  Server; portanto, esta troca de provider não precisa incluir migração de
  dados existentes — apenas a troca do schema e da camada de acesso a
  dados para um PostgreSQL vazio.
- "Atualizar o provider" significa substituir completamente SQL Server por
  PostgreSQL como único banco de dados suportado pela aplicação, e não
  adicionar suporte simultâneo a ambos os bancos.
- A versão do PostgreSQL a ser usada é a versão estável mais recente
  disponível no momento da implementação, sem exigência de compatibilidade
  com uma versão específica mais antiga.
- O ambiente de testes automatizados continuará usando um container de
  banco de dados descartável, agora de PostgreSQL, no lugar do container de
  SQL Server usado atualmente.
