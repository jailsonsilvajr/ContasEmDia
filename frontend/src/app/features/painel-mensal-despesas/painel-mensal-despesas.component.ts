import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PainelMensalDespesasService } from './painel-mensal-despesas.service';
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

function formatBRL(value: number): string {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
}

function dayOfIsoDate(iso: string): number {
  return Number(iso.slice(8, 10));
}

function formatIsoDateAsBR(iso: string): string {
  const [year, month, day] = iso.split('-');
  return `${day}/${month}/${year}`;
}

function formatDateAsBR(date: Date): string {
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  return `${day}/${month}/${date.getFullYear()}`;
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
        valorPrevistoFmt: formatBRL(occurrence.expectedAmount),
        diaLabel: `Dia ${dayOfIsoDate(occurrence.dueDate)}`,
        statusLabel: meta.label,
        statusBg: meta.bg,
        statusColor: meta.color,
        paid,
        notPaid: !paid,
        valorPagoFmt: paid && occurrence.paidAmount != null ? formatBRL(occurrence.paidAmount) : '',
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

  readonly totalPrevistoFmt = computed(() => formatBRL(this.totalPrevisto()));
  readonly totalPagoFmt = computed(() => formatBRL(this.totalPago()));
  readonly totalPendenteFmt = computed(() => formatBRL(this.totalPendente()));

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
  readonly totalAVencerFmt = computed(() => formatBRL(this.totalAVencer()));

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
    this.draftValor.set(occurrence.expectedAmount.toFixed(2).replace('.', ','));
    this.draftData.set(formatDateAsBR(new Date()));
  }

  cancelarEdicao(): void {
    this.editingOccurrenceId.set(null);
    this.draftValor.set('');
    this.draftData.set('');
  }

  confirmarPagamento(occurrenceId: string): void {
    this.painelService.markOccurrenceAsPaid(occurrenceId, this.draftValor(), this.draftData()).subscribe({
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
    this.draftValor.set((event.target as HTMLInputElement).value);
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
