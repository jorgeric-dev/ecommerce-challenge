import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { Product } from '../../../core/api/api.models';
import { ProductsApi } from '../../../core/api/products-api';
import { CartStore } from '../../../core/cart/cart.store';
import { NotificationService } from '../../../core/notifications/notification.service';
import { errorMessage } from '../../../shared/data/error-message';
import { PAGE_SIZE_OPTIONS, PRODUCT_SORT_OPTIONS } from '../../../shared/data/product-filters';
import { CategorySelect } from '../../../shared/ui/category-select/category-select';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { InlineAlert } from '../../../shared/ui/inline-alert/inline-alert';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { SearchField } from '../../../shared/ui/search-field/search-field';
import { ProductCard } from '../product-card/product-card';

@Component({
  selector: 'app-product-catalog',
  imports: [
    PageHeader,
    SearchField,
    CategorySelect,
    EmptyState,
    InlineAlert,
    ProductCard,
    MatFormField,
    MatLabel,
    MatSelect,
    MatOption,
    MatSlideToggle,
    MatPaginator,
    MatProgressBar,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './product-catalog.scss',
  templateUrl: './product-catalog.html',
})
export class ProductCatalog {
  private readonly productsApi = inject(ProductsApi);
  private readonly notifications = inject(NotificationService);
  protected readonly cart = inject(CartStore);

  protected readonly sortOptions = PRODUCT_SORT_OPTIONS;
  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;

  protected readonly search = signal('');
  protected readonly categoryId = signal<number | null>(null);
  protected readonly sortId = signal(PRODUCT_SORT_OPTIONS[0].id);
  protected readonly inStockOnly = signal(true);
  protected readonly pageSize = signal(PAGE_SIZE_OPTIONS[0]);
  protected readonly pageIndex = linkedSignal({
    source: () => [this.search(), this.categoryId(), this.sortId(), this.inStockOnly(), this.pageSize()],
    computation: () => 0,
  });

  private readonly criteria = computed(() => {
    const sort = this.sortOptions.find((option) => option.id === this.sortId()) ?? this.sortOptions[0];
    return {
      search: this.search(),
      categoryId: this.categoryId(),
      inStockOnly: this.inStockOnly(),
      sortBy: sort.sortBy,
      sortDirection: sort.sortDirection,
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
    };
  });

  protected readonly products = rxResource({
    params: () => this.criteria(),
    stream: ({ params }) => this.productsApi.search(params),
  });

  protected readonly loadError = computed(() => (this.products.error() ? errorMessage(this.products.error()) : null));

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.pageIndex.set(event.pageIndex);
  }

  protected addToCart(product: Product): void {
    this.cart.add(product);
    this.notifications.success(`${product.name} added to cart.`);
  }
}
