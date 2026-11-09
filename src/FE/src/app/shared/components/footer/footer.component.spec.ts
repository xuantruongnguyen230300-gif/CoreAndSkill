import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FooterComponent } from './footer.component';

/** Design/Components/Footer.md §Phiên bản hiển thị (Q59). */
describe('FooterComponent — phiên bản', () => {
  let fixture: ComponentFixture<FooterComponent>;
  const root = (): HTMLElement => fixture.nativeElement as HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FooterComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(FooterComponent);
  });

  it('có mã: hiện sau bản quyền, ngăn bằng dấu · aria-hidden', () => {
    fixture.componentRef.setInput('copyright', '© 2026 Core');
    fixture.componentRef.setInput('version', 'Phiên bản ac807bd');
    fixture.detectChanges();

    const dsSpan = Array.from(root().querySelectorAll('.footer__trai > span'));
    expect(dsSpan.map((s) => s.textContent)).toEqual(['© 2026 Core', '·', 'Phiên bản ac807bd']);
    expect(dsSpan[1].getAttribute('aria-hidden')).toBe('true');
  });

  it('version = null: ẩn cả phần phiên bản lẫn dấu ·', () => {
    fixture.componentRef.setInput('copyright', '© 2026 Core');
    fixture.componentRef.setInput('version', null);
    fixture.detectChanges();

    expect(root().textContent).not.toContain('·');
    expect(root().querySelectorAll('.footer__trai > span').length).toBe(1);
  });

  it('không có bản quyền, chỉ có phiên bản: không có dấu · thừa', () => {
    fixture.componentRef.setInput('version', 'Phiên bản ac807bd');
    fixture.detectChanges();

    expect(root().textContent).not.toContain('·');
  });
});

describe('FooterComponent — biến thể sidebar', () => {
  it('hai dòng (bản quyền, phiên bản), không dấu ·', async () => {
    await TestBed.configureTestingModule({
      imports: [FooterComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    const fixture = TestBed.createComponent(FooterComponent);
    fixture.componentRef.setInput('variant', 'sidebar');
    fixture.componentRef.setInput('size', 'sm');
    fixture.componentRef.setInput('copyright', '© 2026 Core');
    fixture.componentRef.setInput('version', 'Phiên bản ac807bd');
    fixture.detectChanges();

    const goc = fixture.nativeElement as HTMLElement;
    const dsSpan = Array.from(goc.querySelectorAll('.footer__trai > span'));
    expect(dsSpan.map((s) => s.textContent)).toEqual(['© 2026 Core', 'Phiên bản ac807bd']);
    expect(goc.querySelector('footer')?.classList.contains('footer--sidebar')).toBeTrue();
  });
});
