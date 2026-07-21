import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function NavBar() {
  const { user, isAuthenticated, hasRole, isStaff, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `px-3 py-2 rounded-md text-sm font-medium transition-colors ${
      isActive ? 'text-brand-700 bg-brand-50' : 'text-slate-600 hover:text-brand-600 hover:bg-slate-100'
    }`;

  return (
    <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/90 backdrop-blur">
      <nav className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
        <Link to="/" className="flex items-center gap-2 text-lg font-bold text-brand-700">
          <span className="text-2xl">🎓</span>
          <span>Formations</span>
        </Link>

        <div className="flex items-center gap-1">
          <NavLink to="/formations" className={linkClass}>
            Formations
          </NavLink>

          {isAuthenticated && !isStaff && (
            <NavLink to="/mes-formations" className={linkClass}>
              Mes formations
            </NavLink>
          )}

          {isStaff && (
            <NavLink to="/gestion/formations" className={linkClass}>
              Gestion
            </NavLink>
          )}

          {hasRole('Admin') && (
            <NavLink to="/admin/dashboard" className={linkClass}>
              Admin
            </NavLink>
          )}
        </div>

        <div className="flex items-center gap-2">
          {isAuthenticated ? (
            <>
              <span className="hidden text-sm text-slate-500 sm:inline">
                Bonjour, <strong className="text-slate-700">{user?.fullName}</strong>
              </span>
              <button
                onClick={handleLogout}
                className="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100"
              >
                Déconnexion
              </button>
            </>
          ) : (
            <>
              <Link
                to="/connexion"
                className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:text-brand-600"
              >
                Connexion
              </Link>
              <Link
                to="/inscription"
                className="rounded-md bg-brand-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-brand-700"
              >
                S'inscrire
              </Link>
            </>
          )}
        </div>
      </nav>
    </header>
  );
}
