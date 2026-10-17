import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LoginRequest, Session } from '../models/auth.models';

const STORAGE_KEY = 'iroute.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<Session | null>(this.restoreSession());

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly username = computed(() => this.session()?.username ?? '');

  get token(): string | null {
    const current = this.session();
    if (current && new Date(current.expiresAt) <= new Date()) {
      this.clearSession();
      return null;
    }
    return current?.token ?? null;
  }

  login(request: LoginRequest): Observable<Session> {
    return this.http
      .post<Session>(`${environment.apiUrl}/auth/login`, request)
      .pipe(tap((session) => this.saveSession(session)));
  }

  logout(): void {
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  private saveSession(session: Session): void {
    this.session.set(session);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Almacenamiento no disponible: la sesión vive solo en memoria.
    }
  }

  private clearSession(): void {
    this.session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* noop */
    }
  }

  private restoreSession(): Session | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const session = JSON.parse(raw) as Session;
      return new Date(session.expiresAt) > new Date() ? session : null;
    } catch {
      return null;
    }
  }
}
