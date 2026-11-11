import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { LoanApiService } from '../../core/loan-api.service';
import { Loan, ProblemDetails } from '../../core/models';
import { Nav } from '../../shared/nav';

@Component({ imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, Nav], templateUrl: './loans.html' })
export class Loans implements OnInit {
  private readonly fb = inject(FormBuilder); private readonly api = inject(LoanApiService);
  readonly loans = signal<Loan[]>([]); readonly loading = signal(true); readonly saving = signal(false);
  readonly message = signal(''); readonly error = signal(''); readonly editingId = signal<string | null>(null);
  readonly form = this.fb.nonNullable.group({
    amount: [1_000_000, [Validators.required, Validators.min(1), Validators.max(1_000_000_000)]],
    termMonths: [12, [Validators.required, Validators.min(1), Validators.max(120)]]
  });
  ngOnInit(): void { this.load(); }
  load(): void { this.loading.set(true); this.error.set(''); this.api.list().pipe(finalize(() => this.loading.set(false))).subscribe({ next: page => this.loans.set(page.items), error: e => this.showError(e) }); }
  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const { amount, termMonths } = this.form.getRawValue();
    const request = this.editingId() ? this.api.update(this.editingId()!, amount, termMonths) : this.api.create(amount, termMonths);
    this.saving.set(true); this.error.set(''); this.message.set('');
    request.pipe(finalize(() => this.saving.set(false))).subscribe({ next: () => { this.message.set(this.editingId() ? 'Solicitud actualizada.' : 'Solicitud enviada.'); this.cancelEdit(); this.load(); }, error: e => this.showError(e) });
  }
  edit(loan: Loan): void { this.editingId.set(loan.id); this.form.setValue({ amount: loan.amount, termMonths: loan.termMonths }); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  cancelEdit(): void { this.editingId.set(null); this.form.reset({ amount: 1_000_000, termMonths: 12 }); }
  remove(loan: Loan): void { if (!confirm('¿Eliminar esta solicitud pendiente?')) return; this.api.remove(loan.id).subscribe({ next: () => { this.message.set('Solicitud eliminada.'); this.load(); }, error: e => this.showError(e) }); }
  private showError(error: HttpErrorResponse): void { this.error.set((error.error as ProblemDetails)?.detail ?? 'No fue posible completar la operación.'); }
}
