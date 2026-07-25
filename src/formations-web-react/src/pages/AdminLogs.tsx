import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/apiClient';
import type { ActivityLog } from '../types/api';
import { Spinner } from '../components/Spinner';

interface LogsResponse {
  items: ActivityLog[];
  total: number;
  page: number;
  pageSize: number;
}

const PAGE_SIZE = 50;

// Actions connues émises côté serveur (journal d'activité).
const ACTIONS = ['Register', 'Login', 'CreateFormation', 'UpdateFormation', 'Inscription'];

const ACTION_STYLE: Record<string, string> = {
  Register: 'bg-green-100 text-green-700',
  Login: 'bg-blue-100 text-blue-700',
  Inscription: 'bg-purple-100 text-purple-700',
};

export function AdminLogs() {
  const [data, setData] = useState<LogsResponse | null>(null);
  const [page, setPage] = useState(1);
  const [action, setAction] = useState('');
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const qs = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) });
      if (action) qs.set('action', action);
      setData(await api.get<LogsResponse>(`api/admin/logs?${qs.toString()}`));
    } catch {
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [page, action]);

  useEffect(() => {
    document.title = "Administration — Journal d'activité";
    load();
  }, [load]);

  const totalPages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-slate-800">Journal d'activité</h1>
          <span className="rounded-full bg-brand-600 px-3 py-1 text-xs font-semibold text-white">Admin</span>
        </div>
        <Link to="/admin/dashboard" className="text-sm font-medium text-brand-600 hover:underline">
          ← Retour au dashboard
        </Link>
      </div>

      <div className="flex items-center gap-3">
        <label className="text-sm font-medium text-slate-600">Filtrer par action :</label>
        <select
          value={action}
          onChange={(e) => {
            setPage(1);
            setAction(e.target.value);
          }}
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm focus:border-brand-500 focus:outline-none"
        >
          <option value="">Toutes les actions</option>
          {ACTIONS.map((a) => (
            <option key={a} value={a}>
              {a}
            </option>
          ))}
        </select>
      </div>

      {loading ? (
        <Spinner label="Chargement du journal..." />
      ) : data && data.items.length > 0 ? (
        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-100 bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3">Date</th>
                <th className="px-4 py-3">Utilisateur</th>
                <th className="px-4 py-3">Action</th>
                <th className="px-4 py-3">Détails</th>
                <th className="px-4 py-3">IP</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data.items.map((log) => (
                <tr key={log.id}>
                  <td className="whitespace-nowrap px-4 py-3 text-slate-500">
                    {new Date(log.createdAt).toLocaleString('fr-FR')}
                  </td>
                  <td className="px-4 py-3 font-medium text-slate-700">{log.userFullName ?? '—'}</td>
                  <td className="px-4 py-3">
                    <span
                      className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                        ACTION_STYLE[log.action] ?? 'bg-slate-100 text-slate-600'
                      }`}
                    >
                      {log.action}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-slate-500">{log.details ?? '—'}</td>
                  <td className="px-4 py-3 text-xs text-slate-400">{log.ipAddress ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="py-16 text-center text-slate-400">Aucune activité enregistrée.</p>
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
