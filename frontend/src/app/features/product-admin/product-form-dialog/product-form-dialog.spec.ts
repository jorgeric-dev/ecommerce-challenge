import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { CategoriesApi } from '../../../core/api/categories-api';
import { Product } from '../../../core/api/api.models';
import { ProductsApi } from '../../../core/api/products-api';
import { ApiError } from '../../../core/http/api-error';
import { aProduct } from '../../../testing/test-data';
import { ProductFormDialog } from './product-form-dialog';

type DialogInternals = {
  form: FormGroup;
  isEdit: boolean;
  save(): void;
  cancel(): void;
  saveError(): string | null;
};

describe('ProductFormDialog', () => {
  const productsApi = { create: vi.fn(), update: vi.fn() };
  const dialogRef = { close: vi.fn() };

  function setup(product: Product | null) {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [ProductFormDialog],
      providers: [
        { provide: ProductsApi, useValue: productsApi },
        { provide: CategoriesApi, useValue: { categories$: of([{ id: 5, name: 'Electronics' }]) } },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { product } },
      ],
    });
    const fixture = TestBed.createComponent(ProductFormDialog);
    return { fixture, dialog: fixture.componentInstance as unknown as DialogInternals };
  }

  it('validates required fields and business limits before saving', () => {
    const { dialog } = setup(null);
    dialog.form.patchValue({ sku: 'bad sku', name: '<b>x</b>', price: 1.999, stock: -1, weightKg: 1.23456 });

    dialog.save();

    expect(productsApi.create).not.toHaveBeenCalled();
    expect(dialog.form.get('sku')?.hasError('pattern')).toBe(true);
    expect(dialog.form.get('name')?.hasError('markup')).toBe(true);
    expect(dialog.form.get('categoryId')?.hasError('required')).toBe(true);
    expect(dialog.form.get('price')?.hasError('maxDecimals')).toBe(true);
    expect(dialog.form.get('stock')?.hasError('min')).toBe(true);
    expect(dialog.form.get('weightKg')?.hasError('maxDecimals')).toBe(true);
  });

  it('creates a product with trimmed values and closes with the result', () => {
    const created = aProduct({ id: 'new' });
    productsApi.create.mockReturnValue(of(created));
    const { dialog } = setup(null);
    dialog.form.patchValue({ sku: 'NEW-1', name: ' Lamp ', description: '  ', categoryId: 5, price: 10.5, stock: 3, weightKg: 1.2 });

    dialog.save();

    expect(productsApi.create).toHaveBeenCalledWith({ sku: 'NEW-1', name: 'Lamp', description: null, categoryId: 5, price: 10.5, stock: 3, weightKg: 1.2 });
    expect(dialogRef.close).toHaveBeenCalledWith(created);
  });

  it('edits an existing product without changing the SKU', () => {
    const product = aProduct();
    productsApi.update.mockReturnValue(of(product));
    const { dialog } = setup(product);

    expect(dialog.isEdit).toBe(true);
    expect(dialog.form.get('sku')?.disabled).toBe(true);
    dialog.form.patchValue({ name: 'Renamed' });
    dialog.save();

    expect(productsApi.update).toHaveBeenCalledWith(product.id, expect.objectContaining({ name: 'Renamed' }));
    expect(productsApi.update.mock.calls[0][1]).not.toHaveProperty('sku');
  });

  it('maps server validation errors to fields and shows other errors inline', () => {
    const { dialog } = setup(null);
    dialog.form.patchValue({ sku: 'DUP-1', name: 'Lamp', categoryId: 5, price: 10, stock: 1, weightKg: 1 });

    productsApi.create.mockReturnValue(throwError(() => new ApiError(400, 'Validation failed', { Sku: ['SKU is invalid.'] })));
    dialog.save();
    expect(dialog.form.get('sku')?.errors).toEqual({ server: 'SKU is invalid.' });

    dialog.form.get('sku')?.setValue('DUP-2');
    productsApi.create.mockReturnValue(throwError(() => new ApiError(409, "A product with SKU 'DUP-2' already exists.")));
    dialog.save();
    expect(dialog.saveError()).toContain('already exists');
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('closes without a result when cancelled', () => {
    const { dialog } = setup(null);

    dialog.cancel();

    expect(dialogRef.close).toHaveBeenCalledWith();
  });
});
