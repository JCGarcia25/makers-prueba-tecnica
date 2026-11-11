import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login/login').then(m => m.Login) },
  { path: 'loans', canActivate: [authGuard], loadComponent: () => import('./features/loans/loans').then(m => m.Loans) },
  { path: 'admin', canActivate: [adminGuard], loadComponent: () => import('./features/admin/admin').then(m => m.Admin) },
  { path: '', pathMatch: 'full', redirectTo: 'loans' },
  { path: '**', redirectTo: 'loans' }
];
