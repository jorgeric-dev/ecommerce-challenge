import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL, toHttpParams } from './api-config';
import { Order, OrderSearchCriteria, OrderSummary, PagedResult, PlaceOrderRequest } from './api.models';

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/orders`;

  search(criteria: OrderSearchCriteria): Observable<PagedResult<OrderSummary>> {
    return this.http.get<PagedResult<OrderSummary>>(this.baseUrl, { params: toHttpParams(criteria) });
  }

  getById(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/${id}`);
  }

  place(request: PlaceOrderRequest): Observable<Order> {
    return this.http.post<Order>(this.baseUrl, request);
  }
}
