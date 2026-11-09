import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { IconButtonComponent } from './icon-button.component';

/** IconButton.md §API: `haspopup`/`expanded` sinh `aria-haspopup`/`aria-expanded`; `pressed` mới sinh `aria-pressed`. */
describe('IconButtonComponent — aria của nút mở lớp nổi', () => {
  let fixture: ComponentFixture<IconButtonComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [IconButtonComponent],
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(IconButtonComponent);
    fixture.componentRef.setInput('icon', 'sun');
    fixture.componentRef.setInput('ariaLabel', 'Giao diện');
  });

  const nut = (): HTMLButtonElement =>
    (fixture.nativeElement as HTMLElement).querySelector('button')!;

  async function capNhat(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  it('mặc định: không aria-haspopup, aria-expanded, aria-pressed', async () => {
    await capNhat();
    expect(nut().hasAttribute('aria-haspopup')).toBeFalse();
    expect(nut().hasAttribute('aria-expanded')).toBeFalse();
    expect(nut().hasAttribute('aria-pressed')).toBeFalse();
  });

  it('haspopup="menu" sinh aria-haspopup="menu"; expanded null thì không sinh aria-expanded', async () => {
    fixture.componentRef.setInput('haspopup', 'menu');
    await capNhat();
    expect(nut().getAttribute('aria-haspopup')).toBe('menu');
    expect(nut().hasAttribute('aria-expanded')).toBeFalse();
    expect(nut().hasAttribute('aria-pressed')).toBeFalse();
  });

  it('expanded true/false sinh aria-expanded đúng chuỗi, vẫn không có aria-pressed', async () => {
    fixture.componentRef.setInput('haspopup', 'menu');
    fixture.componentRef.setInput('expanded', true);
    await capNhat();
    expect(nut().getAttribute('aria-expanded')).toBe('true');
    fixture.componentRef.setInput('expanded', false);
    await capNhat();
    expect(nut().getAttribute('aria-expanded')).toBe('false');
    expect(nut().hasAttribute('aria-pressed')).toBeFalse();
  });

  it('haspopup trở về null thì aria-haspopup biến mất', async () => {
    fixture.componentRef.setInput('haspopup', 'menu');
    await capNhat();
    fixture.componentRef.setInput('haspopup', null);
    await capNhat();
    expect(nut().hasAttribute('aria-haspopup')).toBeFalse();
  });
});
