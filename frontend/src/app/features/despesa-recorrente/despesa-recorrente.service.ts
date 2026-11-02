import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';

import type {
  ApiEnvelope,
  CreateRecurringExpenseRequest,
  CreateRecurringExpenseResponse,
  DeleteRecurringExpenseResponse,
  DeleteScope,
  RecurringExpenseDetailResponse,
  UpdateRecurringExpenseRequest,
  UpdateRecurringExpenseResponse,
} from './despesa-recorrente.model';

@Injectable({ providedIn: 'root' })
export class DespesaRecorrenteService {
  private readonly http = inject(HttpClient);

  create(payload: CreateRecurringExpenseRequest): Observable<CreateRecurringExpenseResponse> {
    return this.http.post<CreateRecurringExpenseResponse>('/api/v1/recurring-expenses', payload);
  }

  getById(id: string): Observable<ApiEnvelope<RecurringExpenseDetailResponse>> {
    return this.http.get<ApiEnvelope<RecurringExpenseDetailResponse>>(`/api/v1/recurring-expenses/${id}`);
  }

  update(id: string, payload: UpdateRecurringExpenseRequest): Observable<ApiEnvelope<UpdateRecurringExpenseResponse>> {
    return this.http.put<ApiEnvelope<UpdateRecurringExpenseResponse>>(`/api/v1/recurring-expenses/${id}`, payload);
  }

  delete(id: string, scope: DeleteScope): Observable<ApiEnvelope<DeleteRecurringExpenseResponse>> {
    const params = new HttpParams().set('scope', scope);
    return this.http.delete<ApiEnvelope<DeleteRecurringExpenseResponse>>(`/api/v1/recurring-expenses/${id}`, { params });
  }
}
