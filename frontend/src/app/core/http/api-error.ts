import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../api/api.models';

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly fieldErrors: Record<string, string[]> = {},
    readonly problem: ProblemDetails | null = null,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  static fromHttpError(error: HttpErrorResponse): ApiError {
    if (error.status === 0) {
      return new ApiError(0, 'The server could not be reached. Please check your connection and try again.');
    }

    const problem = isProblemDetails(error.error) ? error.error : null;
    const message = problem?.detail ?? problem?.title ?? `Request failed with status ${error.status}.`;
    return new ApiError(error.status, message, problem?.errors ?? {}, problem);
  }

  get isValidation(): boolean {
    return this.status === 400 && Object.keys(this.fieldErrors).length > 0;
  }

  fieldError(field: string): string | null {
    const key = Object.keys(this.fieldErrors).find((name) => name.toLowerCase() === field.toLowerCase());
    return key ? this.fieldErrors[key].join(' ') : null;
  }

  get summary(): string {
    if (!this.isValidation) {
      return this.message;
    }
    return Object.values(this.fieldErrors).flat().join(' ');
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'detail' in value);
}
