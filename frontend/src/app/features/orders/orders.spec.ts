import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { OrdersApi } from '../../core/api/orders-api';
import { ApiError } from '../../core/http/api-error';
import { aPage, anOrder } from '../../testing/test-data';
import { OrderDetail } from './order-detail/order-detail';
import { OrderList } from './order-list/order-list';

describe('Orders feature', () => {
  const ordersApi = { search: vi.fn(), getById: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: OrdersApi, useValue: ordersApi }],
    });
  });

  describe('OrderList', () => {
    it('lists orders and opens the selected one', async () => {
      ordersApi.search.mockReturnValue(
        of(aPage([{ id: 'o-1', orderNumber: 'ORD-1', customerName: 'Jane', customerEmail: 'jane@example.com', status: 'Paid', totalAmount: 10, itemCount: 1, createdAtUtc: '2026-09-30T12:00:00Z' }])),
      );
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
      const fixture = TestBed.createComponent(OrderList);
      await fixture.whenStable();

      const row = (fixture.nativeElement as HTMLElement).querySelector('tr.mat-mdc-row') as HTMLElement;
      expect(row.textContent).toContain('ORD-1');
      row.click();

      expect(navigate).toHaveBeenCalledWith(['/orders', 'o-1']);
    });

    it('shows an empty state and handles paging', async () => {
      ordersApi.search.mockReturnValue(of(aPage([])));
      const fixture = TestBed.createComponent(OrderList);
      await fixture.whenStable();

      expect((fixture.nativeElement as HTMLElement).textContent).toContain('No orders yet');

      (fixture.componentInstance as unknown as { onPage(e: { pageIndex: number; pageSize: number }): void }).onPage({ pageIndex: 1, pageSize: 50 });
      await fixture.whenStable();
      expect(ordersApi.search).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 50 }));
    });
  });

  describe('OrderDetail', () => {
    it('renders the order with its items and payment info', async () => {
      ordersApi.getById.mockReturnValue(of(anOrder()));
      const fixture = TestBed.createComponent(OrderDetail);
      fixture.componentRef.setInput('id', 'o-1');
      await fixture.whenStable();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(ordersApi.getById).toHaveBeenCalledWith('o-1');
      expect(text).toContain('Order ORD-ABC123456789');
      expect(text).toContain('Wireless Mouse');
      expect(text).toContain('Card ending in 4242');
    });

    it('shows an error when the order cannot be loaded', async () => {
      ordersApi.getById.mockReturnValue(throwError(() => new ApiError(404, "Order with id 'x' was not found.")));
      const fixture = TestBed.createComponent(OrderDetail);
      fixture.componentRef.setInput('id', 'x');
      await fixture.whenStable();

      expect((fixture.nativeElement as HTMLElement).textContent).toContain('was not found');
    });
  });
});
