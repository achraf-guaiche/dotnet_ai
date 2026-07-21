import { Link } from 'react-router-dom';
import type { FormationSummary } from '../types/api';

export function FormationCard({ formation }: { formation: FormationSummary }) {
  return (
    <Link
      to={`/formations/${formation.id}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm transition hover:-translate-y-1 hover:shadow-md"
    >
      {formation.imageUrl ? (
        <img
          src={formation.imageUrl}
          alt={formation.titre}
          className="h-40 w-full object-cover"
          loading="lazy"
        />
      ) : (
        <div className="flex h-40 w-full items-center justify-center bg-gradient-to-br from-brand-100 to-brand-50 text-5xl">
          🎓
        </div>
      )}

      <div className="flex flex-1 flex-col gap-2 p-4">
        <span className="text-xs font-medium uppercase tracking-wide text-brand-600">
          {formation.categorieNom}
        </span>
        <h3 className="line-clamp-2 font-semibold text-slate-800 group-hover:text-brand-700">
          {formation.titre}
        </h3>

        <div className="mt-auto flex items-center justify-between pt-2 text-sm text-slate-500">
          <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs">{formation.niveauLabel}</span>
          <span>⏱️ {formation.dureeHeures}h</span>
        </div>

        <div className="flex items-center justify-between pt-1">
          <span className="text-xs text-slate-400">👥 {formation.nbInscrits} inscrits</span>
          <span className="font-bold text-brand-700">
            {formation.prix === 0 ? 'GRATUIT' : `${formation.prix} €`}
          </span>
        </div>
      </div>
    </Link>
  );
}
