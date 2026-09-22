import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, formatProblem } from "../api/client";
import { getOrders } from "../api/orders";

export function OrdersPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const query = useQuery({
    queryKey: ["orders", page, pageSize],
    queryFn: () => getOrders(page, pageSize),
    refetchInterval: 2500
  });

  const data = query.data;
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1;

  return (
    <section className="card">
      <h1>Pedidos</h1>
      <p className="muted">Atualização automática a cada 2,5s para acompanhar Pending → Processing → Completed/Failed.</p>

      {query.error instanceof ApiError && <div className="alert">{formatProblem(query.error.problem)}</div>}

      <table>
        <thead>
          <tr>
            <th>OrderId</th>
            <th>Status</th>
            <th>Total</th>
            <th>Criado em</th>
          </tr>
        </thead>
        <tbody>
          {data?.items.map((order) => (
            <tr key={order.orderId}>
              <td>
                <Link to={`/orders/${order.orderId}`}>{order.orderId}</Link>
              </td>
              <td>
                <span className={`status status-${order.status.toLowerCase()}`}>{order.status}</span>
              </td>
              <td>{order.totalAmount.toFixed(2)}</td>
              <td>{new Date(order.createdAtUtc).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="row">
        <button type="button" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>
          Anterior
        </button>
        <span>
          Página {page} de {totalPages}
        </span>
        <button type="button" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>
          Próxima
        </button>
      </div>
    </section>
  );
}
