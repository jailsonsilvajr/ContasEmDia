# Refinamento Técnico — Data de Fim da Despesa Recorrente

## Origem
Tela de design: **"Nova despesa recorrente"** (`design/Cadastro.dc.html`),
atualizada para incluir o campo **"Data de fim"**, obrigatório, ao lado de
"Data de início" — com nota explicativa ("A despesa deixa de gerar novas
ocorrências após esta data. Vigência máxima de 1 ano a partir do início."),
validação de que a data de fim deve ser posterior à data de início e de que a
vigência não pode ultrapassar 1 ano, e texto de cabeçalho/banner de sucesso
ajustados para não mais descrever a geração como "apenas o mês atual, com os
próximos meses gerados depois" (ver "Decisões desta iteração", item 6).

Diferente dos refinamentos originais de domínio/application/api/infrastructure
(`backend/domain-despesa-recorrente.md` etc.), que precederam a implementação,
este documento parte de um **código já implementado e em produção**
(`backend/Domain`, `backend/Application`, `backend/Api`,
`backend/Infrastructure`, `frontend/.../cadastro-despesa-recorrente`,
`frontend/.../editar-despesa-recorrente`) e descreve o incremento necessário
sobre ele. Por isso referencia caminhos de arquivo e nomes reais já
existentes, e não apenas conceitos.

## Feature
Adicionar um campo obrigatório **"Data de fim"** à despesa recorrente, com
vigência máxima de 1 ano, cobrindo:
1. **Frontend** — novo campo nos formulários de **cadastro e edição**, com
   validação client-side (obrigatória, posterior à data de início, vigência
   máxima de 1 ano).
2. **Backend** — novo campo persistido na despesa recorrente, editável, com
   as mesmas validações de domínio aplicadas tanto no cadastro quanto na
   edição.
3. **Backend — geração antecipada de ocorrências no cadastro.** Ao cadastrar
   uma despesa recorrente (Ativa **ou** Pausada — ver decisão 1), gerar de
   uma vez todas as ocorrências mensais entre a competência atual e a
   competência da data de fim (inclusive), em vez de gerar apenas a
   ocorrência da competência atual.
4. **Backend — ajuste de ocorrências na edição.** Quando a data de fim de
   uma despesa já cadastrada for editada, gerar as ocorrências das novas
   competências cobertas (se a vigência for estendida) ou excluir as
   ocorrências das competências que deixaram de ser cobertas (se a vigência
   for reduzida).

## Decisões desta iteração
Respostas do usuário aos pontos em aberto da versão anterior deste
refinamento, incorporadas em todas as seções abaixo:

1. **Piso de geração confirmado (competência atual), estendido a despesas
   Pausadas.** A geração no cadastro cobre da competência atual até a
   competência da data de fim, **independentemente do status ser Ativa ou
   Pausada** — revisa RF11 do domínio (`domain-despesa-recorrente.md`), que
   hoje suprime totalmente a geração quando o status é Pausada.
2. **Teto de vigência: 1 ano**, contado da data de início. Validado tanto no
   frontend quanto no backend.
3. **Dado existente sem data de fim**: migração define `EndDate = StartDate
   + 1 ano` para as linhas já cadastradas antes desta mudança.
4. **Tela de edição também ganha o campo "Data de fim".** Editar a data de
   fim deve gerar ou excluir as ocorrências necessárias para refletir a nova
   vigência.
5. **A validação "fim posterior ao início" (e a do teto de 1 ano) vive no
   Domain**, dentro do aggregate `RecurringExpense`.
6. **Textos do design ajustados** (`design/Cadastro.dc.html`): subtítulo da
   tela, nota do campo "Data de fim", texto do banner de sucesso e o texto
   auxiliar de status na pré-visualização — todos deixaram de descrever a
   geração como "só o mês atual, próximos meses depois" e passaram a
   descrever a geração de toda a vigência (com o teto de 1 ano).
7. **Redução de vigência com ocorrência paga no intervalo removido:
   bloquear a edição.** Se reduzir a data de fim exigiria excluir alguma
   ocorrência já paga, a edição inteira é rejeitada — nenhum campo é salvo,
   nenhuma ocorrência é excluída ou alterada.

## Escopo
Cobre a tela de cadastro (`Cadastro.dc.html` /
`cadastro-despesa-recorrente.component.ts`), a tela de edição já implementada
(`editar-despesa-recorrente.component.ts`) e as camadas de backend que ambas
consomem (`Domain`, `Application`, `Api`, `Infrastructure`), no que for
necessário para: adicionar o campo "Data de fim" (cadastro e edição), aplicar
o teto de vigência de 1 ano, mudar a regra de geração de ocorrências no
cadastro, e gerar/excluir ocorrências quando a data de fim for editada.

Não cobre:
- Qualquer processo de geração mensal automática independente do momento do
  cadastro/edição (ex.: um job mensal) — permanece fora de escopo.
- Revisão completa de RF06/RF07 de `editar-despesa-recorrente.md`
  (geração de ocorrência ao reativar uma despesa Pausada) — como a decisão 1
  faz o cadastro gerar a vigência inteira **independentemente do status**,
  esse RF06/RF07 passa a ser redundante para despesas cadastradas sob esta
  nova regra (a vigência inteira já existe desde o cadastro); a revisão
  formal desse outro documento fica registrada como consequência em "Pontos
  em aberto", não decidida aqui.
- Suporte a frequências diferentes de mensal — continua fora de escopo.

## Referências
- Domain (estado atual): `backend/Domain/Aggregates/RecurringExpense.cs`,
  `backend/Domain/ValueObjects/CalendarDate.cs`,
  `backend/Domain/ValueObjects/ReferencePeriod.cs`.
- Application (estado atual):
  `backend/Application/UseCases/CreateRecurringExpense/*`,
  `backend/Application/UseCases/UpdateRecurringExpense/*`.
- Api (estado atual): `backend/Api/Requests/CreateRecurringExpenseDataRequest.cs`,
  `backend/Api/Requests/UpdateRecurringExpenseDataRequest.cs`,
  `backend/Api/Responses/*RecurringExpenseData*Response.cs`,
  `backend/Api/Mappings/*RecurringExpenseData*Mapping.cs`,
  `backend/Api/Controllers/RecurringExpensesController.cs`.
- Infrastructure (estado atual):
  `backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs`.
- Frontend (estado atual):
  `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component.ts`,
  `frontend/src/app/features/despesa-recorrente/editar-despesa-recorrente/editar-despesa-recorrente.component.ts`,
  `frontend/src/app/features/despesa-recorrente/despesa-recorrente.model.ts`,
  `frontend/src/app/shared/recurring-expense-form.util.ts`.
- Refinamentos existentes: [`backend/domain-despesa-recorrente.md`](backend/domain-despesa-recorrente.md)
  (RF06, RF10, RF11, RF13, RF15 — revisados aqui),
  [`backend/application-despesa-recorrente.md`](backend/application-despesa-recorrente.md)
  (RF03, RF07), [`frontend/cadastro-despesa-recorrente.md`](frontend/cadastro-despesa-recorrente.md),
  [`editar-despesa-recorrente.md`](../editar-despesa-recorrente.md) (RF01,
  RF06–RF08 — impactados), [`backend/editar-despesa-recorrente.md`](backend/editar-despesa-recorrente.md),
  [`frontend/editar-despesa-recorrente.md`](frontend/editar-despesa-recorrente.md).

## Impacto por camada

### Domain (`backend/Domain`)
- **Novo dado no aggregate `RecurringExpense`**: campo `_endDate`
  (`CalendarDate`, mesmo tipo de `_startDate`), recebido no construtor
  público logo após `startDate`, e exposto por um novo `GetEndDate()`.
- **Duas novas invariantes, checadas no aggregate (decisão 5)**, tanto na
  criação quanto em qualquer alteração posterior de `_startDate` ou
  `_endDate`:
  1. **Fim posterior ao início**: rejeitar quando `endDate <= startDate`
     (mensagem já validada no design: "A data de fim deve ser posterior à
     data de início.").
  2. **Vigência máxima de 1 ano (decisão 2)**: rejeitar quando `endDate` for
     posterior a `startDate` mais 1 ano (ex.: "A vigência não pode
     ultrapassar 1 ano a partir da data de início.").
  Por serem invariantes entre dois campos do mesmo aggregate (não de um
  Value Object isolado), a validação vive no aggregate, não em
  `CalendarDate`.
- **Novo método de negócio `ChangeEndDate`**, análogo a `ChangeStartDate`
  já existente, para suportar a edição (decisão 4) — reaplica as duas
  invariantes acima contra o `_startDate` vigente no momento da chamada.
- **`ChangeStartDate` existente passa a poder falhar**: hoje `ChangeStartDate`
  nunca lança exceção (não há nenhuma invariante cruzada implementada ainda).
  Com as duas novas invariantes, alterar a data de início também precisa
  revalidar contra o `_endDate` vigente (ex.: mover o início para depois do
  fim, ou para um ponto que deixe a vigência maior que 1 ano). Isso é uma
  mudança de comportamento de um método já implementado, não só uma adição.
- **Mudança na geração de ocorrências no cadastro** — substitui
  `GenerateOccurrenceForCurrentPeriodIfDue` (hoje: gera no máximo 1
  ocorrência, a da competência atual, só se Ativa e já em vigor) por uma
  geração que cobre **todas** as competências entre a competência atual e a
  competência da data de fim, **independentemente do status ser Ativa ou
  Pausada** (decisão 1) — ver seção dedicada abaixo.
- **Revisão de RF11 e nuance sobre RF15 do domínio**: RF11
  ("despesa cadastrada como Pausada não gera nenhuma ocorrência") deixa de
  valer para a geração em lote do cadastro — é substituída pela regra acima.
  RF15 ("Pausada não gera **novas** ocorrências enquanto permanecer
  pausada") permanece verdadeira apenas no sentido de que não existe, hoje,
  nenhum processo contínuo de geração após o cadastro — toda a vigência já
  nasce gerada de uma vez, então "gerar novas ocorrências depois" não se
  aplica a nenhum status. Na prática, após esta mudança, o status
  Ativa/Pausada deixa de ter qualquer efeito observável sobre a geração de
  ocorrências desta despesa — seu papel remanescente (se algum) fica para
  outras features (ex.: reativação em `editar-despesa-recorrente.md`, ver
  "Pontos em aberto").
- **Geração/exclusão de ocorrências na edição da data de fim (decisão 4)**:
  novo comportamento associado a `ChangeEndDate` (ou orquestrado pelo Use
  Case de edição a partir dele — ver "Application" abaixo):
  - **Vigência estendida** (nova data de fim posterior à anterior): gerar
    uma ocorrência para cada competência entre a antiga competência-teto
    (exclusive) e a nova (inclusive), com o mesmo cálculo de dia de
    vencimento e valor previsto vigente já usado no cadastro, respeitando a
    não duplicidade por competência (RF13 do domínio).
  - **Vigência reduzida** (nova data de fim anterior à anterior): excluir as
    ocorrências cujas competências ficaram fora da nova vigência (posteriores
    à nova data de fim) — **desde que nenhuma delas esteja paga**.
  - **Bloqueio por ocorrência paga (decisão 7)**: se qualquer ocorrência a
    ser excluída pela redução da vigência já estiver com status Paga, a
    edição inteira deve ser rejeitada antes de qualquer alteração — nem a
    data de fim, nem nenhum outro campo da mesma edição é salvo, e nenhuma
    ocorrência é excluída. O erro deve ser reportado no campo `endDate`
    (mesmo padrão de erro de campo já usado pelas demais regras desta
    feature), permitindo à tela informar que existe pagamento no período que
    seria removido.

### Nova regra de geração de ocorrências no cadastro
Geração no cadastro: uma ocorrência por competência mensal, começando na
**competência atual** (piso) e terminando na **competência da data de fim**
(teto, inclusive), **sempre**, seja a despesa Ativa ou Pausada (decisão 1) —
sujeito a:
- **RF13 (não duplicidade por competência)**: cada competência recebe no
  máximo uma ocorrência, mesmo dentro do loop de geração.
- **Dia de vencimento por competência**: mesmo cálculo já existente
  (`Math.Min(dueDay, DateTime.DaysInMonth(...))`), repetido por mês.
- **Valor previsto por competência**: todas usam o valor previsto mensal
  vigente no momento do cadastro (RF12 do domínio) — sem reajuste futuro
  embutido.
- **Status de cada ocorrência**: todas nascem Pendentes (RF10).
- **Teto de 1 ano (decisão 2)**: como a própria data de fim já é limitada a
  no máximo 1 ano após o início (invariante do Domain), a geração nunca
  produz mais do que ~13 ocorrências em um único cadastro — elimina a
  preocupação de volume ilimitado levantada na versão anterior deste
  documento.
- **Data de início no futuro** (competência atual < competência de início):
  mantém-se sem gerar nenhuma ocorrência ainda no cadastro (mesma condição
  já usada hoje) — ver EC06.
- **Data de início retroativa**: a geração começa na competência atual, não
  na de início — competências passadas entre o início e hoje nunca são
  materializadas como ocorrência (mesma limitação já registrada em
  `editar-despesa-recorrente.md`) — ver EC07.

Isso exige uma capacidade nova no domínio, hoje inexistente: avançar uma
`ReferencePeriod` para a competência seguinte (`ReferencePeriod` hoje só
expõe comparação — `CompareTo`/operadores —, sem uma operação de incremento),
necessária para iterar mês a mês entre o piso e o teto — ver "Pontos em
aberto".

### Application (`backend/Application/UseCases/CreateRecurringExpense`)
- **`CreateRecurringExpenseUseCaseInput`**: novo campo obrigatório `EndDate`
  (`string`, formato `yyyy-MM-dd`, mesmo padrão de `StartDate`).
- **Conversão texto → `CalendarDate`**: mesma lógica já usada para
  `StartDate` (`DateOnly.TryParseExact`); texto malformado ou data
  inexistente vira erro do campo `endDate`.
- **Necessário envolver a construção do aggregate em tratamento de erro**:
  hoje `new RecurringExpense(...)` é chamado sem `try/catch` em
  `CreateRecurringExpenseUseCase.ExecuteAsync`, porque nenhuma regra
  cruzada existe ainda no construtor do aggregate. Com as duas novas
  invariantes (fim > início; vigência ≤ 1 ano) passando a viver no
  construtor (decisão 5), essa chamada **precisa** passar a capturar
  `ArgumentException` e traduzir para um erro do campo `endDate` — do
  contrário, uma violação dessas regras (se escapar da validação de
  formato/parse) resultaria em exceção não tratada (`500`) em vez de `400`.
- **`CreateRecurringExpenseUseCaseOutput` / `OccurrenceData`**: saída de
  sucesso passa a incluir `endDate`; `occurrences` deixa de ter no máximo 1
  item — passa a ter de 1 a ~13 itens (teto de 1 ano), um por competência
  entre a competência atual e a data de fim, **mesmo quando o status
  cadastrado for Pausada** (decisão 1).

### Application (`backend/Application/UseCases/UpdateRecurringExpense`)
- **`UpdateRecurringExpenseUseCaseInput`**: novo campo obrigatório
  `EndDate`, mesma conversão de `StartDate` já usada aqui.
- **Chamada a `ChangeEndDate`** quando o valor recebido diferir do atual —
  mesmo padrão condicional já usado para os demais campos
  (`if (startDate!.GetValue() != recurringExpense.GetStartDate().GetValue())
  recurringExpense.ChangeStartDate(startDate);`).
- **`ChangeStartDate` e `ChangeEndDate` precisam de tratamento de erro que
  hoje não existe**: nenhuma das chamadas a `recurringExpense.ChangeXxx(...)`
  neste Use Case está em `try/catch` — eram sempre bem-sucedidas porque
  nenhum método de alteração validava nada além do próprio Value Object.
  Com as invariantes cruzadas (fim > início; vigência ≤ 1 ano) agora
  aplicadas também em `ChangeStartDate`/`ChangeEndDate` (ver "Domain"),
  ambas as chamadas passam a poder lançar `ArgumentException` e precisam
  ser capturadas e convertidas em erro de campo (`startDate` ou `endDate`,
  conforme o método) antes de `UpdateAsync`.
- **Efeito colateral de gerar/excluir ocorrências (decisão 4)**: ao chamar
  `ChangeEndDate` com um valor que amplia ou reduz a vigência, o efeito
  (gerar novas ocorrências ou excluir as que saíram da vigência) precisa
  acontecer como parte da mesma operação de salvar a edição — mesmo
  princípio já usado por RF18 de `editar-despesa-recorrente.md` para a
  reativação. Onde exatamente a decisão de "gerar N ocorrências" ou
  "excluir M ocorrências" é calculada (dentro do próprio `ChangeEndDate` no
  aggregate, ou orquestrada aqui a partir do novo `_endDate` e do estado
  atual de `_occurrences`) é um detalhe de implementação a decidir, mas o
  aggregate precisa continuar sendo o único lugar que decide **quais**
  ocorrências existem, por ser o dono da invariante de não duplicidade
  (RF13) e agora também da nova invariante de cobertura completa da
  vigência.
- **Bloqueio por ocorrência paga é uma falha de validação de negócio, não
  uma exceção inesperada (decisão 7)**: assim como as demais violações de
  regra desta feature, `ChangeEndDate` recusar a alteração por existir uma
  ocorrência paga no intervalo a remover deve chegar à Application da mesma
  forma que as outras invariantes do aggregate (`ArgumentException`
  capturada e convertida em erro do campo `endDate`), não como uma exceção
  de infraestrutura. Nenhum outro campo da mesma requisição de edição deve
  ser aplicado quando isso ocorrer — a checagem de bloqueio deve acontecer
  antes de `UpdateAsync` persistir qualquer alteração.
- **`UpdateRecurringExpenseUseCaseOutput` / `RecurringExpenseData`**: passa
  a incluir `endDate`.

### Api (`backend/Api`)
- **`CreateRecurringExpenseDataRequest`** e
  **`UpdateRecurringExpenseDataRequest`**: novo campo obrigatório `EndDate`
  (`string?`, com `[Required(ErrorMessage = "Data de fim é obrigatória.")]`),
  mesmo padrão dos demais campos obrigatórios já existentes.
- **`CreateRecurringExpenseDataResponse`** e o response de edição: novo
  campo `EndDate` (`string`, `yyyy-MM-dd`).
- **Mappings** correspondentes passam a mapear o novo campo nos dois
  sentidos, sem lógica adicional além do já existente para `startDate`.
- **Contratos dos endpoints** `POST /api/v1/recurring-expenses` e o `PUT`
  (ou `PATCH`) de edição: request e response ganham `endDate`; erro `400`
  pode trazer um item com `field: "endDate"` para qualquer uma das três
  regras (obrigatória, fim > início, vigência ≤ 1 ano). O restante do
  contrato (envelope `ApiResponse`/`ApiError`, códigos de status)
  permanece inalterado.

### Infrastructure (`backend/Infrastructure`)
- **`RecurringExpenseConfigurations`**: nova propriedade mapeada, no mesmo
  padrão de `_startDate` — coluna `EndDate`, tipo `date`, obrigatória.
- **Nova migração EF Core**: adiciona a coluna `EndDate` (não anulável) à
  tabela `RecurringExpenses`. **Estratégia de backfill decidida (decisão
  3)**: para as linhas já existentes, `EndDate = StartDate + 1 ano` — o
  mesmo valor que corresponde exatamente ao novo teto de vigência (decisão
  2), então nenhuma linha existente nasce violando a invariante nova. A
  migração deve aplicar esse cálculo por linha (não um valor fixo) antes de
  tornar a coluna `NOT NULL`.
- **Nenhuma ocorrência retroativa é gerada pela migração** para as despesas
  já existentes — o backfill só preenche a coluna nova; ele não dispara a
  geração em lote (que só existe no fluxo de cadastro/edição). Continua
  valendo a limitação já registrada: despesas cadastradas antes desta
  mudança seguem tendo apenas as ocorrências que já tinham.
- **Volume de escrita por cadastro/edição**: cada cadastro passa a
  persistir, em uma única transação, a despesa e até ~13 ocorrências
  (teto de 1 ano); cada edição de data de fim pode adicionar ou remover um
  número equivalente de linhas em `Occurrences`. Nenhuma mudança de
  abordagem é necessária (ainda um único `SaveChangesAsync`), apenas um
  aumento real, porém pequeno e limitado, de volume por escrita.

### Frontend — Cadastro (`cadastro-despesa-recorrente.component.ts`)
- **Novo signal** `dataFim: signal<string>('')`, mesma forma de
  `dataInicio` (texto `yyyy-MM-dd`).
- **`touched`** ganha a chave `dataFim: boolean`.
- **Validação client-side**, mesma tripla já modelada no design atualizado
  (`Cadastro.dc.html`, função `validate()`): obrigatória; posterior à data
  de início; vigência máxima de 1 ano
  (`dataFim <= addYearsIso(dataInicio, 1)`, ver função utilitária
  `addYearsIso` adicionada ao protótipo). Reaproveita
  `recurring-expense-form.util.ts`, adicionando essas comparações.
- **`CreateRecurringExpenseRequest`**: novo campo obrigatório
  `endDate: string`.
- **`CreateRecurringExpenseResponse`**: novo campo `endDate: string`;
  `occurrences: OccurrenceResponse[]` deixa de ser 0 ou 1 item — passa a
  ter de 1 a ~13 itens, inclusive quando o status enviado for `Paused`
  (decisão 1) — qualquer lógica de tela que hoje assuma "no máximo 1 item"
  precisa ser revista (não identificada nenhuma no componente atual, que já
  apenas exibe a mensagem de sucesso sem iterar a lista).
- **Textos da tela** (já refletidos em `Cadastro.dc.html`, decisão 6):
  subtítulo, nota do campo "Data de fim" e banner de sucesso passam a
  descrever a geração de toda a vigência (com o teto de 1 ano), não mais
  "o mês atual, os próximos depois".

### Frontend — Edição (`editar-despesa-recorrente.component.ts`)
- Mesma adição de campo/estado/validação do cadastro (`dataFim`, `touched`,
  validação obrigatória + posterior ao início + teto de 1 ano) — hoje esta
  tela já replica o padrão de `dataInicio` (ver
  `frontend/editar-despesa-recorrente.md`), então a adição segue o mesmo
  molde.
- **`UpdateRecurringExpenseRequest`** (`Omit<CreateRecurringExpenseRequest,
  'frequency'>`): ganha `endDate` automaticamente assim que o campo for
  adicionado a `CreateRecurringExpenseRequest`, sem precisar de alteração
  own no tipo derivado.
- **`RecurringExpenseDetailResponse`** (usado para pré-carregar o
  formulário via `GET`): ganha `endDate`, para que a tela de edição exiba o
  valor atual ao abrir.
- Nenhuma indicação visual nova é proposta aqui para "esta edição vai gerar
  N ocorrências novas" ou "vai excluir M ocorrências" antes de salvar — a
  tela apenas envia o novo valor e reflete o resultado após a resposta,
  igual ao padrão já usado para os demais campos editáveis. Uma
  confirmação prévia (ex.: "isso vai excluir 3 ocorrências, confirma?") não
  está no design e fica como possível melhoria futura, não coberta aqui.

## Edge Cases

**EC01 — Data de fim igual à data de início.** Rejeitado (regra exige
"posterior", não "igual ou posterior") — nenhuma despesa é criada/editada.

**EC02 — Data de fim anterior à data de início.** Rejeitado, mesma regra de
EC01.

**EC03 — Data de fim no mesmo mês/ano da data de início.** Válido; gera
exatamente uma ocorrência.

**EC04 — Vigência maior que 1 ano (decisão 2, resolvido).** Rejeitado tanto
no client quanto no backend, com erro no campo `endDate` — substitui a
preocupação de "geração ilimitada" da versão anterior deste documento.

**EC05 — Despesa cadastrada como Pausada (decisão 1, revisado).** Gera
normalmente todas as ocorrências entre a competência atual e a data de fim,
exatamente como uma despesa Ativa — deixa de haver diferença de
comportamento de geração entre os dois status no momento do cadastro.

**EC06 — Data de início no futuro** (competência atual < competência de
início). Nenhuma ocorrência é gerada ainda no cadastro — mesma limitação já
registrada em `editar-despesa-recorrente.md` ("Restrições sobre editar a
data de início"): não existe hoje nenhum processo que gere essas ocorrências
quando a competência de início finalmente chegar.

**EC07 — Data de início retroativa.** A geração começa na competência
atual, não na de início — competências passadas entre o início e hoje nunca
são materializadas como ocorrência.

**EC08 — Editar a data de fim para uma data posterior (estender a
vigência).** Gera as ocorrências das competências recém-cobertas, sem
duplicar nenhuma já existente (RF13).

**EC09 — Editar a data de fim para uma data anterior (reduzir a vigência),
sem nenhuma ocorrência paga no intervalo removido.** Exclui as ocorrências
das competências que saíram da vigência.

**EC10 — Editar a data de fim para uma data anterior, com pelo menos uma
ocorrência já paga no intervalo removido.** A edição inteira é rejeitada
(decisão 7) — nenhum campo é salvo, nenhuma ocorrência é excluída ou
alterada; erro reportado no campo `endDate`.

**EC11 — Editar a data de início de forma que ela ultrapasse a data de fim
atual, ou que a vigência resultante exceda 1 ano.** Rejeitado pela mesma
invariante do Domain (agora também aplicada a `ChangeStartDate`) — nenhum
campo é alterado.

**EC12 — Editar a data de fim para um valor que também violaria o teto de 1
ano frente à data de início atual.** Rejeitado, mesma invariante (decisão 2).

## Critérios de aceitação

**CA01.** Dado o formulário de cadastro ou edição sem a data de fim
preenchida, quando o usuário tenta salvar, então a submissão é bloqueada e o
campo "Data de fim" exibe a mensagem de obrigatoriedade, tanto no
client-side quanto na resposta `400` do backend.

**CA02.** Dado que a data de fim informada não é posterior à data de início
(igual ou anterior), então a submissão é bloqueada com a mensagem "A data de
fim deve ser posterior à data de início." — client-side e backend (EC01,
EC02).

**CA03.** Dado que a vigência entre início e fim ultrapassa 1 ano, então a
submissão é bloqueada com uma mensagem indicando o teto de 1 ano —
client-side e backend (EC04).

**CA04.** Dado um cadastro válido com data de início na competência atual e
data de fim N competências à frente (N ≤ 12), quando a despesa é salva com
status Ativa **ou** Pausada, então são geradas exatamente N+1 ocorrências em
ambos os casos (EC05).

**CA05.** Dado que uma despesa recorrente foi cadastrada com data de fim em
competência C, quando o usuário navega, no painel mensal, até qualquer
competência entre a atual e C, então a ocorrência correspondente já aparece
— sem depender de navegação prévia nem de reativação manual (efeito
observável do bug de navegação de mês diagnosticado anteriormente, resolvido
para despesas cadastradas a partir desta mudança).

**CA06.** Dado que a data de fim de uma despesa existente é editada para uma
data posterior à anterior, então as ocorrências das competências recém-
cobertas são criadas, sem duplicar nenhuma ocorrência já existente (EC08).

**CA07.** Dado que a data de fim de uma despesa existente é editada para uma
data anterior à anterior, e nenhuma ocorrência do intervalo removido está
paga, então essas ocorrências são excluídas (EC09).

**CA08.** Dado que a migração de dados já foi aplicada, quando uma despesa
recorrente cadastrada antes desta mudança é consultada, então sua data de
fim é igual à data de início mais 1 ano.

**CA09 (decisão 7, EC10).** Dado que reduzir a data de fim excluiria pelo
menos uma ocorrência já paga, quando o usuário tenta salvar a edição, então
a operação é rejeitada por completo — nenhum campo é alterado, nenhuma
ocorrência é excluída, e o erro é reportado no campo `endDate`.

## Pontos em aberto

- **Consequência sobre a reativação (RF06/RF07 de
  `editar-despesa-recorrente.md`).** Como o cadastro agora gera a vigência
  inteira independentemente do status (decisão 1), reativar uma despesa
  Pausada deixa de precisar gerar a ocorrência da competência atual (ela já
  existiria desde o cadastro) — a menos que a despesa tenha sido cadastrada
  antes desta mudança (dado migrado, decisão 3) ou que `Pause`/`Reactivate`
  ganhem, no futuro, algum efeito sobre a vigência. Não tratado aqui;
  recomenda-se revisar `editar-despesa-recorrente.md` à parte.
- **Nova capacidade em `ReferencePeriod`.** Iterar competência a competência
  (para gerar a vigência inteira, e para calcular a diferença ao editar a
  data de fim) exige uma operação de avanço hoje inexistente no Value
  Object — decisão de nome/forma fica para a implementação.
- **Onde vive o cálculo de diferença de ocorrências na edição.** Se dentro
  de `ChangeEndDate` no aggregate, ou orquestrado pelo Use Case de edição a
  partir do estado exposto pelo aggregate — ver "Application (Update)".
- **Confirmação prévia na tela de edição antes de excluir ocorrências.** Não
  modelada no design nem pedida pelo usuário; fica como possível melhoria
  futura (ver "Frontend — Edição").
