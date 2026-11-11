import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { TokenResponse } from './models';

const STORAGE_KEY = 'makers.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly session = signal<TokenResponse | null>(this.readSession());
  readonly user = computed(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => !!this.session() && new Date(this.session()!.expiresAt) > new Date());
  readonly isAdmin = computed(() => this.user()?.role === 'Admin');
  login(email: string, password: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>('/api/auth/login', { email, password }).pipe(tap(session => this.save(session)));
  }
  token(): string | null { return this.isAuthenticated() ? this.session()?.accessToken ?? null : null; }
  logout(): void { sessionStorage.removeItem(STORAGE_KEY); this.session.set(null); void this.router.navigate(['/login']); }
  private save(session: TokenResponse): void { sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session)); this.session.set(session); }
  private readSession(): TokenResponse | null {
    try { const raw = sessionStorage.getItem(STORAGE_KEY); return raw ? JSON.parse(raw) as TokenResponse : null; }
    catch { sessionStorage.removeItem(STORAGE_KEY); return null; }
  }
}
