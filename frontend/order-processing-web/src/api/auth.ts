import { apiFetch, publicFetch } from "./client";

export type LoginResult = {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  username: string;
};

export function login(username: string, password: string) {
  return publicFetch<LoginResult>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password })
  });
}

export function refresh(refreshToken: string) {
  return publicFetch<LoginResult>("/api/auth/refresh", {
    method: "POST",
    body: JSON.stringify({ refreshToken })
  });
}

export function logout(refreshToken: string) {
  return publicFetch<void>("/api/auth/logout", {
    method: "POST",
    body: JSON.stringify({ refreshToken })
  });
}

export function me() {
  return apiFetch<{ username: string }>("/api/auth/me");
}
