import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslateLoader, TranslationObject, provideTranslateLoader, provideTranslateService } from '@ngx-translate/core';
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
  template: `<app-input [formControl]="control" type="text" />`,
})
class HostComponent {
  readonly control = new FormControl('');
}

describe('InputComponent — ControlValueAccessor', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({ lang: 'vi', fallbackLang: 'vi', loader: provideTranslateLoader(FakeTranslateLoader) }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('gõ vào ô → giá trị đi vào FormControl', () => {
    const input = (fixture.nativeElement as HTMLElement).querySelector('input')!;
    input.value = 'an.nv';
    input.dispatchEvent(new Event('input'));

    expect(host.control.value).toBe('an.nv');
  });

  it('FormControl.setValue() → ô nhập hiện đúng giá trị', () => {
    host.control.setValue('gia-tri-moi');
    fixture.detectChanges();

    const input = (fixture.nativeElement as HTMLElement).querySelector('input')!;
    expect(input.value).toBe('gia-tri-moi');
  });

  it('FormControl.disable() → ô nhập bị disabled thật, không chỉ đổi màu', () => {
    host.control.disable();
    fixture.detectChanges();

    const input = (fixture.nativeElement as HTMLElement).querySelector('input')!;
    expect(input.disabled).toBeTrue();
  });
});
