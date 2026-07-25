import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, ApiError } from '../lib/apiClient';
import type { ModuleDto, ModuleContent, GeneratedModuleResult, GenerateAllResult } from '../types/api';
import { Spinner, SpinnerSm } from '../components/Spinner';

export function GestionModules() {
  const { id } = useParams<{ id: string }>();
  const formationId = Number(id);

  const [modules, setModules] = useState<ModuleDto[]>([]);
  const [formationTitre, setFormationTitre] = useState('');
  const [loading, setLoading] = useState(true);
  const [newTitre, setNewTitre] = useState('');
  const [adding, setAdding] = useState(false);
  const [openId, setOpenId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const mods = await api.get<ModuleDto[]>(`api/modules/formation/${formationId}`);
      setModules(mods.sort((a, b) => a.ordre - b.ordre));
      if (mods[0]) setFormationTitre(mods[0].formationTitre);
      else {
        const f = await api.get<{ titre: string }>(`api/formations/${formationId}`);
        setFormationTitre(f.titre);
      }
    } catch {
      setModules([]);
    } finally {
      setLoading(false);
    }
  }, [formationId]);

  useEffect(() => {
    document.title = 'Gestion des modules';
    load();
  }, [load]);

  const addModule = async (e: FormEvent) => {
    e.preventDefault();
    if (!newTitre.trim()) return;
    setAdding(true);
    setError(null);
    try {
      await api.post('api/modules', {
        formationId,
        titre: newTitre.trim(),
        ordre: modules.length + 1,
        description: null,
      });
      setNewTitre('');
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Échec de l'ajout du module.");
    } finally {
      setAdding(false);
    }
  };

  const deleteModule = async (moduleId: number, titre: string) => {
    if (!confirm(`Supprimer le module « ${titre} » et tout son contenu ?`)) return;
    try {
      await api.del(`api/modules/${moduleId}`);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Échec de la suppression.');
    }
  };

  if (loading) return <Spinner label="Chargement..." />;

  return (
    <div className="space-y-6">
      <div>
        <Link to="/gestion/formations" className="text-sm font-medium text-brand-600 hover:underline">
          ← Retour aux formations
        </Link>
        <h1 className="mt-1 text-2xl font-bold text-slate-800">Modules — {formationTitre}</h1>
      </div>

      {error && <div className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</div>}

      {/* Ajout d'un module */}
      <form onSubmit={addModule} className="flex gap-2 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <input
          className="input"
          placeholder="Titre du nouveau module (ex: Introduction)"
          value={newTitre}
          onChange={(e) => setNewTitre(e.target.value)}
        />
        <button type="submit" disabled={adding}
          className="flex items-center gap-2 whitespace-nowrap rounded-lg bg-brand-600 px-4 py-2 font-semibold text-white hover:bg-brand-700 disabled:opacity-60">
          {adding && <SpinnerSm />} + Module
        </button>
      </form>

      {/* Génération IA de tous les modules à partir d'un prompt global */}
      {modules.length > 0 && <FormationGeneratePanel formationId={formationId} onGenerated={load} />}

      {modules.length === 0 ? (
        <p className="py-8 text-center text-slate-400">Aucun module. Ajoutez-en un ci-dessus.</p>
      ) : (
        <div className="space-y-3">
          {modules.map((m) => (
            <div key={m.id} className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
              <div className="flex items-center gap-3 p-4">
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-brand-700">
                  {m.ordre}
                </span>
                <div className="flex-1">
                  <div className="font-semibold text-slate-800">{m.titre}</div>
                  <div className="text-xs text-slate-400">
                    {m.hasCours ? '📖 cours' : 'pas de cours'} · {m.nbExercices} exercice(s) · {m.nbExamens} examen(s)
                  </div>
                </div>
                <button
                  onClick={() => setOpenId(openId === m.id ? null : m.id)}
                  className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-50">
                  {openId === m.id ? 'Fermer' : 'Gérer le contenu'}
                </button>
                <button
                  onClick={() => deleteModule(m.id, m.titre)}
                  className="rounded-lg bg-red-500 px-3 py-1.5 text-sm font-semibold text-white hover:bg-red-600">
                  Suppr.
                </button>
              </div>

              {openId === m.id && (
                <div className="border-t border-slate-100 bg-slate-50 p-4">
                  <ModuleContentEditor moduleId={m.id} onChanged={load} />
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ── Éditeur de contenu d'un module (cours / exercices / examens) ─────────────
function ModuleContentEditor({ moduleId, onChanged }: { moduleId: number; onChanged: () => void }) {
  const [content, setContent] = useState<ModuleContent | null>(null);
  const [loading, setLoading] = useState(true);
  // Incrémenté à chaque rechargement pour forcer le remontage des éditeurs (leur état
  // interne est initialisé depuis les props ; sans remontage, un contenu régénéré ne
  // s'afficherait pas dans les champs).
  const [ver, setVer] = useState(0);

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      setContent(await api.get<ModuleContent>(`api/modules/${moduleId}`));
      setVer((v) => v + 1);
    } catch {
      setContent(null);
    } finally {
      setLoading(false);
    }
  }, [moduleId]);

  useEffect(() => {
    reload();
  }, [reload]);

  const afterChange = async () => {
    await reload();
    onChanged();
  };

  if (loading) return <Spinner label="Chargement du contenu..." />;
  if (!content) return <p className="text-sm text-red-500">Erreur de chargement.</p>;

  return (
    <div className="space-y-6">
      <ModuleGeneratePanel moduleId={moduleId} onGenerated={afterChange} />
      <CoursEditor key={`cours-${ver}`} moduleId={moduleId} cours={content.cours} onSaved={afterChange} />
      <ExerciceEditor moduleId={moduleId} exercices={content.exercices} onSaved={afterChange} />
      <ExamenEditor key={`exam-${ver}`} moduleId={moduleId} examens={content.examens} onSaved={afterChange} />
    </div>
  );
}

// ── Génération IA (formation entière) ─────────────────────────────────────────
function FormationGeneratePanel({ formationId, onGenerated }: { formationId: number; onGenerated: () => void }) {
  const [prompt, setPrompt] = useState('');
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const generate = async () => {
    if (!prompt.trim()) return;
    setBusy(true);
    setMsg(null);
    setErr(null);
    try {
      const res = await api.post<GenerateAllResult>(`api/formations/${formationId}/generate`, {
        prompt: prompt.trim(),
      });
      setMsg(`✅ Contenu généré pour ${res.modulesGeneres} module(s) : cours, script du prof IA et quiz.`);
      onGenerated();
    } catch (e) {
      setErr(e instanceof ApiError ? e.message : 'Échec de la génération.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-3 rounded-2xl border border-purple-200 bg-purple-50 p-4 shadow-sm">
      <div className="flex items-center gap-2">
        <span className="text-2xl">🤖</span>
        <h2 className="font-bold text-purple-800">Générer tout le contenu avec l'IA</h2>
      </div>
      <p className="text-sm text-purple-700">
        Décrivez le sujet et le public. L'IA rédige, pour <strong>chaque module</strong>, le cours, le
        script présenté par le prof IA vidéo, et un quiz. Le contenu existant sera remplacé.
      </p>
      <textarea
        className="input min-h-20 bg-white"
        placeholder="Ex : Formation d'initiation à Python pour débutants. Ton pédagogique, exemples concrets, progression douce."
        value={prompt}
        onChange={(e) => setPrompt(e.target.value)}
      />
      <div className="flex items-center gap-3">
        <button
          onClick={generate}
          disabled={busy || !prompt.trim()}
          className="flex items-center gap-2 rounded-lg bg-purple-600 px-4 py-2 text-sm font-semibold text-white hover:bg-purple-700 disabled:opacity-60"
        >
          {busy && <SpinnerSm />} {busy ? 'Génération en cours…' : '✨ Générer tous les modules'}
        </button>
        {msg && <span className="text-sm text-green-700">{msg}</span>}
        {err && <span className="text-sm text-red-600">{err}</span>}
      </div>
    </div>
  );
}

// ── Génération IA (un seul module) ────────────────────────────────────────────
function ModuleGeneratePanel({ moduleId, onGenerated }: { moduleId: number; onGenerated: () => void }) {
  const [prompt, setPrompt] = useState('');
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const generate = async () => {
    if (!prompt.trim()) return;
    setBusy(true);
    setMsg(null);
    setErr(null);
    try {
      const res = await api.post<GeneratedModuleResult>(`api/modules/${moduleId}/generate`, {
        prompt: prompt.trim(),
      });
      setMsg(`✅ Généré : cours + script prof IA + ${res.nbQuestionsGenerees} question(s) de quiz.`);
      setPrompt('');
      onGenerated();
    } catch (e) {
      setErr(e instanceof ApiError ? e.message : 'Échec de la génération.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-2 rounded-xl border border-purple-200 bg-purple-50 p-4">
      <h3 className="font-bold text-purple-800">🤖 Générer / régénérer ce module avec l'IA</h3>
      <textarea
        className="input min-h-16 bg-white"
        placeholder="Décrivez ce que ce module doit enseigner (le cours, le script du prof IA et le quiz seront (re)générés)."
        value={prompt}
        onChange={(e) => setPrompt(e.target.value)}
      />
      <div className="flex items-center gap-3">
        <button
          onClick={generate}
          disabled={busy || !prompt.trim()}
          className="flex items-center gap-2 rounded-lg bg-purple-600 px-4 py-2 text-sm font-semibold text-white hover:bg-purple-700 disabled:opacity-60"
        >
          {busy && <SpinnerSm />} {busy ? 'Génération…' : '✨ Générer ce module'}
        </button>
        {msg && <span className="text-sm text-green-700">{msg}</span>}
        {err && <span className="text-sm text-red-600">{err}</span>}
      </div>
    </div>
  );
}

// ── Cours ────────────────────────────────────────────────────────────────────
function CoursEditor({
  moduleId,
  cours,
  onSaved,
}: {
  moduleId: number;
  cours: ModuleContent['cours'];
  onSaved: () => void;
}) {
  const [contenu, setContenu] = useState(cours?.contenu ?? '');
  const [videoUrl, setVideoUrl] = useState(cours?.videoUrl ?? '');
  const [pdfUrl, setPdfUrl] = useState(cours?.pdfUrl ?? '');
  const [textePourAvatar, setTexte] = useState(cours?.textePourAvatar ?? '');
  const [duree, setDuree] = useState(cours?.dureeEstimeeMinutes ?? 15);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  const save = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMsg(null);
    try {
      const body = {
        moduleId,
        contenu: contenu || null,
        videoUrl: videoUrl || null,
        pdfUrl: pdfUrl || null,
        textePourAvatar: textePourAvatar || null,
        dureeEstimeeMinutes: duree,
      };
      // POST si aucun cours n'existe encore, sinon PUT (mise à jour)
      if (cours) await api.put(`api/modules/${moduleId}/cours`, body);
      else await api.post(`api/modules/${moduleId}/cours`, body);
      setMsg('Cours enregistré.');
      onSaved();
    } catch (err) {
      setMsg(err instanceof ApiError ? err.message : "Échec de l'enregistrement.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={save} className="space-y-3 rounded-xl border border-slate-200 bg-white p-4">
      <h3 className="font-bold text-slate-800">📖 Cours</h3>
      <textarea className="input min-h-28" placeholder="Contenu du cours (HTML/texte riche)"
        value={contenu} onChange={(e) => setContenu(e.target.value)} />
      <div className="grid gap-3 sm:grid-cols-2">
        <input className="input" placeholder="URL vidéo (optionnel)" value={videoUrl}
          onChange={(e) => setVideoUrl(e.target.value)} />
        <input className="input" placeholder="URL PDF (optionnel)" value={pdfUrl}
          onChange={(e) => setPdfUrl(e.target.value)} />
      </div>
      <textarea className="input min-h-16" placeholder="Texte à faire présenter par l'avatar IA (optionnel)"
        value={textePourAvatar} onChange={(e) => setTexte(e.target.value)} />
      <label className="block text-sm text-slate-600">
        Durée estimée (min)
        <input type="number" min={1} className="input mt-1 w-32" value={duree}
          onChange={(e) => setDuree(Number(e.target.value))} />
      </label>
      <div className="flex items-center gap-3">
        <button type="submit" disabled={saving}
          className="flex items-center gap-2 rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60">
          {saving && <SpinnerSm />} {cours ? 'Mettre à jour' : 'Créer le cours'}
        </button>
        {msg && <span className="text-sm text-slate-500">{msg}</span>}
      </div>
    </form>
  );
}

// ── Exercices ─────────────────────────────────────────────────────────────────
const TYPE_REPONSE = ['TexteLibre', 'ChoixMultiple', 'FichierUpload', 'VraiOuFaux'];

function ExerciceEditor({
  moduleId,
  exercices,
  onSaved,
}: {
  moduleId: number;
  exercices: ModuleContent['exercices'];
  onSaved: () => void;
}) {
  const [enonce, setEnonce] = useState('');
  const [typeReponse, setTypeReponse] = useState('TexteLibre');
  const [reponseCorrecte, setReponse] = useState('');
  const [points, setPoints] = useState(1);
  const [saving, setSaving] = useState(false);

  const add = async (e: FormEvent) => {
    e.preventDefault();
    if (!enonce.trim()) return;
    setSaving(true);
    try {
      await api.post(`api/modules/${moduleId}/exercices`, {
        moduleId,
        enonce: enonce.trim(),
        typeReponse,
        correctionAuto: !!reponseCorrecte,
        reponseCorrecte: reponseCorrecte || null,
        instructions: null,
        points,
      });
      setEnonce('');
      setReponse('');
      onSaved();
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-3 rounded-xl border border-slate-200 bg-white p-4">
      <h3 className="font-bold text-slate-800">✏️ Exercices ({exercices.length})</h3>
      {exercices.length > 0 && (
        <ul className="space-y-1 text-sm text-slate-600">
          {exercices.map((ex) => (
            <li key={ex.id} className="flex justify-between rounded bg-slate-50 px-3 py-1.5">
              <span>{ex.enonce}</span>
              <span className="text-xs text-slate-400">{ex.typeReponse} · {ex.points} pt</span>
            </li>
          ))}
        </ul>
      )}
      <form onSubmit={add} className="grid gap-3 sm:grid-cols-2">
        <input className="input sm:col-span-2" placeholder="Énoncé de l'exercice" value={enonce}
          onChange={(e) => setEnonce(e.target.value)} />
        <select className="input" value={typeReponse} onChange={(e) => setTypeReponse(e.target.value)}>
          {TYPE_REPONSE.map((t) => <option key={t} value={t}>{t}</option>)}
        </select>
        <input type="number" min={1} className="input" placeholder="Points" value={points}
          onChange={(e) => setPoints(Number(e.target.value))} />
        <input className="input sm:col-span-2" placeholder="Réponse correcte (active la correction auto, optionnel)"
          value={reponseCorrecte} onChange={(e) => setReponse(e.target.value)} />
        <button type="submit" disabled={saving}
          className="flex items-center gap-2 rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60">
          {saving && <SpinnerSm />} + Ajouter l'exercice
        </button>
      </form>
    </div>
  );
}

// ── Examens + questions ───────────────────────────────────────────────────────
function ExamenEditor({
  moduleId,
  examens,
  onSaved,
}: {
  moduleId: number;
  examens: ModuleContent['examens'];
  onSaved: () => void;
}) {
  const [titre, setTitre] = useState('');
  const [dureeMinutes, setDuree] = useState(30);
  const [noteMinPassage, setNote] = useState(50);
  const [tentativesMax, setTentatives] = useState(3);
  const [saving, setSaving] = useState(false);

  const addExamen = async (e: FormEvent) => {
    e.preventDefault();
    if (!titre.trim()) return;
    setSaving(true);
    try {
      await api.post(`api/modules/${moduleId}/examens`, {
        moduleId,
        titre: titre.trim(),
        description: null,
        dureeMinutes,
        noteMinPassage,
        tentativesMax,
      });
      setTitre('');
      onSaved();
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-3 rounded-xl border border-slate-200 bg-white p-4">
      <h3 className="font-bold text-slate-800">📝 Examens / Quiz ({examens.length})</h3>

      {examens.map((ex) => (
        <div key={ex.id} className="rounded-lg border border-slate-200 p-3">
          <div className="flex justify-between">
            <span className="font-medium text-slate-700">{ex.titre}</span>
            <span className="text-xs text-slate-400">
              {ex.nbQuestions} question(s) · {ex.dureeMinutes} min · réussite ≥ {ex.noteMinPassage}%
            </span>
          </div>
          <QuestionAdder examenId={ex.id} onSaved={onSaved} />
        </div>
      ))}

      <form onSubmit={addExamen} className="grid gap-3 sm:grid-cols-4">
        <input className="input sm:col-span-4" placeholder="Titre de l'examen" value={titre}
          onChange={(e) => setTitre(e.target.value)} />
        <label className="text-xs text-slate-500">Durée (min)
          <input type="number" min={1} className="input mt-1" value={dureeMinutes}
            onChange={(e) => setDuree(Number(e.target.value))} />
        </label>
        <label className="text-xs text-slate-500">Note passage (%)
          <input type="number" min={0} max={100} className="input mt-1" value={noteMinPassage}
            onChange={(e) => setNote(Number(e.target.value))} />
        </label>
        <label className="text-xs text-slate-500">Tentatives max
          <input type="number" min={1} className="input mt-1" value={tentativesMax}
            onChange={(e) => setTentatives(Number(e.target.value))} />
        </label>
        <button type="submit" disabled={saving}
          className="flex items-center justify-center gap-2 self-end rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60">
          {saving && <SpinnerSm />} + Examen
        </button>
      </form>
    </div>
  );
}

function QuestionAdder({ examenId, onSaved }: { examenId: number; onSaved: () => void }) {
  const [open, setOpen] = useState(false);
  const [enonce, setEnonce] = useState('');
  const [choix, setChoix] = useState(['', '', '', '']);
  const [bonneReponse, setBonne] = useState('');
  const [points, setPoints] = useState(1);
  const [saving, setSaving] = useState(false);

  const add = async (e: FormEvent) => {
    e.preventDefault();
    const cleaned = choix.map((c) => c.trim()).filter(Boolean);
    if (!enonce.trim() || cleaned.length < 2 || !bonneReponse.trim()) return;
    setSaving(true);
    try {
      await api.post(`api/modules/examens/${examenId}/questions`, {
        examenId,
        enonce: enonce.trim(),
        choix: cleaned,
        bonneReponse: bonneReponse.trim(),
        explication: null,
        points,
        ordre: 0,
      });
      setEnonce('');
      setChoix(['', '', '', '']);
      setBonne('');
      onSaved();
    } finally {
      setSaving(false);
    }
  };

  if (!open) {
    return (
      <button onClick={() => setOpen(true)} className="mt-2 text-xs font-medium text-brand-600 hover:underline">
        + Ajouter une question
      </button>
    );
  }

  return (
    <form onSubmit={add} className="mt-3 space-y-2 rounded-lg bg-slate-50 p-3">
      <input className="input" placeholder="Énoncé de la question" value={enonce}
        onChange={(e) => setEnonce(e.target.value)} />
      <div className="grid gap-2 sm:grid-cols-2">
        {choix.map((c, i) => (
          <input key={i} className="input" placeholder={`Choix ${i + 1}`} value={c}
            onChange={(e) => setChoix(choix.map((v, j) => (j === i ? e.target.value : v)))} />
        ))}
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        <input className="input" placeholder="Bonne réponse (doit correspondre à un choix)"
          value={bonneReponse} onChange={(e) => setBonne(e.target.value)} />
        <input type="number" min={1} className="input" placeholder="Points" value={points}
          onChange={(e) => setPoints(Number(e.target.value))} />
      </div>
      <div className="flex gap-2">
        <button type="submit" disabled={saving}
          className="flex items-center gap-2 rounded-lg bg-brand-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-brand-700 disabled:opacity-60">
          {saving && <SpinnerSm />} Enregistrer la question
        </button>
        <button type="button" onClick={() => setOpen(false)}
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs text-slate-600 hover:bg-white">
          Annuler
        </button>
      </div>
    </form>
  );
}
