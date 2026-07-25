import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '../lib/apiClient';
import type { ModuleContent, ExamenDetail, ExamenAttemptResult, MesResultatsModule, SubmitExerciceResult } from '../types/api';
import { Spinner } from '../components/Spinner';

type Tab = 'cours' | 'exercices' | 'quiz';

export function ModuleLearn() {
  const { moduleId } = useParams<{ moduleId: string }>();
  const modId = Number(moduleId);
  const navigate = useNavigate();

  const [content, setContent] = useState<ModuleContent | null>(null);
  const [results, setResults] = useState<MesResultatsModule | null>(null);
  const [loading, setLoading] = useState(true);
  const [tab, setTab] = useState<Tab>('cours');

  // Résultats de l'apprenant (tentatives passées + progression) — rechargé après chaque soumission.
  const refreshResults = useCallback(async () => {
    const r = await api.get<MesResultatsModule>(`api/modules/${modId}/mes-resultats`).catch(() => null);
    setResults(r);
  }, [modId]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      try {
        const data = await api.get<ModuleContent>(`api/modules/${modId}`);
        if (!cancelled) {
          setContent(data);
          document.title = data.module.titre;
          if (!data.cours) setTab(data.exercices.length > 0 ? 'exercices' : 'quiz');
        }
        await refreshResults();
      } catch {
        if (!cancelled) navigate('/formations', { replace: true });
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [modId, navigate, refreshResults]);

  if (loading) return <Spinner label="Chargement du module..." />;
  if (!content) return null;

  const { module, cours, exercices, examens } = content;

  const tabs: { key: Tab; label: string; count?: number; disabled?: boolean }[] = [
    { key: 'cours', label: '📖 Cours', disabled: !cours },
    { key: 'exercices', label: `📝 Exercices (${exercices.length})`, disabled: exercices.length === 0 },
    { key: 'quiz', label: `🎯 Quiz (${examens.length})`, disabled: examens.length === 0 },
  ];

  return (
    <div className="space-y-6">
      {/* Breadcrumb + titre */}
      <div className="space-y-1">
        <nav className="text-sm text-slate-400">
          <Link to="/formations" className="hover:underline">Formations</Link>
          {' › '}
          <Link to={`/formations/${module.formationId}`} className="hover:underline">{module.formationTitre}</Link>
          {' › '}
          <span className="text-slate-700 font-medium">{module.titre}</span>
        </nav>
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <h1 className="text-2xl font-extrabold text-slate-800">{module.titre}</h1>
          <Link
            to={`/modules/${modId}/avatar`}
            className="rounded-lg bg-purple-600 px-4 py-2 text-sm font-semibold text-white hover:bg-purple-700"
          >
            🤖 Demander au Formateur IA
          </Link>
        </div>
        {module.description && <p className="text-slate-500 text-sm">{module.description}</p>}
      </div>

      {/* Progression */}
      {results && (
        <div className="flex items-center gap-3 rounded-xl border border-slate-200 bg-white p-4">
          <div className="flex-1">
            <div className="mb-1 flex items-center justify-between text-sm">
              <span className="font-medium text-slate-700">
                {results.moduleComplete ? '✅ Module complété' : '⏳ Module en cours'}
              </span>
              <span className="text-slate-500">Progression formation : {results.progressionFormation}%</span>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-slate-100">
              <div className="h-full rounded-full bg-brand-600 transition-all"
                style={{ width: `${results.progressionFormation}%` }} />
            </div>
          </div>
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-1 border-b border-slate-200">
        {tabs.map(t => (
          <button
            key={t.key}
            disabled={t.disabled}
            onClick={() => setTab(t.key)}
            className={`px-4 py-2 text-sm font-medium rounded-t-lg transition-colors ${
              t.disabled
                ? 'text-slate-300 cursor-not-allowed'
                : tab === t.key
                  ? 'bg-white border border-b-white border-slate-200 text-brand-600 -mb-px'
                  : 'text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Contenu */}
      <div className="rounded-xl border border-slate-200 bg-white p-6">
        {tab === 'cours' && cours && <CoursTab cours={cours} moduleId={modId} />}
        {tab === 'exercices' && (
          <ExercicesTab exercices={exercices} moduleId={modId} results={results} onSubmitted={refreshResults} />
        )}
        {tab === 'quiz' && <QuizTab examens={examens} results={results} onFinished={refreshResults} />}
      </div>
    </div>
  );
}

// ── Cours ─────────────────────────────────────────────────────────────────────
function CoursTab({ cours, moduleId }: { cours: NonNullable<ModuleContent['cours']>; moduleId: number }) {
  return (
    <div className="space-y-6">
      {cours.dureeEstimeeMinutes > 0 && (
        <p className="text-sm text-slate-500">⏱️ Durée estimée : {cours.dureeEstimeeMinutes} min</p>
      )}

      {cours.videoUrl && (
        <div className="rounded-xl overflow-hidden bg-slate-900 aspect-video">
          {cours.videoUrl.includes('youtube.com') || cours.videoUrl.includes('youtu.be') ? (
            <iframe
              className="w-full h-full"
              src={toYouTubeEmbed(cours.videoUrl)}
              title="Vidéo du cours"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
            />
          ) : (
            <video src={cours.videoUrl} controls className="w-full h-full" />
          )}
        </div>
      )}

      {cours.pdfUrl && (
        <a
          href={cours.pdfUrl}
          target="_blank"
          rel="noreferrer"
          className="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
        >
          📄 Télécharger le support PDF
        </a>
      )}

      {cours.contenu ? (
        <div
          className="prose prose-slate max-w-none"
          dangerouslySetInnerHTML={{ __html: cours.contenu }}
        />
      ) : (
        <p className="text-slate-400 italic">Contenu du cours à venir.</p>
      )}

      <div className="border-t border-slate-100 pt-4">
        <Link
          to={`/modules/${moduleId}/avatar`}
          className="inline-flex items-center gap-2 rounded-lg bg-purple-600 px-4 py-2 text-sm font-semibold text-white hover:bg-purple-700"
        >
          🤖 Une question sur ce cours ? Demandez au Formateur IA
        </Link>
      </div>
    </div>
  );
}

function toYouTubeEmbed(url: string): string {
  const match = url.match(/(?:youtu\.be\/|watch\?v=|embed\/)([A-Za-z0-9_-]{11})/);
  return match ? `https://www.youtube.com/embed/${match[1]}` : url;
}

// ── Exercices ─────────────────────────────────────────────────────────────────
interface ExerciceState {
  value: string;
  submitting?: boolean;
  isCorrect?: boolean;
  correctionAuto?: boolean;
  reponseCorrecte?: string | null;
  tentativeNumero?: number;
  submitted: boolean;
}

function ExercicesTab({
  exercices,
  moduleId,
  results,
  onSubmitted,
}: {
  exercices: ModuleContent['exercices'];
  moduleId: number;
  results: MesResultatsModule | null;
  onSubmitted: () => void;
}) {
  // État initial : reflète la dernière tentative persistée (consultation).
  const [answers, setAnswers] = useState<Record<number, ExerciceState>>(() => {
    const init: Record<number, ExerciceState> = {};
    for (const r of results?.exercices ?? []) {
      init[r.exerciceId] = {
        value: r.reponse ?? '',
        submitted: true,
        isCorrect: r.isCorrect,
        tentativeNumero: r.tentativeNumero,
      };
    }
    return init;
  });

  if (exercices.length === 0) {
    return <p className="text-slate-400">Aucun exercice pour ce module.</p>;
  }

  const handleChange = (id: number, value: string) => {
    setAnswers(prev => ({ ...prev, [id]: { value, submitted: false } }));
  };

  const retry = (id: number) => {
    setAnswers(prev => ({ ...prev, [id]: { value: '', submitted: false } }));
  };

  const handleSubmit = async (ex: ModuleContent['exercices'][number]) => {
    const ans = answers[ex.id]?.value ?? '';
    setAnswers(prev => ({ ...prev, [ex.id]: { value: ans, submitted: false, submitting: true } }));
    try {
      const res = await api.post<SubmitExerciceResult>(`api/modules/exercices/${ex.id}/submit`, {
        reponse: ans,
      });
      setAnswers(prev => ({
        ...prev,
        [ex.id]: {
          value: ans,
          submitted: true,
          isCorrect: res.isCorrect,
          correctionAuto: res.correctionAuto,
          reponseCorrecte: res.reponseCorrecte,
          tentativeNumero: res.tentativeNumero,
        },
      }));
      onSubmitted(); // met à jour la progression
    } catch {
      setAnswers(prev => ({ ...prev, [ex.id]: { value: ans, submitted: false } }));
    }
  };

  return (
    <div className="space-y-8">
      {exercices.map((ex, idx) => {
        const ans = answers[ex.id];
        return (
          <div key={ex.id} className="space-y-3 rounded-xl border border-slate-100 bg-slate-50 p-5">
            <div className="flex items-start justify-between gap-2">
              <p className="font-semibold text-slate-800">
                <span className="mr-2 text-brand-600">Exercice {idx + 1}.</span>
                {ex.enonce}
              </p>
              <span className="shrink-0 text-xs text-slate-400">{ex.points} pt{ex.points > 1 ? 's' : ''}</span>
            </div>

            {ex.instructions && (
              <p className="text-sm text-slate-500 italic">{ex.instructions}</p>
            )}

            <div className="space-y-2">
              {ex.typeReponse === 'TexteLibre' ? (
                <textarea
                  value={ans?.value ?? ''}
                  onChange={e => handleChange(ex.id, e.target.value)}
                  disabled={ans?.submitted}
                  rows={4}
                  placeholder="Votre réponse..."
                  className="input resize-none"
                />
              ) : (
                <input
                  type="text"
                  value={ans?.value ?? ''}
                  onChange={e => handleChange(ex.id, e.target.value)}
                  disabled={ans?.submitted}
                  placeholder="Votre réponse..."
                  className="input"
                />
              )}

              {!ans?.submitted ? (
                <button
                  onClick={() => handleSubmit(ex)}
                  disabled={!answers[ex.id]?.value || ans?.submitting}
                  className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
                >
                  {ans?.submitting ? 'Envoi…' : 'Valider'}
                </button>
              ) : (
                <div className="space-y-2">
                  <div className={`rounded-lg p-3 text-sm ${
                    ans.isCorrect === true ? 'bg-green-50 text-green-700' :
                    ans.isCorrect === false ? 'bg-red-50 text-red-700' :
                    'bg-blue-50 text-blue-700'
                  }`}>
                    {ans.isCorrect === true && (ans.correctionAuto === false
                      ? '📨 Réponse enregistrée.'
                      : '✅ Bonne réponse !')}
                    {ans.isCorrect === false && '❌ Réponse incorrecte.'}
                    {ans.tentativeNumero ? ` (tentative n°${ans.tentativeNumero})` : ''}
                    {ans.isCorrect === false && ans.reponseCorrecte && (
                      <span className="ml-1 font-medium">Attendu : {ans.reponseCorrecte}</span>
                    )}
                  </div>
                  <button
                    onClick={() => retry(ex.id)}
                    className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-white"
                  >
                    🔄 Recommencer
                  </button>
                </div>
              )}
            </div>
          </div>
        );
      })}

      <div className="border-t border-slate-100 pt-4">
        <Link
          to={`/modules/${moduleId}/avatar`}
          className="inline-flex items-center gap-2 rounded-lg bg-purple-600 px-4 py-2 text-sm font-semibold text-white hover:bg-purple-700"
        >
          🤖 Besoin d'aide ? Demandez au Formateur IA
        </Link>
      </div>
    </div>
  );
}

// ── Quiz ──────────────────────────────────────────────────────────────────────
function QuizTab({
  examens,
  results,
  onFinished,
}: {
  examens: ModuleContent['examens'];
  results: MesResultatsModule | null;
  onFinished: () => void;
}) {
  const [selectedExamen, setSelectedExamen] = useState<number | null>(null);
  const [result, setResult] = useState<ExamenAttemptResult | null>(null);

  if (examens.length === 0) {
    return <p className="text-slate-400">Aucun quiz disponible pour ce module.</p>;
  }

  if (result) {
    return <QuizResult result={result} onRetry={() => { setResult(null); setSelectedExamen(null); }} />;
  }

  if (selectedExamen !== null) {
    return (
      <QuizRunner
        examenId={selectedExamen}
        onFinish={(r) => { setResult(r); onFinished(); }}
        onCancel={() => setSelectedExamen(null)}
      />
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-slate-500">Choisissez un quiz à passer :</p>
      {examens.map(ex => {
        const prev = results?.examens.find(e => e.examenId === ex.id);
        return (
        <div
          key={ex.id}
          className="flex items-center justify-between gap-4 rounded-xl border border-slate-200 p-4"
        >
          <div className="space-y-0.5">
            <p className="font-semibold text-slate-800">{ex.titre}</p>
            {ex.description && <p className="text-sm text-slate-500">{ex.description}</p>}
            <div className="flex gap-3 text-xs text-slate-400 flex-wrap">
              <span>⏱️ {ex.dureeMinutes} min</span>
              <span>✅ Note minimale : {ex.noteMinPassage}%</span>
              <span>🔄 {ex.tentativesMax} tentative{ex.tentativesMax > 1 ? 's' : ''} max</span>
              <span>❓ {ex.nbQuestions} question{ex.nbQuestions > 1 ? 's' : ''}</span>
            </div>
            {prev && (
              <p className={`text-xs font-medium ${prev.isReussi ? 'text-green-600' : 'text-red-600'}`}>
                {prev.isReussi ? '✅ Réussi' : '❌ Échoué'} — dernier score {prev.score}% (tentative n°{prev.tentativeNumero})
              </p>
            )}
          </div>
          {ex.isActif ? (
            <button
              onClick={() => setSelectedExamen(ex.id)}
              className="shrink-0 rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
            >
              {prev ? 'Recommencer' : 'Commencer'}
            </button>
          ) : (
            <span className="text-xs text-slate-400">Désactivé</span>
          )}
        </div>
        );
      })}
    </div>
  );
}

// ── Quiz Runner ───────────────────────────────────────────────────────────────
function QuizRunner({
  examenId,
  onFinish,
  onCancel,
}: {
  examenId: number;
  onFinish: (r: ExamenAttemptResult) => void;
  onCancel: () => void;
}) {
  const [detail, setDetail] = useState<ExamenDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [answers, setAnswers] = useState<Record<number, string>>({});
  const [timeLeft, setTimeLeft] = useState<number>(0);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const startRef = useRef(Date.now());

  useEffect(() => {
    api.get<ExamenDetail>(`api/modules/examens/${examenId}/questions`)
      .then(d => {
        setDetail(d);
        setTimeLeft(d.examen.dureeMinutes * 60);
      })
      .catch(e => setError(e instanceof ApiError ? e.message : 'Erreur de chargement'))
      .finally(() => setLoading(false));
  }, [examenId]);

  useEffect(() => {
    if (!detail || timeLeft <= 0) return;
    const id = setInterval(() => {
      setTimeLeft(t => {
        if (t <= 1) { clearInterval(id); handleSubmit(); return 0; }
        return t - 1;
      });
    }, 1000);
    return () => clearInterval(id);
  }, [detail]);

  const handleSubmit = async () => {
    if (!detail || submitting) return;
    setSubmitting(true);
    const duree = Math.round((Date.now() - startRef.current) / 1000);
    try {
      const result = await api.post<ExamenAttemptResult>(`api/modules/examens/${examenId}/attempt`, {
        examenId,
        reponses: answers,
        dureeSecondes: duree,
      });
      onFinish(result);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Erreur lors de la soumission.');
      setSubmitting(false);
    }
  };

  if (loading) return <Spinner label="Chargement du quiz..." />;
  if (error) return <p className="text-red-600 text-sm">{error}</p>;
  if (!detail) return null;

  const mm = String(Math.floor(timeLeft / 60)).padStart(2, '0');
  const ss = String(timeLeft % 60).padStart(2, '0');
  const timerDanger = timeLeft < 60;

  const answered = Object.keys(answers).length;
  const total = detail.questions.length;

  return (
    <div className="space-y-6">
      {/* En-tête sticky */}
      <div className="sticky top-16 z-10 flex items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white/90 px-5 py-3 backdrop-blur">
        <div>
          <p className="font-semibold text-slate-800">{detail.examen.titre}</p>
          <p className="text-xs text-slate-400">{answered} / {total} réponses</p>
        </div>
        <div className={`text-2xl font-mono font-bold ${timerDanger ? 'text-red-600 animate-pulse' : 'text-slate-700'}`}>
          {mm}:{ss}
        </div>
      </div>

      {/* Questions */}
      {detail.questions.map((q, idx) => (
        <div key={q.id} className="space-y-3 rounded-xl border border-slate-100 bg-slate-50 p-5">
          <p className="font-medium text-slate-800">
            <span className="text-brand-600 font-semibold mr-2">{idx + 1}.</span>
            {q.enonce}
            <span className="ml-2 text-xs text-slate-400">({q.points} pt{q.points > 1 ? 's' : ''})</span>
          </p>

          {q.choix.length > 0 ? (
            <div className="space-y-2">
              {q.choix.map(choice => (
                <label key={choice} className="flex items-center gap-3 cursor-pointer rounded-lg border border-transparent hover:border-brand-200 hover:bg-brand-50 px-3 py-2 transition-colors">
                  <input
                    type="radio"
                    name={`q-${q.id}`}
                    value={choice}
                    checked={answers[q.id] === choice}
                    onChange={() => setAnswers(prev => ({ ...prev, [q.id]: choice }))}
                    className="text-brand-600"
                  />
                  <span className="text-sm text-slate-700">{choice}</span>
                </label>
              ))}
            </div>
          ) : (
            <input
              type="text"
              value={answers[q.id] ?? ''}
              onChange={e => setAnswers(prev => ({ ...prev, [q.id]: e.target.value }))}
              placeholder="Votre réponse..."
              className="input"
            />
          )}
        </div>
      ))}

      {error && <p className="text-sm text-red-600">{error}</p>}

      <div className="flex gap-3">
        <button
          onClick={handleSubmit}
          disabled={submitting}
          className="rounded-lg bg-brand-600 px-6 py-2.5 font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
        >
          {submitting ? 'Envoi...' : 'Soumettre le quiz'}
        </button>
        <button onClick={onCancel} className="rounded-lg border border-slate-300 px-4 py-2 text-sm text-slate-600 hover:bg-slate-50">
          Annuler
        </button>
      </div>
    </div>
  );
}

// ── Quiz Result ───────────────────────────────────────────────────────────────
function QuizResult({ result, onRetry }: { result: ExamenAttemptResult; onRetry: () => void }) {
  return (
    <div className="space-y-6">
      {/* Score */}
      <div className={`rounded-2xl p-6 text-center ${result.isReussi ? 'bg-green-50' : 'bg-red-50'}`}>
        <div className="text-6xl font-extrabold mb-2" style={{ color: result.isReussi ? '#16a34a' : '#dc2626' }}>
          {result.score}%
        </div>
        <p className={`text-lg font-semibold ${result.isReussi ? 'text-green-700' : 'text-red-700'}`}>
          {result.isReussi ? '🎉 Félicitations, vous avez réussi !' : '😕 Score insuffisant — essayez encore !'}
        </p>
        <p className="text-sm text-slate-500 mt-1">
          {result.correctCount} / {result.totalQuestions} bonne{result.correctCount > 1 ? 's' : ''} réponse{result.correctCount > 1 ? 's' : ''}
          {' · '}Tentative n°{result.tentativeNumero}
        </p>
      </div>

      {/* Review */}
      <div className="space-y-3">
        <h3 className="font-semibold text-slate-800">Correction détaillée</h3>
        {result.review.map((r, idx) => (
          <div key={r.questionId} className={`rounded-xl border p-4 space-y-1 ${r.isCorrect ? 'border-green-200 bg-green-50' : 'border-red-200 bg-red-50'}`}>
            <p className="font-medium text-slate-800 text-sm">
              {r.isCorrect ? '✅' : '❌'} <span className="font-semibold mr-1">{idx + 1}.</span>{r.enonce}
            </p>
            {!r.isCorrect && (
              <>
                <p className="text-sm text-red-700">Votre réponse : <em>{r.userAnswer || '(aucune)'}</em></p>
                <p className="text-sm text-green-700 font-medium">Bonne réponse : {r.bonneReponse}</p>
              </>
            )}
            {r.explication && (
              <p className="text-xs text-slate-500 italic mt-1">{r.explication}</p>
            )}
          </div>
        ))}
      </div>

      <button
        onClick={onRetry}
        className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
      >
        ← Retour aux quiz
      </button>
    </div>
  );
}
