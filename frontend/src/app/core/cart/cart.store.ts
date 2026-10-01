import { Injectable, computed, effect, signal } from '@angular/core';
import { Product } from '../api/api.models';

export interface CartItem {
  productId: string;
  name: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  maxQuantity: number;
}

export const CART_STORAGE_KEY = 'ecommerce.cart';
export const MAX_QUANTITY_PER_LINE = 100;

@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly itemsState = signal<CartItem[]>(readFromStorage());

  readonly items = this.itemsState.asReadonly();
  readonly itemCount = computed(() => this.itemsState().reduce((sum, item) => sum + item.quantity, 0));
  readonly total = computed(() =>
    roundCurrency(this.itemsState().reduce((sum, item) => sum + item.unitPrice * item.quantity, 0)),
  );
  readonly isEmpty = computed(() => this.itemsState().length === 0);

  constructor() {
    effect(() => writeToStorage(this.itemsState()));
  }

  add(product: Product, quantity = 1): void {
    const maxQuantity = Math.min(product.stock, MAX_QUANTITY_PER_LINE);
    if (maxQuantity <= 0 || quantity <= 0) {
      return;
    }

    this.itemsState.update((items) => {
      const existing = items.find((item) => item.productId === product.id);
      if (!existing) {
        return [
          ...items,
          {
            productId: product.id,
            name: product.name,
            sku: product.sku,
            unitPrice: product.price,
            quantity: Math.min(quantity, maxQuantity),
            maxQuantity,
          },
        ];
      }

      return items.map((item) =>
        item.productId === product.id
          ? { ...item, unitPrice: product.price, maxQuantity, quantity: Math.min(item.quantity + quantity, maxQuantity) }
          : item,
      );
    });
  }

  updateQuantity(productId: string, quantity: number): void {
    if (quantity <= 0) {
      this.remove(productId);
      return;
    }

    this.itemsState.update((items) =>
      items.map((item) =>
        item.productId === productId ? { ...item, quantity: Math.min(Math.floor(quantity), item.maxQuantity) } : item,
      ),
    );
  }

  quantityOf(productId: string): number {
    return this.itemsState().find((item) => item.productId === productId)?.quantity ?? 0;
  }

  remove(productId: string): void {
    this.itemsState.update((items) => items.filter((item) => item.productId !== productId));
  }

  clear(): void {
    this.itemsState.set([]);
  }
}

function roundCurrency(value: number): number {
  return Math.round(value * 100) / 100;
}

function readFromStorage(): CartItem[] {
  try {
    const raw = globalThis.localStorage?.getItem(CART_STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? (parsed as CartItem[]) : [];
  } catch {
    return [];
  }
}

function writeToStorage(items: CartItem[]): void {
  try {
    globalThis.localStorage?.setItem(CART_STORAGE_KEY, JSON.stringify(items));
  } catch {
    return;
  }
}
