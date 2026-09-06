# Refinamento — Projeto Infrastructure: Despesa Recorrente

## Origem
Tela de design: **"Nova despesa"** (`Cadastro.dc.html` — "Nova despesa recorrente").
Base de domínio já existente: `backend/Domain` (aggregate `RecurringExpense`,
entidade `Occurrence`, Value Objects e a interface `IRecurringExpenseRepository`),
documentado em [`refinements/domain-despesa-recorrente.md`](./domain-despesa-recorrente.md).

## Feature
Criar, no backend, o projeto **Infrastructure**, responsável por implementar a
persistência (Entity Framework Core 10) do Domain já existente, de forma a
suportar todas as operações que a tela "Nova despesa recorrente" e o contrato
de repositório do Domain exigem: salvar uma nova despesa recorrente (com a
ocorrência do mês corrente gerada automaticamente) e recuperar despesas
recorrentes persistidas.

## Escopo
Este refinamento cobre exclusivamente a camada Infrastructure para o aggregate
`RecurringExpense` e a entidade `Occurrence` já modelados no Domain: o
`DbContext`, o mapeamento (EF Core Configurations) do aggregate, da entidade e
de todos os seus Value Objects, as migrations correspondentes, a implementação
concreta de `IRecurringExpenseRepository` e o `RepositoryManager`.

Não cobre: camada de aplicação/API (controllers, endpoints HTTP, DTOs) que
expõe essas operações ao frontend, autenticação/autorização, novas regras de
negócio (toda regra de negócio já está fechada no Domain — Infrastructure
apenas persiste), e qualquer aggregate/repository além de `RecurringExpense`.

## Operações que a Infrastructure precisa suportar
Derivadas da interação da tela com o backend e do contrato já definido em
`IRecurringExpenseRepository`:

- **Salvar despesa recorrente** (botão "Salvar despesa" / `onSalvar`): a
  operação que a tela efetivamente dispara. Deve persistir, em uma única
  operação atômica, a despesa recorrente e — quando gerada — a ocorrência do
  mês corrente que nasce junto com ela.
- **Recuperar despesa recorrente por identificador**: já exigida pelo
  contrato do Domain (RF14 do refinamento de domínio); a tela atual não a
  aciona diretamente, mas o repositório precisa estar implementado por
  completo, sem métodos pendentes.
- **Listar despesas recorrentes ativas**: idem — parte do contrato do Domain,
  reservada para uso por features futuras (ex.: geração mensal automática de
  novas ocorrências), não acionada pela tela atual.

## Componentes técnicos identificados

### `/Contexts`
Um `DbContext` único para o Bounded Context de despesas, nomeado
**`ContasEmDiaDbContext`**, expondo o `RecurringExpense` como conjunto raiz
persistido. A entidade `Occurrence`, por não ser um aggregate independente,
não é exposta como conjunto próprio — é alcançada apenas através do
`RecurringExpense` ao qual pertence. Provedor: **SQL Server**.

### `/Configs`
Classes de configuração (`IEntityTypeConfiguration<T>`), seguindo o padrão de
nome **`<nome do aggregate/entity>Configurations`** (ex.:
`RecurringExpenseConfigurations`, `OccurrenceConfigurations`):
- **`RecurringExpenseConfigurations`**: mapeia identificador, campos e a
  coleção interna de ocorrências, respeitando o encapsulamento do aggregate
  (sem expor propriedades públicas além dos métodos já definidos no Domain —
  o mapeamento acessa o campo privado de apoio, não uma propriedade).
- **`OccurrenceConfigurations`**: mapeia a entidade como **dependente de
  `RecurringExpense`, com chave estrangeira explícita** (relação 1:N via FK
  obrigatória para a despesa dona — não como owned entity).
- Conversões de valor para cada Value Object do Domain (`ExpenseName`,
  `ExpenseCategory`, `Money`, `DueDay`, `CalendarDate`, `Frequency`,
  `RecurringExpenseStatus`, `Note`, `OccurrenceStatus`, `ReferencePeriod`),
  traduzindo cada um para o tipo de coluna primitivo correspondente sem que o
  Domain precise expor esses primitivos diretamente.

### `/Migrations`
Migration inicial que cria o schema (tabelas de despesa recorrente e
ocorrência, com FK explícita entre elas) no SQL Server, a partir do
mapeamento acima. Como não existe nenhuma migration anterior no projeto, esta
é a primeira; migrations futuras de outras features não devem alterar esta.

### `/Repositories`
Implementação concreta de `IRecurringExpenseRepository` (interface já
definida no Domain), cobrindo os três métodos do contrato: inclusão,
recuperação por id e listagem das despesas ativas.

### `RepositoryManager`
Ponto único de acesso aos repositórios da camada Infrastructure, expondo cada
repositório como propriedade `Lazy<T>`, instanciada apenas no primeiro uso,
seguindo o padrão de nome **`<nome do aggregate>Repository`** (ex.:
`RecurringExpenseRepository` como propriedade que expõe
`IRecurringExpenseRepository`).

## Requisitos Funcionais

### RF01 — Mapeamento do aggregate e da entidade
O sistema deve mapear o aggregate `RecurringExpense` e a entidade `Occurrence`
para tabelas relacionais, preservando o encapsulamento do Domain: sem
propriedades públicas com setter, sem construção fora dos construtores
existentes, e usando exclusivamente o construtor privado de reconstrução já
exigido no Domain para materializar instâncias vindas do banco.

### RF02 — Mapeamento de cada Value Object
Cada Value Object usado por `RecurringExpense` e `Occurrence` deve ser
convertido para o tipo de coluna primitivo equivalente (texto, decimal,
inteiro, data, enum) através de conversão de valor, sem que o Domain precise
expor `GetValue()` de forma diferente da já definida nem adicionar
propriedades primitivas às suas classes.

### RF03 — Persistência da relação de posse entre despesa e ocorrências
A relação entre `RecurringExpense` e suas `Occurrence`s deve ser persistida
como uma relação de posse (1:N), garantindo que uma ocorrência nunca exista no
banco sem estar associada a exatamente uma despesa recorrente, refletindo a
mesma regra já validada no Domain (RF12/RF13 do refinamento de domínio).

### RF04 — Salvar despesa recorrente (inclusão)
O repositório deve implementar a inclusão de uma nova despesa recorrente,
persistindo em uma única operação atômica (uma chamada a
`SaveChangesAsync`) tanto a despesa quanto a ocorrência do mês corrente,
quando esta tiver sido gerada pelo Domain no momento da criação.

### RF05 — Recuperar despesa recorrente por identificador
O repositório deve implementar a recuperação de uma despesa recorrente a
partir do seu identificador, retornando a despesa junto de todas as suas
ocorrências já persistidas, ou a ausência de resultado quando o identificador
não existir.

### RF06 — Listar despesas recorrentes ativas
O repositório deve implementar a listagem de todas as despesas recorrentes
cujo status seja Ativa, cada uma retornada junto de suas ocorrências já
persistidas.

### RF07 — Acesso aos repositórios via RepositoryManager
O acesso ao repositório de despesas recorrentes pela camada de aplicação deve
ocorrer exclusivamente através de um `RepositoryManager`, que expõe o
repositório como propriedade `Lazy<T>`.

### RF08 — Unit of Work
Nenhuma abstração adicional de Unit of Work deve ser criada; `SaveChangesAsync`
do `DbContext` é o mecanismo único de confirmação de escrita, chamado
diretamente por quem invoca o repositório.

### RF09 — Migration inicial
Deve existir uma migration inicial responsável por criar o schema necessário
para persistir `RecurringExpense` e `Occurrence` de acordo com o mapeamento
definido. Nenhuma migration existente de outra feature pode ser alterada,
renomeada ou removida por este trabalho (não há nenhuma anterior no momento).

### RF10 — Configuração de acesso ao banco sem segredos no código
A string de conexão e demais configurações sensíveis de acesso ao banco de
dados devem vir de configuração externa (ex.: variáveis de ambiente/arquivo de
configuração não versionado), nunca de valores fixos no código-fonte.

## Regras técnicas adicionais (invariantes de infraestrutura)
- Nenhuma regra de negócio deve ser implementada na camada Infrastructure —
  toda validação e decisão (ex.: gerar ou não a ocorrência do mês corrente)
  já ocorre dentro do Domain antes de a Infrastructure ser acionada.
- A reconstrução de `RecurringExpense` e `Occurrence` a partir do banco deve
  usar apenas o construtor privado de reconstrução; esse caminho nunca deve
  ser usado para contornar as validações do construtor público usado na
  criação.
- A estrutura de pastas do projeto Infrastructure deve conter, no mínimo,
  `/Repositories`, `/Migrations`, `/Configs` e `/Contexts`.
- O projeto Infrastructure deve usar Entity Framework Core 10.

## Fora de escopo (assumido para features futuras)
- Camada de aplicação/API (controllers, endpoints HTTP, DTOs de
  request/response) que efetivamente expõe "salvar despesa recorrente" ao
  frontend a partir da tela — este refinamento cobre apenas a persistência.
- Autenticação e autorização de acesso aos dados.
- Atualização (edição) e exclusão de despesas recorrentes, e qualquer operação
  sobre ocorrências (marcar como paga, desfazer pagamento) — o contrato do
  Domain (`IRecurringExpenseRepository`) ainda não define esses métodos, logo
  a Infrastructure também não os implementa nesta etapa.
- Qualquer outro aggregate ou repositório além de `RecurringExpense`.
- Cache, mensageria, outbox ou qualquer mecanismo de integração assíncrona.

## Decisões confirmadas
- **Provedor de banco de dados**: SQL Server.
- **Mapeamento de `Occurrence`**: entidade dependente de `RecurringExpense`
  com chave estrangeira explícita (não owned entity).
- **Nomenclatura**:
  - `DbContext`: `ContasEmDiaDbContext`.
  - Classes de configuração EF Core: padrão `<nome do aggregate/entity>Configurations`
    (`RecurringExpenseConfigurations`, `OccurrenceConfigurations`).
  - Propriedades do `RepositoryManager`: padrão `<nome do aggregate>Repository`
    (`RecurringExpenseRepository`).
- **Antecipação de RF05/RF06** (recuperar por id e listar ativas, ainda sem
  tela consumidora): confirmada como desejada nesta etapa, para fechar por
  completo o contrato já definido em `IRecurringExpenseRepository`.
