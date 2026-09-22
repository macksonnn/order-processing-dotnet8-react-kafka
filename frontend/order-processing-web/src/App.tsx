import { Link, Navigate, Route, Routes } from "react-router-dom";
import { keycloak } from "./auth/keycloak";
import { CreateOrderPage } from "./pages/CreateOrderPage";
import { OrderDetailPage } from "./pages/OrderDetailPage";
import { OrdersPage } from "./pages/OrdersPage";

export default function App() {
  return (
    <div className="layout">
      <header className="topbar">
        <div>
          <strong>Santa Cruz</strong>
          <p className="muted">Pedidos</p>
        </div>
        <nav>
          <Link to="/">Novo pedido</Link>
          <Link to="/orders">Pedidos</Link>
          <span className="muted">{keycloak.tokenParsed?.preferred_username}</span>
          <button type="button" onClick={() => keycloak.logout()}>
            Sair
          </button>
        </nav>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<CreateOrderPage />} />
          <Route path="/orders" element={<OrdersPage />} />
          <Route path="/orders/:orderId" element={<OrderDetailPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}
