import { useCallback, useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../lib/apiClient';
import {
  NiveauFormation,
  StatutFormation,
  NIVEAU_LABEL,
  type Categorie,
  type FormationSummary,
  type PagedResult,
} from '../types/api';
import { useAuth } from '../context/AuthContext';
import { Spinner, SpinnerSm } from '../components/Spinner';

interface FormState {
  id?: number;
  titre: string;
  description: string;
  categorieId: number | '';
  niveau: NiveauFormation;
  dureeHeures: number;
  prix: number;
  objectifs: string;
  prerequis: string;
  imageUrl: string;
  statut: StatutFormation;
}

const emptyForm: FormState = {
  titre: '',
  description: '',
  categorieId: '',
  niveau: NiveauFormation.Debutant,
  dureeHeures: 10,
  prix: 0,
  objectifs: '',
  prerequis: '',
  imageUrl: '',
  statut: StatutFormation.Brouillon,
};

const STATUT_LABEL: Record<number, string> = {
  0: 'Brouillon',
  1: 'En révision',
  2: 'Publié',
  3: 'Archivé',
};

export function GestionFormations() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');

  const [formations, setFormations] = useState<FormationSummary[]>([]);
  const [categories, setCategories] = useState<Categorie[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState<FormState | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.get<PagedResult<FormationSummary>>(
        'api/formations?publishedOnly=false&pageSize=100',
      );
      setFormations(res.items);
    } catch {
      setFormations([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    document.title = 'Gestion des formations';
    api.get<Categorie[]>('api/categories').then(setCategories).catch(() => setCategories([]));
    load();
  }, [load]);

  const openCreate = () => {
    setError(null);
    setForm({ ...emptyForm, categorieId: categories[0]?.id ?? '' });
  };

  const openEdit = async (id: number) => {
    setError(null);
    try {
      const f = await api.get<{
        id: number;
        titre: string;
        description?: string | null;
        categorieId: number;
        niveau: NiveauFormation;
        dureeHeures: number;
        prix: number;
        objectifs?: string | null;
        prerequis?: string | null;
        imageUrl?: string | null;
        statut: StatutFormation;
      }>(`api/formations/${id}`);
      setForm({
        id: f.id,
        titre: f.titre,
        description: f.description ?? '',
        categorieId: f.categorieId,
        niveau: f.niveau,
        dureeHeures: f.dureeHeures,
        prix: f.prix,
        objectifs: f.objectifs ?? '',
        prerequis: f.prerequis ?? '',
        imageUrl: f.imageUrl ?? '',
        statut: f.statut,
      });
    } catch {
      setError('Impossible de charger la formation.');
    }
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!form || form.categorieId === '') {
      setError('Choisissez une catégorie.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const body = {
        titre: form.titre,
        description: form.description || null,
        categorieId: Number(form.categorieId),
        niveau: form.niveau,
        dureeHeures: form.dureeHeures,
        prix: form.prix,
        objectifs: form.objectifs || null,
        prerequis: form.prerequis || null,
        imageUrl: form.imageUrl || null,
        formateurIds: [],
      };
      if (form.id) {
        await api.put(`api/formations/${form.id}`, { ...body, statut: form.statut });
        setNotice('Formation mise à jour.');
      } else {
        await api.post('api/formations', body);
        setNotice('Formation créée (brouillon). Publiez-la pour la rendre visible.');
      }
      setForm(null);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Échec de l'enregistrement.");
    } finally {
      setSaving(false);
    }
  };

  const publish = async (id: number) => {
    try {
      await api.put(`api/formations/${id}/publish`);
      setNotice('Formation publiée.');
      await load();
    } catch (err) {
      setNotice(err instanceof ApiError ? err.message : 'Échec de la publication.');
    }
  };

  const remove = async (id: number, titre: string) => {
    if (!confirm(`Supprimer la formation « ${titre} » ? Cette action est irréversible.`)) return;
    try {
      await api.del(`api/formations/${id}`);
      setNotice('Formation supprimée.');
      await load();
    } catch (err) {
      setNotice(err instanceof ApiError ? err.message : 'Échec de la suppression.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-800">Gestion des formations</h1>
        <button
          onClick={openCreate}
          className="rounded-lg bg-brand-600 px-4 py-2 font-semibold text-white hover:bg-brand-700"
        >
          + Nouvelle formation
        </button>
      </div>

      {notice && (
        <div className="flex items-center justify-between rounded-lg bg-brand-50 px-4 py-2 text-sm text-brand-700">
          <span>{notice}</span>
          <button onClick={() => setNotice(null)}>✕</button>
        </div>
      )}

      {/* Formulaire création / édition */}
      {form && (
        <form onSubmit={submit} className="space-y-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="text-lg font-bold text-slate-800">
            {form.id ? 'Modifier la formation' : 'Nouvelle formation'}
          </h2>
          {error && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</div>}

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Titre *" className="sm:col-span-2">
              <input className="input" value={form.titre} required
                onChange={(e) => setForm({ ...form, titre: e.target.value })} />
            </Field>

            <Field label="Catégorie *">
              <select className="input" value={form.categorieId}
                onChange={(e) => setForm({ ...form, categorieId: e.target.value ? Number(e.target.value) : '' })}>
                <option value="">— choisir —</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>{c.icone} {c.nom}</option>
                ))}
              </select>
            </Field>

            <Field label="Niveau">
              <select className="input" value={form.niveau}
                onChange={(e) => setForm({ ...form, niveau: Number(e.target.value) })}>
                {Object.entries(NIVEAU_LABEL).map(([v, l]) => (
                  <option key={v} value={v}>{l}</option>
                ))}
              </select>
            </Field>

            <Field label="Durée (heures)">
              <input type="number" min={1} className="input" value={form.dureeHeures}
                onChange={(e) => setForm({ ...form, dureeHeures: Number(e.target.value) })} />
            </Field>

            <Field label="Prix (€, 0 = gratuit)">
              <input type="number" min={0} step="0.01" className="input" value={form.prix}
                onChange={(e) => setForm({ ...form, prix: Number(e.target.value) })} />
            </Field>

            <Field label="Description" className="sm:col-span-2">
              <textarea className="input min-h-20" value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })} />
            </Field>

            <Field label="Objectifs pédagogiques" className="sm:col-span-2">
              <textarea className="input min-h-16" value={form.objectifs}
                onChange={(e) => setForm({ ...form, objectifs: e.target.value })} />
            </Field>

            <Field label="Prérequis" className="sm:col-span-2">
              <textarea className="input min-h-16" value={form.prerequis}
                onChange={(e) => setForm({ ...form, prerequis: e.target.value })} />
            </Field>

            <Field label="URL de l'image" className="sm:col-span-2">
              <input className="input" value={form.imageUrl} placeholder="https://..."
                onChange={(e) => setForm({ ...form, imageUrl: e.target.value })} />
            </Field>
          </div>

          <div className="flex gap-2">
            <button type="submit" disabled={saving}
              className="flex items-center gap-2 rounded-lg bg-brand-600 px-4 py-2 font-semibold text-white hover:bg-brand-700 disabled:opacity-60">
              {saving && <SpinnerSm />}
              {form.id ? 'Enregistrer' : 'Créer'}
            </button>
            <button type="button" onClick={() => setForm(null)}
              className="rounded-lg border border-slate-300 px-4 py-2 font-medium text-slate-600 hover:bg-slate-50">
              Annuler
            </button>
          </div>
        </form>
      )}

      {/* Liste */}
      {loading ? (
        <Spinner label="Chargement..." />
      ) : formations.length > 0 ? (
        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-100 bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3">Titre</th>
                <th className="px-4 py-3">Catégorie</th>
                <th className="px-4 py-3">Niveau</th>
                <th className="px-4 py-3 text-center">Inscrits</th>
                <th className="px-4 py-3 text-center">Statut</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {formations.map((f) => (
                <tr key={f.id}>
                  <td className="px-4 py-3 font-medium text-slate-800">{f.titre}</td>
                  <td className="px-4 py-3 text-slate-600">{f.categorieNom}</td>
                  <td className="px-4 py-3 text-slate-600">{f.niveauLabel}</td>
                  <td className="px-4 py-3 text-center text-slate-600">{f.nbInscrits}</td>
                  <td className="px-4 py-3 text-center">
                    <span className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                      f.statut === StatutFormation.Publie ? 'bg-green-100 text-green-700' : 'bg-slate-100 text-slate-600'
                    }`}>
                      {STATUT_LABEL[f.statut]}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <Link to={`/gestion/formations/${f.id}/modules`}
                        className="rounded-lg border border-slate-300 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50">
                        Modules
                      </Link>
                      <button onClick={() => openEdit(f.id)}
                        className="rounded-lg border border-slate-300 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50">
                        Éditer
                      </button>
                      {isAdmin && f.statut !== StatutFormation.Publie && (
                        <button onClick={() => publish(f.id)}
                          className="rounded-lg bg-green-600 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-green-700">
                          Publier
                        </button>
                      )}
                      {isAdmin && (
                        <button onClick={() => remove(f.id, f.titre)}
                          className="rounded-lg bg-red-500 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-red-600">
                          Suppr.
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="py-12 text-center text-slate-400">Aucune formation. Créez-en une avec le bouton ci-dessus.</p>
      )}
    </div>
  );
}

function Field({ label, children, className }: { label: string; children: ReactNode; className?: string }) {
  return (
    <label className={`block space-y-1 ${className ?? ''}`}>
      <span className="text-sm font-medium text-slate-600">{label}</span>
      {children}
    </label>
  );
}
