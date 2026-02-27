/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useCallback } from 'react';
import type { ReactNode } from 'react';
import type { UserSummary } from '../types';
import { authApi } from '../api/auth';

interface AuthContextValue {
  currentUser: UserSummary | null;
  accessToken: string | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, firstName?: string, lastName?: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  // Lazy initializers read localStorage once on mount — no effect needed
  const [currentUser, setCurrentUser] = useState<UserSummary | null>(() => {
    const stored = localStorage.getItem('currentUser');
    return stored ? JSON.parse(stored) : null;
  });
  const [accessToken, setAccessToken] = useState<string | null>(() =>
    localStorage.getItem('accessToken')
  );
  const [refreshToken, setRefreshToken] = useState<string | null>(() =>
    localStorage.getItem('refreshToken')
  );

  const saveSession = useCallback((access: string, refresh: string, user: UserSummary) => {
    localStorage.setItem('accessToken', access);
    localStorage.setItem('refreshToken', refresh);
    localStorage.setItem('currentUser', JSON.stringify(user));
    setAccessToken(access);
    setRefreshToken(refresh);
    setCurrentUser(user);
  }, []);

  const clearSession = useCallback(() => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('currentUser');
    setAccessToken(null);
    setRefreshToken(null);
    setCurrentUser(null);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authApi.login(email, password);
    saveSession(response.accessToken, response.refreshToken, response.user);
  }, [saveSession]);

  const register = useCallback(async (
    email: string,
    password: string,
    firstName?: string,
    lastName?: string
  ) => {
    const response = await authApi.register(email, password, firstName, lastName);
    saveSession(response.accessToken, response.refreshToken, response.user);
  }, [saveSession]);

  const logout = useCallback(async () => {
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken);
      } catch {
        // Ignore errors on logout
      }
    }
    clearSession();
  }, [refreshToken, clearSession]);

  const refreshSession = useCallback(async () => {
    if (!refreshToken) {
      clearSession();
      return;
    }
    try {
      const response = await authApi.refresh(refreshToken);
      saveSession(response.accessToken, response.refreshToken, response.user);
    } catch {
      clearSession();
    }
  }, [refreshToken, saveSession, clearSession]);

  return (
    <AuthContext.Provider value={{ currentUser, accessToken, isLoading: false, login, register, logout, refreshSession }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
