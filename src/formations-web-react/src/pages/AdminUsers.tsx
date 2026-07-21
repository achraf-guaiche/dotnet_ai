import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/apiClient';
import type { AdminUser } from '../types/api';
import { Spinner } from '../components/Spinner';

interface UsersResponse {
  items: AdminUser[];
  total: number;
  page: number;
  pageSize: number;
}

const ROLES = ['Admin', 'Formateur', 'Apprenant'];
const PAGE_SIZE = 20;

export function AdminUsers() {
  const [data, setData] = useState<UsersResponse | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.get<UsersResponse>(`api/admin/users?page=${page}&pageSize=${PAGE_SIZE}`);
      setData(res);
    } catch {
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => {
    document.title = 'Administration — Utilisateurs';
    load();
  }, [load]);

  const changeRole = async (user: AdminUser, role: string) => {
    if (role === (user.roles[0] ?? '')) return;
    setBusyId(user.id);
    try {
      await api.put(`api/admin/users/${user.id}/role`, { role });
      setNotice(`Rôle de ${user.fullName} changé en ${role}.`);
      await load();
    } catch {
      setNotice('Erreur lors du changement de rôle.');
    } finally {
      setBusyId(null);
    }
  };

  const toggleActive = async (user: AdminUser) => {
    setBusyId(user.id);
    try {
      await api.put(`api/admin/users/${user.id}/toggle-active`, { isActive: !user.isActive });
      setNotice(`Utilisateur ${!user.isActive ? 'activé' : 'désactivé'}.`);
      await load();
    } catch {
      setNotice("Erreur lors de la mise à jour du statut.");
    } finally {
      setBusyId(null);
    }
  };

  const totalPages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-slate-800">Gestion des utilisateurs</h1>
          <span className="rounded-full bg-brand-600 px-3 py-1 text-xs font-semibold text-white">Admin</span>
        </div>
        <Link to="/admin/dashboard" className="text-sm font-medium text-brand-600 hover:underline">
          ← Retour au dashboard
        </Link>
      </div>

      {notice && (
        <div className="flex items-center justify-between rounded-lg bg-brand-50 px-4 py-2 text-sm text-brand-700">
          <span>{notice}</span>
          <button onClick={() => setNotice(null)} className="text-brand-400 hover:text-brand-600">
            ✕
          </button>
        </div>
      )}

      {loading ? (
        <Spinner label="Chargement des utilisateurs..." />
      ) : data && data.items.length > 0 ? (
        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-100 bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3">Utilisateur</th>
                <th className="px-4 py-3">Rôle</th>
                <th className="px-4 py-3 text-center">Inscriptions</th>
                <th className="px-4 py-3 text-center">Statut</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data.items.map((u) => (
                <tr key={u.id} className={busyId === u.id ? 'opacity-50' : ''}>
                  <td className="px-4 py-3">
                    <div className="font-medium text-slate-800">{u.fullName}</div>
                    <div className="text-xs text-slate-400">{u.email}</div>
                  </td>
                  <td className="px-4 py-3">
                    <select
                      value={u.roles[0] ?? ''}
                      onChange={(e) => changeRole(u, e.target.value)}
                      disabled={busyId === u.id}
                      className="rounded-lg border border-slate-300 px-2 py-1 text-sm focus:border-brand-500 focus:outline-none"
                    >
                      {!u.roles[0] && <option value="">—</option>}
                      {ROLES.map((r) => (
                        <option key={r} value={r}>
                          {r}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-4 py-3 text-center text-slate-600">{u.nbInscriptions}</td>
                  <td className="px-4 py-3 text-center">
                    <span
                      className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                        u.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'
                      }`}
                    >
                      {u.isActive ? 'Actif' : 'Désactivé'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => toggleActive(u)}
                      disabled={busyId === u.id}
                      className={`rounded-lg px-3 py-1.5 text-xs font-semibold text-white disabled:opacity-50 ${
                        u.isActive ? 'bg-red-500 hover:bg-red-600' : 'bg-green-600 hover:bg-green-700'
                      }`}
                    >
                      {u.isActive ? 'Désactiver' : 'Activer'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="py-16 text-center text-slate-400">Aucun utilisateur.</p>
      )}

      {data && totalPages > 1 && (
        <div className="flex items-center justify-center gap-4">
          <button
            onClick={() => setPage((p) => p - 1)}
            disabled={page === 1}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm disabled:opacity-40"
          >
            ← Précédent
          </button>
          <span className="text-sm text-slate-500">
            Page {page} / {totalPages}
          </span>
          <button
            onClick={() => setPage((p) => p + 1)}
            disabled={page >= totalPages}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm disabled:opacity-40"
          >
            Suivant →
          </button>
        </div>
      )}
    </div>
  );
}
