import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { PainelMensalDespesasService } from './painel-mensal-despesas.service';
import type {
  ApiEnvelope,
  GetMonthlyPanelResponse,
  MarkOccurrenceAsPaidResponse,
  UndoOccurrencePaymentResponse,
} from './painel-mensal-despesas.model';

describe('PainelMensalDespesasService', () => {
  let service: PainelMensalDespesasService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PainelMensalDespesasService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('GETs /api/v1/occurrences with no query params when year/month are omitted', () => {
    const response: ApiEnvelope<GetMonthlyPanelResponse> = {
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    };

    let result: ApiEnvelope<GetMonthlyPanelResponse> | undefined;
    service.getMonthlyPanel().subscribe((res) => (result = res));

    const req = httpMock.expectOne((r) => r.url === '/api/v1/occurrences');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush(response);

    expect(result).toEqual(response);
  });

  it('GETs /api/v1/occurrences with year and month query params when provided', () => {
    const response: ApiEnvelope<GetMonthlyPanelResponse> = {
      success: true,
      data: { referencePeriod: { year: 2026, month: 8 }, occurrences: [] },
      errors: null,
    };

    service.getMonthlyPanel('2026', '8').subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/occurrences');
    expect(req.request.params.get('year')).toBe('2026');
    expect(req.request.params.get('month')).toBe('8');
    req.flush(response);
  });

  it('PATCHes /api/v1/occurrences/{id}/payment with the raw amount and date strings', () => {
    const response: ApiEnvelope<MarkOccurrenceAsPaidResponse> = {
      success: true,
      data: {
        occurrence: {
          id: 'abc-123',
          name: 'Aluguel',
          category: 'Housing',
          expectedAmount: 1500,
          dueDate: '2026-08-10',
          status: 'Paid',
          paidAmount: 1500,
          paymentDate: '2026-08-18',
        },
      },
      errors: null,
    };

    let result: ApiEnvelope<MarkOccurrenceAsPaidResponse> | undefined;
    service.markOccurrenceAsPaid('abc-123', '1500,00', '18/08/2026').subscribe((res) => (result = res));

    const req = httpMock.expectOne('/api/v1/occurrences/abc-123/payment');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ paidAmount: '1500,00', paymentDate: '18/08/2026' });
    req.flush(response);

    expect(result).toEqual(response);
  });

  it('DELETEs /api/v1/occurrences/{id}/payment to undo a payment', () => {
    const response: ApiEnvelope<UndoOccurrencePaymentResponse> = {
      success: true,
      data: {
        occurrence: {
          id: 'abc-123',
          name: 'Aluguel',
          category: 'Housing',
          expectedAmount: 1500,
          dueDate: '2026-08-10',
          status: 'Pending',
          paidAmount: null,
          paymentDate: null,
        },
      },
      errors: null,
    };

    let result: ApiEnvelope<UndoOccurrencePaymentResponse> | undefined;
    service.undoOccurrencePayment('abc-123').subscribe((res) => (result = res));

    const req = httpMock.expectOne('/api/v1/occurrences/abc-123/payment');
    expect(req.request.method).toBe('DELETE');
    req.flush(response);

    expect(result).toEqual(response);
  });
});
