import { createContext, useContext, useEffect, useState, useCallback } from 'react';
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
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [currentUser, setCurrentUser] = useState<UserSummary | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [refreshToken, setRefreshToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Restore session from localStorage on mount
  useEffect(() => {
    const storedAccess = localStorage.getItem('accessToken');
    const storedRefresh = localStorage.getItem('refreshToken');
    const storedUser = localStorage.getItem('currentUser');

    if (storedAccess && storedRefresh && storedUser) {
      setAccessToken(storedAccess);
      setRefreshToken(storedRefresh);
      setCurrentUser(JSON.parse(storedUser));
    }
    setIsLoading(false);
  }, []);

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

  return (
    <AuthContext.Provider value={{ currentUser, accessToken, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
