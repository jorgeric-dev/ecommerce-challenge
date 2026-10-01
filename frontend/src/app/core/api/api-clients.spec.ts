import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { aPage, aProduct, anOrder } from '../../testing/test-data';
import { toHttpParams } from './api-config';
import { CategoriesApi } from './categories-api';
import { OrdersApi } from './orders-api';
import { ProductsApi } from './products-api';

describe('API clients', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('toHttpParams skips empty values', () => {
    const params = toHttpParams({ search: 'mouse', categoryId: null, minPrice: undefined, empty: '', page: 2, inStockOnly: false });

    expect(params.toString()).toBe('search=mouse&page=2&inStockOnly=false');
  });

  describe('ProductsApi', () => {
    it('searches with query parameters', async () => {
      const api = TestBed.inject(ProductsApi);
      const result = firstValueFrom(api.search({ search: 'mouse', sortBy: 'Price', sortDirection: 'Desc', page: 1, pageSize: 12 }));

      const request = http.expectOne((req) => req.url === '/api/v1/products');
      expect(request.request.params.get('search')).toBe('mouse');
      expect(request.request.params.get('sortBy')).toBe('Price');
      request.flush(aPage([aProduct()]));

      expect((await result).items).toHaveLength(1);
    });

    it('performs CRUD calls against the versioned endpoints', () => {
      const api = TestBed.inject(ProductsApi);
      const details = { name: 'A', description: null, categoryId: 1, price: 1, stock: 1, weightKg: 0 };

      api.getById('p-1').subscribe();
      http.expectOne({ method: 'GET', url: '/api/v1/products/p-1' }).flush(aProduct());

      api.create({ ...details, sku: 'A-1' }).subscribe();
      const create = http.expectOne({ method: 'POST', url: '/api/v1/products' });
      expect(create.request.body.sku).toBe('A-1');
      create.flush(aProduct());

      api.update('p-1', details).subscribe();
      http.expectOne({ method: 'PUT', url: '/api/v1/products/p-1' }).flush(aProduct());

      api.delete('p-1').subscribe();
      http.expectOne({ method: 'DELETE', url: '/api/v1/products/p-1' }).flush(null, { status: 204, statusText: 'No Content' });
    });

    it('uploads the CSV as multipart form data', () => {
      const api = TestBed.inject(ProductsApi);
      const file = new File(['name,sku'], 'products.csv', { type: 'text/csv' });

      api.import(file).subscribe();

      const request = http.expectOne({ method: 'POST', url: '/api/v1/products/import' });
      expect(request.request.body).toBeInstanceOf(FormData);
      expect((request.request.body as FormData).get('file')).toBeTruthy();
      request.flush({ totalRows: 0, created: 0, updated: 0, skipped: 0, failed: 0, errors: [], warnings: [] });
    });
  });

  describe('CategoriesApi', () => {
    it('caches categories after the first request', async () => {
      const api = TestBed.inject(CategoriesApi);

      const first = firstValueFrom(api.categories$);
      http.expectOne('/api/v1/categories').flush([{ id: 1, name: 'Books' }]);
      await first;
      const second = await firstValueFrom(api.categories$);

      expect(second).toEqual([{ id: 1, name: 'Books' }]);
      http.expectNone('/api/v1/categories');
    });
  });

  describe('OrdersApi', () => {
    it('places, fetches and searches orders', () => {
      const api = TestBed.inject(OrdersApi);

      api.place({
        customer: { name: 'J', email: 'j@x.com', shippingAddress: 'A' },
        items: [{ productId: 'p-1', quantity: 1 }],
        payment: { cardHolderName: 'J', cardNumber: '4242424242424242', expiryMonth: 1, expiryYear: 2030, cvv: '123' },
      }).subscribe();
      http.expectOne({ method: 'POST', url: '/api/v1/orders' }).flush(anOrder());

      api.getById('o-1').subscribe();
      http.expectOne({ method: 'GET', url: '/api/v1/orders/o-1' }).flush(anOrder());

      api.search({ customerEmail: 'j@x.com', page: 1 }).subscribe();
      const search = http.expectOne((req) => req.url === '/api/v1/orders');
      expect(search.request.params.get('customerEmail')).toBe('j@x.com');
      search.flush(aPage([]));
    });
  });
});
