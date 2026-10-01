import { Order, PagedResult, Product } from '../core/api/api.models';

export function aProduct(overrides: Partial<Product> = {}): Product {
  return {
    id: 'p-1',
    name: 'Wireless Mouse',
    sku: 'WM-042',
    description: 'Ergonomic wireless mouse',
    categoryId: 5,
    categoryName: 'Electronics',
    price: 29.99,
    stock: 10,
    weightKg: 0.12,
    isInStock: true,
    createdAtUtc: '2026-09-30T12:00:00Z',
    updatedAtUtc: null,
    ...overrides,
  };
}

export function aPage<T>(items: T[], overrides: Partial<PagedResult<T>> = {}): PagedResult<T> {
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
    ...overrides,
  };
}

export function anOrder(overrides: Partial<Order> = {}): Order {
  return {
    id: 'o-1',
    orderNumber: 'ORD-ABC123456789',
    customerName: 'Jane Doe',
    customerEmail: 'jane@example.com',
    shippingAddress: '1 Main Street',
    status: 'Paid',
    totalAmount: 59.98,
    paymentReference: 'PAY-1',
    cardLast4: '4242',
    createdAtUtc: '2026-09-30T12:00:00Z',
    paidAtUtc: '2026-09-30T12:00:01Z',
    items: [{ productId: 'p-1', productName: 'Wireless Mouse', sku: 'WM-042', unitPrice: 29.99, quantity: 2, lineTotal: 59.98 }],
    ...overrides,
  };
}
