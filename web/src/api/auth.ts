import type { AuthResponse } from '../types';

const BASE = `${import.meta.env.VITE_API_URL || ''}/api/v1/auth`;

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    method: 'POST',
    headers: body !== undefined ? { 'Content-Type': 'application/json' } : {},
    body: body !== undefined ? JSON.stringify(body) : undefined,
    credentials: 'include',
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

  refresh: () =>
    post<AuthResponse>('/refresh'),

  logout: () =>
    post<void>('/logout'),

  forgotPassword: (email: string) =>
    post<void>('/forgot-password', { email }),

  resetPassword: (token: string, newPassword: string) =>
    post<void>('/reset-password', { token, newPassword }),

  forgotUsername: (email: string) =>
    post<void>('/forgot-username', { email }),
};
