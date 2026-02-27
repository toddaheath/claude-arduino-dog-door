/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useCallback, useEffect } from 'react';
import type { ReactNode } from 'react';
import type { UserSummary } from '../types';
import { authApi } from '../api/auth';
import { setAccessToken } from '../api/tokenStore';

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
  const [currentUser, setCurrentUser] = useState<UserSummary | null>(() => {
    const stored = localStorage.getItem('currentUser');
    return stored ? JSON.parse(stored) : null;
  });
  const [accessToken, setAccessTokenState] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(() =>
    localStorage.getItem('currentUser') !== null
  );

  const saveSession = useCallback((access: string, user: UserSummary) => {
    localStorage.setItem('currentUser', JSON.stringify(user));
    setAccessToken(access);
    setAccessTokenState(access);
    setCurrentUser(user);
  }, []);

  const clearSession = useCallback(() => {
    localStorage.removeItem('currentUser');
    setAccessToken(null);
    setAccessTokenState(null);
    setCurrentUser(null);
  }, []);

  // On mount: attempt silent refresh to recover access token from httpOnly cookie
  useEffect(() => {
    if (!localStorage.getItem('currentUser')) return;

    authApi.refresh()
      .then(response => {
        saveSession(response.accessToken, response.user);
      })
      .catch(() => {
        clearSession();
      })
      .finally(() => {
        setIsLoading(false);
      });
  }, [saveSession, clearSession]);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authApi.login(email, password);
    saveSession(response.accessToken, response.user);
  }, [saveSession]);

  const register = useCallback(async (
    email: string,
    password: string,
    firstName?: string,
    lastName?: string
  ) => {
    const response = await authApi.register(email, password, firstName, lastName);
    saveSession(response.accessToken, response.user);
  }, [saveSession]);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore errors on logout
    }
    clearSession();
  }, [clearSession]);

  const refreshSession = useCallback(async () => {
    try {
      const response = await authApi.refresh();
      saveSession(response.accessToken, response.user);
    } catch {
      clearSession();
    }
  }, [saveSession, clearSession]);

  return (
    <AuthContext.Provider value={{ currentUser, accessToken, isLoading, login, register, logout, refreshSession }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
