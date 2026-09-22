import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { ApiError, formatProblem } from "../api/client";
import { getOrders } from "../api/orders";
import { OrdersCard } from "../components/organisms/OrdersCard";

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
    <OrdersCard
      orders={data?.items}
      error={query.error instanceof ApiError ? formatProblem(query.error.problem) : null}
      page={page}
      totalPages={totalPages}
      onPrevious={() => setPage((current) => current - 1)}
      onNext={() => setPage((current) => current + 1)}
    />
  );
}
