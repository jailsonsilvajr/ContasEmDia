# Feature Specification: API de Despesa Recorrente (Cadastro)

**Feature Branch**: `004-api-despesa-recorrente`

**Created**: 2026-09-05

**Status**: Draft

**Input**: User description: "@refinements/backend/api-despesa-recorrente.md" — Criar, no backend, o projeto API (camada de Apresentação HTTP), expondo como endpoint versionado a única ação que a tela "Nova despesa recorrente" dispara contra o backend: cadastrar uma despesa recorrente. O endpoint recebe o corpo da requisição, valida apenas forma/presença dos campos, delega toda a orquestração ao `ICreateRecurringExpenseUseCase` já implementado em `backend/Application`, e traduz o `Output` do Use Case em uma resposta HTTP — sem introduzir nenhuma regra de negócio nova. Por ser o primeiro projeto de API do repositório, este refinamento também fixa, pela primeira vez, as convenções obrigatórias pela Constituição para toda API futura (envelope de resposta padrão, versionamento de rota, middleware global de exceções, documentação OpenAPI/SwaggerUI).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar despesa recorrente com sucesso via API (Priority: P1)

Quando o corpo de uma requisição HTTP contém dados equivalentes aos do
formulário "Nova despesa recorrente" e todos são válidos, o sistema precisa
aceitar a requisição, delegar toda a orquestração ao Use Case de cadastro já
existente e devolver ao chamador uma resposta de sucesso contendo os dados da
despesa recorrente criada — e da ocorrência do mês corrente, quando gerada —
para que o cadastro feito na tela seja de fato persistido e confirmado.

**Why this priority**: É a única ação que a tela de origem dispara contra o
backend; sem o endpoint funcionando de ponta a ponta, nenhum cadastro real é
possível através da interface, independentemente de Domain e Application já
estarem prontos.

**Independent Test**: Pode ser testado isoladamente enviando uma requisição
HTTP `POST` com um corpo válido ao endpoint (via `WebApplicationFactory`, sem
UI) e verificando que a resposta é `201 Created`, com o envelope de sucesso
preenchido a partir do resultado do Use Case.

**Acceptance Scenarios**:

1. **Given** um corpo de requisição válido para uma despesa Ativa com início
   na competência corrente, **When** o endpoint de cadastro é chamado,
   **Then** a resposta é `201 Created` com o envelope de sucesso contendo os
   dados da despesa recorrente e da ocorrência do mês corrente gerada.
2. **Given** um corpo de requisição válido para uma despesa Pausada, ou Ativa
   com início em competência futura, **When** o endpoint é chamado, **Then**
   a resposta é `201 Created` com o envelope de sucesso trazendo a lista de
   ocorrências vazia.
3. **Given** uma requisição de cadastro bem-sucedida, **When** a resposta é
   serializada, **Then** os nomes dos campos JSON seguem o mesmo formato
   (`camelCase`) já assumido pelo contrato de API documentado para o
   frontend.

---

### User Story 2 - Receber erro padronizado ao violar uma regra de negócio (Priority: P2)

Quando o corpo da requisição é formalmente válido mas um ou mais campos
violam uma regra de negócio já fechada no Domain (ex.: categoria
inexistente, valor previsto mensal zerado), o sistema precisa responder com
um erro claro, no mesmo formato de envelope usado por qualquer resposta desta
API, listando cada campo inválido, para que o usuário da tela saiba
exatamente o que corrigir.

**Why this priority**: É o outro desfecho possível da única ação da tela; sem
um erro padronizado e completo, o usuário não sabe o que corrigir ou precisa
corrigir e reenviar um campo por vez.

**Independent Test**: Pode ser testado isoladamente enviando um corpo com um
ou mais campos que violam regra de negócio e verificando que a resposta é
`400 Bad Request` com `Success = false`, `Data = null` e um erro por campo
inválido, cada um com mensagem em PT-BR.

**Acceptance Scenarios**:

1. **Given** um corpo de requisição com exatamente um campo violando uma
   regra de negócio, **When** o endpoint é chamado, **Then** a resposta é
   `400 Bad Request` com o envelope de erro contendo um único erro, com o
   nome do campo e a mensagem em PT-BR.
2. **Given** um corpo de requisição com mais de um campo violando regras de
   negócio simultaneamente, **When** o endpoint é chamado, **Then** a
   resposta é `400 Bad Request` com todos os erros agregados na mesma lista,
   e nada é persistido.

---

### User Story 3 - Receber o mesmo formato de erro para dados malformados ou ausentes (Priority: P3)

Quando o corpo da requisição está malformado ou tem um campo obrigatório
ausente (falha de forma, não de negócio), o sistema precisa responder usando
exatamente o mesmo formato de erro do caso de violação de regra de negócio,
para que o chamador não precise distinguir a origem do erro para tratá-lo.

**Why this priority**: Sem essa uniformidade, o frontend precisaria de dois
caminhos de tratamento de erro distintos para o mesmo endpoint, um deles
(o padrão nativo do framework) em formato e idioma diferentes do resto da
API.

**Independent Test**: Pode ser testado isoladamente enviando um corpo JSON
com um campo obrigatório ausente e verificando que a resposta é
`400 Bad Request` no mesmo envelope de erro do cenário de negócio, nunca no
formato padrão nativo do framework.

**Acceptance Scenarios**:

1. **Given** um corpo JSON com um campo obrigatório ausente, **When** o
   endpoint é chamado, **Then** a resposta é `400 Bad Request` usando o
   mesmo envelope de erro (`Success`/`Data`/`Errors`) do cenário de negócio.
2. **Given** uma falha de forma/presença qualquer, **When** o erro é
   retornado, **Then** a mensagem de cada campo está em PT-BR, nunca na
   mensagem padrão em inglês do framework.

---

### User Story 4 - Explorar e testar o endpoint via documentação interativa (Priority: P4)

Um desenvolvedor integrando com a API precisa descobrir e testar o endpoint
de cadastro sem acessar o código-fonte, através de um documento OpenAPI
machine-readable e de uma interface interativa (SwaggerUI), incluindo todos
os formatos de requisição e de resposta possíveis, para reduzir o atrito de
integração e servir como fonte viva de verdade do contrato.

**Why this priority**: Não bloqueia o fluxo de cadastro em si, mas é uma
exigência normativa fixada pela primeira vez nesta feature (primeiro projeto
de API do repositório) e vale para toda API futura, não apenas para este
endpoint.

**Independent Test**: Pode ser testado isoladamente acessando a rota do
documento OpenAPI e a SwaggerUI com a API em execução e verificando que o
endpoint de cadastro, seus tipos de requisição/resposta e todos os seus
retornos possíveis (`201`/`400`/`500`) aparecem documentados.

**Acceptance Scenarios**:

1. **Given** a API em execução, **When** a rota do documento OpenAPI é
   acessada, **Then** um documento válido é servido, cobrindo o endpoint de
   cadastro, seus tipos de requisição/resposta e o envelope de resposta.
2. **Given** a SwaggerUI, **When** o endpoint de cadastro é expandido,
   **Then** todos os seus retornos possíveis (`201`, `400`, `500`) estão
   documentados, junto dos respectivos formatos de corpo.

---

### Edge Cases

- Falha verdadeiramente inesperada durante o processamento (ex.: banco de
  dados indisponível): capturada por um middleware global e respondida como
  `500 Internal Server Error`, no mesmo envelope de resposta, com dados
  genéricos e sem vazar detalhes internos da exceção.
- Campo numérico obrigatório ausente do corpo da requisição: reportado como
  erro de forma/presença (`400`), nunca silenciosamente tratado como o valor
  zero.
- Nenhum outro endpoint além do cadastro existe nesta etapa (sem
  listagem/detalhe/edição/exclusão) — a resposta de sucesso não inclui um
  header `Location` apontando para um recurso, por não existir ainda um
  endpoint de leitura para apontar.
- Nenhuma autenticação, autorização ou política de CORS é aplicada a esta
  etapa — débito explícito, não uma omissão silenciosa.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST expor um único endpoint HTTP, `POST`, que
  recebe um corpo equivalente aos dados de "Nova despesa recorrente", delega
  toda a orquestração ao Use Case de cadastro já existente e traduz o
  resultado devolvido em uma resposta HTTP — nenhuma outra ação é exposta
  por esta feature.
- **FR-002**: O endpoint MUST ser exposto sob um segmento de versão explícito
  na rota (ex.: `/api/v1/...`); toda mudança futura incompatível MUST ser
  introduzida sob uma nova versão, nunca alterando a existente.
- **FR-003**: O corpo da requisição MUST ser validado apenas quanto a
  forma/tipo/presença de cada campo obrigatório (nome, categoria, valor
  previsto mensal, dia de vencimento, data de início, frequência, status) —
  nunca quanto a uma regra de negócio, que permanece exclusiva das camadas já
  implementadas.
- **FR-004**: Cada mensagem de erro de validação de forma/presença MUST estar
  em português do Brasil (PT-BR) — mensagens padrão do framework em inglês
  MUST NOT chegar ao chamador.
- **FR-005**: O endpoint MUST NOT conter nenhuma lógica de negócio, validação
  de negócio ou decisão sobre o resultado além de rotear a saída do Use Case
  para o código HTTP e formato de resposta corretos.
- **FR-006**: Toda resposta deste endpoint — sucesso ou falha — MUST ser
  devolvida dentro de um único envelope de resposta padrão, compartilhado de
  forma idêntica por toda a API; nenhuma resposta MUST expor os dados de
  sucesso ou a lista de erros "crus" no corpo.
- **FR-007**: Quando o cadastro for bem-sucedido, o sistema MUST responder
  `201 Created` com o envelope de sucesso contendo os dados da despesa
  recorrente criada e da lista de ocorrências geradas (vazia quando
  aplicável).
- **FR-008**: Quando o cadastro falhar por violação de uma regra de negócio,
  o sistema MUST responder `400 Bad Request` com o envelope de erro contendo
  um item por campo inválido, cada um com o nome do campo e a mensagem em
  PT-BR fornecida pela camada de negócio, sem reescrevê-la.
- **FR-009**: Quando o corpo da requisição falhar a validação de
  forma/presença (ex.: campo obrigatório ausente), o sistema MUST responder
  `400 Bad Request` usando exatamente o mesmo formato de envelope de erro do
  cenário de negócio (FR-008) — nunca a resposta automática padrão do
  framework para esse cenário, que tem formato diferente.
- **FR-010**: O endpoint MUST declarar explicitamente, em sua documentação,
  todos os seus retornos possíveis (`201`, `400`, `500`), tornando o conjunto
  completo de resultados visível na especificação OpenAPI.
- **FR-011**: O sistema MUST registrar um único middleware global de
  tratamento de exceções que capture qualquer falha verdadeiramente
  inesperada (não relacionada a uma regra de negócio) e a converta em
  `500 Internal Server Error`, no mesmo envelope de resposta, sem expor
  detalhes internos da exceção no corpo; nenhum endpoint MUST implementar
  tratamento de exceção próprio.
- **FR-012**: O sistema MUST expor um documento OpenAPI machine-readable e
  uma interface interativa (SwaggerUI) para explorar e exercitar o endpoint,
  incluindo os tipos de requisição, resposta e o envelope no schema gerado.
- **FR-013**: O endpoint MUST NOT implementar autenticação, autorização ou
  política de CORS nesta etapa — débito explícito a ser fechado antes de
  qualquer ambiente externamente acessível.
- **FR-014**: O sistema MUST compor, na inicialização da aplicação, todas as
  dependências necessárias para o Use Case de cadastro funcionar de ponta a
  ponta, incluindo uma fonte de dados configurável (nunca com credenciais ou
  string de conexão fixas no código) e a implementação concreta e real do
  provedor de data atual exigida por FR-015.
- **FR-015**: O sistema MUST prover, na camada responsável pelo acesso a
  dados, uma implementação concreta e real do provedor de data atual
  consumido pelo Use Case de cadastro (hoje coberto apenas por um dublê de
  teste), devolvendo a data corrente do sistema.
- **FR-016**: O sistema (esta feature) MUST atualizar a documentação de
  contrato de API já existente e assumida pelo frontend — tanto o documento
  de contrato quanto o refinamento de frontend correspondente — para refletir
  a rota versionada e o envelope de resposta definidos por esta feature,
  fechando a divergência hoje existente entre eles.

### Key Entities *(include if feature involves data)*

- **Corpo da requisição de cadastro**: Espelha 1:1 os campos primitivos já
  documentados para "Nova despesa recorrente" (nome, categoria, valor
  previsto mensal, dia de vencimento, data de início como texto, frequência,
  status, observação opcional), validados apenas em forma/presença.
- **Corpo da resposta de sucesso**: Dados da despesa recorrente criada e da
  lista de ocorrências geradas (podendo ser vazia), incluindo o período de
  referência de cada ocorrência como um objeto próprio (ano/mês).
- **Envelope de resposta padrão**: Estrutura única, compartilhada por toda a
  API (sucesso ou erro), indicando se a operação teve sucesso, os dados de
  sucesso (quando aplicável) e a lista de erros — cada um com o nome do
  campo (quando aplicável) e uma mensagem voltada ao usuário final.
- **Provedor de data atual**: Fonte da data corrente do sistema, usada pelo
  Use Case de cadastro para determinar a competência do mês corrente;
  passa a ter, através desta feature, uma implementação real além do dublê
  de teste já existente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das requisições de cadastro com dados válidos resultam em
  uma resposta `201` cujo conteúdo reflete exatamente os dados persistidos,
  incluindo a ocorrência do mês corrente quando gerada.
- **SC-002**: 100% das requisições de cadastro com um ou mais campos
  inválidos — seja por violação de regra de negócio, seja por falha de
  forma/presença — retornam `400` no mesmo formato de envelope de erro, sem
  nenhum erro descartado.
- **SC-003**: 100% das falhas verdadeiramente inesperadas (não relacionadas a
  validação) retornam `500` no mesmo envelope de resposta, sem expor
  detalhes internos.
- **SC-004**: 100% das respostas deste endpoint — sucesso ou erro — chegam ao
  chamador dentro do envelope único da API, nunca com o corpo "cru".
- **SC-005**: 100% dos retornos possíveis do endpoint (`201`/`400`/`500`) e
  seus formatos aparecem documentados na especificação OpenAPI e na
  SwaggerUI, sem necessidade de leitura do código-fonte para descobri-los.
- **SC-006**: 100% das mensagens de erro de validação de forma/presença
  retornadas ao chamador estão em PT-BR, sem nenhuma mensagem padrão em
  inglês do framework vazando na resposta.

## Assumptions

- Domain, Application e Infrastructure (aggregate `RecurringExpense`, Use
  Case de cadastro, persistência via EF Core) já existem e suas regras de
  negócio estão fechadas; esta feature não introduz nem altera nenhuma regra
  de negócio nova, apenas a camada de transporte HTTP sobre o Use Case já
  implementado.
- Este é o primeiro projeto de API do repositório: as convenções aqui fixadas
  pela primeira vez (envelope de resposta único, versionamento de rota por
  segmento literal, middleware global de exceções, documentação
  OpenAPI/SwaggerUI) valem para toda API futura do projeto, não apenas para
  este endpoint.
- O versionamento de rota é resolvido nesta etapa apenas por um prefixo
  literal na URL, sem introduzir uma biblioteca dedicada de versionamento —
  há apenas uma versão e um endpoint até aqui.
- A implementação concreta do provedor de data atual, necessária para a
  composição de dependências deste endpoint, é incluída no escopo desta
  feature, já que nenhuma implementação real existe hoje no repositório
  (apenas um dublê de teste).
- A atualização dos documentos de contrato de API já assumidos pelo
  frontend (hoje descrevendo uma rota sem versão e respostas sem envelope)
  está incluída no escopo desta feature, para que uma futura integração do
  frontend seja construída já contra a forma correta.
- Nenhum outro endpoint (listagem, detalhe, edição, pausa/reativação,
  exclusão, pagamento de ocorrência, catálogo de categorias) é coberto por
  esta feature — a tela de origem não dispara nenhuma outra ação contra o
  backend nesta etapa.
- Autenticação, autorização e política de CORS reais permanecem fora de
  escopo nesta etapa, por exceção de fase já vigente no projeto, e devem ser
  fechadas antes de qualquer deploy em ambiente externamente acessível.
