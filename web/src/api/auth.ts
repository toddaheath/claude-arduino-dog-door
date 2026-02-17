import type { AuthResponse } from '../types';

const BASE = `${import.meta.env.VITE_API_URL || ''}/api/v1/auth`;

async function post<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const authApi = {
  register: (email: string, password: string, firstName?: string, lastName?: string) =>
    post<AuthResponse>('/register', { email, password, firstName, lastName }),

  login: (email: string, password: string) =>
    post<AuthResponse>('/login', { email, password }),

  refresh: (refreshToken: string) =>
    post<AuthResponse>('/refresh', { refreshToken }),

  logout: (refreshToken: string) =>
    post<void>('/logout', { refreshToken }),

  forgotPassword: (email: string) =>
    post<void>('/forgot-password', { email }),

  resetPassword: (token: string, newPassword: string) =>
    post<void>('/reset-password', { token, newPassword }),

  forgotUsername: (email: string) =>
    post<void>('/forgot-username', { email }),
};
