# Refinamento Funcional — Deletar Despesa Recorrente

## Origem
Tela de design: `design/Main.dc.html` ("Painel Mensal de Despesas"), que já
recebeu o botão de exclusão por ocorrência (ícone de lixeira, ao lado do
ícone "Editar") e uma confirmação inline ("Excluir esta despesa?" com os
botões "Excluir"/"Cancelar") em cada linha da lista. **O design ainda não
cobre a segunda pergunta exigida por este refinamento** — se a exclusão deve
apagar todo o histórico da despesa ou preservar as ocorrências já pagas — o
protótipo hoje só pergunta se deve excluir ou não, sem oferecer essa
escolha. Este documento estende o design existente com essa regra, da mesma
forma que `data-fim-despesa-recorrente.md` estendeu `Editar.dc.html` com uma
regra de vigência que o protótipo, sozinho, não cobria por completo.

## Feature
Refinamento **funcional** (sem código, sem assinaturas de método) da
capacidade de excluir uma despesa recorrente já cadastrada, a partir do
Painel Mensal de Despesas: o botão de exclusão por linha, a confirmação
antes de excluir, a escolha entre excluir todo o histórico ou preservar as
ocorrências já pagas, e o efeito de cada escolha sobre as ocorrências já
geradas e sobre a geração futura de ocorrências.

## Escopo
Cobre exclusivamente:
- O botão de exclusão por ocorrência no Painel Mensal (`design/Main.dc.html`)
  e a despesa recorrente **dona** dessa ocorrência — a exclusão é sempre da
  despesa recorrente inteira, nunca de uma ocorrência isolada (mesma
  distinção que `editar-despesa-recorrente.md` já fez para a edição, RF01
  daquele documento).
- A confirmação exigida antes de excluir (RF02).
- A escolha entre duas abrangências de exclusão — "todo o histórico" ou
  "apenas as pendentes/vencidas" (RF03–RF08) — e o efeito de cada uma sobre
  as ocorrências já geradas.
- O que muda no Painel Mensal depois de uma exclusão bem-sucedida (RF09).
- O que um endpoint de exclusão precisa oferecer, em termos funcionais
  (RF10–RF14), no mesmo nível de detalhe que `editar-despesa-recorrente.md`
  já usou para o `GET`/`PUT` daquela feature.

Não cobre:
- Qualquer alteração na tela de cadastro ou de edição, além do efeito
  colateral já descrito em EC07 (uma despesa excluída não pode mais ser
  aberta para edição).
- Exclusão de uma ocorrência isolada, sem excluir a despesa recorrente —
  não existe tal operação; ver "Escopo" acima.
- Qualquer processo de geração mensal automática de ocorrências ainda não
  implementado — fora de escopo em todos os refinamentos anteriores
  (`editar-despesa-recorrente.md`, `data-fim-despesa-recorrente.md`).
- O contrato técnico completo do endpoint de exclusão (verbo HTTP exato,
  onde vai o parâmetro de abrangência, código de status de sucesso) — este
  documento lista **o que** o endpoint precisa oferecer, não **como**; o
  "como" fica para o refinamento técnico de backend.

## Referências
- Design: `design/Main.dc.html` (botão de exclusão e confirmação inline já
  implementados no protótipo nesta mesma sessão).
- Refinamento e código já implementados do Painel Mensal:
  [`painel-mensal-despesas.md`](painel-mensal-despesas.md) e
  `frontend/src/app/features/painel-mensal-despesas/`.
- Refinamento e código já implementados da edição (ponto de comparação para
  "despesa não encontrada" e para o padrão de confirmação):
  [`editar-despesa-recorrente.md`](editar-despesa-recorrente.md) e
  `frontend/src/app/features/despesa-recorrente/editar-despesa-recorrente/`.
- Refinamento e código já implementados da vigência com data de fim (ponto
  de comparação para "excluir ocorrências não afeta as já pagas"):
  [`data-fim-despesa-recorrente.md`](data-fim-despesa-recorrente.md) e
  `backend/Domain/Aggregates/RecurringExpense.cs` (`ChangeEndDate`).
- Código atual do aggregate: `backend/Domain/Aggregates/RecurringExpense.cs`
  e `backend/Domain/Entities/Occurrence.cs` — em particular, o status
  persistido de uma ocorrência só distingue **Pendente** e **Paga**
  (`OccurrenceStatusType`); "Vencida" e "Vence em breve" são status
  **derivados** calculados a partir da data de vencimento
  (`Occurrence.GetDerivedStatus`), não um valor gravado. Por isso, em todo
  este documento, "pendentes/vencidas" significa **qualquer ocorrência com
  status persistido Pendente**, independentemente de o status derivado
  exibido na tela ser "Pendente", "Vence em breve" ou "Vencida".

## Ponto de entrada e confirmação

**RF01 — Botão de exclusão por linha.** Cada linha de ocorrência no Painel
Mensal ganha um botão de exclusão (ícone de lixeira), ao lado do botão
"Editar" já existente — já implementado em `design/Main.dc.html`. Excluir a
partir de qualquer ocorrência de uma despesa recorrente exclui a despesa
recorrente inteira (todas as suas ocorrências, sujeitas à abrangência
escolhida em RF03), não apenas a ocorrência da competência exibida — mesma
regra de "a ação é sobre a despesa, não sobre a ocorrência" que
`editar-despesa-recorrente.md` já aplicou à edição.

**RF02 — Confirmação obrigatória antes de excluir.** Clicar no botão de
exclusão nunca exclui a despesa imediatamente; o sistema deve antes exigir
uma confirmação explícita do usuário. Cancelar a confirmação não tem nenhum
efeito — nenhuma ocorrência ou dado é alterado.

## Abrangência da exclusão

**RF03 — Duas abrangências possíveis, escolhidas na própria confirmação.**
A confirmação de RF02 deve perguntar qual das duas abrangências o usuário
quer:
1. **Excluir apenas as pendentes/vencidas** — preserva o histórico de
   ocorrências já pagas desta despesa.
2. **Excluir todo o histórico** — remove também as ocorrências já pagas;
   depois desta escolha, nenhum dado desta despesa recorrente continua
   acessível em lugar nenhum do sistema.
Uma das duas opções deve estar pré-selecionada por padrão. **Ponto em
aberto**: qual das duas — ver "Pontos em aberto".

**RF04 — "Excluir apenas as pendentes/vencidas" preserva as ocorrências já
pagas.** Ao escolher esta abrangência, toda ocorrência com status
Pendente (RF/EC — inclui as exibidas como "Vencida" ou "Vence em breve", ver
"Referências") é removida; toda ocorrência com status Paga permanece
inalterada e continua sendo exibida normalmente no Painel Mensal quando o
usuário navegar até a competência em que ela foi paga — mesmo princípio já
estabelecido por `data-fim-despesa-recorrente.md` para a redução de
vigência (reduzir a data de fim nunca remove uma ocorrência já paga).

**RF05 — "Excluir apenas as pendentes/vencidas" impede qualquer ocorrência
futura.** Depois desta exclusão parcial, a despesa recorrente nunca mais
gera nenhuma ocorrência nova, em nenhuma competência — diferente de
"Pausada" (`editar-despesa-recorrente.md`, RF05/RF06), que pode ser
reativada; esta é uma exclusão, um estado definitivo, sem volta.

**RF06 — "Excluir todo o histórico" remove absolutamente tudo.** Ao
escolher esta abrangência, todas as ocorrências desta despesa recorrente
são removidas, pagas ou não, e a própria despesa recorrente deixa de
existir — diferente de RF04, aqui não há exceção para ocorrências pagas.
Esta é uma escolha deliberada do usuário, não uma falha: ao contrário da
redução de vigência (`data-fim-despesa-recorrente.md`, que **bloqueia** a
edição se ela removeria uma ocorrência paga), a exclusão com esta
abrangência remove ocorrências pagas de propósito, porque o próprio usuário
pediu explicitamente para apagar todo o histórico.

**RF07 — Exclusão não tem modo intermediário.** Não existe uma terceira
opção (ex.: "excluir só as vencidas, mas manter as pendentes futuras que
ainda não venceram") — a escolha é binária, entre RF04 e RF06.

**RF08 — Exclusão independe do status atual da despesa (Ativa/Pausada).**
Uma despesa recorrente pode ser excluída em qualquer uma das duas
abrangências independentemente de estar Ativa ou Pausada no momento — o
status Ativa/Pausada não bloqueia nem altera o comportamento da exclusão.

## Efeito no Painel Mensal

**RF09 — Lista atualizada após exclusão bem-sucedida.** Depois de uma
exclusão confirmada (qualquer abrangência), a linha correspondente
desaparece da lista de contas da competência exibida, e os totais do topo
do Painel (previsto, pago, pendente) e os alertas de vencidas/vence-em-breve
são recalculados sem considerar a ocorrência removida — mesmo
comportamento de atualização que `painel-mensal-despesas.md` já define para
"Marcar como paga"/"Desfazer" (RF15/RF18 daquele documento), agora também
para exclusão.

## Funcionalidades necessárias em um endpoint de exclusão

**RF10 — Identificador da despesa recorrente e abrangência como entrada.**
Um endpoint de exclusão deve aceitar o identificador da despesa recorrente
(RF01) e a abrangência escolhida (RF03) como entrada; a abrangência é
obrigatória, sem valor padrão assumido do lado do servidor — o padrão de
UI citado em RF03 é só uma pré-seleção visual, o servidor sempre recebe uma
escolha explícita.

**RF11 — Identificador inexistente.** Excluir um identificador que não
corresponde a nenhuma despesa recorrente cadastrada deve ser tratado como
"não encontrado" — mesmo padrão já usado por `editar-despesa-recorrente.md`
(RF12/RF15 daquele documento).

**RF12 — Abrangência inválida ou ausente.** Um valor de abrangência que não
seja nenhuma das duas opções de RF03 (incluindo ausência do parâmetro) deve
ser rejeitado por completo, sem excluir nada.

**RF13 — Resposta de sucesso confirma o que foi feito.** Uma exclusão
bem-sucedida deve devolver uma confirmação identificando ao menos a despesa
recorrente afetada e a abrangência efetivamente aplicada, para que a tela
possa confirmar ao usuário o que de fato aconteceu.

**RF14 — Excluir uma despesa já excluída.** Tentar excluir (em qualquer
abrangência) uma despesa recorrente que já foi excluída anteriormente por
"Excluir todo o histórico" (RF06 — deixou de existir) ou por "Excluir
apenas as pendentes/vencidas" (RF04 — ainda existe, mas está num estado
definitivo, RF05) deve ser tratado como "não encontrado" (RF11) — uma
despesa já excluída não pode ser excluída de novo nem de forma diferente.

## Edge Cases

**EC01 — Excluir (qualquer abrangência) uma despesa que nunca teve nenhuma
ocorrência paga.** RF04 e RF06 produzem o mesmo resultado prático neste
caso (não há ocorrência paga para preservar ou remover), mas a despesa
recorrente em si só continua existindo depois de RF04, nunca depois de
RF06 (RF05 vs. "deixa de existir").

**EC02 — Excluir (abrangência "apenas pendentes/vencidas") uma despesa que
só tem ocorrências pagas.** Nenhuma ocorrência é removida (RF04), mas a
despesa ainda passa a nunca mais gerar ocorrências novas (RF05) — do ponto
de vista do usuário, a "assinatura" foi cancelada, mas o extrato de
pagamentos já feitos continua intacto.

**EC03 — Excluir (abrangência "apenas pendentes/vencidas") uma despesa cuja
ocorrência da competência atual, exibida no Painel, ainda não foi paga.**
Essa ocorrência específica é removida (RF04) e a linha desaparece do
Painel na competência atual (RF09).

**EC04 — Excluir (abrangência "apenas pendentes/vencidas") uma despesa cuja
ocorrência da competência atual já foi paga.** Essa ocorrência específica
**não** é removida (RF04) — a linha continua aparecendo no Painel na
competência atual, marcada como paga, exatamente como antes da exclusão;
só deixam de existir ocorrências futuras (RF05).

**EC05 — Cancelar a confirmação (RF02).** Nenhum dado é alterado; a linha
continua exatamente como estava.

**EC06 — Excluir uma despesa recorrente enquanto uma de suas ocorrências
está em modo de edição de pagamento no Painel** (ver
`painel-mensal-despesas.md`, RF13/RF14). Nenhuma interação entre as duas
ações é definida aqui — mesmo tipo de caso que
`editar-despesa-recorrente.md` já deixou como ponto em aberto (EC07
daquele documento) para a edição; **ponto em aberto** também para a
exclusão.

**EC07 — Tentar editar (`editar-despesa-recorrente.md`) uma despesa que já
foi excluída.** Deve ser tratado como "não encontrado", do mesmo jeito que
RF14 trata uma segunda exclusão — uma despesa excluída não é mais
encontrável nem para leitura, nem para edição, nem para uma nova exclusão.
Isso é um efeito colateral desta feature sobre a feature de edição já
implementada; ver "Pontos em aberto".

**EC08 — Duas exclusões concorrentes da mesma despesa** (ex.: duas abas do
navegador). Nenhum mecanismo de concorrência otimista é assumido — mesma
ausência já registrada em `editar-despesa-recorrente.md` (EC08 daquele
documento); a segunda tentativa, ao ser processada depois da primeira, cai
em RF14 ("não encontrado").

## Critérios de aceitação

**CA01 (RF01–RF02).** Dado um clique no botão de exclusão de uma
ocorrência, então uma confirmação é exigida antes de qualquer exclusão
efetiva; dado que o usuário cancela essa confirmação, então nenhum dado é
alterado (EC05).

**CA02 (RF03–RF04, EC03–EC04).** Dado que o usuário confirma a exclusão
escolhendo "Excluir apenas as pendentes/vencidas", então toda ocorrência
com status Pendente desta despesa é removida, e toda ocorrência com status
Paga permanece exibida normalmente na competência em que foi paga.

**CA03 (RF05).** Dado que uma despesa foi excluída com "Excluir apenas as
pendentes/vencidas", quando chega uma nova competência, então nenhuma
ocorrência nova é gerada para essa despesa.

**CA04 (RF06).** Dado que o usuário confirma a exclusão escolhendo
"Excluir todo o histórico", então nenhuma ocorrência desta despesa — paga
ou não — continua acessível em nenhuma competência, e a despesa recorrente
em si deixa de existir.

**CA05 (RF09).** Dado uma exclusão bem-sucedida (qualquer abrangência),
então a linha correspondente à ocorrência da competência exibida some do
Painel Mensal, e os totais do topo (previsto/pago/pendente) e as contagens
de vencidas/vence-em-breve refletem a remoção sem precisar de um recarregar
manual da página.

**CA06 (RF11, RF14, EC07).** Dado um identificador que não corresponde a
nenhuma despesa recorrente existente — seja porque nunca existiu, seja
porque já foi excluída anteriormente — então qualquer tentativa de excluir,
editar ou consultar essa despesa é tratada como "não encontrado".

**CA07 (RF12).** Dado um pedido de exclusão sem abrangência informada ou
com uma abrangência que não seja nenhuma das duas de RF03, então a
exclusão é rejeitada por completo, sem remover nenhuma ocorrência.

## Pontos em aberto

- **Qual abrangência vem pré-selecionada por padrão (RF03).** Duas leituras
  razoáveis e opostas: (a) "Excluir apenas as pendentes/vencidas" como
  padrão mais seguro, por preservar histórico financeiro por padrão e exigir
  um passo extra deliberado para apagar tudo; (b) "Excluir todo o
  histórico" como padrão, por ser a leitura mais literal de "excluir uma
  despesa" para a maioria dos usuários. Este documento não decide — ver o
  refinamento de frontend para a proposta de UI, que precisa de uma
  decisão explícita antes da implementação.
- **Rótulos exatos das duas opções na tela.** "Excluir apenas as
  pendentes/vencidas" e "Excluir todo o histórico" são nomes de trabalho
  usados neste documento; a redação final voltada ao usuário fica para o
  refinamento de frontend.
- **Interação com edição de pagamento em andamento (EC06).** Mesma lacuna
  já deixada aberta por `editar-despesa-recorrente.md` (EC07 daquele
  documento) para a edição — não definido se excluir uma despesa deveria
  ter algum efeito sobre uma ocorrência dela que esteja, no mesmo instante,
  em modo de edição de pagamento no Painel.
- **Efeito colateral sobre a edição já implementada (EC07).** Este
  documento assume que uma despesa excluída (qualquer abrangência) deve se
  tornar "não encontrada" também para o endpoint de edição já existente
  (`editar-despesa-recorrente.md`) — o que exige uma pequena adaptação
  nesse código já entregue. Ver o refinamento de backend para como isso é
  proposto.
- **Concorrência (EC08).** Nenhum mecanismo de concorrência otimista é
  assumido, mesma lacuna já registrada em `editar-despesa-recorrente.md`.
- **Desfazer uma exclusão.** Não existe nenhuma operação de "desfazer
  exclusão" em nenhuma das duas abrangências — depois de confirmada, RF05 e
  RF06 são ambos definitivos. Se o produto quiser um "desfazer" (ex.: um
  botão temporário de undo, como muitos apps de e-mail oferecem para
  exclusão), isso é uma feature adicional, não coberta aqui.
- **Auditoria/rastro de quem excluiu e quando.** Fora de escopo — o projeto
  ainda não tem autenticação de usuários (Princípio IV da constituição,
  exceção da fase atual), então não há "quem" registrar.
