import { useState, type FormEvent, type ReactNode } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiError } from '../lib/apiClient';
import { SpinnerSm } from '../components/Spinner';
import type { RegisterRole } from '../types/api';

const ROLES: { value: RegisterRole; icon: string; titre: string; desc: string }[] = [
  {
    value: 'Apprenant',
    icon: '🎓',
    titre: 'Apprenant',
    desc: "Je veux suivre des formations, passer les quiz et obtenir des certificats.",
  },
  {
    value: 'Formateur',
    icon: '🧑‍🏫',
    titre: 'Formateur',
    desc: 'Je veux créer et animer des formations, modules, cours et examens.',
  },
];

export function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [role, setRole] = useState<RegisterRole>('Apprenant');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [inviteCode, setInviteCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (password !== confirmPassword) {
      setError('Les mots de passe ne correspondent pas.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      const grantedRoles = await register(fullName, email, password, confirmPassword, role, inviteCode);
      // On redirige selon le rôle réellement accordé (une demande Formateur peut être
      // rétrogradée en Apprenant si le code d'invitation est requis et invalide).
      const isFormateur = grantedRoles.includes('Formateur');
      navigate(isFormateur ? '/gestion/formations' : '/formations', { replace: true });
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Erreur lors de l'inscription. Vérifiez vos informations.",
      );
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-lg">
      <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
        <div className="mb-6 text-center">
          <span className="text-4xl">🎓</span>
          <h1 className="mt-2 text-2xl font-bold text-slate-800">Créer un compte</h1>
          <p className="text-slate-500">Rejoignez notre plateforme de formations</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {error && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</div>}

          <fieldset className="space-y-2">
            <legend className="text-sm font-medium text-slate-600">Je m'inscris en tant que</legend>
            <div className="grid gap-3 sm:grid-cols-2">
              {ROLES.map((r) => {
                const selected = role === r.value;
                return (
                  <label
                    key={r.value}
                    className={`cursor-pointer rounded-xl border-2 p-4 transition ${
                      selected
                        ? 'border-brand-600 bg-brand-50 ring-2 ring-brand-200'
                        : 'border-slate-200 hover:border-slate-300'
                    }`}
                  >
                    <input
                      type="radio"
                      name="role"
                      value={r.value}
                      checked={selected}
                      onChange={() => setRole(r.value)}
                      className="sr-only"
                    />
                    <div className="flex items-center gap-2">
                      <span className="text-2xl">{r.icon}</span>
                      <span className={`font-semibold ${selected ? 'text-brand-700' : 'text-slate-700'}`}>
                        {r.titre}
                      </span>
                    </div>
                    <p className="mt-1 text-xs leading-snug text-slate-500">{r.desc}</p>
                  </label>
                );
              })}
            </div>
          </fieldset>

          {role === 'Formateur' && (
            <Field label="Code d'invitation formateur (si demandé)">
              <input
                type="text"
                value={inviteCode}
                onChange={(e) => setInviteCode(e.target.value)}
                placeholder="Laisser vide si aucun code ne vous a été fourni"
                className="input"
              />
              <span className="text-xs text-slate-400">
                Si la plateforme exige un code, un code manquant ou invalide crée un compte Apprenant.
              </span>
            </Field>
          )}

          <Field label="Nom complet">
            <input
              type="text"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Jean Dupont"
              required
              className="input"
            />
          </Field>

          <Field label="Email">
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="votre@email.com"
              required
              className="input"
            />
          </Field>

          <Field label="Mot de passe">
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Min. 8 caractères, 1 majuscule, 1 chiffre"
              required
              className="input"
            />
          </Field>

          <Field label="Confirmer le mot de passe">
            <input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="input"
            />
          </Field>

          <button
            type="submit"
            disabled={loading}
            className="flex w-full items-center justify-center gap-2 rounded-lg bg-brand-600 py-2.5 font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {loading && <SpinnerSm />}
            Créer mon compte
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-500">
          Déjà un compte ?{' '}
          <Link to="/connexion" className="font-semibold text-brand-600 hover:underline">
            Se connecter
          </Link>
        </p>
      </div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block space-y-1">
      <span className="text-sm font-medium text-slate-600">{label}</span>
      {children}
    </label>
  );
}
