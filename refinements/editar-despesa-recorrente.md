# Refinamento Funcional — Editar Despesa Recorrente

## Origem
**Não há tela de design para esta feature** (não existe um `.dc.html`
equivalente a `Cadastro.dc.html`/`Main.dc.html` para edição). Este
refinamento é construído a partir do modelo de domínio já refinado
(`domain-despesa-recorrente.md`) e por consistência funcional com as duas
telas já refinadas (`cadastro-despesa-recorrente.md` e
`painel-mensal-despesas.md`), que citam explicitamente "edição de despesa
recorrente" como fora de escopo. Por não partir de um design, os requisitos
abaixo devem ser tratados como proposta a validar contra uma tela real
quando ela existir — não como transcrição de um protótipo, como acontece
nos outros dois refinamentos.

## Feature
Refinamento **funcional** (sem sugestão de código, assinaturas ou nomes de
classes) da capacidade de editar os dados de uma despesa recorrente já
cadastrada: quais campos podem ser alterados, quais regras de validação se
aplicam, qual o efeito de cada alteração sobre as ocorrências já geradas
(passadas e da competência atual), e quais funcionalidades de leitura/escrita
um par de endpoints precisa oferecer para isso — no mesmo nível de detalhe
que `painel-mensal-despesas.md` usa para o `GET` daquela tela.

## Escopo
Cobre exclusivamente a edição dos dados próprios de uma despesa recorrente
já existente: quais campos são editáveis, as regras de validação de cada
um (as mesmas do cadastro, RF02–RF09 de `domain-despesa-recorrente.md`), o
que acontece com as ocorrências já geradas quando a despesa é editada, o
caso específico de alternar o status (Ativa ⇄ Pausada) durante a edição, e
o que um endpoint de leitura (para pré-carregar a tela) e um endpoint de
escrita (para salvar a edição) precisam oferecer, em termos funcionais.

Não cobre (fora de escopo desta iteração):
- Exclusão de uma despesa recorrente — é uma capacidade distinta de
  "editar" e fica para uma feature própria.
- Qualquer alteração em ocorrências individuais (marcar como paga,
  desfazer pagamento) — já coberto por `painel-mensal-despesas.md`.
- Suporte a frequências diferentes de mensal — continua fora de escopo,
  igual ao cadastro (`cadastro-despesa-recorrente.md`).
- Estrutura de componentes Angular e estados visuais de tela — sem um
  design de referência (ver "Origem"), esse detalhamento fica para um
  refinamento de frontend dedicado, como o que existe para o cadastro,
  assim que uma tela de design for produzida.
- O contrato técnico completo dos dois endpoints (rota exata, schemas de
  request/response, verbo HTTP — `PUT` vs. `PATCH` —, códigos de status)
  — este documento lista **o que** os endpoints precisam oferecer, não
  **como**, no mesmo espírito de `painel-mensal-despesas.md` para o `GET`
  daquela tela.
- Onde, na navegação atual (painel mensal ou outra tela), o usuário aciona
  a edição — sem um design, esse ponto de entrada não está definido (ver
  "Pontos em aberto").

## Referências
- Modelo de domínio já refinado: [`domain-despesa-recorrente.md`](backend/domain-despesa-recorrente.md)
  e o contrato público correspondente,
  [`specs/001-despesa-recorrente-domain/contracts/domain-public-api.md`](../specs/001-despesa-recorrente-domain/contracts/domain-public-api.md).
- Convenções de API já fixadas pelo endpoint de cadastro:
  [`api-despesa-recorrente.md`](backend/api-despesa-recorrente.md).
- Refinamentos já existentes das outras duas telas, para consistência de
  regras e de estilo: [`cadastro-despesa-recorrente.md`](frontend/cadastro-despesa-recorrente.md)
  e [`painel-mensal-despesas.md`](painel-mensal-despesas.md).

## Pré-requisito: lacunas frente ao domínio e à API já implementados
Assim como `painel-mensal-despesas.md` fez para a listagem, é preciso
registrar, antes de qualquer implementação, o que falta no que já existe:

- O aggregate `RecurringExpense`, conforme `domain-public-api.md`, hoje só
  expõe um construtor e métodos `GetXxx()` — nenhum método de negócio para
  alterar um dado já existe. Pelo Princípio VI da constituição
  (propriedades nunca são diretamente configuráveis; toda mutação passa por
  um método de intenção de negócio), editar exige a criação de novos
  métodos no aggregate (ex.: renomear, trocar categoria, trocar valor,
  trocar dia de vencimento, trocar data de início, trocar observação,
  ativar/pausar) — modelar esses métodos é trabalho de um refinamento de
  domínio à parte, não deste documento funcional.
- Não existe endpoint `GET` por identificador — apenas o `POST` de
  cadastro está implementado (`api-despesa-recorrente.md`, seção "Fora de
  escopo"). A tela de edição precisa desse endpoint para pré-carregar os
  dados atuais da despesa.
- Não existe nenhum endpoint de escrita para edição (`PUT`/`PATCH`).
- `IRecurringExpenseRepository` já expõe `GetByIdAsync`, suficiente para
  buscar a despesa a editar; não deve ser necessário um método de
  repositório dedicado a "atualizar" além de `SaveChangesAsync()`
  (Princípio VII trata `SaveChangesAsync()` como a própria Unit of Work),
  mas isso deve ser confirmado no refinamento técnico da Infrastructure.

## Funcionalidades da edição

**RF01 — Campos editáveis.** É possível alterar: nome, categoria, valor
previsto mensal, dia de vencimento, data de início, status (Ativa/Pausada)
e observação — os mesmos campos que existem no cadastro
(`domain-despesa-recorrente.md`, RF01). A frequência permanece fixa em
"Mensal" e não é editável, pela mesma razão que não é escolhível no
cadastro (RF07 do domínio: nenhuma outra frequência é suportada ainda).

**RF02 — Mesmas validações do cadastro por campo.** Cada campo editado
segue exatamente a mesma regra de validação já definida para o cadastro:
nome não vazio (RF02 do domínio), categoria dentre as 5 suportadas (RF03),
valor previsto mensal maior que zero e com até 2 casas decimais (RF04),
dia de vencimento entre 1 e 31 (RF05), data de início válida (RF06),
status Ativa ou Pausada (RF08). A observação continua opcional e livre
(RF09). Não há relaxamento nem regra adicional só para edição.

**RF03 — Edição não reescreve ocorrências já geradas.** Alterar nome,
categoria ou valor previsto mensal de uma despesa recorrente **não**
altera os dados já gravados em nenhuma ocorrência existente (nem passada,
nem a da competência atual, nem uma já paga) — consistente com a regra já
definida no domínio (`domain-despesa-recorrente.md`, RF12: os dados de
uma ocorrência refletem os dados da despesa **no momento em que a
ocorrência foi gerada**, não em tempo real). Só ocorrências geradas **após**
a edição usam os novos valores.

**RF04 — Edição não reescreve o dia de vencimento de ocorrências já
geradas.** Pela mesma razão de RF03: a data de vencimento de cada
ocorrência já existente foi fixada no momento em que ela foi gerada;
alterar o dia de vencimento da despesa recorrente só afeta ocorrências
geradas depois da edição.

**RF05 — Pausar durante a edição (Ativa → Pausada).** Segue exatamente a
regra já definida no domínio (`domain-despesa-recorrente.md`, RF15): a
despesa deixa de gerar novas ocorrências enquanto permanecer pausada, mas
nenhuma ocorrência já gerada é afetada — inclusive a da competência atual,
se já existir, permanece normalmente (mesmo comportamento já descrito em
`painel-mensal-despesas.md`, EC12, para o caso de pausa após o cadastro).

**RF06 — Reativar durante a edição (Pausada → Ativa).** Ao reativar uma
despesa recorrente que estava pausada, o sistema deve gerar imediatamente
a ocorrência da competência atual, com status Pendente e valor previsto
igual ao valor previsto mensal (vigente após a edição) — **desde que**
ainda não exista uma ocorrência para essa despesa na competência atual e
que a data de início da despesa já tenha começado (seja igual ou anterior
à competência atual). Isso espelha, no momento da reativação, a mesma
regra que já vale no cadastro (RF10/RF11 do domínio) para o momento da
criação. **Assunção a confirmar** — ver "Pontos em aberto".

**RF07 — Não duplicidade também vale para a reativação.** Se, por algum
motivo, já existir uma ocorrência da despesa para a competência atual no
momento da reativação (RF06), nenhuma ocorrência adicional é criada — a
mesma invariante de não duplicidade por competência já definida no domínio
(`domain-despesa-recorrente.md`, RF13) também protege esse caminho.

**RF08 — Editar data de início.** É possível alterar a data de início.
Isso não cria nem apaga retroativamente nenhuma ocorrência para
competências entre a data antiga e a nova — a geração automática de
ocorrências, hoje, só acontece no momento do cadastro (RF10/RF11 do
domínio) e, com este refinamento, no momento da reativação (RF06); não
existe ainda nenhum processo de geração mensal automática independente
dessas duas ações. **Ponto em aberto** sobre se essa regra é suficiente ou
se editar a data de início deveria ter alguma restrição adicional — ver
"Pontos em aberto".

**RF09 — Edição independe do estado de pagamento das ocorrências.** É
possível editar uma despesa recorrente mesmo que ela já tenha ocorrências
marcadas como pagas; a edição nunca lê nem altera valor pago, data de
pagamento ou status de pagamento de nenhuma ocorrência (essas continuam
sendo responsabilidade exclusiva das ações descritas em
`painel-mensal-despesas.md`, RF15/RF18).

**RF10 — Edição sem alterações (no-op).** Salvar a edição sem ter mudado
nenhum valor é uma operação válida e bem-sucedida; não deve gerar,
alterar ou remover nenhuma ocorrência, pois nenhum dos gatilhos de RF05,
RF06 ou RF08 foi acionado.

## Funcionalidades necessárias em um endpoint de leitura (para a tela de edição)

**RF11 — Consulta por identificador.** Um endpoint de leitura deve aceitar
o identificador da despesa recorrente e devolver todos os campos editáveis
(RF01) com seus valores atuais, para pré-carregar o formulário de edição.

**RF12 — Identificador inexistente.** Consultar um identificador que não
corresponde a nenhuma despesa recorrente cadastrada deve ser tratado como
"não encontrado", nunca como uma resposta de sucesso com dados vazios.

**RF13 — Nenhuma ocorrência é necessária nesta consulta.** Diferente do
endpoint de listagem do painel mensal, este endpoint de leitura só precisa
dos dados da despesa recorrente em si (RF11) — a tela de edição não exibe
nem manipula as ocorrências já geradas.

## Funcionalidades necessárias em um endpoint de escrita (para salvar a edição)

**RF14 — Atualização dos campos editáveis.** Um endpoint de escrita deve
aceitar os mesmos campos listados em RF01 e aplicar as mesmas validações
de RF02, delegando a validação de regra de negócio ao domínio (Princípio
VI da constituição) — a camada de API/aplicação não deve reimplementar
nenhuma dessas regras.

**RF15 — Identificador inexistente.** Tentar editar um identificador que
não corresponde a nenhuma despesa recorrente deve ser tratado como "não
encontrado", nunca criar uma nova despesa nem retornar sucesso.

**RF16 — Falha de validação.** Uma alteração que viole qualquer regra de
RF02 deve ser rejeitada por completo (nenhum campo é salvo parcialmente),
identificando o(s) campo(s) inválido(s) — mesmo padrão de erro já
estabelecido pelo endpoint de cadastro (`api-despesa-recorrente.md`).

**RF17 — Resposta de sucesso.** Uma edição bem-sucedida deve devolver os
dados atualizados da despesa recorrente (os mesmos campos de RF01), para
que a tela possa confirmar ao usuário o que foi de fato salvo.

**RF18 — Efeitos colaterais de RF05–RF07 acontecem no mesmo endpoint.**
Quando a edição envolve trocar o status de Pausada para Ativa, a geração
da ocorrência da competência atual (RF06/RF07) deve acontecer como parte
da mesma operação de salvar a edição — a tela não deve precisar chamar um
segundo endpoint para isso.

## Edge Cases

**EC01 — Reativar uma despesa cuja data de início ainda não chegou.** Se a
data de início (após a edição, se também alterada) for posterior à
competência atual, reativar (RF06) muda o status para Ativa mas **não**
gera a ocorrência da competência atual — mesma condição de data já usada
no cadastro (domínio RF10/RF11).

**EC02 — Trocar dia de vencimento para um valor fora de 1–31.** Rejeitado
pela mesma validação do cadastro (RF05 do domínio); nenhum dado é salvo.

**EC03 — Apagar o nome (deixar em branco ou só espaços).** Rejeitado pela
mesma validação do cadastro (RF02 do domínio); nenhum dado é salvo.

**EC04 — Trocar valor previsto mensal para zero, negativo ou com mais de
2 casas decimais.** Rejeitado pela mesma validação do cadastro (RF04 do
domínio); nenhum dado é salvo.

**EC05 — Trocar categoria de uma despesa que já tem ocorrência na
competência atual.** A ocorrência já existente mantém a categoria antiga
para sempre (RF03) — só ocorrências futuras (geradas após a edição) usam a
nova categoria; o mesmo vale para o nome e o valor previsto.

**EC06 — Reativar quando, por alguma inconsistência, já existe uma
ocorrência para a competência atual.** Nenhuma ocorrência adicional é
criada (RF07); o caso é defensivo — não deveria acontecer em operação
normal, já que pausar suprime a geração de novas ocorrências (RF05).

**EC07 — Editar uma despesa recorrente enquanto uma de suas ocorrências
está em modo de edição de pagamento no painel mensal** (ver
`painel-mensal-despesas.md`, RF13/RF14). Nenhuma interação entre as duas
telas é definida — são fluxos independentes; **ponto em aberto** se isso
precisa de algum tratamento.

**EC08 — Duas edições concorrentes da mesma despesa** (ex.: duas abas do
navegador). Nenhum mecanismo de concorrência otimista é assumido aqui;
**ponto em aberto**, ver abaixo.

**EC09 — Editar uma despesa que já tem ocorrências pagas.** Permitido
normalmente (RF09); a edição não lê nem altera nada relacionado a
pagamento.

## Critérios de aceitação

**CA01 (RF01–RF02).** Dado um campo editável com um valor inválido
segundo a mesma regra do cadastro, quando o usuário tenta salvar, então a
edição é rejeitada por completo e nenhum campo é alterado (EC02–EC04).

**CA02 (RF03–RF04).** Dado que uma despesa recorrente tem ao menos uma
ocorrência já gerada, quando o nome, a categoria, o valor previsto ou o
dia de vencimento são editados, então essa ocorrência já existente
continua exibindo os valores antigos (nome/categoria/valor/vencimento)
inalterados, e apenas ocorrências geradas depois da edição usam os novos
valores (EC05).

**CA03 (RF05).** Dado que o status é alterado de Ativa para Pausada,
então nenhuma ocorrência nova é gerada a partir desse momento, e nenhuma
ocorrência já existente é alterada ou removida.

**CA04 (RF06–RF07).** Dado que o status é alterado de Pausada para Ativa,
a data de início já começou e não existe ocorrência da despesa para a
competência atual, então uma ocorrência Pendente para a competência atual
é criada com o valor previsto vigente; dado que já existe uma ocorrência
para essa competência, então nenhuma ocorrência adicional é criada (EC06);
dado que a data de início ainda não chegou, então nenhuma ocorrência é
criada (EC01).

**CA05 (RF08).** Dado que a data de início é editada, então nenhuma
ocorrência é criada ou removida retroativamente para competências entre a
data antiga e a nova.

**CA06 (RF09, EC09).** Dado que a despesa recorrente já tem ocorrências
pagas, quando ela é editada, então nenhum dado de pagamento (valor pago,
data de pagamento, status da ocorrência) é lido ou alterado pela edição.

**CA07 (RF10).** Dado que o usuário abre a edição e salva sem alterar
nenhum campo, então a operação é bem-sucedida e nenhuma ocorrência é
criada, alterada ou removida.

**CA08 (RF11–RF13).** Dado um identificador de despesa recorrente
existente, então a consulta de leitura devolve todos os campos editáveis
com os valores atuais; dado um identificador inexistente, então a
consulta é tratada como "não encontrado" (RF12).

**CA09 (RF14–RF17).** Dado um pedido de edição com todos os campos
válidos, então a despesa é atualizada e a resposta contém os dados já
atualizados; dado um pedido de edição para um identificador inexistente,
então a operação é tratada como "não encontrado", sem criar uma nova
despesa (RF15).

## Pontos em aberto

- **Ausência de tela de design.** Toda esta feature foi refinada sem um
  protótipo visual — diferente do cadastro e do painel mensal. Antes de um
  refinamento de frontend (estrutura de componentes Angular, estados
  visuais, mensagens de erro inline) ser escrito, uma tela de design
  deveria existir, para manter o mesmo padrão de precisão já alcançado nos
  outros dois refinamentos.
- **Ponto de entrada da edição não definido.** Sem um design, não está
  definido de onde o usuário aciona "editar" (ex.: um ícone no cartão do
  painel mensal, uma tela de detalhe da despesa, etc.).
- **Geração automática da ocorrência ao reativar (RF06/RF07).** Esta é a
  decisão funcional mais relevante deste documento e não vem de nenhuma
  tela ou regra de domínio já validada — foi assumida por espelhar
  RF10/RF11 do cadastro, mas precisa de confirmação explícita antes da
  implementação (alternativa possível: reativar nunca gera ocorrência
  automaticamente, deixando a geração para um processo mensal futuro
  ainda não existente).
- **Restrições sobre editar a data de início (RF08).** Não está definido
  se deveria haver alguma restrição (ex.: impedir mover a data de início
  para depois de uma competência que já tem ocorrência gerada, o que
  criaria uma inconsistência lógica). A regra atual (RF08) simplesmente
  não faz nada retroativo, mas não impede essa inconsistência.
- **Concorrência (EC08).** Nenhum mecanismo de concorrência otimista
  (ex.: comparação de versão) é assumido; duas edições simultâneas da
  mesma despesa aplicam "o último a salvar vence", sem aviso.
- **Interação com edição de pagamento em andamento (EC07).** Não definido
  se editar uma despesa recorrente deveria ter algum efeito sobre uma
  ocorrência dela que esteja, no mesmo instante, em modo de edição de
  pagamento no painel mensal.
- **Exclusão de despesa recorrente.** Fora de escopo aqui (ver "Escopo"),
  mas é uma feature relacionada e provavelmente próxima na mesma jornada
  de produto (editar/excluir costumam aparecer juntas na mesma tela).
- **Novos métodos de negócio no aggregate `RecurringExpense`** (listados
  em "Pré-requisito") precisam ser modelados em um refinamento de domínio
  dedicado antes de qualquer implementação técnica desta feature.
