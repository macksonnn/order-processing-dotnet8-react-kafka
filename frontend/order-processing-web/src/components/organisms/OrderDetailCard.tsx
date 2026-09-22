import type { OrderDetails } from "../../api/types";
import { StatusBadge } from "../atoms/StatusBadge";
import { TextLink } from "../atoms/TextLink";

export function OrderDetailCard({ order }: { order: OrderDetails }) {
  return (
    <section className="card">
      <p>
        <TextLink to="/orders">← Voltar</TextLink>
      </p>
      <h1>Pedido {order.orderId}</h1>
      <p>
        <StatusBadge status={order.status} />
      </p>

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
