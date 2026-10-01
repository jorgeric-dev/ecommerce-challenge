import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';

@Component({
  selector: 'app-icon-button',
  imports: [MatIconButton, MatIcon, MatTooltip],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './icon-button.scss',
  template: `
    <button
      matIconButton
      type="button"
      [disabled]="disabled()"
      [attr.aria-label]="label()"
      [matTooltip]="label()"
      [class.app-icon-button--danger]="tone() === 'danger'"
    >
      <mat-icon>{{ icon() }}</mat-icon>
    </button>
  `,
})
export class IconButton {
  readonly icon = input.required<string>();
  readonly label = input.required<string>();
  readonly disabled = input(false);
  readonly tone = input<'default' | 'danger'>('default');
}
