import { useCallback, useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../lib/apiClient';
import type { StartSessionResponse, SendMessageResponse } from '../types/api';

interface ChatMsg {
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnamClient = any;

type VideoState = 'loading' | 'ready' | 'timeout' | 'error';

const ANAM_TIMEOUT_MS = 15_000;

export function AvatarSession() {
  const { moduleId } = useParams<{ moduleId: string }>();
  const modId = Number(moduleId);

  const [session, setSession] = useState<StartSessionResponse | null>(null);
  const [starting, setStarting] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [typing, setTyping] = useState(false);
  const [speaking, setSpeaking] = useState(false);
  const [videoState, setVideoState] = useState<VideoState>('loading');
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<ChatMsg[]>([]);

  const sessionStart = useRef<number>(Date.now());
  const chatEndRef = useRef<HTMLDivElement | null>(null);
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const anamClientRef = useRef<AnamClient>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  // Ref pour accéder à la session courante depuis le cleanup d'unmount (évite stale closure).
  const sessionRef = useRef<StartSessionResponse | null>(null);

  // Callback ref : déclenche le re-render quand l'élément <video> monte dans le DOM.
  const [videoMounted, setVideoMounted] = useState(false);
  const setVideoRef = useCallback((el: HTMLVideoElement | null) => {
    videoRef.current = el;
    setVideoMounted(!!el);
  }, []);
  const setAudioRef = useCallback((el: HTMLAudioElement | null) => {
    audioRef.current = el;
  }, []);

  // ── Démarrage de session ──────────────────────────────────────────────────
  useEffect(() => {
    document.title = 'Session Formateur IA';
    let cancelled = false;
    (async () => {
      try {
        const res = await api.post<StartSessionResponse>('api/avatar/sessions/start', { moduleId: modId });
        if (cancelled) return;
        sessionRef.current = res;
        setSession(res);
        setMessages([{
          role: 'assistant',
          content: res.initialMessage || 'Bonjour ! Je suis votre formateur IA. Comment puis-je vous aider avec ce module ?',
          timestamp: new Date(),
        }]);
      } catch {
        if (!cancelled) setHasError(true);
      } finally {
        if (!cancelled) setStarting(false);
      }
    })();
    return () => { cancelled = true; };
  }, [modId]);

  // ── Libération de la session Anam à la navigation/fermeture ─────────────────
  // Sans ce cleanup, les sessions restent "actives" côté Anam et épuisent le quota concurrent.
  useEffect(() => {
    return () => {
      const s = sessionRef.current;
      if (!s) return;
      clearTimeout(timeoutRef.current!);
      anamClientRef.current?.stopStreaming?.().catch(() => {});
      anamClientRef.current = null;
      const duree = Math.round((Date.now() - sessionStart.current) / 1000);
      api.post(`api/avatar/sessions/${s.dbSessionId}/end`, { dureeSecondes: duree }).catch(() => {});
    };
  }, []); // deps vides : s'exécute uniquement au démontage du composant

  // ── Init SDK Anam — non-bloquant avec timeout ─────────────────────────────
  useEffect(() => {
    if (!session || session.isMockMode || session.avatarProvider !== 'anam') return;
    if (!session.embedToken || !videoMounted || !videoRef.current || !audioRef.current) return;

    let stopped = false;
    let audioCtx: AudioContext | null = null;

    // Timeout : si le stream n'est pas prêt dans 15s, on affiche un message
    // mais le chat reste utilisable.
    timeoutRef.current = setTimeout(() => {
      if (!stopped) setVideoState('timeout');
    }, ANAM_TIMEOUT_MS);

    (async () => {
      try {
        if (!session.avatarPersonaId) {
          console.error('[Anam SDK] AvatarIA__PersonaId non configuré — créez un persona dans le dashboard Anam et ajoutez son ID dans AvatarIA__PersonaId');
          if (!stopped) setVideoState('error');
          return;
        }

        const { createClient } = await import('@anam-ai/js-sdk');

        // createClient(sessionToken, personaConfig, clientOptions) — 3 args distincts.
        // Les handlers vont via addListener, PAS dans personaConfig (arg 2).
        // Événements SDK : 'VIDEO_STREAM_STARTED' et 'CONNECTION_CLOSED'.
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const client: AnamClient = (createClient as any)(
          session.embedToken!,
          { personaId: session.avatarPersonaId },  // arg 2 : personaConfig
          undefined,                                // arg 3 : clientOptions (défaut)
        );
        client.addListener('VIDEO_STREAM_STARTED', () => {
          if (!stopped) {
            clearTimeout(timeoutRef.current!);
            // Log de l'ID de session réel pour debug (utile si session reste bloquée côté Anam)
            console.info('[Anam] session active :', client.getActiveSessionId?.());
            setVideoState('ready');
          }
        });
        client.addListener('CONNECTION_CLOSED', () => {
          if (!stopped && videoState !== 'ready') setVideoState('error');
        });
        anamClientRef.current = client;

        // Le SDK appelle getUserMedia({audio}) si aucun stream n'est fourni.
        // On passe un stream audio silencieux pour éviter la demande de micro
        // (l'interaction est textuelle — pas besoin de voix entrante).
        audioCtx = new AudioContext();
        const silentStream = audioCtx.createMediaStreamDestination().stream;

        // 3e arg = userProvidedMediaStream (bypass getUserMedia)
        // Le SDK localise les éléments via getElementById — IDs string obligatoires.
        client.streamToVideoAndAudioElements('anam-video', 'anam-audio', silentStream).catch((err: unknown) => {
          console.error('[Anam SDK] streamToVideoAndAudioElements :', err);
          if (!stopped) setVideoState('error');
        });

      } catch (err) {
        console.error('[Anam SDK] init :', err);
        if (!stopped) setVideoState('error');
      }
    })();

    return () => {
      stopped = true;
      clearTimeout(timeoutRef.current!);
      anamClientRef.current?.stopStreaming?.().catch(() => {});
      anamClientRef.current = null;
      audioCtx?.close().catch(() => {});
      setVideoState('loading');
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session, videoMounted]);

  // ── Scroll ────────────────────────────────────────────────────────────────
  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, typing]);

  // ── Envoi d'un message ────────────────────────────────────────────────────
  const send = async () => {
    if (!input.trim() || !session) return;
    const msg = input.trim();
    setInput('');
    setMessages(m => [...m, { role: 'user', content: msg, timestamp: new Date() }]);
    setTyping(true);
    setSpeaking(false);

    try {
      const res = await api.post<SendMessageResponse>('api/avatar/sessions/message', {
        sessionId: session.sessionId,
        dbSessionId: session.dbSessionId,
        message: msg,
      });

      const aiText = res.avatarResponse;
      setMessages(m => [...m, { role: 'assistant', content: aiText, timestamp: new Date() }]);

      // Anam : talk(string) → POST /talk?session_id=... { content: string }
      // L'argument doit être la string directement, pas un objet.
      if (session.avatarProvider === 'anam' && anamClientRef.current && videoState === 'ready') {
        setSpeaking(true);
        anamClientRef.current.talk?.(aiText)
          ?.catch(() => {})
          ?.finally(() => setSpeaking(false));
      } else {
        setSpeaking(true);
        setTimeout(() => setSpeaking(false), 2500);
      }
    } catch {
      setMessages(m => [
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
    clearTimeout(timeoutRef.current!);
    const duree = Math.round((Date.now() - sessionStart.current) / 1000);
    await anamClientRef.current?.stopStreaming?.().catch(() => {});
    anamClientRef.current = null;
    await api.post(`api/avatar/sessions/${session.dbSessionId}/end`, { dureeSecondes: duree }).catch(() => {});
    sessionRef.current = null; // empêche le double-appel depuis le cleanup d'unmount
    setSession(null);
  };

  const retryAnam = () => {
    setVideoState('loading');
    setVideoMounted(false);
    setTimeout(() => setVideoMounted(!!videoRef.current), 50);
  };

  // ── Labels ────────────────────────────────────────────────────────────────
  const isAnam = session?.avatarProvider === 'anam' && !session.isMockMode;
  const isHeyGen = session?.avatarProvider === 'heygen' && !session.isMockMode && !!session.streamUrl;
  const anamReady = videoState === 'ready';

  const badge = !session ? null
    : session.isMockMode ? { label: 'Mode démo', cls: 'bg-amber-100 text-amber-700' }
    : isAnam && anamReady ? { label: '🟢 Anam · Connecté', cls: 'bg-green-100 text-green-700' }
    : isAnam && videoState === 'loading' ? { label: '🟡 Anam · Connexion...', cls: 'bg-yellow-100 text-yellow-700' }
    : isAnam && videoState === 'timeout' ? { label: '🟠 Anam · Connexion lente — chat actif', cls: 'bg-orange-100 text-orange-700' }
    : isAnam && videoState === 'error' ? { label: '🔴 Anam · Erreur vidéo — chat actif', cls: 'bg-red-100 text-red-700' }
    : isHeyGen ? { label: '🟢 HeyGen · Connecté', cls: 'bg-green-100 text-green-700' }
    : { label: 'Avatar indisponible', cls: 'bg-slate-100 text-slate-500' };

  return (
    <div className="space-y-4">
      {/* En-tête */}
      <div className="flex items-center gap-3 flex-wrap">
        <h2 className="text-2xl font-bold text-slate-800">🤖 Session Formateur IA</h2>
        {badge && (
          <span className={`rounded-full px-3 py-1 text-xs font-medium ${badge.cls}`}>
            {badge.label}
          </span>
        )}
        {session && (
          <Link to={`/modules/${modId}/apprendre`} className="ml-auto text-sm text-brand-600 hover:underline">
            ← Retour au cours
          </Link>
        )}
      </div>

      {/* Corps */}
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

            {/* ── Panneau vidéo ─────────────────────────────────────────── */}
            <div className="relative flex min-h-[360px] items-center justify-center overflow-hidden rounded-2xl bg-slate-900">

              {/* Anam : éléments vidéo/audio avec IDs fixes — le SDK les localise via getElementById */}
              {isAnam && (
                <>
                  <video
                    id="anam-video"
                    ref={setVideoRef}
                    autoPlay
                    playsInline
                    className="absolute inset-0 h-full w-full object-cover"
                    style={{ opacity: anamReady ? 1 : 0, transition: 'opacity 0.6s ease' }}
                  />
                  {/* eslint-disable-next-line jsx-a11y/media-has-caption */}
                  <audio id="anam-audio" ref={setAudioRef} autoPlay className="hidden" />

                  {/* Overlay chargement */}
                  {videoState === 'loading' && (
                    <div className="relative z-10 flex flex-col items-center gap-3 text-white px-4 text-center">
                      <div className="relative flex h-20 w-20 items-center justify-center rounded-full bg-brand-700 text-4xl">
                        <span className="absolute inset-0 animate-ping rounded-full bg-brand-600 opacity-40" />
                        <span className="relative">🤖</span>
                      </div>
                      <p className="text-sm text-slate-300 animate-pulse">Connexion au flux vidéo Anam...</p>
                      <p className="text-xs text-slate-500">Peut prendre quelques secondes (WebRTC)</p>
                    </div>
                  )}

                  {/* Timeout — vidéo toujours en fond, chat utilisable */}
                  {videoState === 'timeout' && (
                    <div className="relative z-10 flex flex-col items-center gap-3 text-white px-4 text-center">
                      <div className="text-5xl">🤖</div>
                      <p className="text-sm text-slate-300">Vidéo lente à charger.</p>
                      <p className="text-xs text-slate-400">Le chat est disponible pendant l'attente.</p>
                      <button
                        onClick={retryAnam}
                        className="mt-1 rounded-lg border border-slate-500 px-3 py-1 text-xs text-slate-300 hover:bg-slate-700"
                      >
                        Réessayer la vidéo
                      </button>
                    </div>
                  )}

                  {/* Erreur vidéo */}
                  {videoState === 'error' && (
                    <div className="relative z-10 flex flex-col items-center gap-3 text-white px-4 text-center">
                      <div className="text-5xl">🤖</div>
                      <p className="text-sm text-red-400">Flux vidéo indisponible.</p>
                      {!session.avatarPersonaId && (
                        <p className="text-xs text-amber-300 max-w-xs">
                          <code className="rounded bg-slate-800 px-1">AvatarIA__PersonaId</code> n'est pas configuré.
                          Créez un persona dans le dashboard Anam et ajoutez son ID dans votre <code className="rounded bg-slate-800 px-1">.env</code>.
                        </p>
                      )}
                      <p className="text-xs text-slate-400">Le chat textuel reste actif.</p>
                      <button
                        onClick={retryAnam}
                        className="mt-1 rounded-lg border border-red-500 px-3 py-1 text-xs text-red-300 hover:bg-red-900/30"
                      >
                        Réessayer
                      </button>
                    </div>
                  )}

                  {/* Indicateur "parle" quand l'avatar est actif */}
                  {anamReady && speaking && (
                    <div className="absolute bottom-4 left-1/2 z-20 -translate-x-1/2 flex gap-1.5 rounded-full bg-black/50 px-4 py-2">
                      {[0, 150, 300].map(d => (
                        <span key={d} className="h-2.5 w-2.5 animate-bounce rounded-full bg-white" style={{ animationDelay: `${d}ms` }} />
                      ))}
                    </div>
                  )}
                </>
              )}

              {/* HeyGen */}
              {isHeyGen && (
                <iframe src={session.streamUrl!} title="Formateur IA HeyGen"
                  className="h-full w-full" allow="camera; microphone; autoplay" allowFullScreen />
              )}

              {/* Mock / repli texte */}
              {!isAnam && !isHeyGen && (
                <div className="flex flex-col items-center gap-4 p-6 text-white text-center">
                  <div className="text-7xl">{speaking ? '🗣️' : '🤖'}</div>
                  {speaking && (
                    <div className="flex gap-1">
                      {[0, 150, 300].map(d => (
                        <span key={d} className="h-3 w-3 animate-bounce rounded-full bg-brand-400" style={{ animationDelay: `${d}ms` }} />
                      ))}
                    </div>
                  )}
                  <p className="text-sm text-slate-300">{speaking ? 'En train de répondre...' : 'En attente de votre question'}</p>
                  {session.isMockMode && (
                    <p className="text-xs text-slate-500 max-w-xs">
                      Mode démo. Configurez <code className="rounded bg-slate-800 px-1">AvatarIA__Provider=anam</code> dans <code className="rounded bg-slate-800 px-1">.env</code> pour activer la vidéo.
                    </p>
                  )}
                </div>
              )}
            </div>

            {/* ── Panneau chat — toujours disponible ────────────────────── */}
            <div className="flex min-h-[360px] flex-col rounded-2xl border border-slate-200 bg-white">
              <div className="flex-1 space-y-3 overflow-y-auto p-4" style={{ maxHeight: 400 }}>
                {messages.map((m, idx) => (
                  <div key={idx} className={`flex flex-col ${m.role === 'user' ? 'items-end' : 'items-start'}`}>
                    <div className={`max-w-[80%] rounded-2xl px-4 py-2 text-sm leading-relaxed ${
                      m.role === 'user' ? 'bg-brand-600 text-white'
                      : m.role === 'system' ? 'bg-red-50 text-red-600'
                      : 'bg-slate-100 text-slate-700'
                    }`}>{m.content}</div>
                    <span className="mt-1 text-[10px] text-slate-400">
                      {m.timestamp.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                ))}
                {typing && (
                  <div className="flex items-start">
                    <div className="flex gap-1 rounded-2xl bg-slate-100 px-4 py-3">
                      {[0, 150, 300].map(d => (
                        <span key={d} className="h-2 w-2 animate-bounce rounded-full bg-slate-400" style={{ animationDelay: `${d}ms` }} />
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
                  onChange={e => setInput(e.target.value)}
                  onKeyUp={handleKeyUp}
                  placeholder={isAnam && !anamReady ? 'Chat disponible — vidéo en chargement...' : 'Posez votre question au formateur IA...'}
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

          <button onClick={endSession} className="rounded-lg bg-red-600 px-4 py-2 font-semibold text-white hover:bg-red-700">
            Terminer la session
          </button>
        </>
      ) : (
        <div className="rounded-lg bg-slate-100 px-4 py-3 text-slate-600">Session terminée. Merci !</div>
      )}
    </div>
  );
}
