import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export const LOW_STOCK_THRESHOLD = 10;

@Component({
  selector: 'app-stock-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './stock-badge.scss',
  host: {
    '[class]': '"stock-badge stock-badge--" + level()',
    role: 'status',
  },
  template: `{{ label() }}`,
})
export class StockBadge {
  readonly stock = input.required<number>();

  readonly level = computed(() => {
    const stock = this.stock();
    if (stock <= 0) {
      return 'out';
    }
    return stock < LOW_STOCK_THRESHOLD ? 'low' : 'available';
  });

  readonly label = computed(() => {
    switch (this.level()) {
      case 'out':
        return 'Out of stock';
      case 'low':
        return `Only ${this.stock()} left`;
      default:
        return `${this.stock()} in stock`;
    }
  });
}
