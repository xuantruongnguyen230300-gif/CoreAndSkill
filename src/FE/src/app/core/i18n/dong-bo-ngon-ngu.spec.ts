import localeVi from '@angular/common/locales/vi';
import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import {
  TranslateLoader,
  TranslateService,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { PrimeNG, providePrimeNG } from 'primeng/config';
import { PaginatorModule } from 'primeng/paginator';
import { Observable, firstValueFrom, from, of } from 'rxjs';

import { provideCoreI18n } from '../config/core-i18n';
import { provideDongBoNgonNgu } from './dong-bo-ngon-ngu';

/** Hai ngôn ngữ giả — đủ để thấy thứ gì đổi theo ngôn ngữ và thứ gì bị bỏ quên. */
const HAI_NGON_NGU: Readonly<Record<string, TranslationObject>> = {
  vi: {
    thuVienUi: {
      emptySearchMessage: 'Không tìm thấy kết quả',
      aria: { firstPageLabel: 'Trang đầu', pageLabel: 'Trang {page}' },
    },
  },
  xx: {
    thuVienUi: {
      emptySearchMessage: 'XX-khong-co',
      aria: { firstPageLabel: 'XX-dau', pageLabel: 'XX {page}' },
    },
  },
};

class HaiNgonNguLoader extends TranslateLoader {
  getTranslation(lang: string): Observable<TranslationObject> {
    return of(HAI_NGON_NGU[lang] ?? {});
  }
}

function cauHinh(ngonNguMacDinh: string, loader: new () => TranslateLoader): void {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideNoopAnimations(),
      providePrimeNG(),
      provideCoreI18n({
        languages: [
          { code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi },
          { code: 'xx', nativeName: 'XX', localeData: localeVi },
        ],
        defaultLanguage: ngonNguMacDinh,
        sources: ['/i18n/'],
      }),
      // KHÔNG khai `lang`/`fallbackLang` ở đây: ngôn ngữ phải đến từ seam CORE_I18N (luật F13 của
      // lượt review — không viết cứng 'vi' ở hai nơi).
      provideTranslateService({ loader: provideTranslateLoader(loader) }),
      provideDongBoNgonNgu(),
    ],
  });
}

describe('provideDongBoNgonNgu — ngôn ngữ đến từ CORE_I18N (wiki-core/fe/08-i18n.md §7, §8)', () => {
  beforeEach(() => {
    document.documentElement.lang = 'en';
  });

  it('ngôn ngữ đang dùng và ngôn ngữ dự phòng lấy từ CORE_I18N.defaultLanguage, không viết cứng', () => {
    cauHinh('xx', HaiNgonNguLoader);
    const translate = TestBed.inject(TranslateService);
    expect(translate.getCurrentLang()).toBe('xx');
    expect(translate.getFallbackLang()).toBe('xx');
  });

  it('<html lang> theo ngôn ngữ đang dùng, và đổi theo khi đổi ngôn ngữ', async () => {
    cauHinh('vi', HaiNgonNguLoader);
    const translate = TestBed.inject(TranslateService);
    await firstValueFrom(translate.use('vi'));
    expect(document.documentElement.lang).toBe('vi');

    await firstValueFrom(translate.use('xx'));
    expect(document.documentElement.lang).toBe('xx');
  });

  it('bộ chuỗi PrimeNG nạp từ miền thuVienUi, gộp sâu vào aria (không xoá nhãn aria khác), và nạp lại khi đổi ngôn ngữ', async () => {
    cauHinh('vi', HaiNgonNguLoader);
    const translate = TestBed.inject(TranslateService);
    const primeng = TestBed.inject(PrimeNG);
    const nhanSaoMacDinh = primeng.translation.aria?.star;

    await firstValueFrom(translate.use('vi'));
    expect(primeng.translation.emptySearchMessage).toBe('Không tìm thấy kết quả');
    expect(primeng.translation.aria?.firstPageLabel).toBe('Trang đầu');
    expect(primeng.translation.aria?.star)
      .withContext('setTranslation của PrimeNG gộp NÔNG — aria phải gộp tay')
      .toBe(nhanSaoMacDinh);

    await firstValueFrom(translate.use('xx'));
    expect(primeng.translation.emptySearchMessage).toBe('XX-khong-co');
    expect(primeng.translation.aria?.firstPageLabel).toBe('XX-dau');
  });
});

/** Nạp ĐÚNG `public/i18n/vi.json` — câu chữ thật của app, không bản dịch giả. */
class ViJsonThatLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return from(fetch('/i18n/vi.json').then((r) => r.json() as Promise<TranslationObject>));
  }
}

@Component({
  standalone: true,
  imports: [PaginatorModule],
  template: `<p-paginator
    [first]="0"
    [rows]="10"
    [totalRecords]="45"
    [rowsPerPageOptions]="[10, 20]"
  />`,
})
class PaginatorHost {}

describe('provideDongBoNgonNgu — chuỗi PrimeNG thật trong vi.json', () => {
  beforeEach(() => cauHinh('vi', ViJsonThatLoader));

  it('không còn chuỗi tiếng Anh mặc định của PrimeNG ở những chỗ component đang dùng đọc', async () => {
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const t = TestBed.inject(PrimeNG).translation;
    // Autocomplete (danh sách rỗng, vùng thông báo cho trình đọc màn hình), Select (ô số dòng).
    expect(t.emptySearchMessage).not.toBe('No results found');
    expect(t.emptyMessage).not.toBe('No results found');
    expect(t.searchMessage).not.toBe('Search results are available');
    expect(t.selectionMessage).not.toBe('{0} items selected');
    expect(t.emptySelectionMessage).not.toBe('No selected item');
    expect(t.aria?.close).not.toBe('Close');
    expect(t.aria?.listLabel).not.toBe('Option List');
    expect(t.aria?.removeLabel).not.toBe('Remove');
  });

  it('nhãn ARIA của Paginator theo Design/Components/Pagination.md §Accessibility', async () => {
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const fixture = TestBed.createComponent(PaginatorHost);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const nhan = (lop: string): string | null =>
      el.querySelector(`.${lop}`)?.getAttribute('aria-label') ?? null;
    expect(nhan('p-paginator-first')).toBe('Trang đầu');
    expect(nhan('p-paginator-prev')).toBe('Trang trước');
    expect(nhan('p-paginator-next')).toBe('Trang sau');
    expect(nhan('p-paginator-last')).toBe('Trang cuối');
    const trang = Array.from(el.querySelectorAll('.p-paginator-page')).map((b) =>
      b.getAttribute('aria-label'),
    );
    expect(trang).toContain('Trang 3');
  });
});
