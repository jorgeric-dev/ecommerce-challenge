import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard } from '@angular/material/card';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import {
  MatCell,
  MatCellDef,
  MatColumnDef,
  MatHeaderCell,
  MatHeaderCellDef,
  MatHeaderRow,
  MatHeaderRowDef,
  MatRow,
  MatRowDef,
  MatTable,
} from '@angular/material/table';
import { Router } from '@angular/router';
import { OrdersApi } from '../../../core/api/orders-api';
import { errorMessage } from '../../../shared/data/error-message';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { InlineAlert } from '../../../shared/ui/inline-alert/inline-alert';
import { OrderStatusChip } from '../../../shared/ui/order-status-chip/order-status-chip';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { SearchField } from '../../../shared/ui/search-field/search-field';

@Component({
  selector: 'app-order-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    MatCard,
    MatTable,
    MatColumnDef,
    MatHeaderCell,
    MatHeaderCellDef,
    MatCell,
    MatCellDef,
    MatHeaderRow,
    MatHeaderRowDef,
    MatRow,
    MatRowDef,
    MatPaginator,
    MatProgressBar,
    PageHeader,
    SearchField,
    EmptyState,
    InlineAlert,
    OrderStatusChip,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './order-list.scss',
  templateUrl: './order-list.html',
})
export class OrderList {
  private readonly ordersApi = inject(OrdersApi);
  private readonly router = inject(Router);

  protected readonly columns = ['orderNumber', 'createdAtUtc', 'customer', 'itemCount', 'totalAmount', 'status'];
  protected readonly customerEmail = signal('');
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = linkedSignal({
    source: () => [this.customerEmail(), this.pageSize()],
    computation: () => 0,
  });

  protected readonly orders = rxResource({
    params: () => ({ customerEmail: this.customerEmail(), page: this.pageIndex() + 1, pageSize: this.pageSize() }),
    stream: ({ params }) => this.ordersApi.search(params),
  });

  protected readonly loadError = computed(() => (this.orders.error() ? errorMessage(this.orders.error()) : null));

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.pageIndex.set(event.pageIndex);
  }

  protected open(orderId: string): void {
    void this.router.navigate(['/orders', orderId]);
  }
}
