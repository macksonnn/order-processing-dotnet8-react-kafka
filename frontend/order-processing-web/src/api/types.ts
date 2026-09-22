export type Product = {
  id: string;
  name: string;
  price: number;
  createdAtUtc: string;
};

export type CreateOrderResult = {
  orderId: string;
  status: string;
};

export type OrderListItem = {
  orderId: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
};

export type PagedOrders = {
  items: OrderListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type OrderItem = {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
};

export type OrderAttempt = {
  attemptNumber: number;
  startedAtUtc: string;
  finishedAtUtc?: string | null;
  success?: boolean | null;
  errorMessage?: string | null;
};

export type OrderDetails = {
  orderId: string;
  userId: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  items: OrderItem[];
  attempts: OrderAttempt[];
};
