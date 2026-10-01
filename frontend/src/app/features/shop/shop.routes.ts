import { inject } from '@angular/core';
import { Router, Routes } from '@angular/router';
import { CartStore } from '../../core/cart/cart.store';

export const SHOP_ROUTES: Routes = [
  {
    path: '',
    title: 'Shop',
    loadComponent: () => import('./product-catalog/product-catalog').then((m) => m.ProductCatalog),
  },
  {
    path: 'cart',
    title: 'Shopping cart',
    loadComponent: () => import('./cart/cart-page').then((m) => m.CartPage),
  },
  {
    path: 'checkout',
    title: 'Checkout',
    canActivate: [() => !inject(CartStore).isEmpty() || inject(Router).createUrlTree(['/shop/cart'])],
    loadComponent: () => import('./checkout/checkout-page').then((m) => m.CheckoutPage),
  },
];
