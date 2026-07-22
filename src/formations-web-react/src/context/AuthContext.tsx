import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, setTokens, clearTokens, getToken } from '../lib/apiClient';
import type { AuthResponse, RegisterRole } from '../types/api';

interface AuthUser {
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isReady: boolean;
  hasRole: (role: string) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
  isStaff: boolean; // Admin ou Formateur
  login: (email: string, password: string) => Promise<void>;
  register: (
    fullName: string,
    email: string,
    password: string,
    confirmPassword: string,
    role: RegisterRole,
  ) => Promise<void>;
  logout: () => void;
}

const USER_KEY = 'auth_user';

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function persistUser(res: AuthResponse): AuthUser {
  const user: AuthUser = {
    userId: res.userId,
    email: res.email,
    fullName: res.fullName,
    roles: res.roles ?? [],
  };
  setTokens(res.accessToken, res.refreshToken);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
  return user;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isReady, setIsReady] = useState(false);

  // Restaure la session depuis le localStorage au démarrage.
  useEffect(() => {
    const raw = localStorage.getItem(USER_KEY);
    if (raw && getToken()) {
      try {
        setUser(JSON.parse(raw) as AuthUser);
      } catch {
        clearTokens();
        localStorage.removeItem(USER_KEY);
      }
    }
    setIsReady(true);
  }, []);

  const value = useMemo<AuthContextValue>(() => {
    const login = async (email: string, password: string) => {
      const res = await api.post<AuthResponse>('api/auth/login', { email, password });
      setUser(persistUser(res));
    };

    const register = async (
      fullName: string,
      email: string,
      password: string,
      confirmPassword: string,
      role: RegisterRole = 'Apprenant',
    ) => {
      const res = await api.post<AuthResponse>('api/auth/register', {
        fullName,
        email,
        password,
        confirmPassword,
        role,
      });
      setUser(persistUser(res));
    };

    const logout = () => {
      clearTokens();
      localStorage.removeItem(USER_KEY);
      setUser(null);
    };

    return {
      user,
      isAuthenticated: !!user,
      isReady,
      hasRole: (role: string) => user?.roles.includes(role) ?? false,
      hasAnyRole: (roles: string[]) => roles.some((r) => user?.roles.includes(r)) ?? false,
      isStaff: (user?.roles.includes('Admin') || user?.roles.includes('Formateur')) ?? false,
      login,
      register,
      logout,
    };
  }, [user, isReady]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth doit être utilisé dans un AuthProvider');
  return ctx;
}
