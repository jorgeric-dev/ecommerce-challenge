import { Component, Type, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Button } from './button/button';
import { EmptyState } from './empty-state/empty-state';
import { OrderStatusChip } from './order-status-chip/order-status-chip';
import { PageHeader } from './page-header/page-header';
import { QuantityStepper } from './quantity-stepper/quantity-stepper';
import { StockBadge } from './stock-badge/stock-badge';

async function render<T>(component: Type<T>, inputs: Record<string, unknown>): Promise<ComponentFixture<T>> {
  const fixture = TestBed.createComponent(component);
  for (const [name, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(name, value);
  }
  await fixture.whenStable();
  return fixture;
}

describe('StockBadge', () => {
  it.each([
    [0, 'Out of stock', 'stock-badge--out'],
    [3, 'Only 3 left', 'stock-badge--low'],
    [50, '50 in stock', 'stock-badge--available'],
  ])('renders stock %s', async (stock, label, cssClass) => {
    const fixture = await render(StockBadge, { stock });
    const host = fixture.nativeElement as HTMLElement;

    expect(host.textContent?.trim()).toBe(label);
    expect(host.classList).toContain(cssClass);
  });
});

describe('OrderStatusChip', () => {
  it('renders a readable label', async () => {
    const fixture = await render(OrderStatusChip, { status: 'PaymentFailed' });

    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('Payment failed');
  });
});

describe('PageHeader and EmptyState', () => {
  it('render titles and optional texts', async () => {
    const header = await render(PageHeader, { title: 'Products', subtitle: 'Manage' });
    expect((header.nativeElement as HTMLElement).querySelector('h1')?.textContent).toBe('Products');
    expect((header.nativeElement as HTMLElement).querySelector('p')?.textContent).toBe('Manage');

    const empty = await render(EmptyState, { title: 'Nothing here' });
    expect((empty.nativeElement as HTMLElement).querySelector('h2')?.textContent).toBe('Nothing here');
    expect((empty.nativeElement as HTMLElement).querySelector('p')).toBeNull();
  });
});

describe('QuantityStepper', () => {
  it('emits the next value within bounds', async () => {
    const fixture = await render(QuantityStepper, { value: 1, max: 2 });
    const emitted: number[] = [];
    fixture.componentInstance.valueChange.subscribe((value) => emitted.push(value));
    const [decrease, increase] = fixture.debugElement.queryAll(By.css('button'));

    expect((decrease.nativeElement as HTMLButtonElement).disabled).toBe(true);
    increase.nativeElement.click();
    expect(emitted).toEqual([2]);

    fixture.componentRef.setInput('value', 2);
    await fixture.whenStable();
    expect((increase.nativeElement as HTMLButtonElement).disabled).toBe(true);
  });
});

@Component({
  imports: [Button],
  template: `<app-button icon="add" [loading]="loading()" [disabled]="disabled()" (click)="clicks = clicks + 1">Save</app-button>`,
})
class ButtonHost {
  readonly loading = signal(false);
  readonly disabled = signal(false);
  clicks = 0;
}

describe('Button', () => {
  it('projects its label, shows the icon and forwards clicks', async () => {
    const fixture = TestBed.createComponent(ButtonHost);
    await fixture.whenStable();
    const button = (fixture.nativeElement as HTMLElement).querySelector('button')!;

    expect(button.textContent).toContain('Save');
    expect(button.querySelector('mat-icon')?.textContent).toBe('add');
    button.click();
    expect(fixture.componentInstance.clicks).toBe(1);
  });

  it('disables the inner button and shows a spinner while loading', async () => {
    const fixture = TestBed.createComponent(ButtonHost);
    fixture.componentInstance.loading.set(true);
    await fixture.whenStable();
    const button = (fixture.nativeElement as HTMLElement).querySelector('button')!;

    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-busy')).toBe('true');
    expect(button.querySelector('mat-progress-spinner')).not.toBeNull();
  });
});
