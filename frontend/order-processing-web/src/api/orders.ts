import { apiFetch } from "./client";
import type { CreateOrderResult, OrderDetails, PagedOrders, Product } from "./types";

export function getProducts() {
  return apiFetch<Product[]>("/api/products");
}

export function createOrder(items: { productId: string; quantity: number }[]) {
  return apiFetch<CreateOrderResult>("/api/orders", {
    method: "POST",
    body: JSON.stringify({ items })
  });
}

export function getOrders(page: number, pageSize: number) {
  return apiFetch<PagedOrders>(`/api/orders?page=${page}&pageSize=${pageSize}`);
}

export function getOrderById(orderId: string) {
  return apiFetch<OrderDetails>(`/api/orders/${orderId}`);
}
