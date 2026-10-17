import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ProblemDetails } from '../models/auth.models';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

/**
 * Manejo centralizado de errores HTTP: 401 cierra la sesión y el resto
 * se notifica con el mensaje (ProblemDetails.title) que envía el API.
 * Los 400 se propagan para que cada pantalla muestre el detalle.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isLogin = req.url.endsWith('/auth/login');

      if (error.status === 401 && !isLogin) {
        notifications.info('Tu sesión expiró. Inicia sesión nuevamente.');
        auth.logout();
      } else if (error.status === 0) {
        notifications.error('No se pudo conectar con el servidor.');
      } else if (error.status >= 500) {
        notifications.error(getErrorMessage(error));
      }

      return throwError(() => error);
    }),
  );
};

export function getErrorMessage(error: HttpErrorResponse): string {
  const problem = error.error as ProblemDetails | null;
  return problem?.title ?? 'Ocurrió un error inesperado.';
}

export function getErrorDetails(error: HttpErrorResponse): string[] {
  const errors = (error.error as ProblemDetails | null)?.errors;
  if (!errors) return [];
  return Array.isArray(errors) ? errors : Object.values(errors).flat();
}
