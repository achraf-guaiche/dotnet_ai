// Client HTTP centralisé pour l'API Formations.
// Gère le JWT (Bearer depuis localStorage), la sérialisation JSON, les erreurs et le téléchargement de fichiers.

const API_URL = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5000';

const TOKEN_KEY = 'access_token';
const REFRESH_KEY = 'refresh_token';

export class ApiError extends Error {
  status: number;
  body: unknown;
  constructor(status: number, message: string, body?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.body = body;
  }
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setTokens(accessToken: string, refreshToken: string): void {
  localStorage.setItem(TOKEN_KEY, accessToken);
  localStorage.setItem(REFRESH_KEY, refreshToken);
}

export function clearTokens(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_KEY);
}

function authHeaders(extra?: HeadersInit): HeadersInit {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  const token = getToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;
  return { ...headers, ...(extra as Record<string, string>) };
}

async function parseError(res: Response): Promise<never> {
  let body: unknown = null;
  let message = `Erreur ${res.status}`;
  try {
    const text = await res.text();
    if (text) {
      try {
        body = JSON.parse(text);
        const b = body as Record<string, unknown>;
        message = (b.error as string) || (b.message as string) || (b.title as string) || message;
      } catch {
        body = text;
        message = text;
      }
    }
  } catch {
    /* ignore */
  }
  throw new ApiError(res.status, message, body);
}

async function handleJson<T>(res: Response): Promise<T> {
  if (!res.ok) await parseError(res);
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  baseUrl: API_URL,

  async get<T>(endpoint: string, signal?: AbortSignal): Promise<T> {
    const res = await fetch(`${API_URL}/${endpoint.replace(/^\//, '')}`, {
      method: 'GET',
      headers: authHeaders(),
      signal,
    });
    return handleJson<T>(res);
  },

  async post<T>(endpoint: string, body?: unknown, signal?: AbortSignal): Promise<T> {
    const res = await fetch(`${API_URL}/${endpoint.replace(/^\//, '')}`, {
      method: 'POST',
      headers: authHeaders(),
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal,
    });
    return handleJson<T>(res);
  },

  async put<T>(endpoint: string, body?: unknown, signal?: AbortSignal): Promise<T> {
    const res = await fetch(`${API_URL}/${endpoint.replace(/^\//, '')}`, {
      method: 'PUT',
      headers: authHeaders(),
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal,
    });
    return handleJson<T>(res);
  },

  async del<T = void>(endpoint: string): Promise<T> {
    const res = await fetch(`${API_URL}/${endpoint.replace(/^\//, '')}`, {
      method: 'DELETE',
      headers: authHeaders(),
    });
    return handleJson<T>(res);
  },

  // Télécharge un fichier binaire (ex : certificat PDF) et déclenche le téléchargement navigateur.
  async download(endpoint: string, filename: string): Promise<void> {
    const res = await fetch(`${API_URL}/${endpoint.replace(/^\//, '')}`, {
      method: 'GET',
      headers: authHeaders(),
    });
    if (!res.ok) await parseError(res);
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  },
};
