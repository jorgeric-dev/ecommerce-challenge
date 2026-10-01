import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatProgressBar } from '@angular/material/progress-bar';
import {
  MatCell,
  MatCellDef,
  MatColumnDef,
  MatFooterCell,
  MatFooterCellDef,
  MatFooterRow,
  MatFooterRowDef,
  MatHeaderCell,
  MatHeaderCellDef,
  MatHeaderRow,
  MatHeaderRowDef,
  MatRow,
  MatRowDef,
  MatTable,
} from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { OrdersApi } from '../../../core/api/orders-api';
import { errorMessage } from '../../../shared/data/error-message';
import { Button } from '../../../shared/ui/button/button';
import { InlineAlert } from '../../../shared/ui/inline-alert/inline-alert';
import { OrderStatusChip } from '../../../shared/ui/order-status-chip/order-status-chip';
import { PageHeader } from '../../../shared/ui/page-header/page-header';

@Component({
  selector: 'app-order-detail',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatCard,
    MatCardContent,
    MatProgressBar,
    MatTable,
    MatColumnDef,
    MatHeaderCell,
    MatHeaderCellDef,
    MatCell,
    MatCellDef,
    MatFooterCell,
    MatFooterCellDef,
    MatHeaderRow,
    MatHeaderRowDef,
    MatRow,
    MatRowDef,
    MatFooterRow,
    MatFooterRowDef,
    PageHeader,
    Button,
    InlineAlert,
    OrderStatusChip,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './order-detail.scss',
  templateUrl: './order-detail.html',
})
export class OrderDetail {
  private readonly ordersApi = inject(OrdersApi);

  readonly id = input.required<string>();

  protected readonly columns = ['product', 'unitPrice', 'quantity', 'lineTotal'];

  protected readonly order = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.ordersApi.getById(params),
  });

  protected readonly loadError = computed(() => (this.order.error() ? errorMessage(this.order.error()) : null));
}
