import type { OrderListItem } from "../../api/types";
import { Alert } from "../atoms/Alert";
import { StatusBadge } from "../atoms/StatusBadge";
import { TextLink } from "../atoms/TextLink";
import { Pagination } from "../molecules/Pagination";

type OrdersCardProps = {
  orders?: OrderListItem[];
  error: string | null;
  page: number;
  totalPages: number;
  onPrevious: () => void;
  onNext: () => void;
};

export function OrdersCard({ orders, error, page, totalPages, onPrevious, onNext }: OrdersCardProps) {
  return (
    <section className="card">
      <h1>Pedidos</h1>

      {error && <Alert>{error}</Alert>}

      <table>
        <thead>
          <tr>
            <th>Pedido</th>
            <th>Status</th>
            <th>Total</th>
            <th>Criado em</th>
          </tr>
        </thead>
        <tbody>
          {orders?.map((order) => (
            <tr key={order.orderId}>
              <td>
                <TextLink to={`/orders/${order.orderId}`}>{order.orderId}</TextLink>
              </td>
              <td>
                <StatusBadge status={order.status} />
              </td>
              <td>{order.totalAmount.toFixed(2)}</td>
              <td>{new Date(order.createdAtUtc).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <Pagination page={page} totalPages={totalPages} onPrevious={onPrevious} onNext={onNext} />
    </section>
  );
}
