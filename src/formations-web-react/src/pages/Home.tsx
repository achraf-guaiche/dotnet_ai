import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/apiClient';
import type { AppConfig, Categorie, FormationSummary, PagedResult } from '../types/api';
import { FormationCard } from '../components/FormationCard';

const PROVIDER_LABEL: Record<string, string> = {
  claude: 'Claude (Anthropic)',
  openai: 'OpenAI',
  google: 'Google Gemini',
  groq: 'Groq',
  llm: 'LLM générique',
};

export function Home() {
  const [categories, setCategories] = useState<Categorie[]>([]);
  const [featured, setFeatured] = useState<PagedResult<FormationSummary> | null>(null);
  const [config, setConfig] = useState<AppConfig | null>(null);

  useEffect(() => {
    document.title = 'Plateforme Formations — Accueil';
    api.get<Categorie[]>('api/categories').then(setCategories).catch(() => setCategories([]));
    api.get<AppConfig>('api/config').then(setConfig).catch(() => setConfig(null));
    api
      .get<PagedResult<FormationSummary>>('api/formations?page=1&pageSize=6')
      .then(setFeatured)
      .catch(() => setFeatured(null));
  }, []);

  const totalFormations = featured?.totalCount ?? 0;

  return (
    <div className="space-y-14">
      {/* Bandeau Mode dev : fournisseur LLM sélectionné (depuis /api/config) */}
      {config?.isDevMode && (
        <div className="flex flex-wrap items-center gap-3 rounded-xl border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          <span className="rounded-full bg-amber-500 px-2.5 py-0.5 text-xs font-bold uppercase text-white">
            Mode dev
          </span>
          <span>
            🤖 Formateur IA — LLM :{' '}
            <strong>{PROVIDER_LABEL[config.llmProvider] ?? config.llmProvider}</strong>
            {config.llmModel ? ` (${config.llmModel})` : ''}
            {config.llmMock ? ' · réponses simulées (aucune clé API)' : ' · clé API active'}
          </span>
          <span className="text-amber-600">
            Avatar : <strong>{config.avatarProvider}</strong>
            {config.avatarMock ? ' (mock)' : ''}
          </span>
        </div>
      )}

      {/* Hero */}
      <section className="grid items-center gap-8 rounded-2xl bg-gradient-to-br from-brand-600 to-brand-700 p-8 text-white md:grid-cols-2 md:p-12">
        <div className="space-y-5">
          <h1 className="text-3xl font-extrabold leading-tight md:text-4xl">
            Apprenez avec les meilleurs formateurs
          </h1>
          <p className="text-brand-100">
            Accédez à des formations de qualité, avec un formateur IA disponible 24h/24.
          </p>
          <div className="flex flex-wrap gap-3">
            <Link
              to="/formations"
              className="rounded-lg bg-white px-5 py-2.5 font-semibold text-brand-700 hover:bg-brand-50"
            >
              Explorer les formations
            </Link>
            <Link
              to="/inscription"
              className="rounded-lg border border-white/60 px-5 py-2.5 font-semibold text-white hover:bg-white/10"
            >
              Commencer gratuitement
            </Link>
          </div>
        </div>
        <div className="grid grid-cols-3 gap-4">
          <Stat number={`${totalFormations}+`} label="Formations" />
          <Stat number={`${totalFormations * 12}+`} label="Apprenants" />
          <Stat number="IA" label="Formateur 24h/24" />
        </div>
      </section>

      {/* Catégories */}
      {categories.length > 0 && (
        <section className="space-y-4">
          <h2 className="text-2xl font-bold text-slate-800">Explorez par catégorie</h2>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {categories.map((cat) => (
              <Link
                key={cat.id}
                to={`/formations?categorieId=${cat.id}`}
                className="flex flex-col items-center gap-2 rounded-xl border border-slate-200 bg-white p-5 text-center shadow-sm transition hover:-translate-y-1 hover:border-brand-300 hover:shadow-md"
              >
                <span className="text-3xl">{cat.icone || '📚'}</span>
                <span className="font-semibold text-slate-800">{cat.nom}</span>
                <span className="text-xs text-slate-400">{cat.nbFormations} formations</span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {/* Formations populaires */}
      {featured && featured.items.length > 0 && (
        <section className="space-y-4">
          <h2 className="text-2xl font-bold text-slate-800">Formations populaires</h2>
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {featured.items.slice(0, 6).map((f) => (
              <FormationCard key={f.id} formation={f} />
            ))}
          </div>
          <div className="text-center">
            <Link
              to="/formations"
              className="inline-block rounded-lg border border-brand-300 px-5 py-2.5 font-semibold text-brand-700 hover:bg-brand-50"
            >
              Voir toutes les formations
            </Link>
          </div>
        </section>
      )}
    </div>
  );
}

function Stat({ number, label }: { number: string; label: string }) {
  return (
    <div className="rounded-xl bg-white/10 p-4 text-center backdrop-blur">
      <div className="text-2xl font-extrabold">{number}</div>
      <div className="text-xs text-brand-100">{label}</div>
    </div>
  );
}
