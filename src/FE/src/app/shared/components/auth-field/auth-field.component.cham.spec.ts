import { Component, provideZonelessChangeDetection, viewChild } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
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

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, AuthFieldComponent],
  template: `<app-auth-field
    #o
    [formControl]="control"
    label="Nhãn"
    autocomplete="username"
    variant="identifier"
  />`,
})
class HostComponent {
  readonly control = new FormControl('', Validators.required);
  readonly o = viewChild.required<AuthFieldComponent>('o');
}

describe('AuthFieldComponent — touched và cờ ô tự nhận focus (§6.5)', () => {
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
  const roi = (): void => {
    o().dispatchEvent(new Event('blur'));
  };

  it('focus() ban đầu gắn cờ; rời ô khi chưa gõ → không touched', () => {
    host.o().focus();
    expect(o().hasAttribute('data-focus-luc-mo')).toBeTrue();
    roi();
    expect(host.control.touched).toBeFalse();
  });

  it('focus() ban đầu rồi gõ rồi rời ô → touched', () => {
    host.o().focus();
    o().value = 'a';
    o().dispatchEvent(new Event('input'));
    roi();
    expect(host.control.touched).toBeTrue();
  });

  it('focusLai() đặt focus nhưng KHÔNG gắn cờ → rời ô là touched', () => {
    host.o().focusLai();
    expect(document.activeElement).toBe(o());
    expect(o().hasAttribute('data-focus-luc-mo')).toBeFalse();
    roi();
    expect(host.control.touched).toBeTrue();
  });

  it('ô thường rời ô khi chưa gõ → touched', () => {
    roi();
    expect(host.control.touched).toBeTrue();
  });
});
