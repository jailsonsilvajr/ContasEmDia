import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

import { DespesaRecorrenteService } from './despesa-recorrente.service';
import type {
  ApiEnvelope,
  CreateRecurringExpenseRequest,
  CreateRecurringExpenseResponse,
  RecurringExpenseDetailResponse,
  UpdateRecurringExpenseRequest,
  UpdateRecurringExpenseResponse,
} from './despesa-recorrente.model';

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

  it('POSTs to /api/v1/recurring-expenses with the given payload and resolves on 201', () => {
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

    const req = httpMock.expectOne('/api/v1/recurring-expenses');
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

    const req = httpMock.expectOne('/api/v1/recurring-expenses');
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

    const req = httpMock.expectOne('/api/v1/recurring-expenses');
    req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    expect(error).toBeDefined();
  });

  const detail: RecurringExpenseDetailResponse = {
    id: 'abc-123',
    name: 'Aluguel',
    category: 'Housing',
    monthlyAmount: 1500,
    dueDay: 5,
    startDate: '2026-09-01',
    frequency: 'Monthly',
    status: 'Active',
    note: null,
  };

  it('GETs /api/v1/recurring-expenses/{id} and resolves with the envelope on 200', () => {
    const envelope: ApiEnvelope<RecurringExpenseDetailResponse> = { success: true, data: detail, errors: null };

    let result: ApiEnvelope<RecurringExpenseDetailResponse> | undefined;
    service.getById('abc-123').subscribe((res) => (result = res));

    const req = httpMock.expectOne('/api/v1/recurring-expenses/abc-123');
    expect(req.request.method).toBe('GET');
    req.flush(envelope);

    expect(result).toEqual(envelope);
  });

  it('surfaces a 404 from getById as-is to the caller', () => {
    let error: unknown;
    service.getById('missing-id').subscribe({
      next: () => undefined,
      error: (err) => (error = err),
    });

    const req = httpMock.expectOne('/api/v1/recurring-expenses/missing-id');
    req.flush({ success: false, data: null, errors: null }, { status: 404, statusText: 'Not Found' });

    expect(error).toBeDefined();
    expect((error as { status: number }).status).toBe(404);
  });

  it('surfaces a network failure from getById as-is to the caller', () => {
    let error: unknown;
    service.getById('abc-123').subscribe({
      next: () => undefined,
      error: (err) => (error = err),
    });

    const req = httpMock.expectOne('/api/v1/recurring-expenses/abc-123');
    req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    expect(error).toBeDefined();
  });

  const updatePayload: UpdateRecurringExpenseRequest = {
    name: 'Aluguel',
    category: 'Housing',
    monthlyAmount: 1600,
    dueDay: 5,
    startDate: '2026-09-01',
    status: 'Active',
    note: null,
  };

  it('PUTs /api/v1/recurring-expenses/{id} with the given payload and resolves with the envelope on 200', () => {
    const updated: UpdateRecurringExpenseResponse = { ...detail, monthlyAmount: 1600 };
    const envelope: ApiEnvelope<UpdateRecurringExpenseResponse> = { success: true, data: updated, errors: null };

    let result: ApiEnvelope<UpdateRecurringExpenseResponse> | undefined;
    service.update('abc-123', updatePayload).subscribe((res) => (result = res));

    const req = httpMock.expectOne('/api/v1/recurring-expenses/abc-123');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updatePayload);
    req.flush(envelope);

    expect(result).toEqual(envelope);
  });

  it('surfaces a 400 field-validation error from update as-is to the caller', () => {
    let error: unknown;
    service.update('abc-123', updatePayload).subscribe({
      next: () => undefined,
      error: (err) => (error = err),
    });

    const req = httpMock.expectOne('/api/v1/recurring-expenses/abc-123');
    req.flush(
      { success: false, data: null, errors: [{ field: 'monthlyAmount', message: 'Valor previsto mensal deve ser maior que zero.' }] },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(error).toBeDefined();
    expect((error as { status: number }).status).toBe(400);
  });

  it('surfaces a 404 from update as-is to the caller', () => {
    let error: unknown;
    service.update('missing-id', updatePayload).subscribe({
      next: () => undefined,
      error: (err) => (error = err),
    });

    const req = httpMock.expectOne('/api/v1/recurring-expenses/missing-id');
    req.flush({ success: false, data: null, errors: null }, { status: 404, statusText: 'Not Found' });

    expect(error).toBeDefined();
    expect((error as { status: number }).status).toBe(404);
  });

  it('surfaces a network failure from update as-is to the caller', () => {
    let error: unknown;
    service.update('abc-123', updatePayload).subscribe({
      next: () => undefined,
      error: (err) => (error = err),
    });

    const req = httpMock.expectOne('/api/v1/recurring-expenses/abc-123');
    req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    expect(error).toBeDefined();
  });
});
