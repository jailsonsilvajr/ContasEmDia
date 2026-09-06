# Refinamento — Projeto Domain: Despesa Recorrente

## Origem
Tela de design: **"Nova despesa"** (`Cadastro.dc.html` — "Nova despesa recorrente").

## Feature
Criar, no backend, o projeto **Domain** com Aggregates, Entities, Value Objects e
Repositories (apenas interfaces), modelando o domínio necessário para suportar o
cadastro de uma despesa recorrente e a geração automática da sua primeira ocorrência.

## Escopo
Este refinamento cobre exclusivamente as regras de negócio extraídas da tela "Nova
despesa": criação da despesa recorrente e geração da ocorrência do mês corrente.
Não cobre: edição/pagamento de ocorrências, listagem do painel mensal, cálculo de
status "vencida"/"vence em breve" (pertencem a outras features/telas) e nenhuma
camada de infraestrutura, API ou apresentação — apenas o modelo de domínio.

Não há sugestão de código, assinaturas de métodos ou nomes de classes de
implementação nas seções abaixo — apenas conceitos de domínio e regras funcionais.

## Conceitos de domínio identificados

### Aggregate Root: Despesa Recorrente
Representa uma conta que se repete mensalmente (ex.: aluguel, internet, academia).
É a raiz de consistência: toda criação/alteração de dados da despesa e a geração
de novas ocorrências passam por ela.

### Entity (interna ao aggregate): Ocorrência
Representa a instância de cobrança de uma despesa recorrente em um mês/ano
específico (competência). Cada despesa recorrente ativa gera uma ocorrência por
competência. Possui ciclo de vida próprio (pendente → paga, ou desfeita), mas só
existe no contexto de uma Despesa Recorrente.

### Value Objects
- **Valor Monetário**: quantia prevista mensal da despesa, sempre em reais (BRL),
  não negativa, com precisão de centavos.
- **Categoria**: classificação da despesa, restrita a um conjunto fechado de
  valores conhecidos pela tela (Moradia, Serviços, Transporte, Assinaturas, Outra).
- **Dia de Vencimento**: dia do mês em que a despesa vence, com validação de
  faixa e compatibilidade com meses de tamanhos diferentes (ex.: dia 31 em mês
  com menos dias).
- **Competência**: referência de mês/ano à qual uma ocorrência pertence.
- **Status da Despesa Recorrente**: indica se a despesa está Ativa ou Pausada.
- **Status da Ocorrência**: indica se a ocorrência está Pendente ou Paga (os
  status "Vencida" e "Vence em breve" são derivados por comparação de datas em
  tempo de consulta e não fazem parte do estado persistido da ocorrência).

### Repositories (apenas interfaces)
- Interface de repositório da **Despesa Recorrente**, responsável por persistir e
  recuperar despesas recorrentes (a Ocorrência é acessada através do aggregate ao
  qual pertence, por não ser um aggregate independente).

## Requisitos Funcionais

### RF01 — Cadastro de despesa recorrente
O sistema deve permitir criar uma despesa recorrente com os seguintes dados:
nome, categoria, valor previsto mensal, dia de vencimento, data de início,
frequência e observação opcional.

### RF02 — Nome obrigatório
O nome da despesa é obrigatório e não pode ser vazio ou composto apenas por
espaços em branco.

### RF03 — Categoria obrigatória e restrita
A categoria é obrigatória e deve pertencer ao conjunto de categorias suportadas
(Moradia, Serviços, Transporte, Assinaturas, Outra).

### RF04 — Valor previsto mensal válido
O valor previsto mensal é obrigatório, deve ser maior que zero e representar uma
quantia monetária válida em reais (BRL), com no máximo duas casas decimais.

### RF05 — Dia de vencimento válido
O dia de vencimento é obrigatório e deve ser um dia de mês válido (entre 1 e 31).
A despesa recorrente deve tratar corretamente meses cujo número de dias seja
menor que o dia de vencimento informado, definindo de forma consistente qual
data de vencimento vale para essas competências.

### RF06 — Data de início obrigatória
A data de início da despesa é obrigatória e deve ser uma data válida. Ela define
a partir de qual competência a despesa passa a gerar ocorrências.

### RF07 — Frequência da despesa
No momento, a única frequência suportada é a mensal. O domínio deve ser capaz de
registrar a frequência da despesa, ainda que apenas o valor "mensal" seja aceito
nesta etapa (frequências semanal, trimestral e anual são reservadas para uma
evolução futura e não fazem parte deste refinamento).

### RF08 — Status inicial da despesa
Toda despesa recorrente é criada com status Ativa ou Pausada, conforme escolha
no cadastro. O status padrão, quando não alterado pelo usuário, é Ativa.

### RF09 — Observação opcional
A observação é um campo de texto livre, opcional, sem regra de obrigatoriedade
ou formato.

### RF10 — Geração automática da ocorrência do mês corrente
Ao cadastrar uma despesa recorrente com status Ativa, o sistema deve gerar
automaticamente, no mesmo momento da criação, a ocorrência referente à
competência do mês corrente, com status Pendente e valor previsto igual ao valor
previsto mensal da despesa.

### RF11 — Não geração de ocorrência para despesa criada como Pausada
Se a despesa recorrente for cadastrada com status Pausada, nenhuma ocorrência
deve ser gerada automaticamente no momento do cadastro.

### RF12 — Consistência entre despesa e suas ocorrências
Uma ocorrência sempre pertence a exatamente uma despesa recorrente e não pode
existir de forma independente. Os dados de exibição da ocorrência (nome,
categoria, valor previsto) refletem os dados da despesa recorrente no momento em
que a ocorrência foi gerada.

### RF13 — Não duplicidade de ocorrência por competência
Uma mesma despesa recorrente não pode ter mais de uma ocorrência para a mesma
competência (mês/ano).

### RF14 — Persistência da despesa recorrente
O sistema deve ser capaz de armazenar uma nova despesa recorrente, recuperar uma
despesa recorrente existente por identificador e localizar despesas recorrentes
ativas (necessário para a geração futura de novas ocorrências em outras
features).

### RF15 — Papel do status Pausada
Uma despesa recorrente com status Pausada não gera novas ocorrências enquanto
permanecer pausada, mas ocorrências já geradas anteriormente não são afetadas
por essa mudança de status.

## Regras de negócio adicionais (invariantes do domínio)
- Uma despesa recorrente nunca pode ser criada com valor previsto igual ou
  menor que zero.
- Uma despesa recorrente nunca pode ser criada sem categoria válida.
- A ocorrência gerada automaticamente no cadastro nasce sempre com status
  Pendente — nunca é criada já como paga.
- O dia de vencimento é um atributo da despesa recorrente, não da ocorrência
  individual; a data de vencimento de cada ocorrência é derivada do dia de
  vencimento da despesa combinado com a competência da ocorrência.

## Fora de escopo (assumido para features futuras)
- Edição e exclusão de despesas recorrentes.
- Marcar ocorrência como paga, valor pago divergente do previsto e "desfazer
  pagamento" (vistos na tela do painel mensal).
- Cálculo/exibição dos status derivados "Vencida" e "Vence em breve".
- Suporte efetivo a frequências diferentes de mensal.
- Qualquer camada de aplicação, API ou persistência concreta — este refinamento
  trata somente do modelo de domínio (Aggregates, Entities, Value Objects e
  interfaces de Repository).

## Premissas e pontos em aberto
- Assumido que "Ocorrência" é uma entidade interna ao aggregate "Despesa
  Recorrente" (não um aggregate próprio), pois seu ciclo de vida depende
  totalmente da despesa que a originou. Deve ser confirmado antes da
  implementação, especialmente considerando que o painel mensal exibe
  ocorrências de várias despesas ao mesmo tempo.
- Não há, na tela analisada, regra explícita sobre nomes duplicados de despesas
  recorrentes — assumido que não há restrição de unicidade de nome nesta etapa.
- O comportamento do dia de vencimento em meses mais curtos que o dia
  informado (ex.: dia 31 em fevereiro) não é definido pela tela e precisa de
  validação de negócio antes da implementação.
