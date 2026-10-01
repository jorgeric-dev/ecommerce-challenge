import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatBadge } from '@angular/material/badge';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatToolbar } from '@angular/material/toolbar';
import { MatTooltip } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CartStore } from '../../core/cart/cart.store';

interface NavigationLink {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbar, MatButton, MatIconButton, MatIcon, MatBadge, MatTooltip],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './shell.scss',
  templateUrl: './shell.html',
})
export class Shell {
  protected readonly cart = inject(CartStore);

  protected readonly links: NavigationLink[] = [
    { path: '/shop', label: 'Shop', icon: 'storefront' },
    { path: '/admin/products', label: 'Products', icon: 'inventory_2' },
    { path: '/orders', label: 'Orders', icon: 'receipt_long' },
  ];
}
