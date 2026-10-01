import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatCard, MatCardActions, MatCardContent, MatCardHeader, MatCardSubtitle, MatCardTitle } from '@angular/material/card';
import { Product } from '../../../core/api/api.models';
import { Button } from '../../../shared/ui/button/button';
import { StockBadge } from '../../../shared/ui/stock-badge/stock-badge';

@Component({
  selector: 'app-product-card',
  imports: [MatCard, MatCardHeader, MatCardTitle, MatCardSubtitle, MatCardContent, MatCardActions, CurrencyPipe, Button, StockBadge],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './product-card.scss',
  template: `
    <mat-card appearance="outlined">
      <mat-card-header>
        <mat-card-title>{{ product().name }}</mat-card-title>
        <mat-card-subtitle>{{ product().categoryName }} · {{ product().sku }}</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <p class="product-card__description">{{ product().description || 'No description available.' }}</p>
        <div class="product-card__meta">
          <span class="product-card__price">{{ product().price | currency: 'USD' }}</span>
          <app-stock-badge [stock]="product().stock" />
        </div>
      </mat-card-content>
      <mat-card-actions>
        <app-button
          appearance="tonal"
          icon="add_shopping_cart"
          [disabled]="!canAdd()"
          [ariaLabel]="'Add ' + product().name + ' to cart'"
          (click)="addToCart.emit(product())"
        >
          {{ inCart() > 0 ? 'Add another (' + inCart() + ' in cart)' : 'Add to cart' }}
        </app-button>
      </mat-card-actions>
    </mat-card>
  `,
})
export class ProductCard {
  readonly product = input.required<Product>();
  readonly inCart = input(0);
  readonly addToCart = output<Product>();

  protected readonly canAdd = computed(() => this.product().stock > this.inCart());
}
