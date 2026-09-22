import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, formatProblem } from "../api/client";
import { createOrder, getProducts } from "../api/orders";
import { NewOrderCard } from "../components/organisms/NewOrderCard";

export function CreateOrderPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const productsQuery = useQuery({
    queryKey: ["products"],
    queryFn: getProducts
  });

  const mutation = useMutation({
    mutationFn: createOrder,
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: ["orders"] });
      navigate(`/orders/${result.orderId}`);
    },
    onError: (err: unknown) => {
      setSubmitError(err instanceof ApiError ? formatProblem(err.problem) : "Falha ao criar o pedido.");
    }
  });

  return (
    <NewOrderCard
      products={productsQuery.data ?? []}
      productsLoading={productsQuery.isLoading}
      productsError={
        productsQuery.error instanceof ApiError ? formatProblem(productsQuery.error.problem) : null
      }
      submitError={submitError}
      submitting={mutation.isPending}
      onClearSubmitError={() => setSubmitError(null)}
      onSubmit={(items) => {
        setSubmitError(null);
        mutation.mutate(items);
      }}
    />
  );
}
