import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, formatProblem } from "../api/client";
import { createOrder, getProducts } from "../api/orders";

type CartItem = {
  productId: string;
  name: string;
  price: number;
  quantity: number;
};

export function CreateOrderPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [selectedProductId, setSelectedProductId] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [items, setItems] = useState<CartItem[]>([]);
  const [error, setError] = useState<string | null>(null);

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
      setError(err instanceof ApiError ? formatProblem(err.problem) : "Falha ao criar o pedido.");
    }
  });

  const products = productsQuery.data ?? [];
  const total = useMemo(
    () => items.reduce((sum, item) => sum + item.price * item.quantity, 0),
    [items]
  );

  function addItem() {
    const product = products.find((item) => item.id === selectedProductId);
    if (!product || quantity <= 0) {
      setError("Selecione um produto e informe uma quantidade maior que zero.");
      return;
    }

    setError(null);
    setItems((current) => {
      const existing = current.find((item) => item.productId === product.id);
      if (!existing) {
        return [...current, { productId: product.id, name: product.name, price: product.price, quantity }];
      }

      return current.map((item) =>
        item.productId === product.id ? { ...item, quantity: item.quantity + quantity } : item
      );
    });
  }

  function removeItem(productId: string) {
    setItems((current) => current.filter((item) => item.productId !== productId));
  }

  return (
    <section className="card">
      <h1>Novo pedido</h1>

      {productsQuery.isLoading && <p>Carregando produtos...</p>}
      {productsQuery.error instanceof ApiError && (
        <div className="alert">{formatProblem(productsQuery.error.problem)}</div>
      )}

      <div className="row">
        <label>
          Produto
          <select value={selectedProductId} onChange={(event) => setSelectedProductId(event.target.value)}>
            <option value="">Selecione</option>
            {products.map((product) => (
              <option key={product.id} value={product.id}>
                {product.name} — {product.price.toFixed(2)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Quantidade
          <input
            type="number"
            min={1}
            value={quantity}
            onChange={(event) => setQuantity(Number(event.target.value))}
          />
        </label>
        <button type="button" onClick={addItem}>
          Adicionar
        </button>
      </div>

      <table>
        <thead>
          <tr>
            <th>Produto</th>
            <th>Qtd</th>
            <th>Preço</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.productId}>
              <td>{item.name}</td>
              <td>{item.quantity}</td>
              <td>{(item.price * item.quantity).toFixed(2)}</td>
              <td>
                <button type="button" className="link" onClick={() => removeItem(item.productId)}>
                  Remover
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <p>
        <strong>Total estimado:</strong> {total.toFixed(2)}
      </p>

      {error && <div className="alert">{error}</div>}

      <button
        type="button"
        disabled={mutation.isPending || items.length === 0}
        onClick={() => mutation.mutate(items.map((item) => ({ productId: item.productId, quantity: item.quantity })))}
      >
        {mutation.isPending ? "Enviando..." : "Criar pedido"}
      </button>
    </section>
  );
}
