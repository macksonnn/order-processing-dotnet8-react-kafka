import { useMemo, useState } from "react";
import type { Product } from "../../api/types";
import { Alert } from "../atoms/Alert";
import { Button } from "../atoms/Button";
import { Field } from "../molecules/Field";

type CartItem = {
  productId: string;
  name: string;
  price: number;
  quantity: number;
};

type NewOrderCardProps = {
  products: Product[];
  productsLoading: boolean;
  productsError: string | null;
  submitError: string | null;
  submitting: boolean;
  onSubmit: (items: { productId: string; quantity: number }[]) => void;
  onClearSubmitError: () => void;
};

export function NewOrderCard({
  products,
  productsLoading,
  productsError,
  submitError,
  submitting,
  onSubmit,
  onClearSubmitError
}: NewOrderCardProps) {
  const [selectedProductId, setSelectedProductId] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [items, setItems] = useState<CartItem[]>([]);
  const [localError, setLocalError] = useState<string | null>(null);

  const total = useMemo(
    () => items.reduce((sum, item) => sum + item.price * item.quantity, 0),
    [items]
  );

  function addItem() {
    const product = products.find((item) => item.id === selectedProductId);
    if (!product || quantity <= 0) {
      setLocalError("Selecione um produto e informe uma quantidade maior que zero.");
      return;
    }

    setLocalError(null);
    onClearSubmitError();
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

  const error = localError ?? submitError;

  return (
    <section className="card">
      <h1>Novo pedido</h1>

      {productsLoading && <p>Carregando produtos...</p>}
      {productsError && <Alert>{productsError}</Alert>}

      <div className="row">
        <Field label="Produto">
          <select value={selectedProductId} onChange={(event) => setSelectedProductId(event.target.value)}>
            <option value="">Selecione</option>
            {products.map((product) => (
              <option key={product.id} value={product.id}>
                {product.name} — {product.price.toFixed(2)}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Quantidade">
          <input
            type="number"
            min={1}
            value={quantity}
            onChange={(event) => setQuantity(Number(event.target.value))}
          />
        </Field>
        <Button onClick={addItem}>Adicionar</Button>
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
                <Button variant="link" onClick={() => removeItem(item.productId)}>
                  Remover
                </Button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <p>
        <strong>Total estimado:</strong> {total.toFixed(2)}
      </p>

      {error && <Alert>{error}</Alert>}

      <Button
        disabled={submitting || items.length === 0}
        onClick={() => onSubmit(items.map((item) => ({ productId: item.productId, quantity: item.quantity })))}
      >
        {submitting ? "Enviando..." : "Criar pedido"}
      </Button>
    </section>
  );
}
