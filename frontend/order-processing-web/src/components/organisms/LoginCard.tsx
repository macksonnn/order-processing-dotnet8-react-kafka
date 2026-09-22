import { useState, type FormEvent } from "react";
import { Alert } from "../atoms/Alert";
import { Button } from "../atoms/Button";
import { Field } from "../molecules/Field";

type LoginCardProps = {
  submitting: boolean;
  error: string | null;
  onSubmit: (username: string, password: string) => void;
};

export function LoginCard({ submitting, error, onSubmit }: LoginCardProps) {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit(username, password);
  }

  return (
    <section className="card">
      <h1>Entrar</h1>
      <form onSubmit={handleSubmit}>
        <div className="row">
          <Field label="Usuário">
            <input
              value={username}
              autoComplete="username"
              onChange={(event) => setUsername(event.target.value)}
            />
          </Field>
          <Field label="Senha">
            <input
              type="password"
              value={password}
              autoComplete="current-password"
              onChange={(event) => setPassword(event.target.value)}
            />
          </Field>
          <Button type="submit" disabled={submitting || !username || !password}>
            {submitting ? "Entrando..." : "Entrar"}
          </Button>
        </div>
      </form>
      {error && <Alert>{error}</Alert>}
    </section>
  );
}
