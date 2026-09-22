import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ApiError, formatProblem } from "../api/client";
import { getOrderById } from "../api/orders";

export function OrderDetailPage() {
  const { orderId } = useParams();

  const query = useQuery({
    queryKey: ["order", orderId],
    queryFn: () => getOrderById(orderId!),
    enabled: Boolean(orderId),
    refetchInterval: 2500
  });

  if (query.error instanceof ApiError) {
    return <div className="alert">{formatProblem(query.error.problem)}</div>;
  }

  if (!query.data) {
    return <p>Carregando pedido...</p>;
  }

  const order = query.data;

  return (
    <section className="card">
      <p>
        <Link to="/orders">← Voltar</Link>
      </p>
      <h1>Pedido {order.orderId}</h1>
      <p>
        <span className={`status status-${order.status.toLowerCase()}`}>{order.status}</span>
      </p>
      <p className="muted">O status muda sozinho após o consumer processar o evento no Kafka.</p>

      <dl>
        <div>
          <dt>Usuário</dt>
          <dd>{order.userId}</dd>
        </div>
        <div>
          <dt>Total</dt>
          <dd>{order.totalAmount.toFixed(2)}</dd>
        </div>
        <div>
          <dt>Criado</dt>
          <dd>{new Date(order.createdAtUtc).toLocaleString()}</dd>
        </div>
      </dl>

      <h2>Itens</h2>
      <table>
        <thead>
          <tr>
            <th>Produto</th>
            <th>Qtd</th>
            <th>Preço unitário</th>
            <th>Total</th>
          </tr>
        </thead>
        <tbody>
          {order.items.map((item) => (
            <tr key={item.productId}>
              <td>{item.productName}</td>
              <td>{item.quantity}</td>
              <td>{item.unitPrice.toFixed(2)}</td>
              <td>{item.totalPrice.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <h2>Tentativas</h2>
      <table>
        <thead>
          <tr>
            <th>#</th>
            <th>Início</th>
            <th>Fim</th>
            <th>Sucesso</th>
            <th>Erro</th>
          </tr>
        </thead>
        <tbody>
          {order.attempts.map((attempt) => (
            <tr key={attempt.attemptNumber}>
              <td>{attempt.attemptNumber}</td>
              <td>{new Date(attempt.startedAtUtc).toLocaleString()}</td>
              <td>{attempt.finishedAtUtc ? new Date(attempt.finishedAtUtc).toLocaleString() : "—"}</td>
              <td>{attempt.success == null ? "—" : attempt.success ? "Sim" : "Não"}</td>
              <td>{attempt.errorMessage ?? "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
