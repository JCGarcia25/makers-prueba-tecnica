import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/auth.service';
import { ProblemDetails } from '../../core/models';

@Component({ imports: [ReactiveFormsModule], templateUrl: './login.html' })
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly form = this.fb.nonNullable.group({
    email: ['usuario@test.com', [Validators.required, Validators.email]],
    password: ['Demo123456!', [Validators.required, Validators.minLength(10)]]
  });
  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true); this.error.set('');
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: session => void this.router.navigate([session.user.role === 'Admin' ? '/admin' : '/loans']),
      error: (error: HttpErrorResponse) => this.error.set((error.error as ProblemDetails)?.detail ?? 'No fue posible iniciar sesión.')
    });
  }
}
