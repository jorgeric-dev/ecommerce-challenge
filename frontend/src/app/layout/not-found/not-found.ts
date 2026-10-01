import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'app-not-found',
  imports: [EmptyState, Button, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-empty-state icon="explore_off" title="Page not found" message="The page you are looking for does not exist.">
      <app-button routerLink="/shop" icon="storefront">Back to the shop</app-button>
    </app-empty-state>
  `,
})
export class NotFound {}
