import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CategoriesApi } from '../../../core/api/categories-api';
import { ProductsApi } from '../../../core/api/products-api';
import { CartStore } from '../../../core/cart/cart.store';
import { ApiError } from '../../../core/http/api-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import { aPage, aProduct } from '../../../testing/test-data';
import { ProductCatalog } from './product-catalog';

describe('ProductCatalog', () => {
  const productsApi = { search: vi.fn() };
  const notifications = { success: vi.fn(), error: vi.fn() };

  beforeEach(() => {
    localStorage.clear();
    productsApi.search.mockReset();
    TestBed.configureTestingModule({
      imports: [ProductCatalog],
      providers: [
        { provide: ProductsApi, useValue: productsApi },
        { provide: CategoriesApi, useValue: { categories$: of([{ id: 5, name: 'Electronics' }]) } },
        { provide: NotificationService, useValue: notifications },
      ],
    });
  });

  it('loads in-stock products sorted by name on start', async () => {
    productsApi.search.mockReturnValue(of(aPage([aProduct(), aProduct({ id: 'p-2', name: 'Keyboard', sku: 'KB-1' })])));
    const fixture = TestBed.createComponent(ProductCatalog);
    await fixture.whenStable();

    expect(productsApi.search).toHaveBeenCalledWith(
      expect.objectContaining({ inStockOnly: true, sortBy: 'Name', sortDirection: 'Asc', page: 1, pageSize: 12 }),
    );
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('app-product-card')).toHaveLength(2);
  });

  it('adds a product to the cart from its card', async () => {
    productsApi.search.mockReturnValue(of(aPage([aProduct({ name: 'Mouse' })])));
    const fixture = TestBed.createComponent(ProductCatalog);
    await fixture.whenStable();

    const addButton = (fixture.nativeElement as HTMLElement).querySelector('app-product-card button') as HTMLButtonElement;
    addButton.click();
    await fixture.whenStable();

    expect(TestBed.inject(CartStore).itemCount()).toBe(1);
    expect(notifications.success).toHaveBeenCalledWith('Mouse added to cart.');
    expect(addButton.textContent).toContain('1 in cart');
  });

  it('shows an empty state when nothing matches', async () => {
    productsApi.search.mockReturnValue(of(aPage([])));
    const fixture = TestBed.createComponent(ProductCatalog);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No products found');
  });

  it('shows the error message when loading fails', async () => {
    productsApi.search.mockReturnValue(throwError(() => new ApiError(500, 'Server unavailable')));
    const fixture = TestBed.createComponent(ProductCatalog);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('app-inline-alert')?.textContent).toContain('Server unavailable');
  });
});
