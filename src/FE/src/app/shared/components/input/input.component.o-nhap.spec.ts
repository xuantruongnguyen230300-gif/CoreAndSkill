import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { InputComponent } from './input.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, InputComponent],
  template: `<app-input [formControl]="control" type="text" [placeholder]="placeholder()" />`,
})
class HostComponent {
  readonly control = new FormControl('', Validators.required);
  readonly placeholder = signal<string | null>(null);
}

function mauCua(token: string): string {
  const mau = document.createElement('span');
  mau.style.color = `var(${token})`;
  document.body.appendChild(mau);
  const ra = getComputedStyle(mau).color;
  mau.remove();
  return ra;
}

/**
 * DESIGN.md §2.6 *Ô nhập chữ*: một đường biên theo hai trục (hợp lệ/lỗi × focus), hộp ô không đổi;
 * placeholder `null` không được thành chuỗi "null" trong ô. Đo trên Chrome thật.
 */
describe('InputComponent — placeholder và đường biên focus', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    o().style.transition = 'none'; // đo giá trị cuối, không đo giữa chuyển tiếp màu
  });

  const o = (): HTMLInputElement => (fixture.nativeElement as HTMLElement).querySelector('input')!;

  function batLoi(): void {
    // Rời ô → `touched` (qua (blur) của ô, đúng đường thật; OnPush không tự thấy markAsTouched).
    o().focus();
    o().blur();
    fixture.detectChanges();
  }

  it('placeholder = null → <input> không mang thuộc tính placeholder', () => {
    expect(o().hasAttribute('placeholder')).toBeFalse();
  });

  it('placeholder có chuỗi → thuộc tính đúng chuỗi đó', () => {
    host.placeholder.set('Tìm…');
    fixture.detectChanges();
    expect(o().getAttribute('placeholder')).toBe('Tìm…');
  });

  it('ô hợp lệ có focus: không outline ngoài, 2px màu --color-text', () => {
    host.control.setValue('a');
    fixture.detectChanges();
    o().focus();
    const cs = getComputedStyle(o());
    expect(cs.outlineStyle).toBe('none');
    expect(cs.borderTopColor).toBe(mauCua('--color-text'));
    expect(cs.boxShadow).toBe(`${mauCua('--color-text')} 0px 0px 0px 1px inset`);
  });

  it('ô lỗi có focus: 3px, vẫn --color-danger-border', () => {
    batLoi();
    o().focus();
    const cs = getComputedStyle(o());
    expect(cs.outlineStyle).toBe('none');
    expect(cs.borderTopColor).toBe(mauCua('--color-danger-border'));
    expect(cs.boxShadow).toBe(`${mauCua('--color-danger-border')} 0px 0px 0px 2px inset`);
  });

  it('kích thước hộp ô bằng nhau ở bốn trạng thái', () => {
    host.control.setValue('a');
    fixture.detectChanges();
    const cap = (): [number, number] => {
      const r = o().getBoundingClientRect();
      return [r.width, r.height];
    };
    const nghi = cap();
    o().focus();
    const a = cap();
    o().blur();
    host.control.setValue('');
    batLoi();
    const b = cap();
    o().focus();
    const c = cap();
    expect([a, b, c]).toEqual([nghi, nghi, nghi]);
  });

  it('không có quy tắc :hover nào đổi viền của ô (hover không được đè focus hay màu lỗi)', () => {
    // Karma không giả lập được hover; canh bằng cách quét stylesheet đã nạp. Đo bằng
    // CSS.forcePseudoState trên Chrome thật nằm ở báo cáo bàn giao, không ở test này.
    const quy: string[] = [];
    const duyet = (rules: CSSRuleList): void => {
      for (const r of Array.from(rules)) {
        if (
          r instanceof CSSStyleRule &&
          /\.input\b/.test(r.selectorText) &&
          r.selectorText.includes(':hover')
        ) {
          quy.push(r.selectorText);
        } else if ('cssRules' in r) {
          duyet((r as CSSGroupingRule).cssRules);
        }
      }
    };
    for (const sheet of Array.from(document.styleSheets)) {
      try {
        duyet(sheet.cssRules);
      } catch {
        /* stylesheet khác nguồn */
      }
    }
    expect(quy).toEqual([]);
  });
});
