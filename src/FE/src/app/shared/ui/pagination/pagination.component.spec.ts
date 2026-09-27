import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import {
  TranslateLoader,
  TranslateService,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, firstValueFrom, from } from 'rxjs';

import { PaginationComponent } from './pagination.component';

/**
 * Nạp ĐÚNG `public/i18n/vi.json` mà app dùng — không bản dịch giả. Khoá thiếu trong tệp thật thì
 * người dùng thấy chuỗi khoá thô, và chỉ một bộ nạp đọc tệp thật mới thấy điều đó.
 */
class ViJsonThatLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return from(fetch('/i18n/vi.json').then((r) => r.json() as Promise<TranslationObject>));
  }
}

@Component({
  standalone: true,
  imports: [PaginationComponent],
  template: `
    <app-pagination
      [page]="page()"
      [pageSize]="10"
      [pageSizeOptions]="[10, 20]"
      [totalRecords]="totalRecords()"
      ariaLabel="Phân trang thử"
    />
  `,
})
class HostComponent {
  readonly page = signal(1);
  readonly totalRecords = signal(25);
}

describe('PaginationComponent — chỉ báo "x–y trên tổng z" (Design/Components/Pagination.md)', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(ViJsonThatLoader),
        }),
      ],
    });
  });

  async function khoang(page: number, tong: number): Promise<string> {
    // `whenStable()` không chờ `fetch` của bộ nạp — chờ bản dịch về trước rồi mới dựng component.
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.page.set(page);
    fixture.componentInstance.totalRecords.set(tong);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const el = (fixture.nativeElement as HTMLElement).querySelector('.pagination__khoang');
    return el?.textContent?.trim() ?? '';
  }

  it('trang 1 của 25 bản ghi → "1–10 trên tổng 25", không phải chuỗi khoá thô', async () => {
    const chu = await khoang(1, 25);
    expect(chu).not.toContain('chung.phanTrang');
    expect(chu).toBe('1–10 trên tổng 25');
  });

  it('trang cuối không đủ trang → "21–25 trên tổng 25"', async () => {
    expect(await khoang(3, 25)).toBe('21–25 trên tổng 25');
  });
});
