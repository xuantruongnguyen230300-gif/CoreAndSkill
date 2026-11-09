import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
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
  template: `
    <app-input class="khong-id" type="select" [formControl]="a" />
    <app-input class="co-nhan" type="select" ariaLabel="Trạng thái" [formControl]="b" />
    <app-input class="mat-khau" type="password" [formControl]="c" />
    <app-input class="mat-khau-khong-nut" type="password" [revealable]="false" [formControl]="d" />
  `,
})
class HostComponent {
  readonly a = new FormControl('');
  readonly b = new FormControl('');
  readonly c = new FormControl('');
  readonly d = new FormControl('');
}

describe('InputComponent — truy cập và đệm nút hiện/ẩn', () => {
  let el: HTMLElement;

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
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    el = fixture.nativeElement as HTMLElement;
  });

  it('không truyền id → KHÔNG sinh thuộc tính id (không có id="null")', () => {
    const sel = el.querySelector('.khong-id select') as HTMLSelectElement;
    expect(sel.hasAttribute('id')).toBeFalse();
  });

  it('truyền ariaLabel → ô select mang aria-label', () => {
    const sel = el.querySelector('.co-nhan select') as HTMLSelectElement;
    expect(sel.getAttribute('aria-label')).toBe('Trạng thái');
    expect(
      (el.querySelector('.khong-id select') as HTMLElement).hasAttribute('aria-label'),
    ).toBeFalse();
  });

  it('ô mật khẩu có nút hiện/ẩn → đệm phải chừa chỗ nút; không có nút thì không chừa', () => {
    const co = el.querySelector('.mat-khau input') as HTMLInputElement;
    const khong = el.querySelector('.mat-khau-khong-nut input') as HTMLInputElement;
    expect(co.classList).toContain('input--co-nut');
    expect(khong.classList).not.toContain('input--co-nut');
    const nut = el.querySelector('.mat-khau .nut-hien-an') as HTMLElement;
    // Vùng chữ phải kết thúc trước mép trái của nút.
    const content =
      co.getBoundingClientRect().right - parseFloat(getComputedStyle(co).paddingRight);
    expect(content).toBeLessThanOrEqual(nut.getBoundingClientRect().left);
  });
});
