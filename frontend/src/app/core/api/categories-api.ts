import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { Category } from './api.models';

@Injectable({ providedIn: 'root' })
export class CategoriesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/categories`;

  readonly categories$: Observable<Category[]> = this.http
    .get<Category[]>(this.baseUrl)
    .pipe(shareReplay({ bufferSize: 1, refCount: false }));
}
