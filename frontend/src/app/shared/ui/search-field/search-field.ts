import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { outputFromObservable, toObservable } from '@angular/core/rxjs-interop';
import { MatIconButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { debounceTime, distinctUntilChanged, map, skip } from 'rxjs';

export const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-search-field',
  imports: [MatFormField, MatLabel, MatInput, MatIcon, MatPrefix, MatSuffix, MatIconButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './search-field.scss',
  template: `
    <mat-form-field appearance="outline" subscriptSizing="dynamic">
      <mat-label>{{ label() }}</mat-label>
      <mat-icon matPrefix>search</mat-icon>
      <input
        matInput
        type="search"
        [value]="value()"
        [attr.maxlength]="maxLength()"
        [placeholder]="placeholder()"
        (input)="value.set($any($event.target).value)"
      />
      @if (value()) {
        <button matIconButton matSuffix type="button" aria-label="Clear search" (click)="value.set('')">
          <mat-icon>close</mat-icon>
        </button>
      }
    </mat-form-field>
  `,
})
export class SearchField {
  readonly label = input('Search');
  readonly placeholder = input('');
  readonly maxLength = input(100);
  readonly value = model('');

  readonly search = outputFromObservable(
    toObservable(this.value).pipe(
      skip(1),
      map((value) => value.trim()),
      debounceTime(SEARCH_DEBOUNCE_MS),
      distinctUntilChanged(),
    ),
  );
}
