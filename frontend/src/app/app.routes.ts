import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'shop' },
  {
    path: 'shop',
    loadChildren: () => import('./features/shop/shop.routes').then((m) => m.SHOP_ROUTES),
  },
  {
    path: 'admin/products',
    loadChildren: () => import('./features/product-admin/product-admin.routes').then((m) => m.PRODUCT_ADMIN_ROUTES),
  },
  {
    path: 'orders',
    loadChildren: () => import('./features/orders/orders.routes').then((m) => m.ORDERS_ROUTES),
  },
  {
    path: '**',
    title: 'Page not found',
    loadComponent: () => import('./layout/not-found/not-found').then((m) => m.NotFound),
  },
];
