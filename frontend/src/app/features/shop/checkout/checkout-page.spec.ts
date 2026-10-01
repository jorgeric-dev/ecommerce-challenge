import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { OrdersApi } from '../../../core/api/orders-api';
import { CartStore } from '../../../core/cart/cart.store';
import { ApiError } from '../../../core/http/api-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import { aProduct, anOrder } from '../../../testing/test-data';
import { CheckoutPage } from './checkout-page';

type CheckoutInternals = {
  form: FormGroup;
  submit(): void;
  submitError(): string | null;
};

describe('CheckoutPage', () => {
  const ordersApi = { place: vi.fn() };
  const notifications = { success: vi.fn(), error: vi.fn() };

  beforeEach(() => {
    localStorage.clear();
    ordersApi.place.mockReset();
    TestBed.configureTestingModule({
      imports: [CheckoutPage],
      providers: [
        provideRouter([]),
        { provide: OrdersApi, useValue: ordersApi },
        { provide: NotificationService, useValue: notifications },
      ],
    });
    TestBed.inject(CartStore).add(aProduct({ id: 'p-1', price: 29.99 }), 2);
  });

  async function createPage() {
    const fixture = TestBed.createComponent(CheckoutPage);
    await fixture.whenStable();
    return { fixture, page: fixture.componentInstance as unknown as CheckoutInternals };
  }

  function fillValidForm(page: CheckoutInternals): void {
    page.form.patchValue({
      customer: { name: 'Jane Doe', email: 'jane@example.com', shippingAddress: '1 Main Street' },
      payment: { cardHolderName: 'Jane Doe', cardNumber: '4242424242424242', expiryMonth: 12, expiryYear: new Date().getFullYear() + 1, cvv: '123' },
    });
  }

  it('does not submit an invalid form', async () => {
    const { page } = await createPage();

    page.submit();

    expect(page.form.invalid).toBe(true);
    expect(ordersApi.place).not.toHaveBeenCalled();
  });

  it('places the order, clears the cart and navigates to the order', async () => {
    ordersApi.place.mockReturnValue(of(anOrder({ id: 'o-9' })));
    const { page } = await createPage();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fillValidForm(page);

    page.submit();

    expect(ordersApi.place).toHaveBeenCalledWith(
      expect.objectContaining({ items: [{ productId: 'p-1', quantity: 2 }], customer: expect.objectContaining({ email: 'jane@example.com' }) }),
    );
    expect(TestBed.inject(CartStore).isEmpty()).toBe(true);
    expect(navigate).toHaveBeenCalledWith(['/orders', 'o-9']);
  });

  it('shows the server message when the payment is declined', async () => {
    ordersApi.place.mockReturnValue(throwError(() => new ApiError(402, 'Payment for order ORD-1 was declined: The card was declined by the issuer.')));
    const { page } = await createPage();
    fillValidForm(page);

    page.submit();

    expect(page.submitError()).toContain('declined');
    expect(TestBed.inject(CartStore).isEmpty()).toBe(false);
  });

  it('maps server validation errors onto the form fields', async () => {
    ordersApi.place.mockReturnValue(throwError(() => new ApiError(400, 'Validation failed', { 'Payment.Expiry': ['The card has expired.'], 'Customer.Email': ['Invalid email.'] })));
    const { page } = await createPage();
    fillValidForm(page);

    page.submit();

    expect(page.submitError()).toBe('Please review the highlighted fields.');
    expect(page.form.get('customer.email')?.errors).toEqual({ server: 'Invalid email.' });
  });
});
