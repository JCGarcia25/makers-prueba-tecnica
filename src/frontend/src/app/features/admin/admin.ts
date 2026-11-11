import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { LoanApiService } from '../../core/loan-api.service';
import { Loan, LoanStatus, ProblemDetails } from '../../core/models';
import { Nav } from '../../shared/nav';

@Component({ imports: [FormsModule, CurrencyPipe, DatePipe, Nav], templateUrl: './admin.html' })
export class Admin implements OnInit {
  private readonly api = inject(LoanApiService);
  readonly loans = signal<Loan[]>([]); readonly loading = signal(true); readonly error = signal(''); readonly message = signal('');
  readonly reasons = new Map<string, string>(); filter: LoanStatus | '' = 'Pending';
  ngOnInit(): void { this.load(); }
  load(): void { this.loading.set(true); this.api.list(this.filter || undefined).pipe(finalize(() => this.loading.set(false))).subscribe({ next: p => this.loans.set(p.items), error: e => this.showError(e) }); }
  reason(id: string): string { return this.reasons.get(id) ?? ''; }
  setReason(id: string, value: string): void { this.reasons.set(id, value); }
  decide(loan: Loan, status: 'Approved' | 'Rejected'): void { const reason = this.reason(loan.id).trim(); if (!reason) { this.error.set('Escribe un motivo antes de decidir.'); return; } this.api.decide(loan.id, status, reason).subscribe({ next: () => { this.message.set(`Solicitud ${status === 'Approved' ? 'aprobada' : 'rechazada'}.`); this.reasons.delete(loan.id); this.load(); }, error: e => this.showError(e) }); }
  private showError(error: HttpErrorResponse): void { this.error.set((error.error as ProblemDetails)?.detail ?? 'No fue posible completar la operación.'); }
}
