import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { PainelMensalDespesasComponent } from './painel-mensal-despesas.component';
import { routes } from '../../app.routes';
import { formatEUR } from '../../shared/currency-format.util';
import type {
  ApiEnvelope,
  GetMonthlyPanelResponse,
  MarkOccurrenceAsPaidResponse,
  PanelOccurrenceResponse,
  UndoOccurrencePaymentResponse,
} from './painel-mensal-despesas.model';

function setInputValue(input: HTMLInputElement, value: string): void {
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

function makeOccurrence(overrides: Partial<PanelOccurrenceResponse>): PanelOccurrenceResponse {
  return {
    id: 'id-1',
    name: 'Aluguel',
    category: 'Housing',
    expectedAmount: 1500,
    dueDate: '2026-08-10',
    status: 'Pending',
    paidAmount: null,
    paymentDate: null,
    ...overrides,
  };
}

describe('PainelMensalDespesasComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PainelMensalDespesasComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function createAndFlush(response: ApiEnvelope<GetMonthlyPanelResponse>) {
    const fixture = TestBed.createComponent(PainelMensalDespesasComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/v1/occurrences');
    req.flush(response);
    fixture.detectChanges();

    return fixture;
  }

  it('shows the correct status badge label for each derived status', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [
          makeOccurrence({ id: '1', status: 'Paid', paidAmount: 1500, paymentDate: '2026-08-05' }),
          makeOccurrence({ id: '2', status: 'Overdue' }),
          makeOccurrence({ id: '3', status: 'DueSoon' }),
          makeOccurrence({ id: '4', status: 'Pending' }),
        ],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    const labels = Array.from(compiled.querySelectorAll('[data-testid="occurrence-row"] span'))
      .map((el) => el.textContent?.trim())
      .filter(Boolean);

    expect(labels).toContain('Paga');
    expect(labels).toContain('Vencida');
    expect(labels).toContain('Vence em breve');
    expect(labels).toContain('Pendente');
  });

  it('shows both banners when there are overdue and due-soon occurrences', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [
          makeOccurrence({ id: '1', status: 'Overdue' }),
          makeOccurrence({ id: '2', status: 'DueSoon' }),
        ],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="banner-vencidas"]')).toBeTruthy();
    expect(compiled.querySelector('[data-testid="banner-vence-em-breve"]')).toBeTruthy();
  });

  it('hides both banners when there are no overdue or due-soon occurrences', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1', status: 'Pending' })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="banner-vencidas"]')).toBeFalsy();
    expect(compiled.querySelector('[data-testid="banner-vence-em-breve"]')).toBeFalsy();
  });

  it('sums the three totals correctly across paid, overdue, and pending occurrences', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [
          makeOccurrence({ id: '1', status: 'Paid', expectedAmount: 100, paidAmount: 100, paymentDate: '2026-08-05' }),
          makeOccurrence({ id: '2', status: 'Overdue', expectedAmount: 200 }),
          makeOccurrence({ id: '3', status: 'Pending', expectedAmount: 300 }),
        ],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="total-previsto"]')?.textContent).toContain(formatEUR(600));
    expect(compiled.querySelector('[data-testid="total-pago"]')?.textContent).toContain(formatEUR(100));
    expect(compiled.querySelector('[data-testid="total-pendente"]')?.textContent).toContain(formatEUR(500));
  });

  it('shows the empty state with zero accounts, € 0,00 totals, and no banners', () => {
    const fixture = createAndFlush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    const zero = formatEUR(0);

    expect(compiled.querySelector('[data-testid="item-count"]')?.textContent).toContain('0 contas');
    expect(compiled.querySelector('[data-testid="total-previsto"]')?.textContent).toContain(zero);
    expect(compiled.querySelector('[data-testid="total-pago"]')?.textContent).toContain(zero);
    expect(compiled.querySelector('[data-testid="total-pendente"]')?.textContent).toContain(zero);
    expect(compiled.querySelector('[data-testid="banner-vencidas"]')).toBeFalsy();
    expect(compiled.querySelector('[data-testid="banner-vence-em-breve"]')).toBeFalsy();
  });

  it('shows "diferente do previsto" only when the paid amount differs from expected', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [
          makeOccurrence({ id: '1', status: 'Paid', expectedAmount: 1500, paidAmount: 1600, paymentDate: '2026-08-05' }),
          makeOccurrence({ id: '2', status: 'Paid', expectedAmount: 1500, paidAmount: 1500, paymentDate: '2026-08-05' }),
        ],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    const rows = compiled.querySelectorAll('[data-testid="occurrence-row"]');

    expect(rows[0].textContent).toContain('diferente do previsto');
    expect(rows[1].textContent).not.toContain('diferente do previsto');
  });

  it('starting edit pre-fills the expected amount (masked) and today as the draft date (ISO, native picker)', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1', expectedAmount: 1850 })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="marcar-paga-btn"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    const valorInput = compiled.querySelector('[data-testid="draft-valor-input"]') as HTMLInputElement;
    const dataInput = compiled.querySelector('[data-testid="draft-data-input"]') as HTMLInputElement;
    const today = new Date();
    const expectedIsoDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

    expect(valorInput.value).toBe('1.850,00');
    expect(dataInput.type).toBe('date');
    expect(dataInput.value).toBe(expectedIsoDate);
  });

  it('starting edit on a second occurrence cancels the first without a network call for it', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1' }), makeOccurrence({ id: '2' })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = () => Array.from(compiled.querySelectorAll('[data-testid="marcar-paga-btn"]')) as HTMLButtonElement[];

    buttons()[0].click();
    fixture.detectChanges();
    expect(compiled.querySelectorAll('[data-testid="edicao-pagamento"]').length).toBe(1);

    buttons()[0].click(); // second row's "Marcar como paga" button after the first re-renders as the edit form
    fixture.detectChanges();

    const editingForms = compiled.querySelectorAll('[data-testid="edicao-pagamento"]');
    expect(editingForms.length).toBe(1);

    httpMock.expectNone(() => true);
  });

  it('confirming with valid amount/date PATCHes the occurrence and exits edit mode', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1', expectedAmount: 1500 })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="marcar-paga-btn"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    setInputValue(compiled.querySelector('[data-testid="draft-valor-input"]') as HTMLInputElement, '150000');
    setInputValue(compiled.querySelector('[data-testid="draft-data-input"]') as HTMLInputElement, '2026-08-18');
    (compiled.querySelector('[data-testid="confirmar-btn"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne('/api/v1/occurrences/1/payment');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ paidAmount: '1.500,00', paymentDate: '18/08/2026' });

    const response: ApiEnvelope<MarkOccurrenceAsPaidResponse> = {
      success: true,
      data: {
        occurrence: makeOccurrence({ id: '1', status: 'Paid', paidAmount: 1500, paymentDate: '2026-08-18' }),
      },
      errors: null,
    };
    req.flush(response);
    fixture.detectChanges();

    expect(compiled.querySelector('[data-testid="edicao-pagamento"]')).toBeFalsy();
    expect(compiled.querySelector('[data-testid="occurrence-row"]')?.textContent).toContain('pago em 18/08/2026');
  });

  it('confirming with an empty/invalid amount still forwards the raw value to the server', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1', expectedAmount: 1500 })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="marcar-paga-btn"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    setInputValue(compiled.querySelector('[data-testid="draft-valor-input"]') as HTMLInputElement, '');
    (compiled.querySelector('[data-testid="confirmar-btn"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne('/api/v1/occurrences/1/payment');
    expect(req.request.body.paidAmount).toBe('');

    req.flush({
      success: true,
      data: { occurrence: makeOccurrence({ id: '1', status: 'Paid', paidAmount: 1500, paymentDate: '2026-08-18' }) },
      errors: null,
    } satisfies ApiEnvelope<MarkOccurrenceAsPaidResponse>);
  });

  it('confirming with an empty date still forwards the raw value to the server', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1' })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="marcar-paga-btn"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    setInputValue(compiled.querySelector('[data-testid="draft-data-input"]') as HTMLInputElement, '');
    (compiled.querySelector('[data-testid="confirmar-btn"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne('/api/v1/occurrences/1/payment');
    expect(req.request.body.paymentDate).toBe('');

    req.flush({
      success: true,
      data: { occurrence: makeOccurrence({ id: '1', status: 'Paid', paidAmount: 1500, paymentDate: '2026-08-15' }) },
      errors: null,
    } satisfies ApiEnvelope<MarkOccurrenceAsPaidResponse>);
  });

  it('cancel exits edit mode without calling the server or changing the occurrence data', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1', expectedAmount: 1500 })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="marcar-paga-btn"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    setInputValue(compiled.querySelector('[data-testid="draft-valor-input"]') as HTMLInputElement, '999,00');
    (compiled.querySelector('[data-testid="cancelar-btn"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    httpMock.expectNone(() => true);
    expect(compiled.querySelector('[data-testid="edicao-pagamento"]')).toBeFalsy();
    expect(compiled.querySelector('[data-testid="marcar-paga-btn"]')).toBeTruthy();
  });

  it('undoing a payment reverts the occurrence to not-paid with no paidAmount/paymentDate, recalculating status to Overdue', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1', status: 'Paid', paidAmount: 1500, paymentDate: '2026-08-05' })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    const desfazerBtn = compiled.querySelector('[data-testid="desfazer-btn"]') as HTMLButtonElement;
    expect(desfazerBtn.tagName).toBe('BUTTON');

    desfazerBtn.click();

    const req = httpMock.expectOne('/api/v1/occurrences/1/payment');
    expect(req.request.method).toBe('DELETE');

    req.flush({
      success: true,
      data: { occurrence: makeOccurrence({ id: '1', status: 'Overdue' }) },
      errors: null,
    } satisfies ApiEnvelope<UndoOccurrencePaymentResponse>);
    fixture.detectChanges();

    expect(compiled.querySelector('[data-testid="desfazer-btn"]')).toBeFalsy();
    expect(compiled.querySelector('[data-testid="marcar-paga-btn"]')).toBeTruthy();
    const row = compiled.querySelector('[data-testid="occurrence-row"]');
    expect(row?.textContent).toContain('Vencida');
    expect(row?.textContent).not.toContain('pago em');
  });

  it('undoing a payment can recalculate status to Pending when the due date is far in the future', () => {
    const fixture = createAndFlush({
      success: true,
      data: {
        referencePeriod: { year: 2026, month: 8 },
        occurrences: [makeOccurrence({ id: '1', status: 'Paid', paidAmount: 1500, paymentDate: '2026-08-05' })],
      },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="desfazer-btn"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne('/api/v1/occurrences/1/payment');
    req.flush({
      success: true,
      data: { occurrence: makeOccurrence({ id: '1', status: 'Pending' }) },
      errors: null,
    } satisfies ApiEnvelope<UndoOccurrencePaymentResponse>);
    fixture.detectChanges();

    const row = compiled.querySelector('[data-testid="occurrence-row"]');
    expect(row?.textContent).toContain('Pendente');
  });

  it('clicking "próximo mês" reloads with the next month in the same year', () => {
    const fixture = createAndFlush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="mes-proximo-btn"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/occurrences');
    expect(req.request.params.get('year')).toBe('2026');
    expect(req.request.params.get('month')).toBe('9');

    req.flush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 9 }, occurrences: [] },
      errors: null,
    });
    fixture.detectChanges();

    expect(compiled.querySelector('[data-testid="mes-atual-label"]')?.textContent).toContain('Setembro 2026');
  });

  it('clicking "mês anterior" reloads with the previous month in the same year', () => {
    const fixture = createAndFlush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="mes-anterior-btn"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/occurrences');
    expect(req.request.params.get('year')).toBe('2026');
    expect(req.request.params.get('month')).toBe('7');

    req.flush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 7 }, occurrences: [] },
      errors: null,
    });
  });

  it('clicking "mês anterior" from January rolls back to December of the previous year', () => {
    const fixture = createAndFlush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 1 }, occurrences: [] },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="mes-anterior-btn"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/occurrences');
    expect(req.request.params.get('year')).toBe('2025');
    expect(req.request.params.get('month')).toBe('12');

    req.flush({
      success: true,
      data: { referencePeriod: { year: 2025, month: 12 }, occurrences: [] },
      errors: null,
    });
  });

  it('clicking "próximo mês" from December rolls forward to January of the next year', () => {
    const fixture = createAndFlush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 12 }, occurrences: [] },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="mes-proximo-btn"]') as HTMLButtonElement).click();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/occurrences');
    expect(req.request.params.get('year')).toBe('2027');
    expect(req.request.params.get('month')).toBe('1');

    req.flush({
      success: true,
      data: { referencePeriod: { year: 2027, month: 1 }, occurrences: [] },
      errors: null,
    });
  });

  it('clicking "próximo mês" then "mês anterior" returns exactly to the original competência', () => {
    const fixture = createAndFlush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    });

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="mes-proximo-btn"]') as HTMLButtonElement).click();
    httpMock.expectOne((r) => r.url === '/api/v1/occurrences').flush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 9 }, occurrences: [] },
      errors: null,
    });
    fixture.detectChanges();

    (compiled.querySelector('[data-testid="mes-anterior-btn"]') as HTMLButtonElement).click();
    const req = httpMock.expectOne((r) => r.url === '/api/v1/occurrences');
    expect(req.request.params.get('year')).toBe('2026');
    expect(req.request.params.get('month')).toBe('8');
    req.flush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    });
    fixture.detectChanges();

    expect(compiled.querySelector('[data-testid="mes-atual-label"]')?.textContent).toContain('Agosto 2026');
  });

  it('the "Nova despesa" button is associated with routerLink="/despesas/nova"', () => {
    const fixture = createAndFlush({
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    });

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('[data-testid="nova-despesa-btn"]') as HTMLButtonElement).click();

    expect(navigateSpy).toHaveBeenCalled();
    const navigatedUrl = navigateSpy.mock.calls[0][0];
    expect(String(navigatedUrl)).toBe('/despesas/nova');
  });
});
