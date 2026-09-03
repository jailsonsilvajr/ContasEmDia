import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { DespesaRecorrenteService } from './despesa-recorrente.service';
import type { CreateRecurringExpenseRequest, CreateRecurringExpenseResponse } from './despesa-recorrente.model';

describe('DespesaRecorrenteService', () => {
  let service: DespesaRecorrenteService;
  let httpMock: HttpTestingController;

  const payload: CreateRecurringExpenseRequest = {
    name: 'Aluguel',
    category: 'Housing',
    monthlyAmount: 1500,
    dueDay: 5,
    startDate: '2026-09-01',
    frequency: 'Monthly',
    status: 'Active',
    note: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DespesaRecorrenteService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('POSTs to /api/recurring-expenses with the given payload and resolves on 201', () => {
    const response: CreateRecurringExpenseResponse = {
      id: 'abc-123',
      name: payload.name,
      category: payload.category,
      monthlyAmount: payload.monthlyAmount,
      dueDay: payload.dueDay,
      startDate: payload.startDate,
      frequency: 'Monthly',
      status: 'Active',
      note: null,
      occurrences: [],
    };

    let result: CreateRecurringExpenseResponse | undefined;
    service.create(payload).subscribe((res) => (result = res));

    const req = httpMock.expectOne('/api/recurring-expenses');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(response, { status: 201, statusText: 'Created' });

    expect(result).toEqual(response);
  });

  it('surfaces a 400 error response as-is to the caller', () => {
    let error: unknown;
    service.create(payload).subscribe({
      next: () => undefined,
      error: (err) => (error = err),
    });

    const req = httpMock.expectOne('/api/recurring-expenses');
    req.flush(
      { errors: [{ field: 'name', message: 'Nome é obrigatório.' }] },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(error).toBeDefined();
    expect((error as { status: number }).status).toBe(400);
  });

  it('surfaces a network failure as-is to the caller', () => {
    let error: unknown;
    service.create(payload).subscribe({
      next: () => undefined,
      error: (err) => (error = err),
    });

    const req = httpMock.expectOne('/api/recurring-expenses');
    req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    expect(error).toBeDefined();
  });
});
