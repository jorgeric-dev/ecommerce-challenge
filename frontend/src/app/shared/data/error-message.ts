import { ApiError } from '../../core/http/api-error';

export function errorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (error instanceof ApiError) {
    return error.summary;
  }
  if (error instanceof Error && error.message) {
    return error.message;
  }
  return fallback;
}
