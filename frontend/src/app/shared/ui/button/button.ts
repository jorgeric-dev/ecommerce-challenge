import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

export type ButtonAppearance = 'filled' | 'tonal' | 'outlined' | 'text' | 'elevated';

@Component({
  selector: 'app-button',
  imports: [MatButton, MatIcon, MatProgressSpinner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './button.scss',
  template: `
    <button
      [matButton]="appearance()"
      [type]="type()"
      [disabled]="disabled() || loading()"
      [attr.aria-label]="ariaLabel()"
      [attr.aria-busy]="loading()"
      [class.app-button--danger]="tone() === 'danger'"
    >
      @if (loading()) {
        <mat-progress-spinner mode="indeterminate" diameter="18" />
      } @else if (icon()) {
        <mat-icon>{{ icon() }}</mat-icon>
      }
      <ng-content />
    </button>
  `,
})
export class Button {
  readonly appearance = input<ButtonAppearance>('filled');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly icon = input<string | null>(null);
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly tone = input<'default' | 'danger'>('default');
  readonly ariaLabel = input<string | null>(null);
}
