# Refinamento Funcional — Painel Mensal de Despesas

## Origem
Tela de design: **"Painel mensal"** (`design/Main.dc.html`).

## Feature
Refinamento **funcional** (sem sugestão de código, assinaturas ou nomes de
classes) da tela que lista as ocorrências de despesas recorrentes de um mês
(competência), permite marcar uma ocorrência como paga ou desfazer um
pagamento, e do endpoint `GET` necessário para prover os dados exibidos
nessa tela.

## Escopo
Cobre exclusivamente a tela "Painel mensal": todas as funcionalidades
observáveis em `design/Main.dc.html` (cabeçalho, banners de alerta, cartões
de resumo, lista de ocorrências, fluxo de marcar/desfazer pagamento) e as
funcionalidades que um endpoint `GET` precisa oferecer para alimentar essa
tela com dados reais, além de edge cases e critérios de aceitação para tudo
isso.

Não cobre (fora de escopo desta iteração):
- O contrato técnico completo do endpoint `GET` (rotas exatas, schemas de
  request/response, códigos de status) — este documento lista **o que** o
  endpoint precisa fornecer, não **como**, no mesmo espírito de
  "não sugerir código" pedido para o restante do documento.
- Os endpoints de escrita usados pelas ações "Marcar como paga" e
  "Desfazer" (seriam `POST`/`PATCH`, não `GET`) — o comportamento dessas
  ações **é** documentado abaixo, por fazerem parte das funcionalidades da
  tela, mas o contrato de API para executá-las fica para um refinamento
  futuro dedicado a elas.
- Cadastro de despesa recorrente (`design/Cadastro.dc.html`), já coberto por
  [`cadastro-despesa-recorrente.md`](frontend/cadastro-despesa-recorrente.md).
- Edição/exclusão de uma despesa recorrente existente, e frequências
  diferentes de mensal (nenhuma delas aparece no design).

## Referências
- Design analisado: `design/Main.dc.html`.
- Modelo de domínio já implementado: [`domain-despesa-recorrente.md`](backend/domain-despesa-recorrente.md)
  e o contrato público correspondente,
  [`specs/001-despesa-recorrente-domain/contracts/domain-public-api.md`](../specs/001-despesa-recorrente-domain/contracts/domain-public-api.md).
- Convenções de API já fixadas pelo primeiro endpoint (`POST`):
  [`api-despesa-recorrente.md`](backend/api-despesa-recorrente.md).
- Refinamento da tela de cadastro (mesmo tipo de documento, para
  comparação de estilo): [`cadastro-despesa-recorrente.md`](frontend/cadastro-despesa-recorrente.md).

## Pré-requisito: lacunas frente ao domínio já implementado
O domínio implementado em `001-despesa-recorrente-domain` **não** cobre
ainda tudo que esta tela precisa. Isso não é resolvido aqui (está fora de
escopo, ver acima), mas precisa ficar registrado antes de qualquer
implementação do endpoint `GET` ou das ações de pagamento:

- `Occurrence.GetStatus()` hoje só existe com valor `Pending`; não há, no
  domínio atual, conceito persistido de "valor pago" nem "data de
  pagamento" — ambos exibidos pelo design (bloco "pago em {{data}}").
  Marcar como paga / desfazer é, portanto, um comportamento de domínio
  ainda não modelado, não apenas uma chamada de API que falta.
- `Occurrence.GetDueDate()` já existe como data completa (`CalendarDate`),
  não apenas "dia do mês" — isso é relevante porque o protótipo do design
  (ver "Cálculo de status derivado" abaixo) compara apenas o número do dia,
  o que é uma simplificação do protótipo, não a forma real do dado.
- `IRecurringExpenseRepository` hoje só expõe `GetByIdAsync` e
  `GetActiveAsync` — nenhum método permite buscar ocorrências por
  competência (mês/ano) através de todas as despesas recorrentes, ativas ou
  pausadas. Isso é exatamente o dado que a listagem do painel mensal
  precisa; um endpoint `GET` para esta tela depende de uma capacidade de
  consulta que ainda não existe no repositório.

## Funcionalidades da tela (`design/Main.dc.html`)

### Cabeçalho

**RF01 — Identificação do período exibido.** A tela mostra sempre um único
mês/competência por vez ("Agosto 2026" no protótipo) e o mesmo período é
repetido no título da lista ("Contas de Agosto 2026").

**RF02 — Navegação entre meses (não funcional no protótipo atual).** O
cabeçalho tem setas de "mês anterior"/"próximo mês", mas nenhuma delas tem
ação associada no design (`cursor: default`, sem `onClick`) — hoje são
apenas visuais. Funcionalmente, a existência dessas setas indica que a tela
deve suportar consultar meses diferentes do atual, mas o design não define
o comportamento real dessa troca (ver "Pontos em aberto").

**RF03 — Botão "Nova despesa" (fora de escopo aqui).** Também sem `onClick`
no design; presumivelmente navega para a tela de cadastro
(`design/Cadastro.dc.html`), já coberta por outro refinamento. Nenhum
comportamento é definido aqui.

### Banners de alerta (condicionais)

**RF04 — Banner de contas vencidas.** Exibido somente quando existe ao
menos uma ocorrência com status derivado "Vencida" no período. Mostra a
contagem de ocorrências vencidas.

**RF05 — Banner de contas vencendo em breve.** Exibido somente quando
existe ao menos uma ocorrência com status derivado "Vence em breve" no
período. Mostra a contagem dessas ocorrências e a soma do valor previsto
apenas delas ("Total a vencer no período").

Os dois banners são independentes entre si e podem aparecer juntos, um só,
ou nenhum.

### Cartões de resumo (sempre visíveis)

**RF06 — Total previsto no mês.** Soma do valor previsto de **todas** as
ocorrências do período, pagas ou não.

**RF07 — Total pago.** Soma do valor efetivamente pago das ocorrências já
pagas no período (usa o valor pago, não o valor previsto).

**RF08 — Total pendente.** Soma do valor previsto apenas das ocorrências
ainda não pagas no período.

### Lista de ocorrências

**RF09 — Cabeçalho da lista.** Título com o período por extenso ("Contas de
{mês} {ano}") e contador do total de ocorrências listadas.

**RF10 — Um cartão por ocorrência**, mostrando: indicador de cor por
categoria, nome da despesa, categoria, valor previsto formatado em BRL, dia
de vencimento ("Dia N") e um selo de status.

**RF11 — Cálculo de status derivado.** Cada ocorrência tem um de quatro
status, calculados (não armazenados) a partir de `paga`/`não paga` e da
comparação entre o vencimento e "hoje":
- **Paga** — ocorrência já marcada como paga (tem prioridade sobre as
  demais condições).
- **Vencida** — não paga e o vencimento já passou.
- **Vence em breve** — não paga, vencimento ainda não passou, e falta no
  máximo 7 dias para vencer (janela inclusiva nas duas pontas: vence hoje
  conta como "vence em breve", vence em exatamente 7 dias também conta).
- **Pendente** — não paga e faltam mais de 7 dias para o vencimento.

No protótipo, essa comparação é feita usando apenas o **número do dia do
mês** (`dia`) contra um "hoje" também expresso só como número de dia
(`HOJE_DEMO = 18`), o que só funciona porque o protótipo simula um único
mês fixo. Isso é uma simplificação do protótipo — ver "Edge Cases" para o
que muda ao usar datas completas e meses/competências reais.

### Ações por ocorrência

**RF12 — "Marcar como paga" (só aparece se não paga e não em edição).**
Abre um modo de edição inline para aquela ocorrência, pré-preenchendo o
valor a pagar com o valor previsto e a data de pagamento com a data de
hoje.

**RF13 — Edição inline do pagamento.** Enquanto em edição, a ocorrência
mostra dois campos editáveis (valor a pagar, em reais; data de pagamento,
`dd/mm/aaaa`) e dois botões, "Confirmar" e "Cancelar".

**RF14 — Edição exclusiva (uma ocorrência por vez).** Só existe um "modo de
edição" ativo por vez na tela inteira. Iniciar a edição de uma ocorrência
enquanto outra já está em edição substitui qual ocorrência está em edição;
a anterior volta ao estado "não paga" normal, e o que havia sido digitado
nela é descartado.

**RF15 — Confirmar pagamento.** Ao confirmar: o valor digitado é
interpretado como número (vírgula como separador decimal); se o texto
digitado não for um número válido, o valor previsto da ocorrência é usado
no lugar; se a data ficar vazia, a data de hoje é usada no lugar. A
ocorrência passa a "paga", com o valor e a data resultantes, e a tela sai
do modo de edição.

**RF16 — Cancelar edição.** Sai do modo de edição sem alterar nenhum dado
da ocorrência.

**RF17 — Exibição de ocorrência paga (não em edição).** Mostra o valor
pago, a data de pagamento ("pago em {data}") e um link "Desfazer". Se o
valor pago for diferente do valor previsto (mesmo que por poucos
centavos), o valor pago é destacado em outra cor com o aviso "diferente do
previsto"; se for igual, é destacado normalmente (sem aviso).

**RF18 — Desfazer pagamento.** Reverte a ocorrência para "não paga",
apagando o valor pago e a data de pagamento registrados. Não existe
confirmação nem "desfazer o desfazer" — a ação é imediata.

## Funcionalidades necessárias em um endpoint `GET`

Este é o recorte apenas de **leitura**: o que o endpoint `GET` desta tela
precisa entregar para que as funcionalidades RF01–RF18 acima possam ser
implementadas com dados reais. Não inclui os endpoints de escrita usados
por RF15/RF18 (fora de escopo, ver "Escopo").

**RF19 — Consulta por competência (mês/ano).** O endpoint deve aceitar um
parâmetro de período (mês e ano) e retornar os dados referentes a essa
competência — não apenas ao "mês atual" fixo do protótipo, já que RF02
indica que a tela é feita para navegar entre meses. Quando nenhum período é
informado, o padrão deve ser a competência atual.

**RF20 — Escopo das ocorrências retornadas.** Devem ser incluídas todas as
ocorrências já existentes para a competência pedida, independentemente do
status atual (Ativa/Pausada) da despesa recorrente à qual pertencem — uma
despesa pausada depois de gerar uma ocorrência não deve fazer essa
ocorrência desaparecer da listagem de um mês passado (consistente com a
regra de domínio já registrada em `domain-despesa-recorrente.md`, RF15).

**RF21 — Dados retornados por ocorrência.** Para cada ocorrência da
competência: identificador, nome da despesa, categoria, valor previsto,
data de vencimento completa (não apenas o número do dia — ver "Pré-requisito"
acima), status persistido (paga/não paga) e, quando paga, o valor
efetivamente pago e a data de pagamento.

**RF22 — Status derivado calculado no backend.** "Vencida" e "Vence em
breve" (RF11) devem ser calculados no servidor, usando a data completa de
vencimento de cada ocorrência e a data atual do servidor — não o número
"dia do mês" isolado do protótipo. Calcular isso no backend evita duplicar
a regra de negócio no frontend e evita a limitação descrita em "Edge
Cases" (comparação que quebra na virada de mês).

**RF23 — Totais e contagens ficam por conta do cliente.** Como o endpoint
já entrega o status derivado pronto por ocorrência (RF22), os totais e
contagens do topo da tela (RF04–RF08) são simples somas/contagens sobre a
lista já classificada — não é necessário que o backend também calcule e
devolva esses agregados prontos; o cliente pode derivá-los da lista, como
já faz o protótipo hoje. (Ver "Pontos em aberto" para o caso de a lista vir
paginada, o que invalidaria essa premissa.)

**RF24 — Nenhuma categoria adicional exposta.** Assim como no cadastro
(`cadastro-despesa-recorrente.md`), o conjunto de categorias é fechado e já
conhecido pelo cliente; o endpoint não precisa expor um catálogo de
categorias — apenas o valor da categoria de cada ocorrência.

## Edge Cases

**EC01 — Competência sem nenhuma ocorrência.** Lista vazia, contador "0
contas", os três cartões de resumo zerados (R$ 0,00) e nenhum dos dois
banners aparece.

**EC02 — Todas as ocorrências da competência já pagas.** "Total pendente" =
R$ 0,00, e nenhuma ocorrência conta como "Vencida" ou "Vence em breve" —
esses dois status só se aplicam a ocorrências não pagas, mesmo que a data
de vencimento já tenha passado.

**EC03 — Ocorrência vencendo exatamente hoje.** Não é "Vencida" (a regra é
estritamente "vencimento já passou"); é "Vence em breve" (0 dias está
dentro da janela de 0 a 7 dias).

**EC04 — Ocorrência vencendo em exatamente 7 dias.** Ainda conta como
"Vence em breve" (limite superior da janela é inclusivo).

**EC05 — Ocorrência vencendo em 8 dias ou mais.** É "Pendente" — não entra
em nenhum dos dois banners nem no "Total a vencer no período".

**EC06 — Vencimento perto da virada de mês.** Uma competência que inclua,
por exemplo, os últimos dias do mês corrente: comparar apenas "número do
dia" (como faz o protótipo) contra o "dia de hoje" pode classificar
errado uma ocorrência cujo vencimento é no mês seguinte. O endpoint deve
comparar datas completas (RF22), não apenas o número do dia — este edge
case é a razão funcional para essa exigência.

**EC07 — Valor pago diferente do valor previsto.** Tanto para mais quanto
para menos: a ocorrência exibe o aviso "diferente do previsto" (RF17), mas
o "Total pago" soma sempre o valor realmente pago, nunca o previsto.

**EC08 — Confirmar pagamento com valor em branco ou não numérico.** O
protótipo substitui silenciosamente pelo valor previsto, sem avisar o
usuário do que aconteceu (ver "Pontos em aberto" — recomenda-se validar em
vez de substituir silenciosamente na implementação real).

**EC09 — Confirmar pagamento com data em branco.** O protótipo usa a data
de hoje como padrão; numa implementação real, "hoje" deve vir da data atual
do servidor, não de um valor fixo como no protótipo (`18/08/2026`).

**EC10 — Trocar de ocorrência em edição sem confirmar nem cancelar a
anterior.** Só uma ocorrência pode estar em edição por vez (RF14); a
anterior volta ao estado normal e o rascunho digitado nela é perdido sem
aviso.

**EC11 — Desfazer pagamento de uma ocorrência com valor pago divergente.**
Volta a "não paga"; o valor pago divergente e a data de pagamento são
descartados — não há histórico de pagamentos desfeitos.

**EC12 — Despesa recorrente pausada após a ocorrência do mês já existir.**
A ocorrência permanece normalmente na listagem do painel (RF20); só deixa
de haver **novas** ocorrências para competências futuras enquanto a
despesa continuar pausada.

**EC13 — Categoria não reconhecida.** Não deveria acontecer, já que o
conjunto de categorias é fechado (RF24), mas o indicador de cor da
ocorrência cai num cinza neutro padrão quando a categoria não é uma das
conhecidas pelo cliente.

**EC14 — Duas ocorrências com o mesmo dia de vencimento.** Ambas aparecem
normalmente na lista, sem nenhum agrupamento ou tratamento especial.

**EC15 — Competência anterior à data de início da despesa recorrente, ou
despesa cadastrada como Pausada desde o início.** Nenhuma ocorrência é
gerada para essa competência (regra já definida em
`domain-despesa-recorrente.md`, RF10/RF11); o efeito no painel é o mesmo do
EC01 para essa despesa específica — ela simplesmente não aparece na lista
daquele mês.

**EC16 — Parâmetro de período inválido na consulta ao endpoint** (ex.: mês
fora de 1–12, ano ausente ou não numérico). O design não trata isso, por
ser uma tela sem estado de erro modelado para a listagem em si — comportamento
de erro do endpoint fica como ponto em aberto (ver abaixo).

## Critérios de aceitação

**CA01 (RF01/RF09).** Dado que a tela carregou uma competência com N
ocorrências, então o título da lista mostra o mês/ano dessa competência e o
contador mostra exatamente N.

**CA02 (RF04).** Dado que existe ao menos 1 ocorrência com status "Vencida"
na competência exibida, então o banner de vencidas aparece com a contagem
correta; dado que não existe nenhuma, então o banner não aparece (EC01).

**CA03 (RF05).** Dado que existe ao menos 1 ocorrência com status "Vence em
breve" na competência exibida, então o banner correspondente aparece com a
contagem e a soma corretas dos valores previstos apenas dessas ocorrências;
dado que não existe nenhuma, então o banner não aparece.

**CA04 (RF06–RF08).** Dado o conjunto de ocorrências da competência, então
"Total previsto" = soma de todos os valores previstos, "Total pago" = soma
dos valores efetivamente pagos das ocorrências pagas, e "Total pendente" =
soma dos valores previstos das ocorrências não pagas — e
`Total previsto = Total pago + Total pendente` só é verdade quando nenhuma
ocorrência paga tem valor divergente do previsto (EC07 quebra essa
igualdade de propósito).

**CA05 (RF11, EC03–EC06).** Dado o dia de vencimento de uma ocorrência não
paga e a data atual, então: vencimento no passado → "Vencida"; vencimento
hoje ou em até 7 dias no futuro → "Vence em breve"; vencimento em mais de 7
dias → "Pendente"; ocorrência já paga → "Paga", independentemente da data
de vencimento (EC02).

**CA06 (RF12–RF14).** Dado que o usuário clica em "Marcar como paga" numa
ocorrência não paga, então essa ocorrência entra em modo de edição com
valor pré-preenchido igual ao valor previsto e data pré-preenchida igual a
hoje; dado que o usuário então clica em "Marcar como paga" de outra
ocorrência sem confirmar/cancelar a primeira, então a primeira volta ao
estado normal (não editando, não paga) e a segunda entra em edição (EC10).

**CA07 (RF15, EC08–EC09).** Dado que o usuário confirma o pagamento com um
valor numérico válido e uma data preenchida, então a ocorrência passa a
"Paga" com exatamente esse valor e essa data; dado que o valor está vazio
ou não é numérico, então o valor previsto é usado no lugar; dado que a data
está vazia, então a data de hoje é usada no lugar.

**CA08 (RF16).** Dado que o usuário clica em "Cancelar" durante a edição de
uma ocorrência, então nenhum dado da ocorrência é alterado e ela volta ao
estado anterior à edição.

**CA09 (RF17, EC07).** Dado que uma ocorrência está paga, então ela mostra
o valor pago e "pago em {data}"; dado que o valor pago é diferente do valor
previsto, então aparece também o aviso "diferente do previsto" com destaque
visual distinto do caso em que os valores coincidem.

**CA10 (RF18, EC11).** Dado que o usuário clica em "Desfazer" numa
ocorrência paga, então ela volta a "não paga", sem valor pago nem data de
pagamento associados, e volta a ser elegível para "Marcar como paga"
novamente.

**CA11 (RF19–RF20, EC12, EC15).** Dado um pedido `GET` para uma
competência específica, então a resposta contém exatamente as ocorrências
já geradas para aquela competência — de despesas ativas ou pausadas —,
excluindo qualquer despesa que ainda não tinha começado ou que nunca esteve
ativa naquela competência.

**CA12 (RF21–RF22, EC06).** Dado que uma ocorrência tem data de vencimento
completa, então o status derivado retornado pelo endpoint ("Vencida"/"Vence
em breve"/"Pendente"/"Paga") é calculado a partir dessa data completa
comparada à data atual do servidor — nunca apenas pelo número do dia do
mês isolado da competência.

**CA13 (RF23).** Dado que o endpoint já retorna o status derivado pronto
por ocorrência, então o cliente consegue calcular os três cartões de
resumo (RF06–RF08) e os dois banners (RF04–RF05) somando/contando apenas os
itens já recebidos, sem precisar reimplementar a regra de "quantos dias
faltam para vencer".

**CA14 (RF19, EC16).** Dado um pedido `GET` sem parâmetro de período, então
a competência atual é usada como padrão; dado um parâmetro de período em
formato inválido, então o endpoint não retorna dados como se fosse uma
competência válida (comportamento exato de erro é um ponto em aberto, mas
o requisito é: nunca silenciosamente tratar um período inválido como
válido).

## Pontos em aberto

- **Comportamento real da navegação entre meses (RF02).** O design só
  modela o estado visual das setas, sem `onClick`. Fica em aberto se a
  troca de mês deve recarregar a lista via um novo `GET` (mais provável,
  dado RF19) ou se existe algum limite de quantos meses para trás/frente
  podem ser consultados.
- **Validação do valor e da data ao confirmar pagamento (EC08–EC09).** O
  protótipo substitui silenciosamente por um valor padrão em vez de
  bloquear a confirmação ou mostrar um erro inline (padrão diferente do já
  adotado no cadastro, que tem validação inline por campo — ver
  `cadastro-despesa-recorrente.md`). Recomenda-se decidir, antes da
  implementação real, se o pagamento deve seguir o mesmo padrão de
  validação inline do cadastro em vez de substituição silenciosa.
- **Paginação e ordenação da listagem (RF19–RF21).** O design não modela
  nem paginação nem uma ordem explícita da lista (a ordem no protótipo é a
  ordem de inserção do array de exemplo). Se a lista vier paginada no
  futuro, o RF23 (totais calculados no cliente a partir da lista recebida)
  deixa de valer, e os agregados precisariam ser calculados pelo backend.
- **Lacunas de domínio listadas em "Pré-requisito"** (valor pago/data de
  pagamento não modelados na entidade `Occurrence`, e ausência de um método
  de repositório para buscar ocorrências por competência através de todas
  as despesas recorrentes) precisam ser resolvidas — em um refinamento de
  domínio à parte — antes de qualquer refinamento técnico do endpoint `GET`
  ou dos endpoints de pagamento.
- **Truncamento de nomes muito longos.** O design não trata overflow do
  nome da despesa no cartão da ocorrência; não modelado nem aqui nem no
  protótipo.
- **Comportamento de erro do endpoint `GET` para período inválido
  (EC16/CA14).** O código de status e o formato exato do erro ficam para o
  refinamento técnico do endpoint (fora de escopo aqui — ver "Escopo"), mas
  o requisito funcional de nunca tratar um período inválido como válido
  está registrado em CA14.
