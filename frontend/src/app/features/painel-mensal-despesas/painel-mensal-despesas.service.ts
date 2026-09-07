import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';

import type {
  ApiEnvelope,
  GetMonthlyPanelResponse,
  MarkOccurrenceAsPaidResponse,
  UndoOccurrencePaymentResponse,
} from './painel-mensal-despesas.model';

@Injectable({ providedIn: 'root' })
export class PainelMensalDespesasService {
  private readonly http = inject(HttpClient);

  getMonthlyPanel(year?: string, month?: string): Observable<ApiEnvelope<GetMonthlyPanelResponse>> {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    if (month) params = params.set('month', month);

    return this.http.get<ApiEnvelope<GetMonthlyPanelResponse>>('/api/v1/occurrences', { params });
  }

  markOccurrenceAsPaid(
    occurrenceId: string,
    paidAmount: string,
    paymentDate: string,
  ): Observable<ApiEnvelope<MarkOccurrenceAsPaidResponse>> {
    return this.http.patch<ApiEnvelope<MarkOccurrenceAsPaidResponse>>(
      `/api/v1/occurrences/${occurrenceId}/payment`,
      { paidAmount, paymentDate },
    );
  }

  undoOccurrencePayment(occurrenceId: string): Observable<ApiEnvelope<UndoOccurrencePaymentResponse>> {
    return this.http.delete<ApiEnvelope<UndoOccurrencePaymentResponse>>(`/api/v1/occurrences/${occurrenceId}/payment`);
  }
}
