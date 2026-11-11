import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({ selector: 'app-nav', imports: [RouterLink, RouterLinkActive], template: `<header class="topbar"><div class="brand">Makers Loans</div><nav aria-label="Navegación principal"><a routerLink="/loans" routerLinkActive="active">Mis préstamos</a>@if (auth.isAdmin()) { <a routerLink="/admin" routerLinkActive="active">Administración</a> }<button class="link-button" type="button" (click)="auth.logout()">Cerrar sesión</button></nav></header>` })
export class Nav { readonly auth = inject(AuthService); }
