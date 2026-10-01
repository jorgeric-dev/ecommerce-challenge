import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogContent, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';
import { MatError, MatFormField, MatHint, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatOption, MatSelect } from '@angular/material/select';
import { Observable } from 'rxjs';
import { Product, ProductDetails } from '../../../core/api/api.models';
import { CategoriesApi } from '../../../core/api/categories-api';
import { ProductsApi } from '../../../core/api/products-api';
import { ApiError } from '../../../core/http/api-error';
import { errorMessage } from '../../../shared/data/error-message';
import { FieldErrorPipe } from '../../../shared/forms/field-error.pipe';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { AppValidators } from '../../../shared/forms/validators';
import { Button } from '../../../shared/ui/button/button';
import { InlineAlert } from '../../../shared/ui/inline-alert/inline-alert';
import { PRODUCT_CONSTRAINTS as C } from '../product-constraints';

export interface ProductFormDialogData {
  product: Product | null;
}

@Component({
  selector: 'app-product-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatFormField,
    MatLabel,
    MatInput,
    MatError,
    MatHint,
    MatPrefix,
    MatSuffix,
    MatSelect,
    MatOption,
    Button,
    InlineAlert,
    FieldErrorPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './product-form-dialog.scss',
  templateUrl: './product-form-dialog.html',
})
export class ProductFormDialog {
  private readonly productsApi = inject(ProductsApi);
  private readonly dialogRef = inject<MatDialogRef<ProductFormDialog, Product>>(MatDialogRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  protected readonly data = inject<ProductFormDialogData>(MAT_DIALOG_DATA);

  protected readonly constraints = C;
  protected readonly isEdit = this.data.product !== null;
  protected readonly categories = toSignal(inject(CategoriesApi).categories$, { initialValue: [] });
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    sku: [
      { value: this.data.product?.sku ?? '', disabled: this.isEdit },
      [Validators.required, Validators.maxLength(C.skuMaxLength), Validators.pattern(C.skuPattern)],
    ],
    name: [
      this.data.product?.name ?? '',
      [Validators.required, AppValidators.notBlank, AppValidators.noMarkup, Validators.maxLength(C.nameMaxLength)],
    ],
    description: [this.data.product?.description ?? '', [AppValidators.noMarkup, Validators.maxLength(C.descriptionMaxLength)]],
    categoryId: [this.data.product?.categoryId ?? (null as number | null), [Validators.required]],
    price: [
      this.data.product?.price ?? (null as number | null),
      [Validators.required, Validators.min(C.minPrice), Validators.max(C.maxPrice), AppValidators.maxDecimals(C.priceDecimals)],
    ],
    stock: [
      this.data.product?.stock ?? 0,
      [Validators.required, Validators.min(C.minStock), Validators.max(C.maxStock), Validators.pattern(/^\d+$/)],
    ],
    weightKg: [
      this.data.product?.weightKg ?? 0,
      [Validators.required, Validators.min(C.minWeightKg), Validators.max(C.maxWeightKg), AppValidators.maxDecimals(C.weightDecimals)],
    ],
  });

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.saveError.set(null);

    this.request().subscribe({
      next: (product) => this.dialogRef.close(product),
      error: (error: unknown) => {
        this.saving.set(false);
        if (error instanceof ApiError && applyServerErrors(this.form, error)) {
          return;
        }
        this.saveError.set(errorMessage(error));
      },
    });
  }

  protected cancel(): void {
    this.dialogRef.close();
  }

  private request(): Observable<Product> {
    const value = this.form.getRawValue();
    const details: ProductDetails = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      categoryId: Number(value.categoryId),
      price: Number(value.price),
      stock: Number(value.stock),
      weightKg: Number(value.weightKg),
    };

    return this.data.product
      ? this.productsApi.update(this.data.product.id, details)
      : this.productsApi.create({ ...details, sku: value.sku.trim() });
  }
}
