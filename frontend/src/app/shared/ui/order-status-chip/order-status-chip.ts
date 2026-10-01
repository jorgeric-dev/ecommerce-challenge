import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrderStatus } from '../../../core/api/api.models';

const LABELS: Record<OrderStatus, string> = {
  Pending: 'Pending',
  Paid: 'Paid',
  PaymentFailed: 'Payment failed',
};

@Component({
  selector: 'app-order-status-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': '"status-chip status-chip--" + status().toLowerCase()',
  },
  styles: `
    :host {
      display: inline-flex;
      padding: 2px 10px;
      border-radius: var(--app-shape-full);
      font: var(--mat-sys-label-medium);
      white-space: nowrap;
    }
    :host(.status-chip--paid) {
      background: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
    }
    :host(.status-chip--pending) {
      background: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
    }
    :host(.status-chip--paymentfailed) {
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }
  `,
  template: `{{ label() }}`,
})
export class OrderStatusChip {
  readonly status = input.required<OrderStatus>();

  protected readonly label = computed(() => LABELS[this.status()] ?? this.status());
}
