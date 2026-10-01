import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDivider } from '@angular/material/divider';
import { RouterLink } from '@angular/router';
import { CartStore } from '../../../core/cart/cart.store';
import { Button } from '../../../shared/ui/button/button';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { IconButton } from '../../../shared/ui/icon-button/icon-button';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { QuantityStepper } from '../../../shared/ui/quantity-stepper/quantity-stepper';

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink, MatCard, MatCardContent, MatDivider, PageHeader, EmptyState, Button, IconButton, QuantityStepper],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './cart-page.scss',
  templateUrl: './cart-page.html',
})
export class CartPage {
  protected readonly cart = inject(CartStore);
}
