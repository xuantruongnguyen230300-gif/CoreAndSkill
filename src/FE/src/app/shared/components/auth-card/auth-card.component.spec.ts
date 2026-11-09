import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthCardComponent } from './auth-card.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({ chung: { thongBao: { loi: 'Lỗi' } } });
  }
}

/**
 * Style của khung bọc trong khối `app-auth-card { … }` (fe-ui-conventions.md §4.2 luật 4), nên phép
 * kiểm phải dựng khung qua CHÍNH phần tử `<app-auth-card>` — host mặc định của TestBed là `<div>`.
 */
@Component({
  standalone: true,
  imports: [AuthCardComponent],
  template: `<app-auth-card
    title="Đăng nhập"
    description="Mô tả"
    brandName="Sản phẩm X"
    [errorMessage]="loi()"
  />`,
})
class HostComponent {
  readonly loi = signal<string | null>(null);
}

/**
 * quy-uoc/fe-ui-conventions.md §6.2: câu của `applyFormFailure` mang mỗi mã một dòng, nối bằng
 * `\n`; màn xác thực đưa câu đó vào khu lỗi của `AuthCard` qua `errorMessage`. Khu lỗi phải giữ
 * ngắt dòng — không thì hai lý do dính thành một câu.
 */
describe('AuthCardComponent — khu lỗi', () => {
  let fixture: ComponentFixture<HostComponent>;

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
  });

  function khuLoi(): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('.auth-card__khu-loi');
  }

  it('errorMessage = null → không có khu lỗi', () => {
    fixture.detectChanges();
    expect(khuLoi()).toBeNull();
  });

  it('câu nhiều dòng → khu lỗi role=alert giữ ngắt dòng (white-space: pre-line)', () => {
    fixture.componentInstance.loi.set('Lý do một\nLý do hai');
    fixture.detectChanges();

    const khu = khuLoi();
    expect(khu?.querySelector('[role="alert"]')).not.toBeNull();
    const cau = khu?.querySelector('.notice-banner__than');
    expect(cau?.textContent).toBe('Lý do một\nLý do hai');
    expect(getComputedStyle(cau as Element).whiteSpace)
      .withContext('phần tử mang chữ của NoticeBanner giữ ngắt dòng')
      .toBe('pre-line');
  });

  it('khu lỗi là NoticeBanner vai danger có tiêu đề tiền tố "Lỗi", đứng TRƯỚC form trong DOM', () => {
    fixture.componentInstance.loi.set('Sai');
    fixture.detectChanges();
    const khu = khuLoi() as HTMLElement;
    expect(khu.tagName.toLowerCase()).toBe('app-notice-banner');
    expect(khu.querySelector('.notice-banner--danger')).not.toBeNull();
    expect(khu.querySelector('.notice-banner__tieu-de')?.textContent).toBe('Lỗi');
    const goc = fixture.nativeElement as HTMLElement;
    expect(khu.compareDocumentPosition(goc.querySelector('h1') as Node)).toBe(
      Node.DOCUMENT_POSITION_PRECEDING,
    );
  });

  it('vùng thương hiệu: tên từ CORE_BRANDING đứng TRƯỚC <h1>', () => {
    fixture.detectChanges();
    const goc = fixture.nativeElement as HTMLElement;
    const hieu = goc.querySelector('.auth-card__thuong-hieu') as HTMLElement;
    expect(hieu.textContent?.trim()).toBe('Sản phẩm X');
    expect(hieu.compareDocumentPosition(goc.querySelector('h1') as Node)).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
  });

  it('aria-busy KHÔNG nằm trên khung — nó thuộc <form> của trang', () => {
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('[aria-busy]')).toBeNull();
  });

  it('khe tiêu đề → mô tả = --sp-3 (6px), không phải 18px', () => {
    fixture.detectChanges();
    const goc = fixture.nativeElement as HTMLElement;
    const khe =
      (goc.querySelector('.auth-card__mo-ta') as HTMLElement).getBoundingClientRect().top -
      (goc.querySelector('h1') as HTMLElement).getBoundingClientRect().bottom;
    expect(khe).toBeCloseTo(6, 1);
  });
});
