const accessKey = "sc.access";
const refreshKey = "sc.refresh";
const usernameKey = "sc.username";

export type Session = {
  accessToken: string;
  refreshToken: string;
  username: string;
};

export function readSession(): Session | null {
  const accessToken = sessionStorage.getItem(accessKey);
  const refreshToken = sessionStorage.getItem(refreshKey);
  const username = sessionStorage.getItem(usernameKey) ?? "";

  if (!accessToken || !refreshToken) {
    return null;
  }

  return { accessToken, refreshToken, username };
}

export function writeSession(session: Session) {
  sessionStorage.setItem(accessKey, session.accessToken);
  sessionStorage.setItem(refreshKey, session.refreshToken);
  sessionStorage.setItem(usernameKey, session.username);
}

export function clearSession() {
  sessionStorage.removeItem(accessKey);
  sessionStorage.removeItem(refreshKey);
  sessionStorage.removeItem(usernameKey);
}
