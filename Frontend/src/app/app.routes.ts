import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guards/auth.guard';

// Cada pantalla se carga de forma diferida (lazy loading).
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Iniciar sesión | iRoute Commerce',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'upload' },
      {
        path: 'upload',
        title: 'Cargar archivo | iRoute Commerce',
        loadComponent: () => import('./features/upload/upload').then((m) => m.Upload),
      },
      {
        path: 'process',
        title: 'Procesar | iRoute Commerce',
        loadComponent: () => import('./features/process/process').then((m) => m.Process),
      },
      {
        path: 'quarantine',
        title: 'Cuarentena | iRoute Commerce',
        loadComponent: () => import('./features/quarantine/quarantine').then((m) => m.Quarantine),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
