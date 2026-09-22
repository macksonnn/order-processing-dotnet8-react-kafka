import { useState } from "react";
import { Navigate } from "react-router-dom";
import { ApiError, formatProblem } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { LoginCard } from "../components/organisms/LoginCard";

export function LoginPage() {
  const { username, login } = useAuth();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (username) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="layout">
      <LoginCard
        submitting={submitting}
        error={error}
        onSubmit={async (user, password) => {
          setSubmitting(true);
          setError(null);
          try {
            await login(user, password);
          } catch (err) {
            setError(err instanceof ApiError ? formatProblem(err.problem) : "Não deu para entrar.");
          } finally {
            setSubmitting(false);
          }
        }}
      />
    </div>
  );
}
