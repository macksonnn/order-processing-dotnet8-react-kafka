import { Button } from "../atoms/Button";
import { TextLink } from "../atoms/TextLink";

type AppHeaderProps = {
  username?: string;
  onLogout: () => void;
};

export function AppHeader({ username, onLogout }: AppHeaderProps) {
  return (
    <header className="topbar">
      <div>
        <strong>Santa Cruz</strong>
        <p className="muted">Pedidos</p>
      </div>
      <nav>
        <TextLink to="/">Novo pedido</TextLink>
        <TextLink to="/orders">Pedidos</TextLink>
        <span className="muted">{username}</span>
        <Button onClick={onLogout}>Sair</Button>
      </nav>
    </header>
  );
}
