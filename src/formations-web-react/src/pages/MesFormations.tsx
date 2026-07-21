import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/apiClient';
import { StatutInscription, STATUT_INSCRIPTION_LABEL, type Inscription } from '../types/api';
import { Spinner } from '../components/Spinner';

function statutClasses(statut: StatutInscription): string {
  switch (statut) {
    case StatutInscription.Termine:
    case StatutInscription.Certifie:
      return 'bg-green-100 text-green-700';
    case StatutInscription.EnCours:
      return 'bg-brand-100 text-brand-700';
    case StatutInscription.Abandonne:
      return 'bg-amber-100 text-amber-700';
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function MesFormations() {
  const [inscriptions, setInscriptions] = useState<Inscription[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloadingId, setDownloadingId] = useState<number | null>(null);

  useEffect(() => {
    document.title = 'Mes Formations';
    api
      .get<Inscription[]>('api/inscriptions/mes-formations')
      .then(setInscriptions)
      .catch(() => setInscriptions([]))
      .finally(() => setLoading(false));
  }, []);

  const downloadCertificate = async (inscriptionId: number, titre: string) => {
    setDownloadingId(inscriptionId);
    try {
      const filename = `certificat_${titre.replace(/\s+/g, '_')}.pdf`;
      await api.download(`api/inscriptions/${inscriptionId}/certificate`, filename);
    } catch (e) {
      console.error(e);
      alert("Impossible de télécharger le certificat.");
    } finally {
      setDownloadingId(null);
    }
  };

  if (loading) return <Spinner label="Chargement..." />;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-slate-800">Mes formations</h1>

      {inscriptions && inscriptions.length > 0 ? (
        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {inscriptions.map((i) => {
            const canCertify =
              i.statut === StatutInscription.Termine || i.statut === StatutInscription.Certifie;
            return (
              <div
                key={i.id}
                className="flex flex-col overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm"
              >
                {i.formationImage ? (
                  <img src={i.formationImage} alt={i.formationTitre} className="h-36 w-full object-cover" />
                ) : (
                  <div className="flex h-36 items-center justify-center bg-gradient-to-br from-brand-100 to-brand-50 text-5xl">
                    🎓
                  </div>
                )}

                <div className="flex flex-1 flex-col gap-3 p-4">
                  <h3 className="font-semibold text-slate-800">{i.formationTitre}</h3>

                  <div className="space-y-1">
                    <div className="flex justify-between text-sm text-slate-500">
                      <span>Progression</span>
                      <span>{i.progressionPourcent}%</span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-slate-100">
                      <div
                        className="h-full rounded-full bg-brand-600 transition-all"
                        style={{ width: `${i.progressionPourcent}%` }}
                      />
                    </div>
                    <small className="text-slate-400">
                      {i.modulesCompletes} / {i.totalModules} modules terminés
                    </small>
                  </div>

                  <span
                    className={`inline-block w-fit rounded-full px-3 py-1 text-xs font-medium ${statutClasses(i.statut)}`}
                  >
                    {STATUT_INSCRIPTION_LABEL[i.statut] ?? 'Statut inconnu'}
                  </span>

                  <div className="mt-auto flex gap-2 pt-2">
                    <Link
                      to={`/formations/${i.formationId}`}
                      className="flex-1 rounded-lg border border-slate-300 py-2 text-center text-sm font-medium text-slate-600 hover:bg-slate-50"
                    >
                      Voir la formation
                    </Link>
                    {canCertify && (
                      <button
                        onClick={() => downloadCertificate(i.id, i.formationTitre)}
                        disabled={downloadingId === i.id}
                        className="rounded-lg bg-brand-600 px-3 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
                      >
                        {downloadingId === i.id ? '...' : '📜 Certificat'}
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="flex flex-col items-center gap-3 py-16 text-slate-400">
          <span className="text-5xl">📚</span>
          <p>Vous n'êtes encore inscrit à aucune formation.</p>
          <Link to="/formations" className="rounded-lg bg-brand-600 px-4 py-2 text-white">
            Explorer les formations
          </Link>
        </div>
      )}
    </div>
  );
}
