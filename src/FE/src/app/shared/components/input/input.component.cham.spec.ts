import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { datFocusLucMo } from '../../forms/focus-luc-mo';
import { InputComponent } from './input.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, InputComponent],
  template: `<app-input [formControl]="control" type="text" />`,
})
class HostComponent {
  readonly control = new FormControl('', [Validators.required, Validators.pattern(/^\d+$/)]);
}

/**
 * FormRow.md, bảng *Lỗi hiện khi nào*; fe-ui-conventions.md §6.5. `touched` chỉ đặt khi rời ô, và
 * ô tự đặt focus lúc mở mà chưa sửa thì rời ô cũng không làm `touched`.
 */
describe('InputComponent — khi nào control thành touched', () => {
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

  const o = (): HTMLInputElement => (fixture.nativeElement as HTMLElement).querySelector('input')!;
  const go = (gt: string): void => {
    o().value = gt;
    o().dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };
  const roi = (): void => {
    o().dispatchEvent(new Event('blur'));
    fixture.detectChanges();
  };

  it('ô tự nhận focus lúc mở, rời ô khi chưa gõ → không touched, không aria-invalid', () => {
    datFocusLucMo(o());
    roi();
    expect(host.control.touched).toBeFalse();
    expect(o().hasAttribute('aria-invalid')).toBeFalse();
  });

  it('ô tự nhận focus lúc mở, gõ rồi xoá hết rồi rời ô → touched, báo lỗi', () => {
    datFocusLucMo(o());
    go('1');
    go('');
    roi();
    expect(host.control.touched).toBeTrue();
    expect(o().getAttribute('aria-invalid')).toBe('true');
  });

  it('ô tự nhận focus lúc mở, bấm gửi (markAllAsTouched) → touched', () => {
    datFocusLucMo(o());
    host.control.markAllAsTouched();
    expect(host.control.touched).toBeTrue();
  });

  it('đang gõ lần đầu (sai định dạng), chưa rời ô → chưa touched, chưa báo lỗi', () => {
    go('abc');
    expect(host.control.touched).toBeFalse();
    expect(o().hasAttribute('aria-invalid')).toBeFalse();
  });

  it('gõ sai định dạng rồi rời ô → touched, báo lỗi', () => {
    go('abc');
    roi();
    expect(host.control.touched).toBeTrue();
    expect(o().getAttribute('aria-invalid')).toBe('true');
  });

  it('ô thường (không cờ) rời ô khi chưa gõ → touched, báo "bắt buộc"', () => {
    roi();
    expect(host.control.touched).toBeTrue();
    expect(o().getAttribute('aria-invalid')).toBe('true');
  });

  it('ô đã báo lỗi vẫn kiểm lại theo từng ký tự: sửa thành hợp lệ → hết lỗi ngay', () => {
    go('abc');
    roi();
    expect(o().getAttribute('aria-invalid')).toBe('true');
    go('123');
    expect(o().hasAttribute('aria-invalid')).toBeFalse();
  });
});
