import { CATEGORY_COLORS, CATEGORY_OPTIONS, type ApiEnvelope, type CategoryValue } from '../despesa-recorrente/despesa-recorrente.model';

export { CATEGORY_COLORS, CATEGORY_OPTIONS };
export type { ApiEnvelope, CategoryValue };

export type DerivedStatusValue = 'Paid' | 'Overdue' | 'DueSoon' | 'Pending';

export interface StatusMeta {
  label: string;
  bg: string;
  color: string;
}

export const STATUS_META: Record<DerivedStatusValue, StatusMeta> = {
  Paid: { label: 'Paga', bg: '#E7F6EF', color: '#0F7B4E' },
  Overdue: { label: 'Vencida', bg: '#FDECEA', color: '#C0362C' },
  DueSoon: { label: 'Vence em breve', bg: '#FFF6E5', color: '#9A6300' },
  Pending: { label: 'Pendente', bg: '#EEF2FF', color: '#3446C9' },
};

export interface ReferencePeriodResponse {
  year: number;
  month: number;
}

export interface PanelOccurrenceResponse {
  id: string;
  recurringExpenseId: string;
  name: string;
  category: CategoryValue;
  expectedAmount: number;
  dueDate: string;
  status: DerivedStatusValue;
  paidAmount: number | null;
  paymentDate: string | null;
}

export interface GetMonthlyPanelResponse {
  referencePeriod: ReferencePeriodResponse;
  occurrences: PanelOccurrenceResponse[];
}

export interface MarkOccurrenceAsPaidResponse {
  occurrence: PanelOccurrenceResponse;
}

export interface UndoOccurrencePaymentResponse {
  occurrence: PanelOccurrenceResponse;
}
