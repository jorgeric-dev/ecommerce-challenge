import { ChangeDetectionStrategy, Component, inject, input, model } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatOption, MatSelect } from '@angular/material/select';
import { CategoriesApi } from '../../../core/api/categories-api';

@Component({
  selector: 'app-category-select',
  imports: [MatFormField, MatLabel, MatSelect, MatOption],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host {
      display: block;
      min-width: 200px;
    }
    mat-form-field {
      width: 100%;
    }
  `,
  template: `
    <mat-form-field subscriptSizing="dynamic">
      <mat-label>{{ label() }}</mat-label>
      <mat-select [value]="value()" (valueChange)="value.set($event)">
        <mat-option [value]="null">All categories</mat-option>
        @for (category of categories(); track category.id) {
          <mat-option [value]="category.id">{{ category.name }}</mat-option>
        }
      </mat-select>
    </mat-form-field>
  `,
})
export class CategorySelect {
  readonly label = input('Category');
  readonly value = model<number | null>(null);

  protected readonly categories = toSignal(inject(CategoriesApi).categories$, { initialValue: [] });
}
