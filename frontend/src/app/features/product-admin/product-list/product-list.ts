import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCard } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatSort, MatSortHeader, Sort } from '@angular/material/sort';
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
import { filter, switchMap } from 'rxjs';
import { Product, ProductSortField, SortDirection } from '../../../core/api/api.models';
import { ProductsApi } from '../../../core/api/products-api';
import { NotificationService } from '../../../core/notifications/notification.service';
import { errorMessage } from '../../../shared/data/error-message';
import { Button } from '../../../shared/ui/button/button';
import { CategorySelect } from '../../../shared/ui/category-select/category-select';
import { ConfirmDialogService } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { IconButton } from '../../../shared/ui/icon-button/icon-button';
import { InlineAlert } from '../../../shared/ui/inline-alert/inline-alert';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { SearchField } from '../../../shared/ui/search-field/search-field';
import { StockBadge } from '../../../shared/ui/stock-badge/stock-badge';
import { ImportDialog } from '../import-dialog/import-dialog';
import { ProductFormDialog, ProductFormDialogData } from '../product-form-dialog/product-form-dialog';

const SORTABLE_COLUMNS: Record<string, ProductSortField> = {
  sku: 'Sku',
  name: 'Name',
  category: 'Category',
  price: 'Price',
  stock: 'Stock',
};

@Component({
  selector: 'app-product-list',
  imports: [
    CurrencyPipe,
    DecimalPipe,
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
    MatSort,
    MatSortHeader,
    MatPaginator,
    MatProgressBar,
    PageHeader,
    SearchField,
    CategorySelect,
    Button,
    IconButton,
    EmptyState,
    InlineAlert,
    StockBadge,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './product-list.scss',
  templateUrl: './product-list.html',
})
export class ProductList {
  private readonly productsApi = inject(ProductsApi);
  private readonly dialog = inject(MatDialog);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly notifications = inject(NotificationService);

  protected readonly columns = ['sku', 'name', 'category', 'price', 'stock', 'weight', 'actions'];

  protected readonly search = signal('');
  protected readonly categoryId = signal<number | null>(null);
  protected readonly sort = signal<{ sortBy: ProductSortField; sortDirection: SortDirection }>({ sortBy: 'Name', sortDirection: 'Asc' });
  protected readonly sortActive = computed(
    () => Object.keys(SORTABLE_COLUMNS).find((column) => SORTABLE_COLUMNS[column] === this.sort().sortBy) ?? 'name',
  );
  protected readonly sortDirection = computed(() => (this.sort().sortDirection === 'Desc' ? 'desc' : 'asc'));
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = linkedSignal({
    source: () => [this.search(), this.categoryId(), this.sort(), this.pageSize()],
    computation: () => 0,
  });

  protected readonly products = rxResource({
    params: () => ({
      search: this.search(),
      categoryId: this.categoryId(),
      ...this.sort(),
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
    }),
    stream: ({ params }) => this.productsApi.search(params),
  });

  protected readonly loadError = computed(() => (this.products.error() ? errorMessage(this.products.error()) : null));

  protected onSort(sort: Sort): void {
    const sortBy = SORTABLE_COLUMNS[sort.active] ?? 'Name';
    this.sort.set({ sortBy, sortDirection: sort.direction === 'desc' ? 'Desc' : 'Asc' });
  }

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.pageIndex.set(event.pageIndex);
  }

  protected openCreate(): void {
    this.openForm(null);
  }

  protected openEdit(product: Product): void {
    this.openForm(product);
  }

  protected openImport(): void {
    this.dialog
      .open<ImportDialog, void, boolean>(ImportDialog, { width: '640px', maxHeight: '90vh' })
      .afterClosed()
      .pipe(filter((changed) => changed === true))
      .subscribe(() => this.products.reload());
  }

  protected delete(product: Product): void {
    this.confirmDialog
      .confirm({
        title: 'Delete product',
        message: `Delete "${product.name}" (${product.sku})? It will no longer be available in the shop.`,
        confirmText: 'Delete',
        tone: 'danger',
      })
      .pipe(
        filter((confirmed) => confirmed),
        switchMap(() => this.productsApi.delete(product.id)),
      )
      .subscribe({
        next: () => {
          this.notifications.success(`${product.name} deleted.`);
          this.products.reload();
        },
        error: (error: unknown) => this.notifications.error(errorMessage(error)),
      });
  }

  private openForm(product: Product | null): void {
    this.dialog
      .open<ProductFormDialog, ProductFormDialogData, Product>(ProductFormDialog, { data: { product }, width: '640px' })
      .afterClosed()
      .pipe(filter((saved): saved is Product => saved !== undefined))
      .subscribe((saved) => {
        this.notifications.success(`${saved.name} ${product ? 'updated' : 'created'}.`);
        this.products.reload();
      });
  }
}
