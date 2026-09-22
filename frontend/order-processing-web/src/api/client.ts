import { clearSession, readSession, writeSession } from "../auth/session";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

export type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  constructor(public readonly problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? "Request failed");
  }
}

export function formatProblem(problem: ProblemDetails): string {
  const fieldErrors = problem.errors
    ? Object.entries(problem.errors)
        .flatMap(([field, messages]) => messages.map((message) => `${field}: ${message}`))
        .join(" ")
    : "";

  return [problem.title, problem.detail, fieldErrors].filter(Boolean).join(" — ");
}

type UnauthorizedHandler = () => void;

let onUnauthorized: UnauthorizedHandler | null = null;
let refreshInFlight: Promise<string | null> | null = null;

export function setOnUnauthorized(handler: UnauthorizedHandler | null) {
  onUnauthorized = handler;
}

export function publicFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  return send<T>(path, options);
}

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const session = readSession();
  const response = await rawSend(path, options, session?.accessToken);

  if (response.status !== 401 || !session?.refreshToken) {
    return parse<T>(response);
  }

  const nextAccess = await refreshAccess(session.refreshToken);
  if (!nextAccess) {
    clearSession();
    onUnauthorized?.();
    return parse<T>(response);
  }

  return send<T>(path, options, nextAccess);
}

async function refreshAccess(refreshToken: string): Promise<string | null> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        const result = await publicFetch<{
          accessToken: string;
          refreshToken: string;
          username: string;
        }>("/api/auth/refresh", {
          method: "POST",
          body: JSON.stringify({ refreshToken })
        });

        writeSession({
          accessToken: result.accessToken,
          refreshToken: result.refreshToken,
          username: result.username
        });

        return result.accessToken;
      } catch {
        return null;
      } finally {
        refreshInFlight = null;
      }
    })();
  }

  return refreshInFlight;
}

async function send<T>(path: string, options: RequestInit, accessToken?: string): Promise<T> {
  return parse<T>(await rawSend(path, options, accessToken));
}

async function rawSend(path: string, options: RequestInit, accessToken?: string): Promise<Response> {
  return fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "X-Correlation-ID": crypto.randomUUID(),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...(options.headers ?? {})
    }
  });
}

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let problem: ProblemDetails = {
      title: "Request failed",
      detail: response.statusText,
      status: response.status
    };

    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
    }

    throw new ApiError(problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
