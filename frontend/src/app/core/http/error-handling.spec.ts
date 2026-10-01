import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from '../notifications/notification.service';
import { ApiError } from './api-error';
import { errorInterceptor } from './error.interceptor';

describe('ApiError', () => {
  it('maps network failures to a friendly message', () => {
    const error = ApiError.fromHttpError(new HttpErrorResponse({ status: 0 }));

    expect(error.status).toBe(0);
    expect(error.message).toContain('could not be reached');
  });

  it('uses the problem details payload', () => {
    const error = ApiError.fromHttpError(
      new HttpErrorResponse({
        status: 400,
        error: { title: 'Validation failed', detail: 'One or more validation errors occurred.', errors: { Name: ['Name is required.'] } },
      }),
    );

    expect(error.isValidation).toBe(true);
    expect(error.fieldError('name')).toBe('Name is required.');
    expect(error.fieldError('price')).toBeNull();
    expect(error.summary).toBe('Name is required.');
  });

  it('falls back to a generic message when the body is not problem details', () => {
    const error = ApiError.fromHttpError(new HttpErrorResponse({ status: 502, error: 'Bad gateway' }));

    expect(error.message).toBe('Request failed with status 502.');
    expect(error.isValidation).toBe(false);
    expect(error.summary).toBe(error.message);
  });
});

describe('errorInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  const notifications = { error: vi.fn(), success: vi.fn() };

  beforeEach(() => {
    notifications.error.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: NotificationService, useValue: notifications },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  it('converts HTTP errors into ApiError without notifying for client errors', async () => {
    const result = firstValueFrom(http.get('/api/v1/products/x'));
    controller.expectOne('/api/v1/products/x').flush({ title: 'Resource not found', detail: 'Product was not found.' }, { status: 404, statusText: 'Not Found' });

    await expect(result).rejects.toMatchObject({ status: 404, message: 'Product was not found.' });
    expect(notifications.error).not.toHaveBeenCalled();
  });

  it('notifies the user about server errors', async () => {
    const result = firstValueFrom(http.get('/api/v1/products'));
    controller.expectOne('/api/v1/products').flush({ title: 'Internal server error', detail: 'Unexpected.' }, { status: 500, statusText: 'Error' });

    await expect(result).rejects.toBeInstanceOf(ApiError);
    expect(notifications.error).toHaveBeenCalledWith('Unexpected.');
  });
});
