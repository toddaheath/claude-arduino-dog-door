import type { UserProfile, Guest, Invitation } from '../types';

const BASE = `${import.meta.env.VITE_API_URL || ''}/api/v1/users`;

function authHeaders(token: string) {
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  };
}

async function request<T>(
  method: string,
  path: string,
  token: string,
  body?: unknown
): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: authHeaders(token),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const usersApi = {
  getProfile: (token: string) => request<UserProfile>('GET', '/me', token),

  updateProfile: (token: string, data: Partial<UserProfile>) =>
    request<UserProfile>('PUT', '/me', token, data),

  changePassword: (token: string, currentPassword: string, newPassword: string) =>
    request<void>('PUT', '/me/password', token, { currentPassword, newPassword }),

  getGuests: (token: string) => request<Guest[]>('GET', '/me/guests', token),

  inviteGuest: (token: string, email: string) =>
    request<void>('POST', '/me/guests/invite', token, { email }),

  removeGuest: (token: string, guestId: number) =>
    request<void>('DELETE', `/me/guests/${guestId}`, token),

  getInvitations: (token: string) =>
    request<Invitation[]>('GET', '/me/invitations', token),

  acceptInvitation: (token: string, invitationToken: string) =>
    request<void>('POST', `/me/invitations/accept/${invitationToken}`, token),
};
