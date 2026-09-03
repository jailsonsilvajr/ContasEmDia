export type CategoryValue = 'Housing' | 'Services' | 'Transportation' | 'Subscriptions' | 'Other';
export type StatusValue = 'ativa' | 'pausada';
export type FormStatus = 'idle' | 'loading' | 'success' | 'error';

export interface CategoryOption {
  value: CategoryValue;
  label: string;
}

export const CATEGORY_OPTIONS: CategoryOption[] = [
  { value: 'Housing', label: 'Moradia' },
  { value: 'Services', label: 'Serviços' },
  { value: 'Transportation', label: 'Transporte' },
  { value: 'Subscriptions', label: 'Assinaturas' },
  { value: 'Other', label: 'Outra' },
];

export const CATEGORY_COLORS: Record<CategoryValue, string> = {
  Housing: '#2E6FF2',
  Services: '#0E9384',
  Transportation: '#B8790A',
  Subscriptions: '#7A5AF8',
  Other: '#667085',
};

export interface CreateRecurringExpenseRequest {
  name: string;
  category: CategoryValue;
  monthlyAmount: number;
  dueDay: number;
  startDate: string;
  frequency: 'Monthly';
  status: 'Active' | 'Paused';
  note: string | null;
}

export interface OccurrenceResponse {
  id: string;
  referencePeriod: { year: number; month: number };
  dueDate: string;
  status: 'Pending';
  expectedAmount: number;
  name: string;
  category: CategoryValue;
}

export interface CreateRecurringExpenseResponse {
  id: string;
  name: string;
  category: CategoryValue;
  monthlyAmount: number;
  dueDay: number;
  startDate: string;
  frequency: 'Monthly';
  status: 'Active' | 'Paused';
  note: string | null;
  occurrences: OccurrenceResponse[];
}

export interface FieldError {
  field: string;
  message: string;
}

export interface ApiErrorResponse {
  errors: FieldError[];
}
