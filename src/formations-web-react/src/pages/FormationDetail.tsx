import { useEffect, useState, type ReactNode } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '../lib/apiClient';
import type { Formation, ModuleDto, Inscription } from '../types/api';
import { useAuth } from '../context/AuthContext';
import { Spinner, SpinnerSm } from '../components/Spinner';

export function FormationDetail() {
  const { id } = useParams<{ id: string }>();
  const formationId = Number(id);
  const navigate = useNavigate();
  const { isAuthenticated, isStaff } = useAuth();

  const [formation, setFormation] = useState<Formation | null>(null);
  const [modules, setModules] = useState<ModuleDto[]>([]);
  const [isInscrit, setIsInscrit] = useState(false);
  const [loading, setLoading] = useState(true);
  const [enrolling, setEnrolling] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      try {
        const f = await api.get<Formation>(`api/formations/${formationId}`);
        if (cancelled) return;
        setFormation(f);
        document.title = f.titre;

        // Les modules ne sont pas inclus dans le détail formation : appel séparé.
        const mods = await api.get<ModuleDto[]>(`api/modules/formation/${formationId}`).catch(() => []);
        if (!cancelled) setModules(mods.sort((a, b) => a.ordre - b.ordre));

        if (isAuthenticated) {
          const mesForms = await api.get<Inscription[]>('api/inscriptions/mes-formations').catch(() => []);
          if (!cancelled) setIsInscrit(mesForms.some((i) => i.formationId === formationId));
        }
      } catch (e) {
        if (!cancelled) setFormation(null);
        console.error(e);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [formationId, isAuthenticated]);

  const enroll = async () => {
    setEnrolling(true);
    setError(null);
    try {
      await api.post('api/inscriptions', { formationId });
      setIsInscrit(true);
      navigate('/mes-formations');
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Échec de l'inscription.");
    } finally {
      setEnrolling(false);
    }
  };

  if (loading) return <Spinner label="Chargement..." />;

  if (!formation) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-slate-400">
        <span className="text-5xl">❌</span>
        <p>Formation introuvable.</p>
        <Link to="/formations" className="rounded-lg bg-brand-600 px-4 py-2 text-white">
          Retour aux formations
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Hero */}
      <div
        className="relative overflow-hidden rounded-2xl bg-slate-800 bg-cover bg-center p-8 text-white"
        style={formation.imageUrl ? { backgroundImage: `url('${formation.imageUrl}')` } : undefined}
      >
        <div className="absolute inset-0 bg-slate-900/70" />
        <div className="relative space-y-3">
          <nav className="text-sm text-slate-300">
            <Link to="/" className="hover:underline">
              Accueil
            </Link>{' '}
            ›{' '}
            <Link to="/formations" className="hover:underline">
              Formations
            </Link>{' '}
            › <span>{formation.titre}</span>
          </nav>
          <h1 className="text-3xl font-extrabold">{formation.titre}</h1>
          <div className="flex flex-wrap gap-3 text-sm">
            <span className="rounded-full bg-brand-600 px-3 py-1">{formation.niveauLabel}</span>
            <span>📁 {formation.categorieNom}</span>
            <span>⏱️ {formation.dureeHeures} heures</span>
            <span>👥 {formation.nbInscrits} inscrits</span>
            <span>📦 {formation.nbModules} modules</span>
          </div>
        </div>
      </div>

      <div className="grid gap-8 lg:grid-cols-[1fr_320px]">
        {/* Contenu principal */}
        <div className="space-y-6">
          {formation.description && (
            <Section title="À propos de cette formation">{formation.description}</Section>
          )}
          {formation.objectifs && <Section title="Objectifs pédagogiques">{formation.objectifs}</Section>}
          {formation.prerequis && <Section title="Prérequis">{formation.prerequis}</Section>}

          <section className="space-y-3">
            <h2 className="text-xl font-bold text-slate-800">Contenu ({modules.length} modules)</h2>
            <div className="divide-y divide-slate-100 rounded-xl border border-slate-200 bg-white">
              {modules.length === 0 && (
                <p className="p-4 text-sm text-slate-400">Aucun module publié pour le moment.</p>
              )}
              {modules.map((m) => (
                <div key={m.id} className="flex items-center gap-3 p-4">
                  <span className="text-2xl">{m.hasCours ? '📖' : '📝'}</span>
                  <div className="flex-1">
                    <span className="font-medium text-slate-700">{m.titre}</span>
                    <div className="flex gap-2 text-xs text-slate-400 mt-0.5">
                      {m.hasCours && <span>📄 Cours</span>}
                      {m.nbExercices > 0 && <span>📝 {m.nbExercices} exercice{m.nbExercices > 1 ? 's' : ''}</span>}
                      {m.nbExamens > 0 && <span>🎯 {m.nbExamens} quiz</span>}
                    </div>
                  </div>
                  {isInscrit && (
                    <div className="flex gap-2 shrink-0">
                      <Link
                        to={`/modules/${m.id}/apprendre`}
                        className="rounded-lg bg-brand-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-brand-700"
                      >
                        📖 Apprendre
                      </Link>
                      <Link
                        to={`/modules/${m.id}/avatar`}
                        className="rounded-lg bg-purple-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-purple-700"
                      >
                        🤖 IA
                      </Link>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </section>
        </div>

        {/* Sidebar inscription */}
        <aside className="h-fit space-y-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:sticky lg:top-20">
          <div className="text-center text-3xl font-extrabold text-brand-700">
            {formation.prix === 0 ? 'GRATUIT' : `${formation.prix} €`}
          </div>

          {error && (
            <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</div>
          )}

          {isStaff ? (
            /* Admin / Formateur : pas d'inscription, mais accès à la gestion du contenu */
            <Link
              to={`/gestion/formations/${formation.id}/modules`}
              className="block rounded-lg bg-brand-600 py-3 text-center font-semibold text-white hover:bg-brand-700"
            >
              🛠️ Gérer le contenu
            </Link>
          ) : isAuthenticated ? (
            isInscrit ? (
              <>
                <div className="rounded-lg bg-green-50 py-2 text-center text-sm font-medium text-green-700">
                  ✅ Déjà inscrit
                </div>
                <Link
                  to="/mes-formations"
                  className="block rounded-lg border border-brand-300 py-2.5 text-center font-semibold text-brand-700 hover:bg-brand-50"
                >
                  Voir ma progression
                </Link>
              </>
            ) : (
              <button
                onClick={enroll}
                disabled={enrolling}
                className="flex w-full items-center justify-center gap-2 rounded-lg bg-brand-600 py-3 font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
              >
                {enrolling && <SpinnerSm />}
                S'inscrire maintenant
              </button>
            )
          ) : (
            <>
              <Link
                to={`/connexion?returnUrl=/formations/${formation.id}`}
                className="block rounded-lg bg-brand-600 py-3 text-center font-semibold text-white hover:bg-brand-700"
              >
                Se connecter pour s'inscrire
              </Link>
              <Link
                to="/inscription"
                className="block rounded-lg border border-slate-300 py-2.5 text-center font-medium text-slate-600 hover:bg-slate-50"
              >
                Créer un compte gratuit
              </Link>
            </>
          )}

          <ul className="space-y-1.5 border-t border-slate-100 pt-4 text-sm text-slate-600">
            <li>📦 {formation.nbModules} modules de cours</li>
            <li>⏱️ {formation.dureeHeures} heures de contenu</li>
            <li>🤖 Formateur IA disponible 24h/24</li>
            <li>📜 Certificat de réussite</li>
            <li>♾️ Accès à vie</li>
          </ul>
        </aside>
      </div>
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="space-y-2">
      <h2 className="text-xl font-bold text-slate-800">{title}</h2>
      <p className="leading-relaxed text-slate-600">{children}</p>
    </section>
  );
}
