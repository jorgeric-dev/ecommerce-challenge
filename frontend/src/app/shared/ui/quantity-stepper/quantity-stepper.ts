import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-quantity-stepper',
  imports: [MatIconButton, MatIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './quantity-stepper.scss',
  template: `
    <button matIconButton type="button" aria-label="Decrease quantity" [disabled]="value() <= min()" (click)="change(value() - 1)">
      <mat-icon>remove</mat-icon>
    </button>
    <span class="quantity" aria-live="polite">{{ value() }}</span>
    <button matIconButton type="button" aria-label="Increase quantity" [disabled]="value() >= max()" (click)="change(value() + 1)">
      <mat-icon>add</mat-icon>
    </button>
  `,
})
export class QuantityStepper {
  readonly value = input.required<number>();
  readonly min = input(1);
  readonly max = input(100);
  readonly valueChange = output<number>();

  protected change(next: number): void {
    if (next >= this.min() && next <= this.max()) {
      this.valueChange.emit(next);
    }
  }
}
