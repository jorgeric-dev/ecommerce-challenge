import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatError, MatFormField, MatHint, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { Router } from '@angular/router';
import { PlaceOrderRequest } from '../../../core/api/api.models';
import { OrdersApi } from '../../../core/api/orders-api';
import { CartStore } from '../../../core/cart/cart.store';
import { ApiError } from '../../../core/http/api-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import { errorMessage } from '../../../shared/data/error-message';
import { FieldErrorPipe } from '../../../shared/forms/field-error.pipe';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { AppValidators } from '../../../shared/forms/validators';
import { Button } from '../../../shared/ui/button/button';
import { InlineAlert } from '../../../shared/ui/inline-alert/inline-alert';
import { PageHeader } from '../../../shared/ui/page-header/page-header';

const CURRENT_YEAR = new Date().getFullYear();

@Component({
  selector: 'app-checkout-page',
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    MatCard,
    MatCardContent,
    MatFormField,
    MatLabel,
    MatInput,
    MatError,
    MatHint,
    PageHeader,
    Button,
    InlineAlert,
    FieldErrorPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './checkout-page.scss',
  templateUrl: './checkout-page.html',
})
export class CheckoutPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly ordersApi = inject(OrdersApi);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  protected readonly cart = inject(CartStore);

  protected readonly currentYear = CURRENT_YEAR;
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    customer: this.formBuilder.group({
      name: ['', [Validators.required, AppValidators.notBlank, AppValidators.noMarkup, Validators.maxLength(150)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      shippingAddress: ['', [Validators.required, AppValidators.notBlank, AppValidators.noMarkup, Validators.maxLength(500)]],
    }),
    payment: this.formBuilder.group({
      cardHolderName: ['', [Validators.required, AppValidators.notBlank, AppValidators.noMarkup, Validators.maxLength(150)]],
      cardNumber: ['', [Validators.required, Validators.pattern(/^\d{12,19}$/), AppValidators.luhn]],
      expiryMonth: [12, [Validators.required, Validators.min(1), Validators.max(12)]],
      expiryYear: [CURRENT_YEAR + 2, [Validators.required, Validators.min(CURRENT_YEAR), Validators.max(CURRENT_YEAR + 20)]],
      cvv: ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]],
    }),
  });

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    this.ordersApi.place(this.buildRequest()).subscribe({
      next: (order) => {
        this.cart.clear();
        this.notifications.success(`Order ${order.orderNumber} confirmed.`);
        void this.router.navigate(['/orders', order.id]);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        if (error instanceof ApiError && applyServerErrors(this.form, error)) {
          this.submitError.set('Please review the highlighted fields.');
          return;
        }
        this.submitError.set(errorMessage(error));
      },
    });
  }

  private buildRequest(): PlaceOrderRequest {
    const { customer, payment } = this.form.getRawValue();
    return {
      customer: {
        name: customer.name.trim(),
        email: customer.email.trim(),
        shippingAddress: customer.shippingAddress.trim(),
      },
      items: this.cart.items().map((item) => ({ productId: item.productId, quantity: item.quantity })),
      payment: {
        ...payment,
        cardHolderName: payment.cardHolderName.trim(),
        expiryMonth: Number(payment.expiryMonth),
        expiryYear: Number(payment.expiryYear),
      },
    };
  }
}
