import type { ReactNode } from "react";
import { Link } from "react-router-dom";

export function TextLink({ to, children }: { to: string; children: ReactNode }) {
  return <Link to={to}>{children}</Link>;
}
