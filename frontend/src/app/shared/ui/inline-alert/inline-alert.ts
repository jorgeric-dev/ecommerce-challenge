import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-inline-alert',
  imports: [MatIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './inline-alert.scss',
  host: {
    '[class]': '"inline-alert inline-alert--" + tone()',
    role: 'alert',
  },
  template: `
    <mat-icon>{{ icon() }}</mat-icon>
    <div class="inline-alert__content"><ng-content /></div>
  `,
})
export class InlineAlert {
  readonly tone = input<'error' | 'info' | 'success'>('error');

  protected readonly icon = computed(() => {
    switch (this.tone()) {
      case 'success':
        return 'check_circle';
      case 'info':
        return 'info';
      default:
        return 'error';
    }
  });
}
