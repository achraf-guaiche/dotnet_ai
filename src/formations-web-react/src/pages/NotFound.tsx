import { Link } from 'react-router-dom';

export function NotFound() {
  return (
    <div className="flex flex-col items-center gap-4 py-24 text-center text-slate-500">
      <span className="text-6xl">🧭</span>
      <h1 className="text-2xl font-bold text-slate-700">Page introuvable</h1>
      <p>La page que vous cherchez n'existe pas ou a été déplacée.</p>
      <Link to="/" className="rounded-lg bg-brand-600 px-5 py-2.5 font-semibold text-white hover:bg-brand-700">
        Retour à l'accueil
      </Link>
    </div>
  );
}
