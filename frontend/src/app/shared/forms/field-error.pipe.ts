import { Pipe, PipeTransform } from '@angular/core';
import { AbstractControl } from '@angular/forms';

export function describeControlError(control: AbstractControl | null, label: string): string {
  const errors = control?.errors;
  if (!errors) {
    return '';
  }

  if (errors['server']) return String(errors['server']);
  if (errors['required']) return `${label} is required.`;
  if (errors['email']) return 'Enter a valid email address.';
  if (errors['markup']) return `${label} must not contain HTML or script markup.`;
  if (errors['maxlength']) return `${label} must not exceed ${errors['maxlength'].requiredLength} characters.`;
  if (errors['min']) return `${label} must be at least ${errors['min'].min}.`;
  if (errors['max']) return `${label} must be at most ${errors['max'].max}.`;
  if (errors['maxDecimals']) return `${label} must not have more than ${errors['maxDecimals'].decimals} decimal places.`;
  if (errors['luhn']) return 'Card number is invalid.';
  if (errors['pattern']) return `${label} has an invalid format.`;
  return `${label} is invalid.`;
}

@Pipe({ name: 'fieldError', pure: false })
export class FieldErrorPipe implements PipeTransform {
  transform(control: AbstractControl | null, label: string): string {
    return describeControlError(control, label);
  }
}
