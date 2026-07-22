import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../lib/apiClient';
import type { StartSessionResponse, SendMessageResponse } from '../types/api';

interface ChatMsg {
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
}

export function AvatarSession() {
  const { moduleId } = useParams<{ moduleId: string }>();
  const modId = Number(moduleId);

  const [session, setSession] = useState<StartSessionResponse | null>(null);
  const [starting, setStarting] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [typing, setTyping] = useState(false);
  const [speaking, setSpeaking] = useState(false);
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<ChatMsg[]>([]);
  const sessionStart = useRef<number>(Date.now());
  const chatEndRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    document.title = 'Session Formateur IA';
    let cancelled = false;
    (async () => {
      try {
        const res = await api.post<StartSessionResponse>('api/avatar/sessions/start', { moduleId: modId });
        if (cancelled) return;
        setSession(res);
        setMessages([
          {
            role: 'assistant',
            content: res.initialMessage || 'Bonjour ! Je suis votre formateur IA. Comment puis-je vous aider avec ce module ?',
            timestamp: new Date(),
          },
        ]);
      } catch {
        if (!cancelled) setHasError(true);
      } finally {
        if (!cancelled) setStarting(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [modId]);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, typing]);

  const send = async () => {
    if (!input.trim() || !session) return;
    const msg = input.trim();
    setInput('');
    setMessages((m) => [...m, { role: 'user', content: msg, timestamp: new Date() }]);
    setTyping(true);
    setSpeaking(false);

    try {
      const res = await api.post<SendMessageResponse>('api/avatar/sessions/message', {
        sessionId: session.sessionId,
        dbSessionId: session.dbSessionId,
        message: msg,
      });
      setMessages((m) => [...m, { role: 'assistant', content: res.avatarResponse, timestamp: new Date() }]);
      setSpeaking(true);
      setTimeout(() => setSpeaking(false), 2000);
    } catch {
      setMessages((m) => [
        ...m,
        { role: 'system', content: 'Erreur de communication avec le formateur IA.', timestamp: new Date() },
      ]);
    } finally {
      setTyping(false);
    }
  };

  const handleKeyUp = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') send();
  };

  const endSession = async () => {
    if (!session) return;
    const duree = Math.round((Date.now() - sessionStart.current) / 1000);
    await api
      .post(`api/avatar/sessions/${session.dbSessionId}/end`, { dureeSecondes: duree })
      .catch(() => undefined);
    setSession(null);
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <h2 className="text-2xl font-bold text-slate-800">🤖 Session avec le Formateur IA</h2>
        {session && (
          <span
            className={`rounded-full px-3 py-1 text-xs font-medium ${
              session.isMockMode || !session.streamUrl
                ? 'bg-amber-100 text-amber-700'
                : 'bg-green-100 text-green-700'
            }`}
          >
            {session.isMockMode
              ? 'Mode démo'
              : session.streamUrl
                ? 'Avatar connecté'
                : session.embedToken
                  ? 'Token avatar obtenu — SDK vidéo non intégré'
                  : 'Avatar indisponible — repli simulé'}
          </span>
        )}
      </div>

      {starting ? (
        <div className="flex flex-col items-center gap-4 py-20 text-slate-500">
          <div className="relative flex h-24 w-24 items-center justify-center rounded-full bg-brand-100 text-5xl">
            <span className="absolute inset-0 animate-ping rounded-full bg-brand-200 opacity-60" />
            <span className="relative">🤖</span>
          </div>
          <p>Connexion au formateur IA en cours...</p>
        </div>
      ) : hasError ? (
        <div className="rounded-lg bg-red-50 px-4 py-3 text-red-600">
          Impossible de démarrer la session IA. Vérifiez votre connexion ou réessayez plus tard.
        </div>
      ) : session ? (
        <>
          <div className="grid gap-4 lg:grid-cols-2">
            {/* Panneau vidéo */}
            <div className="flex min-h-[320px] items-center justify-center overflow-hidden rounded-2xl bg-slate-900">
              {/* Pas d'URL de stream (mode simulé, repli, ou fournisseur piloté par SDK)
                  → on affiche l'avatar animé plutôt qu'une iframe cassée. */}
              {session.isMockMode || !session.streamUrl ? (
                <div className="flex flex-col items-center gap-4 text-white">
                  <div className="text-7xl">🤖</div>
                  {speaking && (
                    <div className="flex gap-1">
                      {[0, 150, 300].map((d) => (
                        <span
                          key={d}
                          className="h-3 w-3 animate-bounce rounded-full bg-brand-400"
                          style={{ animationDelay: `${d}ms` }}
                        />
                      ))}
                    </div>
                  )}
                  <div className="text-sm text-slate-300">
                    {speaking ? 'En train de parler...' : 'En attente de votre question'}
                  </div>
                </div>
              ) : (
                <iframe
                  src={session.streamUrl ?? ''}
                  title="Formateur IA"
                  className="h-full w-full"
                  allow="camera; microphone; autoplay"
                  allowFullScreen
                />
              )}
            </div>

            {/* Panneau chat */}
            <div className="flex min-h-[320px] flex-col rounded-2xl border border-slate-200 bg-white">
              <div className="flex-1 space-y-3 overflow-y-auto p-4" style={{ maxHeight: 380 }}>
                {messages.map((m, idx) => (
                  <div
                    key={idx}
                    className={`flex flex-col ${m.role === 'user' ? 'items-end' : 'items-start'}`}
                  >
                    <div
                      className={`max-w-[80%] rounded-2xl px-4 py-2 text-sm ${
                        m.role === 'user'
                          ? 'bg-brand-600 text-white'
                          : m.role === 'system'
                            ? 'bg-red-50 text-red-600'
                            : 'bg-slate-100 text-slate-700'
                      }`}
                    >
                      {m.content}
                    </div>
                    <span className="mt-1 text-[10px] text-slate-400">
                      {m.timestamp.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                ))}
                {typing && (
                  <div className="flex items-start">
                    <div className="flex gap-1 rounded-2xl bg-slate-100 px-4 py-3">
                      {[0, 150, 300].map((d) => (
                        <span
                          key={d}
                          className="h-2 w-2 animate-bounce rounded-full bg-slate-400"
                          style={{ animationDelay: `${d}ms` }}
                        />
                      ))}
                    </div>
                  </div>
                )}
                <div ref={chatEndRef} />
              </div>

              <div className="flex gap-2 border-t border-slate-100 p-3">
                <input
                  type="text"
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyUp={handleKeyUp}
                  placeholder="Posez votre question au formateur IA..."
                  disabled={typing}
                  className="input"
                />
                <button
                  onClick={send}
                  disabled={typing || !input.trim()}
                  className="rounded-lg bg-brand-600 px-4 py-2 font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
                >
                  ➤
                </button>
              </div>
            </div>
          </div>

          <div>
            <button
              onClick={endSession}
              className="rounded-lg bg-red-600 px-4 py-2 font-semibold text-white hover:bg-red-700"
            >
              Terminer la session
            </button>
          </div>
        </>
      ) : (
        <div className="rounded-lg bg-slate-100 px-4 py-3 text-slate-600">Session terminée. Merci !</div>
      )}
    </div>
  );
}
