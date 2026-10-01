import { TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { ProductsApi } from '../../../core/api/products-api';
import { ApiError } from '../../../core/http/api-error';
import { ImportDialog, MAX_IMPORT_FILE_BYTES, validateFile } from './import-dialog';

describe('validateFile', () => {
  it.each([
    [null, null],
    [new File(['a'], 'products.txt'), 'Only .csv files can be imported.'],
    [new File([], 'products.csv'), 'The selected file is empty.'],
    [new File(['a'], 'PRODUCTS.CSV'), null],
  ])('validates %s', (file, expected) => {
    expect(validateFile(file)).toBe(expected);
  });

  it('rejects files larger than the limit', () => {
    const file = new File(['a'], 'big.csv');
    Object.defineProperty(file, 'size', { value: MAX_IMPORT_FILE_BYTES + 1 });

    expect(validateFile(file)).toBe('The file must not exceed 5 MB.');
  });
});

describe('ImportDialog', () => {
  const productsApi = { import: vi.fn() };
  const dialogRef = { close: vi.fn() };

  beforeEach(() => {
    productsApi.import.mockReset();
    dialogRef.close.mockReset();
    TestBed.configureTestingModule({
      imports: [ImportDialog],
      providers: [
        { provide: ProductsApi, useValue: productsApi },
        { provide: MatDialogRef, useValue: dialogRef },
      ],
    });
  });

  function selectFile(fixture: ReturnType<typeof TestBed.createComponent<ImportDialog>>, file: File): void {
    const input = (fixture.nativeElement as HTMLElement).querySelector('input[type=file]') as HTMLInputElement;
    Object.defineProperty(input, 'files', { value: { item: () => file }, configurable: true });
    input.dispatchEvent(new Event('change'));
  }

  it('uploads the selected file and shows the summary', async () => {
    productsApi.import.mockReturnValue(
      of({ totalRows: 3, created: 1, updated: 1, skipped: 0, failed: 1, errors: [{ lineNumber: 3, sku: 'YM-015', messages: ["Price 'free' is not a valid number."] }], warnings: [] }),
    );
    const fixture = TestBed.createComponent(ImportDialog);
    await fixture.whenStable();

    selectFile(fixture, new File(['name'], 'products.csv'));
    await fixture.whenStable();
    const importButton = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).find((b) => b.textContent?.includes('Import'))!;
    importButton.click();
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(productsApi.import).toHaveBeenCalledOnce();
    expect(text).toContain('1 created, 1 updated');
    expect(text).toContain("Price 'free' is not a valid number.");
  });

  it('closes reporting whether the catalog changed', async () => {
    productsApi.import.mockReturnValue(of({ totalRows: 1, created: 1, updated: 0, skipped: 0, failed: 0, errors: [], warnings: [] }));
    const fixture = TestBed.createComponent(ImportDialog);
    await fixture.whenStable();
    selectFile(fixture, new File(['name'], 'products.csv'));
    await fixture.whenStable();
    (fixture.componentInstance as unknown as { upload(): void }).upload();
    (fixture.componentInstance as unknown as { close(): void }).close();

    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('shows validation and server errors', async () => {
    productsApi.import.mockReturnValue(throwError(() => new ApiError(400, 'The CSV file is missing required columns: price.')));
    const fixture = TestBed.createComponent(ImportDialog);
    await fixture.whenStable();

    selectFile(fixture, new File(['x'], 'notes.txt'));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Only .csv files can be imported.');

    selectFile(fixture, new File(['x'], 'products.csv'));
    (fixture.componentInstance as unknown as { upload(): void }).upload();
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('missing required columns');
  });
});
