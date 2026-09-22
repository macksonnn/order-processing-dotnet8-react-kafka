import { Navigate, Route, Routes } from "react-router-dom";
import { keycloak } from "./auth/keycloak";
import { AppLayout } from "./components/templates/AppLayout";
import { CreateOrderPage } from "./pages/CreateOrderPage";
import { OrderDetailPage } from "./pages/OrderDetailPage";
import { OrdersPage } from "./pages/OrdersPage";

export default function App() {
  return (
    <AppLayout
      username={keycloak.tokenParsed?.preferred_username}
      onLogout={() => keycloak.logout()}
    >
      <Routes>
        <Route path="/" element={<CreateOrderPage />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/orders/:orderId" element={<OrderDetailPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppLayout>
  );
}
