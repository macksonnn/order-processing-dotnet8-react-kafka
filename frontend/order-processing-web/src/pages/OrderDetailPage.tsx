import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import { ApiError, formatProblem } from "../api/client";
import { getOrderById } from "../api/orders";
import { Alert } from "../components/atoms/Alert";
import { OrderDetailCard } from "../components/organisms/OrderDetailCard";

export function OrderDetailPage() {
  const { orderId } = useParams();

  const query = useQuery({
    queryKey: ["order", orderId],
    queryFn: () => getOrderById(orderId!),
    enabled: Boolean(orderId),
    refetchInterval: 2500
  });

  if (query.error instanceof ApiError) {
    return <Alert>{formatProblem(query.error.problem)}</Alert>;
  }

  if (!query.data) {
    return <p>Carregando pedido...</p>;
  }

  return <OrderDetailCard order={query.data} />;
}
