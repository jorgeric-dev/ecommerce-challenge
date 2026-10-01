import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class AppValidators {
  static noMarkup(control: AbstractControl<string | null>): ValidationErrors | null {
    return typeof control.value === 'string' && /[<>]/.test(control.value) ? { markup: true } : null;
  }

  static notBlank(control: AbstractControl<string | null>): ValidationErrors | null {
    return typeof control.value === 'string' && control.value.length > 0 && control.value.trim().length === 0
      ? { required: true }
      : null;
  }

  static maxDecimals(decimals: number): ValidatorFn {
    return (control: AbstractControl<number | null>): ValidationErrors | null => {
      const value = control.value;
      if (value === null || value === undefined || Number.isNaN(Number(value))) {
        return null;
      }
      const [, fraction = ''] = String(value).split('.');
      return fraction.length > decimals ? { maxDecimals: { decimals } } : null;
    };
  }

  static luhn(control: AbstractControl<string | null>): ValidationErrors | null {
    const value = control.value ?? '';
    if (!/^\d{12,19}$/.test(value)) {
      return null;
    }
    return passesLuhnCheck(value) ? null : { luhn: true };
  }
}

export function passesLuhnCheck(cardNumber: string): boolean {
  let sum = 0;
  let doubleDigit = false;
  for (let i = cardNumber.length - 1; i >= 0; i--) {
    let digit = Number(cardNumber[i]);
    if (doubleDigit) {
      digit *= 2;
      if (digit > 9) {
        digit -= 9;
      }
    }
    sum += digit;
    doubleDigit = !doubleDigit;
  }
  return sum % 10 === 0;
}
