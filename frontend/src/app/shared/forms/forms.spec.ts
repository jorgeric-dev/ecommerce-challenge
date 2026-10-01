import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ApiError } from '../../core/http/api-error';
import { errorMessage } from '../data/error-message';
import { describeControlError, FieldErrorPipe } from './field-error.pipe';
import { applyServerErrors } from './server-errors';
import { AppValidators, passesLuhnCheck } from './validators';

describe('AppValidators', () => {
  it.each([
    ['<script>', { markup: true }],
    ['plain text', null],
    [null, null],
  ])('noMarkup(%s)', (value, expected) => {
    expect(AppValidators.noMarkup(new FormControl(value))).toEqual(expected);
  });

  it.each([
    ['   ', { required: true }],
    ['', null],
    ['value', null],
  ])('notBlank(%j)', (value, expected) => {
    expect(AppValidators.notBlank(new FormControl(value))).toEqual(expected);
  });

  it.each([
    [10.5, 2, null],
    [10.555, 2, { maxDecimals: { decimals: 2 } }],
    [1.2345, 3, { maxDecimals: { decimals: 3 } }],
    [null, 2, null],
  ])('maxDecimals(%s, %s)', (value, decimals, expected) => {
    expect(AppValidators.maxDecimals(decimals)(new FormControl(value))).toEqual(expected);
  });

  it.each([
    ['4242424242424242', null],
    ['4242424242424241', { luhn: true }],
    ['123', null],
  ])('luhn(%s)', (value, expected) => {
    expect(AppValidators.luhn(new FormControl(value))).toEqual(expected);
  });

  it('passesLuhnCheck validates well known test cards', () => {
    expect(passesLuhnCheck('4000000000000002')).toBe(true);
    expect(passesLuhnCheck('5555555555554444')).toBe(true);
    expect(passesLuhnCheck('1234567890123456')).toBe(false);
  });
});

describe('describeControlError', () => {
  const describe_ = (errors: Record<string, unknown> | null) => {
    const control = new FormControl('');
    control.setErrors(errors);
    return describeControlError(control, 'Price');
  };

  it('returns an empty string for valid controls', () => {
    expect(describe_(null)).toBe('');
    expect(describeControlError(null, 'Price')).toBe('');
  });

  it.each([
    [{ server: 'From server' }, 'From server'],
    [{ required: true }, 'Price is required.'],
    [{ email: true }, 'Enter a valid email address.'],
    [{ markup: true }, 'Price must not contain HTML or script markup.'],
    [{ maxlength: { requiredLength: 5 } }, 'Price must not exceed 5 characters.'],
    [{ min: { min: 1 } }, 'Price must be at least 1.'],
    [{ max: { max: 9 } }, 'Price must be at most 9.'],
    [{ maxDecimals: { decimals: 2 } }, 'Price must not have more than 2 decimal places.'],
    [{ luhn: true }, 'Card number is invalid.'],
    [{ pattern: {} }, 'Price has an invalid format.'],
    [{ custom: true }, 'Price is invalid.'],
  ])('describes %j', (errors, expected) => {
    expect(describe_(errors)).toBe(expected);
  });

  it('is exposed through the fieldError pipe', () => {
    const control = new FormControl('', Validators.required);
    expect(new FieldErrorPipe().transform(control, 'Name')).toBe('Name is required.');
  });
});

describe('applyServerErrors', () => {
  it('maps nested, case-insensitive server field names onto controls', () => {
    const form = new FormGroup({
      name: new FormControl(''),
      customer: new FormGroup({ email: new FormControl('') }),
    });
    const error = new ApiError(400, 'Validation failed', { Name: ['Name is taken.'], 'Customer.Email': ['Invalid email.'], Unknown: ['x'] });

    expect(applyServerErrors(form, error)).toBe(true);
    expect(form.controls.name.errors).toEqual({ server: 'Name is taken.' });
    expect(form.controls.customer.controls.email.errors).toEqual({ server: 'Invalid email.' });
  });

  it('returns false when no field matches', () => {
    const form = new FormGroup({ name: new FormControl('') });

    expect(applyServerErrors(form, new ApiError(409, 'Conflict'))).toBe(false);
  });
});

describe('errorMessage', () => {
  it('prefers ApiError summaries, then error messages, then the fallback', () => {
    expect(errorMessage(new ApiError(409, 'Conflict happened'))).toBe('Conflict happened');
    expect(errorMessage(new Error('Boom'))).toBe('Boom');
    expect(errorMessage('weird', 'Fallback')).toBe('Fallback');
  });
});
