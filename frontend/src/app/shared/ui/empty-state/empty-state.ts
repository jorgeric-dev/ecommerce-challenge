import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-empty-state',
  imports: [MatIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './empty-state.scss',
  template: `
    <section class="empty-state" role="status">
      <mat-icon class="empty-state__icon">{{ icon() }}</mat-icon>
      <h2>{{ title() }}</h2>
      @if (message()) {
        <p>{{ message() }}</p>
      }
      <ng-content />
    </section>
  `,
})
export class EmptyState {
  readonly icon = input('inventory_2');
  readonly title = input.required<string>();
  readonly message = input<string | null>(null);
}
