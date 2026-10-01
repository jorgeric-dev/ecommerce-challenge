import { AbstractControl, FormGroup } from '@angular/forms';
import { ApiError } from '../../core/http/api-error';

export function applyServerErrors(form: FormGroup, error: ApiError): boolean {
  let applied = false;

  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const control = findControl(form, field);
    if (control) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
      applied = true;
    }
  }

  return applied;
}

function findControl(form: FormGroup, path: string): AbstractControl | null {
  let current: AbstractControl | null = form;

  for (const segment of path.split('.')) {
    if (!(current instanceof FormGroup)) {
      return null;
    }
    const key: string | undefined = Object.keys(current.controls).find((name) => name.toLowerCase() === segment.toLowerCase());
    current = key ? current.controls[key] : null;
  }

  return current === form ? null : current;
}
