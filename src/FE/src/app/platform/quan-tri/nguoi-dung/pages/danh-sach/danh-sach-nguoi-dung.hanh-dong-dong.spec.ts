import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { PagedList } from '../../../../../core/http/paged.model';
import { VaiTroService } from '../../../vai-tro/services/vai-tro.service';
import { NguoiDung } from '../../models/nguoi-dung.model';
import { NguoiDungService } from '../../services/nguoi-dung.service';
import { DanhSachNguoiDungPage } from './danh-sach-nguoi-dung.page';
import { HanhDongDongNguoiDungComponent } from './hanh-dong-dong-nguoi-dung.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const DONG_BINH_THUONG: NguoiDung = {
  id: 'u1',
  userName: 'an.nguyen',
  email: 'an@b.vn',
  fullName: 'Nguyễn An',
  roles: [],
  isLocked: false,
  lockoutEnd: null,
  lockedByAdmin: false,
  mustChangePassword: false,
  createdAt: null,
  version: 'v-7',
};
const DONG_BI_KHOA: NguoiDung = {
  ...DONG_BINH_THUONG,
  id: 'u2',
  userName: 'bi.khoa',
  isLocked: true,
  version: 'v-9',
};

function trang(...dong: NguoiDung[]): PagedList<NguoiDung> {
  return { items: dong, page: 1, pageSize: 20, totalCount: dong.length };
}

/** Bề mặt protected của component cột hành động mà spec cần chạm. */
interface HanhDongTest {
  mucMenu(nd: NguoiDung, compact: boolean): { key: string }[];
  onChonMenu(khoa: string, nd: NguoiDung): void;
  hop: { hopDangMo(): string | null; hienThiXacNhanKhoa(): boolean };
}

/**
 * Design/Screens/10-nguoi-dung.md §"Hành động dòng" + DataTable.md §"Cột hành động": khối danh sách
 * liền (Toolbar attached trong khung), nút chính ở Toolbar, cột hành động theo từng khoá quyền.
 */
describe('DanhSachNguoiDungPage — khối liền và cột hành động dòng', () => {
  let service: jasmine.SpyObj<NguoiDungService>;
  let fixture: ComponentFixture<DanhSachNguoiDungPage>;
  let el: HTMLElement;

  async function dung(
    quyen: readonly string[],
    tuId = 'nguoi-khac',
    ...dong: NguoiDung[]
  ): Promise<void> {
    service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', [
      'danhSach',
      'tao',
      'moKhoa',
    ]);
    service.danhSach.and.returnValue(of(trang(...(dong.length ? dong : [DONG_BINH_THUONG]))));
    service.moKhoa.and.returnValue(of(undefined));
    await TestBed.configureTestingModule({
      imports: [DanhSachNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: NguoiDungService, useValue: service },
        {
          provide: AuthService,
          useValue: {
            nguoiDung: signal({ id: tuId, tenantName: 'Cty A' }),
            coQuyen: (ma: string) => quyen.includes(ma),
          },
        },
        {
          provide: VaiTroService,
          useValue: jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem', 'chiTiet']),
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DanhSachNguoiDungPage);
    el = fixture.nativeElement as HTMLElement;
    document.body.appendChild(el);
    await ve();
  }

  async function ve(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  }

  afterEach(() => el.remove());

  const hanhDong = (): HanhDongTest =>
    fixture.debugElement.query(By.directive(HanhDongDongNguoiDungComponent))
      .componentInstance as HanhDongTest;
  const nutTrongDong = (i = 0): HTMLButtonElement[] => {
    const hang = el.querySelectorAll<HTMLElement>('tbody tr')[i];
    return Array.from(
      hang.querySelectorAll<HTMLButtonElement>('td.data-table__o-hanh-dong button'),
    );
  };

  describe('khối danh sách liền một viền', () => {
    beforeEach(() => dung(['core.user.write', 'core.user.read']));

    it('PageHeader chỉ còn tiêu đề và mô tả — không nút nào', () => {
      const dau = el.querySelector('app-page-header')!;
      expect(dau.querySelector('h1')).not.toBeNull();
      expect(dau.querySelectorAll('button, a').length).toBe(0);
    });

    it('Toolbar placement="attached" nằm trong chỗ cắm đầu khung của DataTable', () => {
      const dauKhung = el.querySelector('app-data-table .data-table__dau')!;
      expect(dauKhung.querySelector('app-toolbar .toolbar--attached')).not.toBeNull();
      expect(el.querySelectorAll('app-toolbar').length).toBe(1);
    });

    it('nút chính "Thêm …" nằm trong nhóm hành động của Toolbar (có core.user.write), giữ nhãn đầy đủ', () => {
      const nut = el.querySelector('.toolbar__hanh-dong app-button button')!;
      expect(nut.textContent).toContain('nguoiDung.hanhDong.them');
      expect(nut.querySelector('.pi-plus')).not.toBeNull();
    });

    it('không còn ô lọc nào nằm thẳng trong chỗ cắm đầu khung; nút lọc đứng trong Toolbar', () => {
      const dauKhung = el.querySelector('app-data-table .data-table__dau')!;
      expect(dauKhung.querySelector('app-input, app-autocomplete')).toBeNull();
      const nutLoc = dauKhung.querySelector('app-toolbar .toolbar__nut-loc button')!;
      expect(nutLoc.getAttribute('aria-haspopup')).toBe('dialog');
      expect(nutLoc.getAttribute('aria-expanded')).toBe('false');
      expect(nutLoc.getAttribute('aria-controls')).toBe('nguoi-dung-bang-loc');
    });
  });

  it('không có core.user.write → không nút "Thêm" ở Toolbar', async () => {
    await dung(['core.user.read']);
    expect(el.querySelector('.toolbar__hanh-dong button')).toBeNull();
  });

  describe('cột hành động theo quyền', () => {
    it('không khoá nào của bảng "Hành động dòng" → không cột, kể cả th', async () => {
      await dung(['core.user.read']);
      expect(el.querySelectorAll('.data-table__o-hanh-dong').length).toBe(0);
    });

    it('chỉ core.user.write → nút Sửa (pencil) có aria-label nêu bản ghi; không nút ⋯', async () => {
      await dung(['core.user.write']);
      const nut = nutTrongDong();
      expect(nut.length).toBe(1);
      expect(nut[0].querySelector('.pi-pencil')).not.toBeNull();
      expect(nut[0].getAttribute('aria-label')).toBe('nguoiDung.hanhDong.suaDong');
      expect(el.querySelector('thead th.data-table__o-hanh-dong .sr-only')?.textContent).toContain(
        'nguoiDung.cot.hanhDong',
      );
    });

    it('chỉ core.user.lock → không Sửa; nút ⋯ aria-haspopup="menu" + aria-expanded', async () => {
      await dung(['core.user.lock']);
      const nut = nutTrongDong();
      expect(nut.length).toBe(1);
      expect(nut[0].querySelector('.pi-ellipsis-v')).not.toBeNull();
      expect(nut[0].getAttribute('aria-haspopup')).toBe('menu');
      expect(nut[0].getAttribute('aria-expanded')).toBe('false');
    });

    it('đủ quyền: hai nút nhìn thấy (Sửa, ⋯); menu có Khoá, Đặt lại, Gán vai trò', async () => {
      await dung([
        'core.user.write',
        'core.user.lock',
        'core.user.reset-password',
        'core.user.role.assign',
        'core.role.read',
      ]);
      expect(nutTrongDong().length).toBe(2);
      expect(
        hanhDong()
          .mucMenu(DONG_BINH_THUONG, false)
          .map((m) => m.key),
      ).toEqual(['khoa', 'dat-lai', 'gan-vai-tro']);
    });

    it('dòng đang bị khoá → "Mở khoá" thay "Khoá"; Gán vai trò đòi CẢ core.role.read', async () => {
      await dung(['core.user.lock', 'core.user.role.assign']);
      expect(
        hanhDong()
          .mucMenu(DONG_BI_KHOA, false)
          .map((m) => m.key),
      ).toEqual(['mo-khoa']);
      expect(
        hanhDong()
          .mucMenu(DONG_BINH_THUONG, false)
          .map((m) => m.key),
      ).toEqual(['khoa']);
    });

    it('dòng là CHÍNH MÌNH → không Khoá, không Đặt lại mật khẩu', async () => {
      await dung(
        ['core.user.lock', 'core.user.reset-password', 'core.user.role.assign', 'core.role.read'],
        DONG_BINH_THUONG.id,
      );
      expect(
        hanhDong()
          .mucMenu(DONG_BINH_THUONG, false)
          .map((m) => m.key),
      ).toEqual(['gan-vai-tro']);
    });

    it('compact (dạng thẻ): menu có cả "Sửa" đứng đầu', async () => {
      await dung(['core.user.write', 'core.user.lock']);
      expect(
        hanhDong()
          .mucMenu(DONG_BINH_THUONG, true)
          .map((m) => m.key),
      ).toEqual(['sua', 'khoa']);
    });
  });

  describe('thao tác dùng dòng, không GET chi tiết', () => {
    it('bấm Sửa → mở hộp "Sửa người dùng" của đúng dòng', async () => {
      await dung(['core.user.write']);
      nutTrongDong()[0].click();
      // Không vẽ lại: dựng hộp thật sẽ bật z-index toàn cục của PrimeNG và làm lệch spec lớp chồng.
      expect(hanhDong().hop.hopDangMo()).toBe('sua');
    });

    it('menu "Khoá tài khoản" → hộp xác nhận khoá; "Mở khoá" gửi ngay với version của dòng rồi tải lại danh sách', async () => {
      await dung(['core.user.lock'], 'nguoi-khac', DONG_BI_KHOA);
      const truoc = service.danhSach.calls.count();
      hanhDong().onChonMenu('mo-khoa', DONG_BI_KHOA);
      await ve();
      expect(service.moKhoa).toHaveBeenCalledOnceWith('u2', { version: 'v-9' });
      expect(service.danhSach.calls.count()).toBe(truoc + 1);

      hanhDong().onChonMenu('khoa', DONG_BINH_THUONG);
      expect(hanhDong().hop.hienThiXacNhanKhoa()).toBeTrue();
    });
  });
});
