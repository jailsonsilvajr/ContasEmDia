# Refinamento — Projeto Application: Despesa Recorrente (Cadastro)

## Origem
Tela de design: **"Nova despesa"** (`Cadastro.dc.html` — "Nova despesa recorrente").
Base já existente: `backend/Domain` (aggregate `RecurringExpense`, entidade
`Occurrence`, Value Objects e a interface `IRecurringExpenseRepository`) e
`backend/Infrastructure` (persistência via EF Core 10, `RecurringExpenseRepository`
e `RepositoryManager`), documentados em
[`domain-despesa-recorrente.md`](./domain-despesa-recorrente.md) e
[`infrastructure-despesa-recorrente.md`](./infrastructure-despesa-recorrente.md).

## Feature
Criar, no backend, o projeto **Application**, contendo o Use Case que orquestra
a única ação que a tela "Nova despesa recorrente" dispara contra o backend:
cadastrar uma despesa recorrente. O Use Case recebe dados primitivos
(equivalentes ao corpo do `POST /api/recurring-expenses` já documentado no
contrato de API), converte-os para os Value Objects do Domain, determina a
competência do mês corrente, cria o aggregate `RecurringExpense` pelo seu
construtor público, persiste através do contrato de repositório já existente e
devolve um resultado — pronto para uma futura camada de API traduzir em `201`
ou `400` — sem nenhuma camada de transporte HTTP envolvida.

## Escopo
Cobre exclusivamente o Use Case de cadastro de despesa recorrente (a tela
analisada não dispara nenhuma outra ação contra o backend: o botão "Cancelar"
não tem handler, e não há edição, listagem nem pagamento de ocorrências nesta
tela). Cobre: o Input (dados de entrada do Use Case), o Output (dados de saída,
tanto de sucesso quanto de falha de validação), a conversão de cada campo
primitivo para o Value Object do Domain correspondente, a estratégia de
agregação de erros de campo e um novo *port* (interface) para obtenção da data
atual, necessário para calcular a competência corrente.

Não cobre: controllers, rotas HTTP, model binding, serialização JSON, códigos
de status HTTP e autenticação/autorização (Princípio IV) — tudo isso pertence a
uma futura camada de API/Apresentação, ainda não iniciada. Não cobre nenhuma
regra de negócio nova (todas já fechadas no Domain) nem qualquer alteração em
Domain ou Infrastructure já implementados. Não cobre nenhum outro Use Case além
do cadastro.

Não há sugestão de código, assinaturas de métodos ou nomes de classes de
implementação nas seções abaixo — apenas conceitos, contratos funcionais e
decisões de arquitetura necessários antes da implementação.

## Referências
- Domain: [`domain-despesa-recorrente.md`](./domain-despesa-recorrente.md);
  código em `backend/Domain`.
- Infrastructure: [`infrastructure-despesa-recorrente.md`](./infrastructure-despesa-recorrente.md);
  código em `backend/Infrastructure`.
- Contrato de API consumido pelo frontend:
  [`../frontend/cadastro-despesa-recorrente.md`](../frontend/cadastro-despesa-recorrente.md)
  e [`../../specs/002-cadastro-despesa-recorrente/contracts/api-contract.md`](../../specs/002-cadastro-despesa-recorrente/contracts/api-contract.md).
- Constituição do projeto: [`../../.specify/memory/constitution.md`](../../.specify/memory/constitution.md)
  (Princípios I, II, IV e V se aplicam; não há, hoje, um princípio dedicado à
  camada Application — ver "Pontos em aberto").

## Conceitos identificados

### Use Case: Cadastrar Despesa Recorrente
Único caso de uso desta camada nesta etapa. Orquestra, nesta ordem conceitual:
conversão/validação da entrada, cálculo da competência corrente, criação do
aggregate, persistência e montagem da saída. Não contém nenhuma regra de
negócio própria — todas as regras (RF02–RF15 do refinamento de domínio) já
estão fechadas dentro do aggregate `RecurringExpense` e de seus Value Objects;
o Use Case apenas traduz a entrada primitiva para o Domain e a saída do Domain
de volta para primitivos.

### Input do Use Case
Estrutura de entrada que espelha 1:1 os campos primitivos do corpo do
`POST /api/recurring-expenses` (nome, categoria, valor previsto mensal, dia de
vencimento, data de início como texto, frequência, status e observação
opcional), todos como tipos primitivos — nunca como Value Objects do Domain.
Quem preenche este Input a partir do JSON recebido é a futura camada de API,
fora do escopo deste refinamento.

### Output do Use Case
Estrutura de saída que representa duas possibilidades mutuamente exclusivas:
- **Sucesso**: os dados da despesa recorrente criada e da ocorrência gerada
  (se houver), na mesma forma do corpo `201` já documentado no contrato de
  API — porém ainda como um objeto interno da aplicação, não uma resposta
  HTTP.
- **Falha de validação**: uma lista de erros de campo (nome do campo mais
  mensagem em PT-BR voltada ao usuário), no mesmo formato já documentado no
  contrato de erro `400`.

Este objeto de saída evita que o Use Case lance exceções para representar uma
falha de validação de negócio esperada; exceções continuam reservadas para
falhas verdadeiramente inesperadas (ex.: indisponibilidade do banco de dados).

### Port: Data atual
Uma abstração (interface) para obter a data corrente, da qual o Use Case
deriva a competência do mês corrente exigida pelo construtor de
`RecurringExpense`. É necessária porque o construtor do aggregate recebe a
competência corrente como parâmetro em vez de calculá-la internamente — o
Domain permanece determinístico e sem dependência do relógio do sistema. Sem
este *port*, o Use Case chamaria diretamente uma API de data/hora do runtime,
tornando-se não determinístico e difícil de testar de forma isolada (Princípio
II — os testes do Use Case precisam poder controlar "hoje"). Esta é a única
abstração de infraestrutura nova introduzida por este refinamento.

### Conversão primitivo → Value Object
Para cada campo do Input, o Use Case deve tentar construir o Value Object
correspondente do Domain (`ExpenseName`, `ExpenseCategory`, `Money`, `DueDay`,
`CalendarDate`, `Frequency`, `RecurringExpenseStatus`, `Note`) e, quando o
construtor rejeitar o valor, capturar essa falha e repassar a mensagem em
PT-BR já fornecida pelo Domain (ver "Mensagens de erro em PT-BR" em
"Premissas e pontos em aberto") como erro do campo correspondente no Output —
sem duplicar a regra de validação em si nem reescrever o texto da mensagem,
que vive exclusivamente nos Value Objects e no aggregate.

## Requisitos Funcionais

### RF01 — Execução do Use Case de cadastro
O sistema deve prover um Use Case único responsável por cadastrar uma despesa
recorrente a partir de dados equivalentes ao corpo do
`POST /api/recurring-expenses`.

### RF02 — Conversão de cada campo para o Value Object correspondente
O Use Case deve converter nome, categoria, valor previsto mensal, dia de
vencimento, data de início, frequência, status e observação para os
respectivos Value Objects do Domain antes de construir a despesa recorrente,
sem reimplementar nenhuma das regras de validação já expressas nos Value
Objects (RF02–RF09 do refinamento de domínio).

### RF03 — Interpretação da data de início
A data de início, recebida como texto (`yyyy-MM-dd`), deve ser convertida
para o tipo de data usado pelo Domain; um texto malformado ou uma data
inexistente no calendário deve ser reportado como erro do campo de data de
início (RF06 do domínio). Isso é necessário porque o Value Object de data do
Domain não valida a correção calendária por si só — aceita qualquer data já
construída pelo runtime; a validação de "é uma data de calendário válida"
acontece, na prática, no momento da conversão texto → data, que é
responsabilidade do Use Case.

### RF04 — Interpretação da categoria
Um valor de categoria que não corresponda a nenhuma das cinco categorias
suportadas deve ser reportado como erro do campo de categoria (RF03 do
domínio), mesmo antes de tentar construir o Value Object de categoria.

### RF05 — Agregação de todos os erros de campo em uma única resposta
Quando mais de um campo de entrada for inválido, o Use Case deve tentar
converter todos os campos — sem interromper a validação no primeiro erro
encontrado — e retornar todos os erros de campo já identificados em uma única
lista, para que a tela possa destacar todos os campos inválidos de uma vez,
como já ocorre na validação client-side da tela.

### RF06 — Mensagens de erro em PT-BR
Cada erro de campo retornado pelo Use Case deve conter a mensagem de
validação em PT-BR voltada ao usuário final já fornecida pelo Value Object
(ou aggregate) do Domain correspondente; o Use Case repassa essa mensagem tal
como recebida, sem reescrevê-la, traduzi-la nem manter um texto próprio e
duplicado na Application (ver "Mensagens de erro em PT-BR" em "Premissas e
pontos em aberto").

### RF07 — Cálculo da competência corrente
O Use Case deve obter a data atual através do Port de data atual e derivar
dela a competência (mês/ano) corrente, usada como parâmetro do construtor de
`RecurringExpense` — responsável por decidir a geração ou não da ocorrência do
mês corrente (RF10/RF11 do domínio).

### RF08 — Criação do aggregate exclusivamente pelo construtor público
O Use Case deve criar a despesa recorrente exclusivamente através do
construtor público do aggregate `RecurringExpense`, nunca através do
construtor privado de reconstrução (reservado à Infrastructure) nem por
qualquer outro meio que contorne as validações do construtor público.

### RF09 — Persistência via contrato já existente do Domain/Infrastructure
Após a criação bem-sucedida do aggregate, o Use Case deve persisti-lo através
do repositório de despesas recorrentes (`IRecurringExpenseRepository`),
acessado através da interface de `RepositoryManager` declarada no Domain (ver
"Direção de dependência entre Application e Infrastructure" em "Premissas e
pontos em aberto"), nunca diretamente através da implementação concreta na
Infrastructure.

### RF10 — Montagem da saída de sucesso
Em caso de sucesso, o Use Case deve montar a saída com os dados primitivos da
despesa recorrente criada e de sua ocorrência (se gerada), obtidos
exclusivamente através dos métodos de leitura já expostos pelo aggregate e
pela entidade — nunca acessando estado interno diretamente.

### RF11 — Ocorrência ausente refletida fielmente na saída
Quando a despesa for criada como Pausada, ou quando a competência corrente for
anterior à competência da data de início, a lista de ocorrências da saída deve
vir vazia, espelhando fielmente o que o aggregate retornar (RF10/RF11 do
domínio) — o Use Case não decide isso, apenas reporta o que o Domain já
decidiu.

### RF12 — Falhas inesperadas não viram erro de validação
Falhas que não sejam de validação de negócio (ex.: indisponibilidade do banco
de dados) não devem ser convertidas em erros de campo pelo Use Case; devem
propagar como exceção, para que uma camada superior (futura API) decida como
respondê-las. Isso preserva a distinção já assumida no contrato de API do
frontend entre "requisição inválida" (`400`) e "falha do sistema" (`5xx`).

## Regras técnicas adicionais
- O projeto Application não deve depender de nenhum detalhe de transporte
  HTTP (sem tipos do ASP.NET Core, sem atributos de serialização) — apenas
  tipos primitivos e tipos do próprio Application/Domain — para permanecer
  testável isoladamente e reutilizável por qualquer futura camada de
  apresentação (Princípio I).
- O Use Case não deve conter nenhuma regra de negócio própria (nenhuma
  decisão que já esteja coberta por uma RF do domínio) — apenas orquestração
  e tradução de erros.
- O Port de data atual deve ser a única abstração de infraestrutura nova
  introduzida por este refinamento; nenhuma outra abstração (ex.: cache,
  mensageria) deve ser criada sem necessidade concreta (Princípio V).
- Seguindo o padrão já usado por `Domain.Tests` e `Infrastructure.Tests`, a
  lógica do Use Case deve ter testes unitários próprios (`Application.Tests`),
  substituindo o repositório e o Port de data atual por dublês de teste, cobrindo
  cada RF acima (Princípio II).

## Fora de escopo
- Controllers, rotas HTTP, DTOs de request/response serializáveis,
  versionamento de API e autenticação/autorização (Princípio IV) — camada de
  Apresentação/API, ainda não iniciada.
- Implementação concreta do Port de data atual (pertence à Infrastructure,
  papel análogo ao já desempenhado pelo `RepositoryManager`).
- Qualquer Use Case além do cadastro — a tela analisada não exige nenhum
  outro.
- Qualquer alteração em Domain ou Infrastructure já implementados.
- Erros de desserialização/model binding de tipos primitivos malformados
  (ex.: um corpo JSON com `dueDay` não numérico) — o Input do Use Case assume
  que os tipos primitivos já chegaram corretamente tipados; isso é
  responsabilidade da futura camada de API.

## Premissas e pontos em aberto

### Direção de dependência entre Application e Infrastructure (decidido)
A Infrastructure já implementada expõe o repositório exclusivamente através
da classe concreta `RepositoryManager` (não de uma interface) — decisão
tomada no refinamento de Infrastructure (RF07 daquele documento). Decisão
tomada: extrair uma interface para `RepositoryManager`, declarada no projeto
**Domain**, mantendo a implementação concreta na Infrastructure — opção (b)
anteriormente cogitada. O Use Case depende apenas dessa interface (via
Domain), nunca da classe concreta, preservando a inversão de dependência
esperada em Clean Architecture (Application/Domain não dependem de
Infrastructure). Consequência: esta decisão revisita e substitui a decisão
registrada no refinamento de Infrastructure (RF07 daquele documento), que
hoje expõe `RepositoryManager` apenas como classe concreta — aquele
refinamento precisa ser atualizado para refletir a nova interface antes ou
durante a implementação deste Use Case.

### Onde vive o Port de data atual (decidido)
Decisão tomada: a interface do Port de data atual deve ser declarada no
projeto **Application** e implementada na Infrastructure, seguindo o mesmo
padrão de inversão de dependência do repositório (interface do lado que a
consome, implementação do lado que depende do runtime) — mesmo padrão da
decisão anterior, mas com a interface vivendo em Application em vez de
Domain, já que o Port de data atual é uma necessidade do Use Case e não do
aggregate em si.

### Mensagens de erro em PT-BR: o Domain fornece o texto por campo (decidido)
Decisão tomada: o **Domain** deve fornecer o texto de mensagem em PT-BR por
campo, substituindo a suposição anterior de que a Application manteria seu
próprio texto desacoplado das exceções do Domain. Na prática, cada Value
Object (e o aggregate, quando aplicável) passa a expor a mensagem de
validação já em PT-BR e voltada ao usuário final (ex.: "Nome é
obrigatório."), e o Use Case apenas repassa essa mensagem para o campo
correspondente no Output de falha, sem reescrevê-la nem manter um texto
duplicado na Application. Consequência: as mensagens hoje em inglês nos
Value Objects do Domain (ver `domain-despesa-recorrente.md`) precisam ser
revisadas para PT-BR como parte da implementação — este refinamento não cobre
essa alteração de Domain diretamente, apenas assume o resultado dela como
pré-requisito. Permanece válida a limitação já identificada: como cada Value
Object hoje só sinaliza a primeira regra violada dentro do próprio construtor
(ex.: `Money` verifica "maior que zero" antes de "no máximo duas casas
decimais"), quando um campo violar mais de uma regra ao mesmo tempo, apenas a
mensagem da primeira regra checada estará disponível — aceitável, pois o
comportamento final (uma mensagem por campo) já é o mesmo aceito na validação
client-side do frontend.

### Ausência de um princípio de constituição dedicado à camada Application
Diferente de Domain (Princípio VI) e Infrastructure (Princípio VII), a
constituição do projeto ainda não define regras específicas para uma camada
Application/Use Cases (estrutura de pastas, nomenclatura, necessidade ou não
de uma interface genérica de Use Case). Este refinamento assume a estrutura
mínima necessária (Input, Output, Use Case, Port de data atual) sem introduzir
uma abstração genérica de Use Case compartilhada, por não haver ainda mais de
um Use Case que a justifique (Princípio V). Recomenda-se propor uma emenda à
constituição quando o projeto Application for de fato criado, análoga às já
existentes para Domain e Infrastructure.
