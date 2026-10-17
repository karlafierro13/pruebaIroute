import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

/** Solo usuarios autenticados. */
export const authGuard: CanActivateFn = () =>
  inject(AuthService).token ? true : inject(Router).createUrlTree(['/login']);

/** Evita volver al login si ya existe una sesión activa. */
export const guestGuard: CanActivateFn = () =>
  inject(AuthService).token ? inject(Router).createUrlTree(['/']) : true;
