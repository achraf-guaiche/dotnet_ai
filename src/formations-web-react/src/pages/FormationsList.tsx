import { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api } from '../lib/apiClient';
import type { Categorie, FormationSummary, PagedResult } from '../types/api';
import { FormationCard } from '../components/FormationCard';
import { Spinner } from '../components/Spinner';

export function FormationsList() {
  const [searchParams, setSearchParams] = useSearchParams();
  const initialCat = searchParams.get('categorieId');

  const [formations, setFormations] = useState<PagedResult<FormationSummary> | null>(null);
  const [categories, setCategories] = useState<Categorie[]>([]);
  const [search, setSearch] = useState('');
  const [categorieId, setCategorieId] = useState<number | undefined>(
    initialCat ? Number(initialCat) : undefined,
  );
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    document.title = 'Toutes les formations';
    api.get<Categorie[]>('api/categories').then(setCategories).catch(() => setCategories([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '12' });
      if (categorieId) params.set('categorieId', String(categorieId));
      if (search) params.set('search', search);
      const res = await api.get<PagedResult<FormationSummary>>(`api/formations?${params}`);
      setFormations(res);
    } catch {
      setFormations(null);
    } finally {
      setLoading(false);
    }
  }, [page, categorieId, search]);

  useEffect(() => {
    load();
  }, [load]);

  const onSearchChange = (value: string) => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      setPage(1);
      setSearch(value);
    }, 400);
  };

  const onCategorieChange = (value: string) => {
    const id = value ? Number(value) : undefined;
    setCategorieId(id);
    setPage(1);
    setSearchParams(id ? { categorieId: String(id) } : {});
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">Nos formations</h1>
        <p className="text-slate-500">{formations?.totalCount ?? 0} formation(s) disponible(s)</p>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row">
        <input
          type="search"
          placeholder="Rechercher..."
          defaultValue={search}
          onChange={(e) => onSearchChange(e.target.value)}
          className="flex-1 rounded-lg border border-slate-300 px-4 py-2 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100"
        />
        <select
          value={categorieId ?? ''}
          onChange={(e) => onCategorieChange(e.target.value)}
          className="rounded-lg border border-slate-300 px-4 py-2 focus:border-brand-500 focus:outline-none"
        >
          <option value="">Toutes les catégories</option>
          {categories.map((cat) => (
            <option key={cat.id} value={cat.id}>
              {cat.icone} {cat.nom}
            </option>
          ))}
        </select>
      </div>

      {loading ? (
        <Spinner label="Chargement des formations..." />
      ) : formations && formations.items.length > 0 ? (
        <>
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {formations.items.map((f) => (
              <FormationCard key={f.id} formation={f} />
            ))}
          </div>

          {formations.totalPages > 1 && (
            <div className="flex items-center justify-center gap-4 pt-4">
              <button
                onClick={() => setPage((p) => p - 1)}
                disabled={page === 1}
                className="rounded-lg border border-slate-300 px-4 py-2 text-sm disabled:opacity-40"
              >
                ← Précédent
              </button>
              <span className="text-sm text-slate-500">
                Page {page} / {formations.totalPages}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page >= formations.totalPages}
                className="rounded-lg border border-slate-300 px-4 py-2 text-sm disabled:opacity-40"
              >
                Suivant →
              </button>
            </div>
          )}
        </>
      ) : (
        <div className="flex flex-col items-center gap-3 py-16 text-slate-400">
          <span className="text-5xl">📚</span>
          <p>Aucune formation trouvée pour ces critères.</p>
        </div>
      )}
    </div>
  );
}
