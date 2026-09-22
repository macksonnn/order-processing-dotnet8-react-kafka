import type { ReactNode } from "react";
import { AppHeader } from "../organisms/AppHeader";

type AppLayoutProps = {
  username?: string;
  onLogout: () => void;
  children: ReactNode;
};

export function AppLayout({ username, onLogout, children }: AppLayoutProps) {
  return (
    <div className="layout">
      <AppHeader username={username} onLogout={onLogout} />
      <main>{children}</main>
    </div>
  );
}
