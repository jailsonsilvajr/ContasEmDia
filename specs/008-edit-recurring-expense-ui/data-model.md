# Data Model: Editar Despesa Recorrente (Tela)

Esta feature não introduz nenhuma entidade de domínio nova (isso pertence
a `007-edit-recurring-expense`, backend). O que segue é o modelo de
**dados client-side** — estado do componente e formas de request/response
que o frontend consome — derivado da spec 008 e do design de referência
(`design/Editar.dc.html`).

## Estado da tela (`EditarDespesaRecorrenteComponent`)

| Campo | Tipo | Origem / regra |
|---|---|---|
| `nome` | `signal<string>` | Carregado do `GET`; editável (FR-001); validação: não vazio após trim |
| `categoria` | `signal<CategoryValue>` | Carregado do `GET`; editável (FR-001); validação: um dos 5 valores fechados |
| `valor` | `signal<string>` | Carregado do `GET` (já formatado); editável (FR-001); validação: > 0, até 2 casas decimais |
| `dia` | `signal<string>` | Carregado do `GET`; editável (FR-001); validação: inteiro 1-31 |
| `dataInicio` | `signal<string>` | Carregado do `GET`; editável (FR-001); validação: data válida |
| `status` | `signal<'ativa' \| 'pausada'>` | Carregado do `GET`; editável (FR-001) |
| `observacao` | `signal<string>` | Carregado do `GET`; editável (FR-001); sem validação |
| `loadStatus` | `signal<'loading' \| 'loaded' \| 'not-found' \| 'error'>` | Controla o carregamento inicial (FR-003–FR-006); inicia em `'loading'` |
| `formStatus` | `signal<'idle' \| 'loading' \| 'success' \| 'error'>` | Controla o salvamento (FR-008–FR-012); inicia em `'idle'` só após `loadStatus() === 'loaded'` |
| `initialSnapshot` | `signal<CampoValores \| null>` | Cópia dos 7 campos acima capturada no momento do carregamento; usada para a comparação por valor de FR-013/FR-014 (Decisão 4 de `research.md`); atualizada para os novos valores após um salvamento bem-sucedido |
| `touched` | `signal<{nome, valor, dia, dataInicio: boolean}>` | Mesma estrutura já usada no cadastro; controla quando revelar erro por campo |
| `submitAttempted` | `signal<boolean>` | Mesmo papel do cadastro |
| `apiFieldErrors` | `signal<Partial<Record<ApiField, string>>>` | Erros `400` por campo devolvidos pelo `PUT` (FR-012) |
| `showExitConfirm` | `signal<boolean>` | Controla o diálogo de confirmação de saída (FR-013/FR-014) |

### Computeds (derivados, sem estado próprio)

- `hasUnsavedData`: `true` se qualquer um dos 7 campos editáveis difere do
  valor correspondente em `initialSnapshot` (comparação por valor,
  Decisão 4); `false` se `initialSnapshot` ainda não foi carregado.
- `nomeError` / `valorError` / `diaError` / `dataInicioError`: idênticos
  em regra ao cadastro (FR-007).
- `isFormValid`: `true` somente se os quatro computeds acima forem `null`.
- `nomePreview` / `catColor` / `valorFmt` / `diaLabel`: idênticos ao
  cadastro, alimentam `DespesaPreviewComponent` sem nenhuma mudança nele.
- `statusHelperLabel`: texto persistente junto ao controle de status
  (FR-016, clarificado) — variante para `'ativa'` menciona a possível
  geração automática da ocorrência do mês corrente ao salvar.

### Transições de estado relevantes

```text
loadStatus:  'loading' --(GET 200)--> 'loaded'
             'loading' --(GET 404)--> 'not-found'
             'loading' --(GET falha rede/5xx)--> 'error'
             'error'   --(retry)--> 'loading'

formStatus (só alcançável a partir de loadStatus === 'loaded'):
             'idle'    --(salvar, válido)--> 'loading'
             'loading' --(PUT 200)--> 'success'
             'loading' --(PUT 400/falha)--> 'error'
             'error'   --(tentar novamente)--> 'loading'
```

Não há transição de volta de `'success'`/`'not-found'` para o formulário
editável nesta tela (FR-010: a única ação oferecida no sucesso é voltar
ao painel mensal — não existe "editar outra despesa" aqui).

## Formas de request/response consumidas (contrato já definido por `007-edit-recurring-expense`)

Ver [`contracts/edit-screen-contract.md`](./contracts/edit-screen-contract.md)
para o recorte completo consumido por esta tela; resumo dos tipos
client-side novos em `despesa-recorrente.model.ts`:

| Tipo | Uso |
|---|---|
| `RecurringExpenseDetailResponse` | Resposta do `GET /api/v1/recurring-expenses/{id}` — os 7 campos editáveis + `frequency` (sempre `"Monthly"`, exibido mas não editável) |
| `UpdateRecurringExpenseRequest` | Corpo do `PUT /api/v1/recurring-expenses/{id}` — os 7 campos editáveis, mesmo shape de `CreateRecurringExpenseRequest` sem `frequency` |
| `UpdateRecurringExpenseResponse` | Resposta de sucesso do `PUT` — mesmo shape de `RecurringExpenseDetailResponse` |

Erros de ambos os endpoints reaproveitam `ApiErrorResponse`/`FieldError`,
já existentes em `despesa-recorrente.model.ts`.

## Item do painel mensal (dado consumido, não pertence a esta feature)

`PanelOccurrenceResponse` (`painel-mensal-despesas.model.ts`) precisa
ganhar um campo `recurringExpenseId: string` — ver "Cross-Feature
Dependency" em `plan.md` e Decisão 5 em `research.md`. Até essa mudança
ser feita no backend, o ícone de edição por item não tem valor real para
usar como `:id` da rota.
