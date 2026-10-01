import { Routes } from '@angular/router';

export const PRODUCT_ADMIN_ROUTES: Routes = [
  {
    path: '',
    title: 'Manage products',
    loadComponent: () => import('./product-list/product-list').then((m) => m.ProductList),
  },
];
