export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface Category {
  id: number;
  name: string;
}

export interface Product {
  id: string;
  name: string;
  sku: string;
  description: string | null;
  categoryId: number;
  categoryName: string;
  price: number;
  stock: number;
  weightKg: number;
  isInStock: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface ProductDetails {
  name: string;
  description: string | null;
  categoryId: number;
  price: number;
  stock: number;
  weightKg: number;
}

export interface CreateProductRequest extends ProductDetails {
  sku: string;
}

export type UpdateProductRequest = ProductDetails;

export type ProductSortField = 'Name' | 'Sku' | 'Price' | 'Stock' | 'CreatedAt' | 'Category';
export type SortDirection = 'Asc' | 'Desc';

export interface ProductSearchCriteria {
  search?: string;
  categoryId?: number | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  inStockOnly?: boolean;
  sortBy?: ProductSortField;
  sortDirection?: SortDirection;
  page?: number;
  pageSize?: number;
}

export interface ProductImportIssue {
  lineNumber: number;
  sku: string | null;
  messages: string[];
}

export interface ProductImportResult {
  totalRows: number;
  created: number;
  updated: number;
  skipped: number;
  failed: number;
  errors: ProductImportIssue[];
  warnings: ProductImportIssue[];
}

export type OrderStatus = 'Pending' | 'Paid' | 'PaymentFailed';

export interface OrderItem {
  productId: string;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  orderNumber: string;
  customerName: string;
  customerEmail: string;
  shippingAddress: string;
  status: OrderStatus;
  totalAmount: number;
  paymentReference: string | null;
  cardLast4: string | null;
  createdAtUtc: string;
  paidAtUtc: string | null;
  items: OrderItem[];
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  customerName: string;
  customerEmail: string;
  status: OrderStatus;
  totalAmount: number;
  itemCount: number;
  createdAtUtc: string;
}

export interface PlaceOrderRequest {
  customer: { name: string; email: string; shippingAddress: string };
  items: { productId: string; quantity: number }[];
  payment: {
    cardHolderName: string;
    cardNumber: string;
    expiryMonth: number;
    expiryYear: number;
    cvv: string;
  };
}

export interface OrderSearchCriteria {
  customerEmail?: string;
  page?: number;
  pageSize?: number;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  orderNumber?: string;
  errors?: Record<string, string[]>;
}
