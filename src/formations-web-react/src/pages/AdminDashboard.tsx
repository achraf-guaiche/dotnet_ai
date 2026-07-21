import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/apiClient';
import type { DashboardStats } from '../types/api';
import { Spinner } from '../components/Spinner';

export function AdminDashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);

  useEffect(() => {
    document.title = 'Administration — Dashboard';
    api.get<DashboardStats>('api/admin/dashboard').then(setStats).catch(() => setStats(null));
  }, []);

  if (!stats) return <Spinner label="Chargement du dashboard..." />;

  const maxInscrits = Math.max(1, ...stats.topFormations.map((f) => f.nbInscrits));

  return (
    <div className="space-y-8">
      <div className="flex items-center gap-3">
        <h1 className="text-2xl font-bold text-slate-800">Dashboard Administration</h1>
        <span className="rounded-full bg-brand-600 px-3 py-1 text-xs font-semibold text-white">Admin</span>
      </div>

      {/* Widgets stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatWidget icon="📚" color="bg-blue-100" value={stats.totalFormations} label="Formations" sub={`${stats.formationsPubliees} publiées`} />
        <StatWidget icon="👥" color="bg-green-100" value={stats.totalApprenants} label="Apprenants" />
        <StatWidget icon="🎓" color="bg-purple-100" value={stats.totalFormateurs} label="Formateurs" />
        <StatWidget icon="📈" color="bg-orange-100" value={`${stats.tauxCompletionMoyen}%`} label="Taux complétion" />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Graphique : inscrits par formation (barres CSS, sans dépendance) */}
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="mb-4 font-bold text-slate-800">Top formations — inscrits</h3>
          <div className="space-y-3">
            {stats.topFormations.length === 0 && <p className="text-sm text-slate-400">Aucune donnée.</p>}
            {stats.topFormations.map((f) => (
              <div key={f.id} className="space-y-1">
                <div className="flex justify-between text-sm">
                  <span className="truncate pr-2 text-slate-600">{f.titre}</span>
                  <span className="font-semibold text-slate-800">{f.nbInscrits}</span>
                </div>
                <div className="h-3 overflow-hidden rounded-full bg-slate-100">
                  <div
                    className="h-full rounded-full bg-gradient-to-r from-brand-500 to-brand-700"
                    style={{ width: `${(f.nbInscrits / maxInscrits) * 100}%` }}
                  />
                </div>
                <div className="text-right text-xs text-slate-400">Complétion {f.tauxCompletion}%</div>
              </div>
            ))}
          </div>
        </div>

        {/* Inscriptions récentes */}
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="mb-4 font-bold text-slate-800">Inscriptions récentes</h3>
          <ul className="space-y-3">
            {stats.recentesInscriptions.length === 0 && (
              <li className="text-sm text-slate-400">Aucune inscription récente.</li>
            )}
            {stats.recentesInscriptions.map((i, idx) => (
              <li key={idx} className="flex items-center gap-3 text-sm">
                <span className="text-lg">📝</span>
                <div className="flex-1">
                  <strong className="text-slate-700">{i.userFullName}</strong> s'est inscrit à{' '}
                  <em className="text-slate-600">{i.formationTitre}</em>
                </div>
                <span className="text-xs text-slate-400">
                  {new Date(i.date).toLocaleDateString('fr-FR', {
                    day: '2-digit',
                    month: '2-digit',
                    hour: '2-digit',
                    minute: '2-digit',
                  })}
                </span>
              </li>
            ))}
          </ul>
        </div>
      </div>

      {/* Navigation admin */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <NavCard to="/admin/utilisateurs" label="👤 Gérer les utilisateurs" />
        <NavCard to="/formations" label="📚 Voir les formations" />
      </div>
    </div>
  );
}

function StatWidget({
  icon,
  color,
  value,
  label,
  sub,
}: {
  icon: string;
  color: string;
  value: string | number;
  label: string;
  sub?: string;
}) {
  return (
    <div className="flex items-center gap-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className={`flex h-12 w-12 items-center justify-center rounded-xl text-2xl ${color}`}>{icon}</div>
      <div>
        <div className="text-2xl font-extrabold text-slate-800">{value}</div>
        <div className="text-sm text-slate-500">{label}</div>
        {sub && <div className="text-xs text-slate-400">{sub}</div>}
      </div>
    </div>
  );
}

function NavCard({ to, label }: { to: string; label: string }) {
  return (
    <Link
      to={to}
      className="rounded-xl border border-slate-200 bg-white p-4 text-center font-medium text-slate-700 shadow-sm transition hover:-translate-y-0.5 hover:border-brand-300 hover:shadow-md"
    >
      {label}
    </Link>
  );
}
