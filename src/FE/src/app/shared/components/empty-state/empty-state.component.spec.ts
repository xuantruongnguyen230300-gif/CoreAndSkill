import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { EmptyStateComponent } from './empty-state.component';

describe('EmptyStateComponent — headingLevel', () => {
  function dung(muc: 1 | 2 | 3 | 4): HTMLElement {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideRouter([])],
    });
    const fixture = TestBed.createComponent(EmptyStateComponent);
    fixture.componentRef.setInput('variant', 'error');
    fixture.componentRef.setInput('title', 'Tiêu đề');
    fixture.componentRef.setInput('headingLevel', muc);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('headingLevel = 1 → đúng một <h1>, không thẻ tiêu đề khác', () => {
    const el = dung(1);
    expect(el.querySelectorAll('h1').length).toBe(1);
    expect(el.querySelector('h1')?.textContent).toContain('Tiêu đề');
    expect(el.querySelectorAll('h2, h3, h4').length).toBe(0);
  });

  for (const [muc, the] of [
    [2, 'h2'],
    [3, 'h3'],
    [4, 'h4'],
  ] as const) {
    it(`headingLevel = ${muc} → <${the}>`, () => {
      const el = dung(muc);
      expect(el.querySelectorAll(the).length).toBe(1);
      expect(el.querySelectorAll('h1').length).toBe(0);
    });
  }
});
