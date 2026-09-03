import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

import { DespesaRecorrenteService } from '../despesa-recorrente.service';
import { DespesaPreviewComponent } from '../despesa-preview/despesa-preview.component';
import {
  CATEGORY_COLORS,
  CATEGORY_OPTIONS,
  type ApiErrorResponse,
  type CategoryValue,
  type CreateRecurringExpenseRequest,
  type FormStatus,
  type StatusValue,
} from '../despesa-recorrente.model';

type ApiField = 'name' | 'category' | 'monthlyAmount' | 'dueDay' | 'startDate';
const KNOWN_API_FIELDS: ReadonlySet<string> = new Set<ApiField>([
  'name',
  'category',
  'monthlyAmount',
  'dueDay',
  'startDate',
]);

function isApiErrorResponse(value: unknown): value is ApiErrorResponse {
  return (
    typeof value === 'object' &&
    value !== null &&
    Array.isArray((value as { errors?: unknown }).errors)
  );
}

interface ParsedDate {
  year: number;
  month: number;
  day: number;
}

function parseValor(raw: string): number | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;
  const normalized = trimmed.includes(',') ? trimmed.replace(/\./g, '').replace(',', '.') : trimmed;
  if (!/^\d+(\.\d+)?$/.test(normalized)) return null;
  const num = Number(normalized);
  return Number.isFinite(num) ? num : null;
}

function parseDia(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+$/.test(trimmed)) return null;
  return Number(trimmed);
}

function parseDataInicio(raw: string): ParsedDate | null {
  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(raw.trim());
  if (!match) return null;
  const day = Number(match[1]);
  const month = Number(match[2]);
  const year = Number(match[3]);
  const date = new Date(year, month - 1, day);
  if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) return null;
  return { year, month, day };
}

function toIsoDate(parsed: ParsedDate): string {
  const mm = String(parsed.month).padStart(2, '0');
  const dd = String(parsed.day).padStart(2, '0');
  return `${parsed.year}-${mm}-${dd}`;
}

@Component({
  selector: 'app-cadastro-despesa-recorrente',
  imports: [DespesaPreviewComponent],
  templateUrl: './cadastro-despesa-recorrente.component.html',
})
export class CadastroDespesaRecorrenteComponent {
  private readonly despesaRecorrenteService = inject(DespesaRecorrenteService);

  protected readonly categoryOptions = CATEGORY_OPTIONS;

  readonly nome = signal('');
  readonly categoria = signal<CategoryValue>('Housing');
  readonly valor = signal('');
  readonly dia = signal('');
  readonly dataInicio = signal('');
  readonly status = signal<StatusValue>('ativa');
  readonly observacao = signal('');

  readonly formStatus = signal<FormStatus>('idle');
  readonly submitErrorMessage = signal<string | null>(null);
  readonly savedName = signal<string | null>(null);

  readonly touched = signal({ nome: false, valor: false, dia: false, dataInicio: false });
  readonly submitAttempted = signal(false);
  readonly apiFieldErrors = signal<Partial<Record<ApiField, string>>>({});

  readonly nomePreview = computed(() => this.nome().trim() || 'Nome da despesa');
  readonly categoriaLabel = computed(
    () => CATEGORY_OPTIONS.find((o) => o.value === this.categoria())?.label ?? '',
  );
  readonly catColor = computed(() => CATEGORY_COLORS[this.categoria()] ?? '#667085');
  readonly valorNum = computed(() => parseValor(this.valor()));
  readonly valorFmt = computed(() =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(this.valorNum() ?? 0),
  );
  readonly diaLabel = computed(() => {
    const dia = parseDia(this.dia());
    return dia !== null && dia >= 1 && dia <= 31 ? `Dia ${dia}` : 'Dia --';
  });
  readonly statusHelperLabel = computed(() =>
    this.status() === 'ativa'
      ? 'Ativa — a ocorrência deste mês será gerada automaticamente.'
      : 'Pausada — nenhuma ocorrência será gerada até reativar.',
  );
  readonly isAtiva = computed(() => this.status() === 'ativa');
  readonly isPausada = computed(() => this.status() === 'pausada');
  readonly isLoading = computed(() => this.formStatus() === 'loading');
  readonly isSuccess = computed(() => this.formStatus() === 'success');
  readonly isError = computed(() => this.formStatus() === 'error');

  readonly nomeError = computed(() => {
    const trimmed = this.nome().trim();
    if (!trimmed) return 'Nome é obrigatório.';
    if (trimmed.length > 100) return 'Nome deve ter no máximo 100 caracteres.';
    return this.apiFieldErrors().name ?? null;
  });
  readonly valorError = computed(() => {
    const raw = this.valor().trim();
    const decimals = raw.includes(',') ? raw.split(',')[1] : raw.split('.')[1];
    const num = parseValor(raw);
    if (num === null) return 'Valor previsto mensal é obrigatório e deve ser um número válido.';
    if (num <= 0) return 'Valor previsto mensal deve ser maior que zero.';
    if (decimals && decimals.length > 2) return 'Valor previsto mensal deve ter no máximo duas casas decimais.';
    return null;
  });
  readonly diaError = computed(() => {
    const dia = parseDia(this.dia());
    if (dia === null || dia < 1 || dia > 31) return 'Dia de vencimento deve ser um número entre 1 e 31.';
    return null;
  });
  readonly dataInicioError = computed(() => {
    if (parseDataInicio(this.dataInicio()) === null) return 'Data de início deve ser uma data válida (dd/mm/aaaa).';
    return null;
  });
  readonly isFormValid = computed(
    () => !this.nomeError() && !this.valorError() && !this.diaError() && !this.dataInicioError(),
  );

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
    this.valor.set((event.target as HTMLInputElement).value);
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

  protected onSalvar(): void {
    if (this.formStatus() === 'loading') return;

    if (!this.isFormValid()) {
      this.submitAttempted.set(true);
      return;
    }

    const parsedData = parseDataInicio(this.dataInicio());
    const payload: CreateRecurringExpenseRequest = {
      name: this.nome().trim(),
      category: this.categoria(),
      monthlyAmount: parseValor(this.valor()) ?? 0,
      dueDay: parseDia(this.dia()) ?? 0,
      startDate: parsedData ? toIsoDate(parsedData) : '',
      frequency: 'Monthly',
      status: this.status() === 'ativa' ? 'Active' : 'Paused',
      note: this.observacao().trim() ? this.observacao().trim() : null,
    };

    this.formStatus.set('loading');
    this.submitErrorMessage.set(null);
    this.apiFieldErrors.set({});

    this.despesaRecorrenteService.create(payload).subscribe({
      next: () => {
        this.savedName.set(payload.name);
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
          matchedKnownField ? null : 'Não foi possível salvar a despesa. Tente novamente.',
        );
      },
    });
  }

  protected onTentarNovamente(): void {
    this.onSalvar();
  }

  protected onNovaDespesa(): void {
    this.nome.set('');
    this.categoria.set('Housing');
    this.valor.set('');
    this.dia.set('');
    this.dataInicio.set('');
    this.status.set('ativa');
    this.observacao.set('');
    this.formStatus.set('idle');
    this.savedName.set(null);
    this.submitErrorMessage.set(null);
    this.touched.set({ nome: false, valor: false, dia: false, dataInicio: false });
    this.submitAttempted.set(false);
    this.apiFieldErrors.set({});
  }
}
