import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PainelMensalDespesasService } from './painel-mensal-despesas.service';
import { maskCurrencyDigits } from '../../shared/currency-mask.util';
import { formatEUR } from '../../shared/currency-format.util';
import {
  CATEGORY_COLORS,
  CATEGORY_OPTIONS,
  STATUS_META,
  type PanelOccurrenceResponse,
  type ReferencePeriodResponse,
} from './painel-mensal-despesas.model';

const MONTH_NAMES = [
  'Janeiro',
  'Fevereiro',
  'Março',
  'Abril',
  'Maio',
  'Junho',
  'Julho',
  'Agosto',
  'Setembro',
  'Outubro',
  'Novembro',
  'Dezembro',
];

function dayOfIsoDate(iso: string): number {
  return Number(iso.slice(8, 10));
}

function formatIsoDateAsBR(iso: string): string {
  const [year, month, day] = iso.split('-');
  return `${day}/${month}/${year}`;
}

function formatDateAsIso(date: Date): string {
  const yyyy = date.getFullYear();
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const dd = String(date.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

export interface PanelDisplayItem {
  id: string;
  nome: string;
  categoria: string;
  catColor: string;
  valorPrevistoFmt: string;
  diaLabel: string;
  statusLabel: string;
  statusBg: string;
  statusColor: string;
  paid: boolean;
  notPaid: boolean;
  valorPagoFmt: string;
  dataPagamentoLabel: string;
  valorDiferente: boolean;
  isEditing: boolean;
}

@Component({
  selector: 'app-painel-mensal-despesas',
  imports: [RouterLink],
  templateUrl: './painel-mensal-despesas.component.html',
})
export class PainelMensalDespesasComponent implements OnInit {
  private readonly painelService = inject(PainelMensalDespesasService);

  readonly occurrences = signal<PanelOccurrenceResponse[]>([]);
  readonly referencePeriod = signal<ReferencePeriodResponse | null>(null);
  readonly editingOccurrenceId = signal<string | null>(null);
  readonly draftValor = signal('');
  readonly draftData = signal('');

  readonly monthLabel = computed(() => {
    const period = this.referencePeriod();
    if (!period) return '';
    return `${MONTH_NAMES[period.month - 1]} ${period.year}`;
  });

  readonly displayItems = computed<PanelDisplayItem[]>(() => {
    const editingId = this.editingOccurrenceId();

    return this.occurrences().map((occurrence) => {
      const meta = STATUS_META[occurrence.status];
      const paid = occurrence.status === 'Paid';
      const categoriaLabel = CATEGORY_OPTIONS.find((option) => option.value === occurrence.category)?.label ?? occurrence.category;
      const valorDiferente =
        paid && occurrence.paidAmount != null && Math.abs(occurrence.paidAmount - occurrence.expectedAmount) > 0.001;

      return {
        id: occurrence.id,
        nome: occurrence.name,
        categoria: categoriaLabel,
        catColor: CATEGORY_COLORS[occurrence.category] ?? '#667085',
        valorPrevistoFmt: formatEUR(occurrence.expectedAmount),
        diaLabel: `Dia ${dayOfIsoDate(occurrence.dueDate)}`,
        statusLabel: meta.label,
        statusBg: meta.bg,
        statusColor: meta.color,
        paid,
        notPaid: !paid,
        valorPagoFmt: paid && occurrence.paidAmount != null ? formatEUR(occurrence.paidAmount) : '',
        dataPagamentoLabel: paid && occurrence.paymentDate ? formatIsoDateAsBR(occurrence.paymentDate) : '',
        valorDiferente,
        isEditing: occurrence.id === editingId,
      };
    });
  });

  readonly itemCount = computed(() => this.occurrences().length);

  readonly totalPrevisto = computed(() =>
    this.occurrences().reduce((sum, occurrence) => sum + occurrence.expectedAmount, 0),
  );
  readonly totalPago = computed(() =>
    this.occurrences()
      .filter((occurrence) => occurrence.status === 'Paid')
      .reduce((sum, occurrence) => sum + (occurrence.paidAmount ?? 0), 0),
  );
  readonly totalPendente = computed(() =>
    this.occurrences()
      .filter((occurrence) => occurrence.status !== 'Paid')
      .reduce((sum, occurrence) => sum + occurrence.expectedAmount, 0),
  );

  readonly totalPrevistoFmt = computed(() => formatEUR(this.totalPrevisto()));
  readonly totalPagoFmt = computed(() => formatEUR(this.totalPago()));
  readonly totalPendenteFmt = computed(() => formatEUR(this.totalPendente()));

  readonly vencidasCount = computed(
    () => this.occurrences().filter((occurrence) => occurrence.status === 'Overdue').length,
  );
  readonly venceEmBreveCount = computed(
    () => this.occurrences().filter((occurrence) => occurrence.status === 'DueSoon').length,
  );
  readonly hasVencidas = computed(() => this.vencidasCount() > 0);
  readonly hasVenceEmBreve = computed(() => this.venceEmBreveCount() > 0);

  readonly totalAVencer = computed(() =>
    this.occurrences()
      .filter((occurrence) => occurrence.status === 'DueSoon')
      .reduce((sum, occurrence) => sum + occurrence.expectedAmount, 0),
  );
  readonly totalAVencerFmt = computed(() => formatEUR(this.totalAVencer()));

  ngOnInit(): void {
    this.load();
  }

  mesAnterior(): void {
    this.loadPeriod(this.shiftPeriod(-1));
  }

  proximoMes(): void {
    this.loadPeriod(this.shiftPeriod(1));
  }

  iniciarPagamento(occurrenceId: string): void {
    const occurrence = this.occurrences().find((item) => item.id === occurrenceId);
    if (!occurrence) return;

    this.editingOccurrenceId.set(occurrenceId);
    this.draftValor.set(maskCurrencyDigits(String(Math.round(occurrence.expectedAmount * 100))));
    this.draftData.set(formatDateAsIso(new Date()));
  }

  cancelarEdicao(): void {
    this.editingOccurrenceId.set(null);
    this.draftValor.set('');
    this.draftData.set('');
  }

  confirmarPagamento(occurrenceId: string): void {
    const paymentDateParam = this.draftData() ? formatIsoDateAsBR(this.draftData()) : '';
    this.painelService.markOccurrenceAsPaid(occurrenceId, this.draftValor(), paymentDateParam).subscribe({
      next: (envelope) => {
        if (envelope.success && envelope.data) {
          this.replaceOccurrence(envelope.data.occurrence);
        }
        this.cancelarEdicao();
      },
      error: () => this.cancelarEdicao(),
    });
  }

  desfazerPagamento(occurrenceId: string): void {
    this.painelService.undoOccurrencePayment(occurrenceId).subscribe({
      next: (envelope) => {
        if (envelope.success && envelope.data) {
          this.replaceOccurrence(envelope.data.occurrence);
        }
      },
      error: () => undefined,
    });
  }

  onDraftValorInput(event: Event): void {
    this.draftValor.set(maskCurrencyDigits((event.target as HTMLInputElement).value));
  }

  onDraftDataInput(event: Event): void {
    this.draftData.set((event.target as HTMLInputElement).value);
  }

  private replaceOccurrence(updated: PanelOccurrenceResponse): void {
    this.occurrences.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
  }

  private shiftPeriod(delta: number): { year: number; month: number } {
    const period = this.referencePeriod();
    const totalMonths = (period ? period.year * 12 + (period.month - 1) : 0) + delta;
    return { year: Math.floor(totalMonths / 12), month: (totalMonths % 12) + 1 };
  }

  private loadPeriod(period: { year: number; month: number }): void {
    this.painelService.getMonthlyPanel(String(period.year), String(period.month)).subscribe({
      next: (envelope) => {
        if (envelope.success && envelope.data) {
          this.referencePeriod.set(envelope.data.referencePeriod);
          this.occurrences.set(envelope.data.occurrences);
        }
      },
      error: () => undefined,
    });
  }

  private load(): void {
    this.painelService.getMonthlyPanel().subscribe({
      next: (envelope) => {
        if (envelope.success && envelope.data) {
          this.referencePeriod.set(envelope.data.referencePeriod);
          this.occurrences.set(envelope.data.occurrences);
        }
      },
      error: () => undefined,
    });
  }
}
