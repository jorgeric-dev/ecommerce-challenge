import { TestBed } from '@angular/core/testing';
import { aProduct } from '../../testing/test-data';
import { CART_STORAGE_KEY, CartStore, MAX_QUANTITY_PER_LINE } from './cart.store';

describe('CartStore', () => {
  let store: CartStore;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    store = TestBed.inject(CartStore);
  });

  it('starts empty', () => {
    expect(store.isEmpty()).toBe(true);
    expect(store.itemCount()).toBe(0);
    expect(store.total()).toBe(0);
  });

  it('adds a product and computes totals', () => {
    store.add(aProduct({ price: 10.1 }), 3);

    expect(store.items()).toHaveLength(1);
    expect(store.itemCount()).toBe(3);
    expect(store.total()).toBe(30.3);
  });

  it('merges repeated additions of the same product', () => {
    const product = aProduct();
    store.add(product);
    store.add(product, 2);

    expect(store.items()).toHaveLength(1);
    expect(store.quantityOf(product.id)).toBe(3);
  });

  it('never exceeds the available stock', () => {
    const product = aProduct({ stock: 2 });
    store.add(product, 5);
    store.add(product);

    expect(store.quantityOf(product.id)).toBe(2);
  });

  it('caps quantity at the per-line maximum', () => {
    store.add(aProduct({ stock: 1000 }), 500);

    expect(store.itemCount()).toBe(MAX_QUANTITY_PER_LINE);
  });

  it('ignores out of stock products and non-positive quantities', () => {
    store.add(aProduct({ id: 'a', stock: 0 }));
    store.add(aProduct({ id: 'b' }), 0);

    expect(store.isEmpty()).toBe(true);
  });

  it('updates quantity within bounds and removes when set to zero', () => {
    const product = aProduct({ stock: 4 });
    store.add(product);

    store.updateQuantity(product.id, 10);
    expect(store.quantityOf(product.id)).toBe(4);

    store.updateQuantity(product.id, 0);
    expect(store.isEmpty()).toBe(true);
  });

  it('removes a single product and clears everything', () => {
    store.add(aProduct({ id: 'a' }));
    store.add(aProduct({ id: 'b' }));

    store.remove('a');
    expect(store.items().map((item) => item.productId)).toEqual(['b']);

    store.clear();
    expect(store.isEmpty()).toBe(true);
  });

  it('persists the cart to local storage', () => {
    store.add(aProduct());
    TestBed.tick();

    const saved = JSON.parse(localStorage.getItem(CART_STORAGE_KEY) ?? '[]');
    expect(saved).toHaveLength(1);
  });

  it('restores a persisted cart and tolerates corrupted data', () => {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify([{ productId: 'x', name: 'X', sku: 'X', unitPrice: 2, quantity: 2, maxQuantity: 5 }]));
    TestBed.resetTestingModule();
    expect(TestBed.inject(CartStore).total()).toBe(4);

    localStorage.setItem(CART_STORAGE_KEY, '{not json');
    TestBed.resetTestingModule();
    expect(TestBed.inject(CartStore).isEmpty()).toBe(true);
  });
});
