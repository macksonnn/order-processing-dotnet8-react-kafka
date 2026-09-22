import { keycloak } from "../auth/keycloak";

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

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  await keycloak.updateToken(30);

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers: {
      Authorization: `Bearer ${keycloak.token ?? ""}`,
      "Content-Type": "application/json",
      "X-Correlation-ID": crypto.randomUUID(),
      ...(options.headers ?? {})
    }
  });

  if (!response.ok) {
    let problem: ProblemDetails = {
      title: "Request failed",
      detail: response.statusText,
      status: response.status
    };

    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      // Keep the generic problem when the body is not JSON.
    }

    throw new ApiError(problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
