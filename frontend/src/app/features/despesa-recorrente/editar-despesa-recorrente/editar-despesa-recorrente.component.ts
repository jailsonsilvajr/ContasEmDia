import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { DespesaRecorrenteService } from '../despesa-recorrente.service';
import { DespesaPreviewComponent } from '../despesa-preview/despesa-preview.component';
import { maskCurrencyDigits } from '../../../shared/currency-mask.util';
import { formatEUR } from '../../../shared/currency-format.util';
import {
  parseValor,
  parseDia,
  getNomeError,
  getValorError,
  getDiaError,
  getDataInicioError,
} from '../../../shared/recurring-expense-form.util';
import {
  CATEGORY_COLORS,
  CATEGORY_OPTIONS,
  type ApiErrorResponse,
  type CategoryValue,
  type RecurringExpenseDetailResponse,
  type StatusValue,
  type UpdateRecurringExpenseRequest,
} from '../despesa-recorrente.model';

type ApiField = 'name' | 'category' | 'monthlyAmount' | 'dueDay' | 'startDate';
const KNOWN_API_FIELDS: ReadonlySet<string> = new Set<ApiField>([
  'name',
  'category',
  'monthlyAmount',
  'dueDay',
  'startDate',
]);

type LoadStatus = 'loading' | 'loaded' | 'not-found' | 'error';
type SaveStatus = 'idle' | 'loading' | 'success' | 'error';

interface EditableFields {
  nome: string;
  categoria: CategoryValue;
  valor: string;
  dia: string;
  dataInicio: string;
  status: StatusValue;
  observacao: string;
}

function isApiErrorResponse(value: unknown): value is ApiErrorResponse {
  return (
    typeof value === 'object' &&
    value !== null &&
    Array.isArray((value as { errors?: unknown }).errors)
  );
}

function toValorMasked(amount: number): string {
  return maskCurrencyDigits(String(Math.round(amount * 100)));
}

function toFields(detail: RecurringExpenseDetailResponse): EditableFields {
  return {
    nome: detail.name,
    categoria: detail.category,
    valor: toValorMasked(detail.monthlyAmount),
    dia: String(detail.dueDay),
    dataInicio: detail.startDate,
    status: detail.status === 'Active' ? 'ativa' : 'pausada',
    observacao: detail.note ?? '',
  };
}

@Component({
  selector: 'app-editar-despesa-recorrente',
  imports: [DespesaPreviewComponent, RouterLink],
  templateUrl: './editar-despesa-recorrente.component.html',
})
export class EditarDespesaRecorrenteComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly despesaRecorrenteService = inject(DespesaRecorrenteService);
  private readonly router = inject(Router);

  private readonly despesaId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly categoryOptions = CATEGORY_OPTIONS;

  readonly nome = signal('');
  readonly categoria = signal<CategoryValue>('Housing');
  readonly valor = signal('');
  readonly dia = signal('');
  readonly dataInicio = signal('');
  readonly status = signal<StatusValue>('ativa');
  readonly observacao = signal('');

  readonly loadStatus = signal<LoadStatus>('loading');
  readonly initialSnapshot = signal<EditableFields | null>(null);

  readonly formStatus = signal<SaveStatus>('idle');
  readonly submitErrorMessage = signal<string | null>(null);
  readonly savedName = signal<string | null>(null);

  readonly touched = signal({ nome: false, valor: false, dia: false, dataInicio: false });
  readonly submitAttempted = signal(false);
  readonly apiFieldErrors = signal<Partial<Record<ApiField, string>>>({});

  readonly showExitConfirmDialog = signal(false);

  readonly isLoadingDados = computed(() => this.loadStatus() === 'loading');
  readonly isNotFound = computed(() => this.loadStatus() === 'not-found');
  readonly isLoadError = computed(() => this.loadStatus() === 'error');
  readonly isLoaded = computed(() => this.loadStatus() === 'loaded');

  readonly nomePreview = computed(() => this.nome().trim() || 'Nome da despesa');
  readonly categoriaLabel = computed(
    () => CATEGORY_OPTIONS.find((o) => o.value === this.categoria())?.label ?? '',
  );
  readonly catColor = computed(() => CATEGORY_COLORS[this.categoria()] ?? '#667085');
  readonly valorNum = computed(() => parseValor(this.valor()));
  readonly valorFmt = computed(() => formatEUR(this.valorNum() ?? 0));
  readonly diaLabel = computed(() => {
    const dia = parseDia(this.dia());
    return dia !== null && dia >= 1 && dia <= 31 ? `Dia ${dia}` : 'Dia --';
  });
  readonly statusHelperLabel = computed(() =>
    this.status() === 'ativa'
      ? 'Ativa — se ainda não existir uma ocorrência deste mês, ela é gerada ao salvar.'
      : 'Pausada — ocorrências já geradas não são afetadas; nenhuma nova é criada.',
  );
  readonly isAtiva = computed(() => this.status() === 'ativa');
  readonly isPausada = computed(() => this.status() === 'pausada');
  readonly isSaving = computed(() => this.formStatus() === 'loading');
  readonly isSuccess = computed(() => this.formStatus() === 'success');
  readonly isError = computed(() => this.formStatus() === 'error');

  readonly nomeError = computed(() => getNomeError(this.nome()) ?? this.apiFieldErrors().name ?? null);
  readonly valorError = computed(() => getValorError(this.valor()));
  readonly diaError = computed(() => getDiaError(this.dia()));
  readonly dataInicioError = computed(() => getDataInicioError(this.dataInicio()));
  readonly isFormValid = computed(
    () => !this.nomeError() && !this.valorError() && !this.diaError() && !this.dataInicioError(),
  );

  readonly hasUnsavedData = computed(() => {
    const initial = this.initialSnapshot();
    if (!initial || this.formStatus() === 'success') return false;
    return (
      this.nome() !== initial.nome ||
      this.categoria() !== initial.categoria ||
      this.valor() !== initial.valor ||
      this.dia() !== initial.dia ||
      this.dataInicio() !== initial.dataInicio ||
      this.status() !== initial.status ||
      this.observacao() !== initial.observacao
    );
  });

  readonly showNomeError = computed(
    () =>
      (this.touched().nome || this.submitAttempted() || !!this.apiFieldErrors().name) &&
      this.nomeError() !== null,
  );
  readonly showValorError = computed(
    () => (this.touched().valor || this.submitAttempted()) && this.valorError() !== null,
  );
  readonly showDiaError = computed(
    () => (this.touched().dia || this.submitAttempted()) && this.diaError() !== null,
  );
  readonly showDataInicioError = computed(
    () => (this.touched().dataInicio || this.submitAttempted()) && this.dataInicioError() !== null,
  );

  constructor() {
    this.loadDespesa();
  }

  private loadDespesa(): void {
    this.loadStatus.set('loading');

    this.despesaRecorrenteService.getById(this.despesaId).subscribe({
      next: (envelope) => {
        if (!envelope.success || !envelope.data) {
          this.loadStatus.set('error');
          return;
        }

        const fields = toFields(envelope.data);
        this.nome.set(fields.nome);
        this.categoria.set(fields.categoria);
        this.valor.set(fields.valor);
        this.dia.set(fields.dia);
        this.dataInicio.set(fields.dataInicio);
        this.status.set(fields.status);
        this.observacao.set(fields.observacao);
        this.initialSnapshot.set(fields);
        this.loadStatus.set('loaded');
      },
      error: (err: unknown) => {
        if (err instanceof HttpErrorResponse && err.status === 404) {
          this.loadStatus.set('not-found');
          return;
        }
        this.loadStatus.set('error');
      },
    });
  }

  protected onTentarNovamenteCarregar(): void {
    this.loadDespesa();
  }

  protected onNomeInput(event: Event): void {
    this.nome.set((event.target as HTMLInputElement).value);
  }

  protected onNomeBlur(): void {
    this.touched.update((t) => ({ ...t, nome: true }));
  }

  protected onValorBlur(): void {
    this.touched.update((t) => ({ ...t, valor: true }));
  }

  protected onDiaBlur(): void {
    this.touched.update((t) => ({ ...t, dia: true }));
  }

  protected onDataInicioBlur(): void {
    this.touched.update((t) => ({ ...t, dataInicio: true }));
  }

  protected onCategoriaChange(event: Event): void {
    this.categoria.set((event.target as HTMLSelectElement).value as CategoryValue);
  }

  protected onValorInput(event: Event): void {
    this.valor.set(maskCurrencyDigits((event.target as HTMLInputElement).value));
  }

  protected onDiaInput(event: Event): void {
    this.dia.set((event.target as HTMLInputElement).value);
  }

  protected onDataInicioInput(event: Event): void {
    this.dataInicio.set((event.target as HTMLInputElement).value);
  }

  protected onObservacaoInput(event: Event): void {
    this.observacao.set((event.target as HTMLTextAreaElement).value);
  }

  protected setStatus(value: StatusValue): void {
    this.status.set(value);
  }

  protected onClickVoltar(): void {
    if (this.hasUnsavedData()) {
      this.showExitConfirmDialog.set(true);
      return;
    }
    this.router.navigateByUrl('/');
  }

  protected onCancelExit(): void {
    this.showExitConfirmDialog.set(false);
  }

  protected onConfirmExit(): void {
    this.showExitConfirmDialog.set(false);
    this.router.navigateByUrl('/');
  }

  protected onSalvar(): void {
    if (this.formStatus() === 'loading') return;

    if (!this.isFormValid()) {
      this.submitAttempted.set(true);
      return;
    }

    const payload: UpdateRecurringExpenseRequest = {
      name: this.nome().trim(),
      category: this.categoria(),
      monthlyAmount: parseValor(this.valor()) ?? 0,
      dueDay: parseDia(this.dia()) ?? 0,
      startDate: this.dataInicio(),
      status: this.status() === 'ativa' ? 'Active' : 'Paused',
      note: this.observacao().trim() ? this.observacao().trim() : null,
    };

    this.formStatus.set('loading');
    this.submitErrorMessage.set(null);
    this.apiFieldErrors.set({});

    this.despesaRecorrenteService.update(this.despesaId, payload).subscribe({
      next: (envelope) => {
        if (!envelope.success || !envelope.data) {
          this.formStatus.set('error');
          this.submitErrorMessage.set('Não foi possível salvar as alterações. Tente novamente.');
          return;
        }

        this.savedName.set(envelope.data.name);
        this.initialSnapshot.set(toFields(envelope.data));
        this.formStatus.set('success');
      },
      error: (err: unknown) => {
        this.formStatus.set('error');

        const fieldErrors: Partial<Record<ApiField, string>> = {};
        let matchedKnownField = false;
        if (err instanceof HttpErrorResponse && err.status === 400 && isApiErrorResponse(err.error)) {
          for (const fieldError of err.error.errors) {
            if (KNOWN_API_FIELDS.has(fieldError.field)) {
              fieldErrors[fieldError.field as ApiField] = fieldError.message;
              matchedKnownField = true;
            }
          }
        }
        this.apiFieldErrors.set(fieldErrors);
        this.submitErrorMessage.set(
          matchedKnownField ? null : 'Não foi possível salvar as alterações. Tente novamente.',
        );
      },
    });
  }

  protected onTentarNovamente(): void {
    this.onSalvar();
  }
}
