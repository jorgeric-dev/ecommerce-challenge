import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CartStore } from '../../../core/cart/cart.store';
import { Shell } from '../../../layout/shell/shell';
import { aProduct } from '../../../testing/test-data';
import { CartPage } from './cart-page';

describe('CartPage', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('shows an empty state when the cart is empty', async () => {
    const fixture = TestBed.createComponent(CartPage);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Your cart is empty');
  });

  it('lists items with totals and supports quantity changes and removal', async () => {
    const cart = TestBed.inject(CartStore);
    cart.add(aProduct({ id: 'a', name: 'Mouse', price: 10, stock: 5 }), 2);
    const fixture = TestBed.createComponent(CartPage);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('Mouse');
    expect(element.textContent).toContain('$20.00');

    (element.querySelector('button[aria-label="Increase quantity"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    expect(cart.quantityOf('a')).toBe(3);

    (element.querySelector('app-icon-button button') as HTMLButtonElement).click();
    await fixture.whenStable();
    expect(cart.isEmpty()).toBe(true);
  });
});

describe('Shell', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('renders navigation links and the cart badge', async () => {
    TestBed.inject(CartStore).add(aProduct(), 3);
    const fixture = TestBed.createComponent(Shell);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(Array.from(element.querySelectorAll('nav a')).map((a) => a.textContent?.trim())).toEqual([
      'storefrontShop',
      'inventory_2Products',
      'receipt_longOrders',
    ]);
    expect(element.querySelector('.mat-badge-content')?.textContent).toBe('3');
  });
});
