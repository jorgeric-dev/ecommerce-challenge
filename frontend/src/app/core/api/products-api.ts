import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL, toHttpParams } from './api-config';
import {
  CreateProductRequest,
  PagedResult,
  Product,
  ProductImportResult,
  ProductSearchCriteria,
  UpdateProductRequest,
} from './api.models';

@Injectable({ providedIn: 'root' })
export class ProductsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/products`;

  search(criteria: ProductSearchCriteria): Observable<PagedResult<Product>> {
    return this.http.get<PagedResult<Product>>(this.baseUrl, { params: toHttpParams(criteria) });
  }

  getById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, request);
  }

  update(id: string, request: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  import(file: File): Observable<ProductImportResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ProductImportResult>(`${this.baseUrl}/import`, form);
  }
}
