import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Spinner } from './Spinner';
import type { ReactNode } from 'react';

interface Props {
  children: ReactNode;
  role?: string; // rôle unique requis (ex : "Admin")
  roles?: string[]; // au moins un de ces rôles (ex : ["Admin","Formateur"])
}

export function ProtectedRoute({ children, role, roles }: Props) {
  const { isAuthenticated, isReady, hasRole, hasAnyRole } = useAuth();
  const location = useLocation();

  if (!isReady) return <Spinner label="Chargement..." />;

  if (!isAuthenticated) {
    const returnUrl = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/connexion?returnUrl=${returnUrl}`} replace />;
  }

  if (role && !hasRole(role)) return <Navigate to="/" replace />;
  if (roles && !hasAnyRole(roles)) return <Navigate to="/" replace />;

  return <>{children}</>;
}
