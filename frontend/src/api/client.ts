import type { AuthResult } from "./types";

const API_BASE = import.meta.env.VITE_API_URL ?? "";
const REFRESH_KEY = "bidguard.refresh";

/** An API failure carrying the RFC 7807 ProblemDetails the backend returns. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = "ApiError";
  }
}

let accessToken: string | null = null;
let refreshInFlight: Promise<boolean> | null = null;
let sessionExpiredHandler: () => void = () => {};

export const tokenStore = {
  set(auth: AuthResult) {
    accessToken = auth.accessToken;
    localStorage.setItem(REFRESH_KEY, auth.refreshToken);
  },
  clear() {
    accessToken = null;
    localStorage.removeItem(REFRESH_KEY);
  },
  refreshToken: () => localStorage.getItem(REFRESH_KEY),
  hasAccessToken: () => accessToken !== null,
};

/** Called when the session cannot be renewed (refresh token expired, revoked or reused). */
export function onSessionExpired(handler: () => void) {
  sessionExpiredHandler = handler;
}

export interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  form?: FormData;
  /** Return the raw Response (for file downloads). */
  raw?: boolean;
}

const NO_REFRESH_PATHS = ["/api/auth/login", "/api/auth/register", "/api/auth/refresh"];

export async function api<T = unknown>(path: string, options: RequestOptions = {}): Promise<T> {
  let response = await send(path, options);

  if (response.status === 401 && !NO_REFRESH_PATHS.some((p) => path.startsWith(p))) {
    if (await refreshSession()) {
      response = await send(path, options);
    } else {
      sessionExpiredHandler();
    }
  }

  if (!response.ok) throw await toApiError(response);
  if (options.raw) return response as T;
  if (response.status === 204 || response.headers.get("Content-Length") === "0") return undefined as T;
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/** Refresh the access token; concurrent callers share one request so a rotated token is never reused. */
export function refreshSession(): Promise<boolean> {
  refreshInFlight ??= doRefresh().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

async function doRefresh(): Promise<boolean> {
  const refreshToken = tokenStore.refreshToken();
  if (!refreshToken) return false;
  try {
    const response = await fetch(`${API_BASE}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });
    if (!response.ok) {
      tokenStore.clear();
      return false;
    }
    tokenStore.set((await response.json()) as AuthResult);
    return true;
  } catch {
    return false;
  }
}

function send(path: string, { method = "GET", body, form }: RequestOptions): Promise<Response> {
  const headers = new Headers();
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
  let payload: BodyInit | undefined;
  if (form) {
    payload = form;
  } else if (body !== undefined) {
    headers.set("Content-Type", "application/json");
    payload = JSON.stringify(body);
  }
  return fetch(`${API_BASE}${path}`, { method, headers, body: payload });
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: { title?: string; detail?: string; errors?: Record<string, string[]> } = {};
  try {
    problem = await response.json();
  } catch {
    // not JSON (e.g. a proxy error page)
  }
  const fieldErrors = problem.errors ?? {};
  const firstFieldError = Object.values(fieldErrors).flat()[0];
  const message =
    firstFieldError ?? problem.detail ?? problem.title ?? (response.status === 429
      ? "Too many attempts. Please wait a minute and try again."
      : `Request failed (${response.status}).`);
  return new ApiError(response.status, message, fieldErrors);
}
