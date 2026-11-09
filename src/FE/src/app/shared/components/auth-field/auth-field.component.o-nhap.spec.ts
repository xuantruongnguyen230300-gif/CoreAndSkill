import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthFieldComponent } from './auth-field.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/** Chuyển một token màu về chuỗi `rgb(...)` mà `getComputedStyle` trả — để so với giá trị đã tính. */
function mauCua(token: string): string {
  const mau = document.createElement('span');
  mau.style.color = `var(${token})`;
  document.body.appendChild(mau);
  const ra = getComputedStyle(mau).color;
  mau.remove();
  return ra;
}

/**
 * DESIGN.md §2.6 *Ô nhập chữ*: một đường biên theo hai trục (hợp lệ/lỗi × focus) — không vòng
 * ngoài, không `--color-focus`; phần dày thêm vẽ vào trong nên hộp ô không đổi. Khe nhãn → ô và
 * ô → dòng phụ theo DESIGN.md §4. Đo trên Chrome thật (Karma), không suy từ SCSS.
 */
describe('AuthFieldComponent — placeholder, khe, đường biên focus', () => {
  let fixture: ComponentFixture<AuthFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuthFieldComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AuthFieldComponent);
    fixture.componentRef.setInput('label', 'Nhãn');
    fixture.componentRef.setInput('autocomplete', 'username');
    fixture.componentRef.setInput('variant', 'identifier');
    fixture.detectChanges();
    o().style.transition = 'none'; // đo giá trị cuối, không đo giữa chuyển tiếp màu
    icon().style.transition = 'none';
  });

  const goc = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const o = (): HTMLInputElement => goc().querySelector('input')!;
  const icon = (): HTMLElement => goc().querySelector('.auth-field__icon')!;

  function batLoi(): void {
    fixture.componentRef.setInput('errorMessage', 'Sai rồi');
    fixture.detectChanges();
  }

  function focus(): void {
    o().focus();
    fixture.detectChanges();
  }

  it('placeholder = null → <input> KHÔNG mang thuộc tính placeholder (không hiện chữ "null")', () => {
    expect(o().hasAttribute('placeholder')).toBeFalse();
  });

  it('placeholder có chuỗi → thuộc tính placeholder đúng chuỗi đó', () => {
    fixture.componentRef.setInput('placeholder', 'vd: an.nv');
    fixture.detectChanges();
    expect(o().getAttribute('placeholder')).toBe('vd: an.nv');
  });

  it('khe nhãn → ô là --sp-4 (8px), ô → dòng lỗi là --sp-3 (6px)', () => {
    expect(getComputedStyle(document.documentElement).fontSize).withContext('rem gốc').toBe('16px');
    batLoi();
    const nhan = goc().querySelector('.auth-field__nhan')!.getBoundingClientRect();
    const khung = goc().querySelector('.auth-field__khung')!.getBoundingClientRect();
    const loi = goc().querySelector('.auth-field__loi')!.getBoundingClientRect();
    expect(khung.top - nhan.bottom).toBeCloseTo(8, 1);
    expect(loi.top - khung.bottom).toBeCloseTo(6, 1);
  });

  it('khe ô → dòng gợi ý cũng là --sp-3 (6px)', () => {
    fixture.componentRef.setInput('hint', 'Gợi ý');
    fixture.detectChanges();
    const khung = goc().querySelector('.auth-field__khung')!.getBoundingClientRect();
    const goiY = goc().querySelector('.auth-field__goi-y')!.getBoundingClientRect();
    expect(goiY.top - khung.bottom).toBeCloseTo(6, 1);
  });

  it('ô hợp lệ có focus: không outline ngoài, viền thật 1px + bóng trong 1px = 2px màu --color-text', () => {
    focus();
    expect(document.activeElement).toBe(o());
    const cs = getComputedStyle(o());
    expect(cs.outlineStyle).toBe('none');
    expect(cs.borderTopWidth).toBe('1px');
    expect(cs.borderTopColor).toBe(mauCua('--color-text'));
    expect(cs.boxShadow).toBe(`${mauCua('--color-text')} 0px 0px 0px 1px inset`);
  });

  it('ô lỗi có focus: dày 3px, VẪN màu --color-danger-border, không outline ngoài', () => {
    batLoi();
    focus();
    const cs = getComputedStyle(o());
    expect(cs.outlineStyle).toBe('none');
    expect(cs.borderTopColor).toBe(mauCua('--color-danger-border'));
    expect(cs.boxShadow).toBe(`${mauCua('--color-danger-border')} 0px 0px 0px 2px inset`);
  });

  it('ô lỗi không focus: 2px màu --color-danger-border', () => {
    batLoi();
    const cs = getComputedStyle(o());
    expect(cs.borderTopColor).toBe(mauCua('--color-danger-border'));
    expect(cs.boxShadow).toBe(`${mauCua('--color-danger-border')} 0px 0px 0px 1px inset`);
  });

  it('kích thước hộp ô bằng nhau ở bốn trạng thái (nghỉ, focus, lỗi, lỗi + focus)', () => {
    const cap = (): [number, number] => {
      const r = o().getBoundingClientRect();
      return [r.width, r.height];
    };
    const nghi = cap();
    focus();
    const focusHopLe = cap();
    o().blur();
    batLoi();
    const loiNghi = cap();
    focus();
    const loiFocus = cap();
    expect([focusHopLe, loiNghi, loiFocus]).toEqual([nghi, nghi, nghi]);
  });

  it('icon dẫn: --color-text-muted khi nghỉ, chuyển --color-text khi ô focus', () => {
    expect(getComputedStyle(icon()).color).toBe(mauCua('--color-text-muted'));
    focus();
    expect(getComputedStyle(icon()).color).toBe(mauCua('--color-text'));
  });

  it('nút hiện/ẩn mật khẩu GIỮ vòng --color-focus có khoảng hở (không bị trung hoà như ô nhập)', () => {
    fixture.componentRef.setInput('variant', 'password');
    fixture.detectChanges();
    const nut = goc().querySelector<HTMLElement>('.auth-field__nut-hien-an')!;
    nut.focus();
    const cs = getComputedStyle(nut);
    expect(cs.outlineStyle).toBe('solid');
    expect(cs.outlineColor).toBe(mauCua('--color-focus'));
    expect(cs.outlineOffset).toBe('2px');
  });
});
