import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthFieldComponent } from '../auth-field/auth-field.component';
import { ButtonComponent } from '../button/button.component';
import { AuthCardComponent } from './auth-card.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/**
 * Trang xác thực KHÔNG khai stylesheet nào — mô phỏng đúng cách hai trang thật dùng `AuthCard`:
 * một `<form>` trần với các `app-auth-field` và một `app-button` ở cuối.
 */
@Component({
  standalone: true,
  imports: [ReactiveFormsModule, AuthCardComponent, AuthFieldComponent, ButtonComponent],
  template: `
    <app-auth-card title="Đăng nhập">
      <form [formGroup]="form">
        <app-auth-field
          variant="identifier"
          controlId="khe-1"
          label="Một"
          autocomplete="username"
          [formControl]="form.controls.mot"
          [errorMessage]="loi1()"
        />
        <app-auth-field
          variant="password"
          controlId="khe-2"
          label="Hai"
          autocomplete="current-password"
          [formControl]="form.controls.hai"
          [errorMessage]="loi2()"
          [hint]="goiY2()"
        />
        <app-button variant="primary" size="lg" [block]="true" type="submit">Gửi</app-button>
      </form>
    </app-auth-card>
  `,
})
class HostComponent {
  readonly form = new FormGroup({ mot: new FormControl(''), hai: new FormControl('') });
  readonly loi1 = signal<string | null>(null);
  readonly loi2 = signal<string | null>(null);
  readonly goiY2 = signal<string | null>(null);
}

/**
 * AuthCard.md §Kích thước: khe giữa hai trường `--sp-6` (16px), trường cuối → nút chính `--sp-7`
 * (20px), đo từ phần tử cuối của trường (dòng lỗi/gợi ý nếu đang hiện). Khe là của khung — host
 * trên không tự khai gì. Đo bằng `getBoundingClientRect` trên Chrome thật, cỡ chữ gốc 16px.
 */
describe('AuthCardComponent — khe trong form là của khung', () => {
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
  });

  const goc = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const rect = (el: Element): DOMRect => el.getBoundingClientRect();
  const truong = (): HTMLElement[] => Array.from(goc().querySelectorAll('app-auth-field'));
  const nut = (): HTMLElement => goc().querySelector('app-button')!;

  it('canary: cỡ chữ gốc 16px, nếu không các số 16/20 dưới đây không có nghĩa', () => {
    expect(getComputedStyle(document.documentElement).fontSize).toBe('16px');
  });

  it('không có dòng lỗi: ô trường trên → nhãn trường dưới = 16px; ô trường cuối → nút = 20px', () => {
    const [t1, t2] = truong();
    expect(
      rect(t2.querySelector('.auth-field__nhan')!).top -
        rect(t1.querySelector('.auth-field__khung')!).bottom,
    ).toBeCloseTo(16, 1);
    expect(rect(nut()).top - rect(t2.querySelector('.auth-field__khung')!).bottom).toBeCloseTo(
      20,
      1,
    );
  });

  it('trường trên đang lỗi: đo từ mép dưới DÒNG LỖI tới nhãn trường dưới = 16px', () => {
    host.loi1.set('Sai rồi');
    fixture.detectChanges();
    const [t1, t2] = truong();
    const loi = t1.querySelector('.auth-field__loi')!;
    expect(rect(t2.querySelector('.auth-field__nhan')!).top - rect(loi).bottom).toBeCloseTo(16, 1);
  });

  it('trường cuối đang lỗi hoặc có gợi ý: đo từ mép dưới dòng phụ tới nút = 20px', () => {
    host.goiY2.set('Gợi ý chính sách');
    fixture.detectChanges();
    const t2 = truong()[1];
    expect(rect(nut()).top - rect(t2.querySelector('.auth-field__goi-y')!).bottom).toBeCloseTo(
      20,
      1,
    );

    host.loi2.set('Sai rồi');
    fixture.detectChanges();
    expect(rect(nut()).top - rect(t2.querySelector('.auth-field__loi')!).bottom).toBeCloseTo(20, 1);
  });
});
