import { Routes } from '@angular/router';

export const ORDERS_ROUTES: Routes = [
  {
    path: '',
    title: 'Orders',
    loadComponent: () => import('./order-list/order-list').then((m) => m.OrderList),
  },
  {
    path: ':id',
    title: 'Order details',
    loadComponent: () => import('./order-detail/order-detail').then((m) => m.OrderDetail),
  },
];
