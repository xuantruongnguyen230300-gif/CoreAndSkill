import { Type, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { CORE_ROUTES } from '../../core/config/core-routes';
import { KhongCoQuyenPage } from './khong-co-quyen.page';
import { KhongTimThayPage } from './khong-tim-thay.page';

/** Câu giả mang tên khoá — đủ để khẳng định khoá nào nằm ở phần tử nào, không phụ thuộc câu chữ. */
class KhoaLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    const man = (ma: string) => ({
      tieuDe: `${ma}-tieuDe`,
      moTa: `${ma}-moTa`,
      diTiep: `${ma}-diTiep`,
    });
    return of({ trangLoi: { khongCoQuyen: man('403'), khongTimThay: man('404') } });
  }
}

interface Ca {
  readonly ten: string;
  readonly page: Type<unknown>;
  readonly ma: string;
  readonly icon: string;
}

const CAC_MAN: readonly Ca[] = [
  { ten: 'Không có quyền', page: KhongCoQuyenPage, ma: '403', icon: 'pi-lock' },
  { ten: 'Không tìm thấy', page: KhongTimThayPage, ma: '404', icon: 'pi-compass' },
];

/**
 * Design/Screens/02-trang-loi.md §Sơ đồ bố cục: PageHeader `minimal` mang <h1>, thân là EmptyState
 * cỡ `page` với biến thể `no-permission` / `not-found`, headingLevel 2, icon mặc định của biến thể,
 * đường đi tiếp là LIÊN KẾT tới `sauDangNhap` của CORE_ROUTES — không phải Button.
 */
describe('Trang lỗi điều hướng — dựng theo 02-trang-loi.md', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(KhoaLoader),
        }),
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/d',
            khongCoQuyen: '/khong-co-quyen',
            sauDangNhap: '/trang-chu',
          },
        },
      ],
    });
  });

  for (const ca of CAC_MAN) {
    describe(ca.ten, () => {
      async function dung(): Promise<HTMLElement> {
        const fixture = TestBed.createComponent(ca.page);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        return fixture.nativeElement as HTMLElement;
      }

      it('mở đầu bằng PageHeader mang <h1> duy nhất là tiêu đề trang', async () => {
        const el = await dung();
        const tieuDe = el.querySelectorAll('h1');
        expect(el.querySelector('app-page-header')).withContext('PageHeader').not.toBeNull();
        expect(tieuDe.length).toBe(1);
        expect(tieuDe[0].closest('app-page-header')).not.toBeNull();
        expect(tieuDe[0].textContent?.trim()).toBe(`${ca.ma}-tieuDe`);
      });

      it('thân là EmptyState cỡ page, tiêu đề <h2>, mô tả, icon mặc định của biến thể', async () => {
        const el = await dung();
        const than = el.querySelector('app-empty-state .empty-state');
        expect(than).withContext('EmptyState').not.toBeNull();
        expect(than?.classList.contains('empty-state--page')).withContext('cỡ page').toBeTrue();
        expect(than?.querySelector('h2')?.textContent?.trim()).toBe(`${ca.ma}-tieuDe`);
        expect(than?.textContent).toContain(`${ca.ma}-moTa`);
        expect(than?.querySelector(`i.${ca.icon}`))
          .withContext(ca.icon)
          .not.toBeNull();
      });

      it('đường đi tiếp là liên kết tới sauDangNhap, không phải nút', async () => {
        const el = await dung();
        const than = el.querySelector('app-empty-state');
        const lienKet = than?.querySelector('a');
        expect(lienKet?.getAttribute('href')).toBe('/trang-chu');
        expect(lienKet?.textContent?.trim()).toBe(`${ca.ma}-diTiep`);
        expect(than?.querySelector('button')).toBeNull();
      });
    });
  }
});
