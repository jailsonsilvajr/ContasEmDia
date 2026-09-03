import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import type { Observable } from 'rxjs';

import type { CreateRecurringExpenseRequest, CreateRecurringExpenseResponse } from './despesa-recorrente.model';

@Injectable({ providedIn: 'root' })
export class DespesaRecorrenteService {
  private readonly http = inject(HttpClient);

  create(payload: CreateRecurringExpenseRequest): Observable<CreateRecurringExpenseResponse> {
    return this.http.post<CreateRecurringExpenseResponse>('/api/recurring-expenses', payload);
  }
}
