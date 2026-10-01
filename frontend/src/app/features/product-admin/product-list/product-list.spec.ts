import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of, Subject, throwError } from 'rxjs';
import { CategoriesApi } from '../../../core/api/categories-api';
import { ProductsApi } from '../../../core/api/products-api';
import { ApiError } from '../../../core/http/api-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import { ConfirmDialogService } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import { aPage, aProduct } from '../../../testing/test-data';
import { ProductList } from './product-list';

type ProductListInternals = {
  openCreate(): void;
  openEdit(product: ReturnType<typeof aProduct>): void;
  openImport(): void;
  delete(product: ReturnType<typeof aProduct>): void;
  onSort(sort: { active: string; direction: 'asc' | 'desc' | '' }): void;
  onPage(event: { pageIndex: number; pageSize: number; length: number }): void;
};

describe('ProductList', () => {
  const productsApi = { search: vi.fn(), delete: vi.fn() };
  const notifications = { success: vi.fn(), error: vi.fn() };
  const confirmDialog = { confirm: vi.fn() };
  let afterClosed: Subject<unknown>;
  const dialog = { open: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    afterClosed = new Subject();
    dialog.open.mockReturnValue({ afterClosed: () => afterClosed });
    productsApi.search.mockReturnValue(of(aPage([aProduct(), aProduct({ id: 'p-2', sku: 'KB-1', name: 'Keyboard', stock: 0 })], { totalCount: 2 })));
    TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        { provide: ProductsApi, useValue: productsApi },
        { provide: CategoriesApi, useValue: { categories$: of([]) } },
        { provide: NotificationService, useValue: notifications },
        { provide: ConfirmDialogService, useValue: confirmDialog },
      ],
    });
    TestBed.overrideProvider(MatDialog, { useValue: dialog });
  });

  async function create() {
    const fixture = TestBed.createComponent(ProductList);
    await fixture.whenStable();
    return { fixture, list: fixture.componentInstance as unknown as ProductListInternals };
  }

  it('renders a row per product', async () => {
    const { fixture } = await create();

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tr.mat-mdc-row');
    expect(rows).toHaveLength(2);
    expect(rows[1].textContent).toContain('Out of stock');
  });

  it('maps table sorting and paging to the search criteria', async () => {
    const { fixture, list } = await create();

    list.onSort({ active: 'price', direction: 'desc' });
    await fixture.whenStable();
    expect(productsApi.search).toHaveBeenLastCalledWith(expect.objectContaining({ sortBy: 'Price', sortDirection: 'Desc', page: 1 }));

    list.onPage({ pageIndex: 2, pageSize: 50, length: 200 });
    await fixture.whenStable();
    expect(productsApi.search).toHaveBeenLastCalledWith(expect.objectContaining({ page: 3, pageSize: 50 }));
  });

  it.each([
    ['Name', 'Name', 'Desc', 'Asc'],
    ['Price', 'Price', 'Asc', 'Desc'],
    ['Category', 'Category', 'Asc', 'Desc'],
  ])('toggles %s header between ascending and descending across reloads', async (header, sortBy, first, second) => {
    const { fixture } = await create();
    const clickHeader = async () => {
      const headers = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLElement>('th .mat-sort-header-container'));
      headers.find((element) => element.textContent?.trim() === header)!.click();
      await fixture.whenStable();
    };

    await clickHeader();
    expect(productsApi.search).toHaveBeenLastCalledWith(expect.objectContaining({ sortBy, sortDirection: first }));

    await clickHeader();
    expect(productsApi.search).toHaveBeenLastCalledWith(expect.objectContaining({ sortBy, sortDirection: second }));

    await clickHeader();
    expect(productsApi.search).toHaveBeenLastCalledWith(expect.objectContaining({ sortBy, sortDirection: first }));
  });

  it('reloads after a product is created or edited', async () => {
    const { fixture, list } = await create();
    const calls = productsApi.search.mock.calls.length;

    list.openCreate();
    afterClosed.next(aProduct({ name: 'New thing' }));
    await fixture.whenStable();

    expect(notifications.success).toHaveBeenCalledWith('New thing created.');
    expect(productsApi.search.mock.calls.length).toBeGreaterThan(calls);

    afterClosed = new Subject();
    list.openEdit(aProduct());
    afterClosed.next(undefined);
    expect(notifications.success).toHaveBeenCalledTimes(1);
  });

  it('reloads after an import that changed the catalog', async () => {
    const { fixture, list } = await create();
    const calls = productsApi.search.mock.calls.length;

    list.openImport();
    afterClosed.next(true);
    await fixture.whenStable();

    expect(productsApi.search.mock.calls.length).toBeGreaterThan(calls);
  });

  it('deletes a product after confirmation', async () => {
    confirmDialog.confirm.mockReturnValue(of(true));
    productsApi.delete.mockReturnValue(of(undefined));
    const { list } = await create();

    list.delete(aProduct({ name: 'Mouse' }));

    expect(productsApi.delete).toHaveBeenCalledWith('p-1');
    expect(notifications.success).toHaveBeenCalledWith('Mouse deleted.');
  });

  it('does nothing when deletion is cancelled and reports failures', async () => {
    const { list } = await create();

    confirmDialog.confirm.mockReturnValue(of(false));
    list.delete(aProduct());
    expect(productsApi.delete).not.toHaveBeenCalled();

    confirmDialog.confirm.mockReturnValue(of(true));
    productsApi.delete.mockReturnValue(throwError(() => new ApiError(404, 'Product was not found.')));
    list.delete(aProduct());
    expect(notifications.error).toHaveBeenCalledWith('Product was not found.');
  });
});
