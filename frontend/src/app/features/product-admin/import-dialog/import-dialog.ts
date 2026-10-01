import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatDialogActions, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatList, MatListItem, MatListItemIcon, MatListItemLine, MatListItemTitle } from '@angular/material/list';
import { ProductImportIssue, ProductImportResult } from '../../../core/api/api.models';
import { ProductsApi } from '../../../core/api/products-api';
import { errorMessage } from '../../../shared/data/error-message';
import { Button } from '../../../shared/ui/button/button';
import { InlineAlert } from '../../../shared/ui/inline-alert/inline-alert';

export const MAX_IMPORT_FILE_BYTES = 5 * 1024 * 1024;

@Component({
  selector: 'app-import-dialog',
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatIcon,
    MatList,
    MatListItem,
    MatListItemTitle,
    MatListItemIcon,
    MatListItemLine,
    Button,
    InlineAlert,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './import-dialog.scss',
  templateUrl: './import-dialog.html',
})
export class ImportDialog {
  private readonly productsApi = inject(ProductsApi);
  private readonly dialogRef = inject<MatDialogRef<ImportDialog, boolean>>(MatDialogRef);

  protected readonly file = signal<File | null>(null);
  protected readonly uploading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly result = signal<ProductImportResult | null>(null);

  protected readonly hasChanges = computed(() => {
    const result = this.result();
    return result !== null && result.created + result.updated > 0;
  });

  protected selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = input.files?.item(0) ?? null;
    input.value = '';

    this.result.set(null);
    this.error.set(validateFile(selected));
    this.file.set(this.error() ? null : selected);
  }

  protected upload(): void {
    const file = this.file();
    if (!file || this.uploading()) {
      return;
    }

    this.uploading.set(true);
    this.error.set(null);

    this.productsApi.import(file).subscribe({
      next: (result) => {
        this.result.set(result);
        this.file.set(null);
        this.uploading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(errorMessage(error));
        this.uploading.set(false);
      },
    });
  }

  protected close(): void {
    this.dialogRef.close(this.hasChanges());
  }

  protected describe(issue: ProductImportIssue): string {
    return issue.messages.join(' ');
  }
}

export function validateFile(file: File | null): string | null {
  if (!file) {
    return null;
  }
  if (!file.name.toLowerCase().endsWith('.csv')) {
    return 'Only .csv files can be imported.';
  }
  if (file.size === 0) {
    return 'The selected file is empty.';
  }
  if (file.size > MAX_IMPORT_FILE_BYTES) {
    return 'The file must not exceed 5 MB.';
  }
  return null;
}
