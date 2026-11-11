import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Loan, LoanStatus, Page } from './models';

@Injectable({ providedIn: 'root' })
export class LoanApiService {
  private readonly http = inject(HttpClient);
  list(status?: LoanStatus) { return this.http.get<Page<Loan>>('/api/loans', { params: status ? { status } : {} }); }
  create(amount: number, termMonths: number) { return this.http.post<Loan>('/api/loans', { amount, termMonths }); }
  update(id: string, amount: number, termMonths: number) { return this.http.put<Loan>(`/api/loans/${id}`, { amount, termMonths }); }
  remove(id: string) { return this.http.delete<void>(`/api/loans/${id}`); }
  decide(id: string, status: 'Approved' | 'Rejected', reason: string) { return this.http.patch<Loan>(`/api/loans/${id}/decision`, { status, reason }); }
}
