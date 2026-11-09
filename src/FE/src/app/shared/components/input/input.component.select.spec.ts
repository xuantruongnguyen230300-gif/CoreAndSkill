import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { InputComponent, SegmentOption } from './input.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const LUA_CHON: readonly SegmentOption[] = [
  { value: '', label: 'Tất cả' },
  { value: 'active', label: 'Đang hoạt động' },
  { value: 'locked', label: 'Đã khoá' },
];

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, InputComponent],
  template: `<app-input type="select" [options]="luaChon" [formControl]="control" />`,
})
class HostComponent {
  readonly luaChon = LUA_CHON;
  readonly control = new FormControl('active');
}

describe('InputComponent — biến thể select, giá trị có sẵn từ lúc dựng', () => {
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

  const oChon = (): HTMLSelectElement =>
    (fixture.nativeElement as HTMLElement).querySelector('select')!;

  it('FormControl đã mang giá trị trước khi ô dựng → ô chọn hiện đúng mục đó', () => {
    expect(oChon().value).toBe('active');
  });

  it('FormControl.setValue() sau đó → ô chọn đổi theo', () => {
    host.control.setValue('locked');
    fixture.detectChanges();

    expect(oChon().value).toBe('locked');
  });

  it('chọn mục khác → giá trị đi vào FormControl', () => {
    const o = oChon();
    o.value = 'locked';
    o.dispatchEvent(new Event('change'));

    expect(host.control.value).toBe('locked');
  });
});
