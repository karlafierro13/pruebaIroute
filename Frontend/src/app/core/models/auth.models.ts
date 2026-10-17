export interface LoginRequest {
  username: string;
  password: string;
}

export interface Session {
  token: string;
  username: string;
  expiresAt: string;
}

/** Respuesta de error estándar del API (RFC 7807). */
export interface ProblemDetails {
  title?: string;
  status?: number;
  errors?: string[] | Record<string, string[]>;
}
