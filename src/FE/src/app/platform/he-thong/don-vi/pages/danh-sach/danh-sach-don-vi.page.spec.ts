import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { ApiFailure, ApiFailureError } from '../../../../../core/http/api-result.model';
import { PagedList } from '../../../../../core/http/paged.model';
import { ToastService } from '../../../../../core/toast/toast.service';
import { ScreenExtColumn, provideCoreScreenExt } from '../../../../config/core-screen-ext';
import { DonVi } from '../../models/don-vi.model';
import { DonViService } from '../../services/don-vi.service';
import { DanhSachDonViPage } from './danh-sach-don-vi.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const DON_VI: DonVi = {
  id: 'dv-1',
  code: 'ABC',
  name: 'Đơn vị ABC',
  isActive: true,
  createdAt: null,
};
const TRANG: PagedList<DonVi> = { items: [DON_VI], page: 1, pageSize: 20, totalCount: 1 };

function loiApi(code: string): ApiFailureError {
  const body: ApiFailure = {
    success: false,
    data: null,
    error: {
      code,
      type: 'NotFound',
      message: `msg:${code}`,
      messageParams: null,
      fieldErrors: null,
    },
    traceId: 't',
  };
  return new ApiFailureError(body);
}

/** Bề mặt protected của page mà spec cần chạm — không có DOM hộp thoại nào phải bấm thật. */
interface PageTest {
  moXacNhanNgung(dv: DonVi): void;
  xacNhanNgung(): void;
  loiXacNhanNgung(): string | null;
  quanTriHop: {
    moKhoiPhuc(dv: DonVi): void;
    moTaoQuanTri(dv: DonVi): void;
    formKhoiPhuc: {
      setValue(v: { userName: string; tempPassword: string; goLaiMa: string }): void;
    };
    formTaoQuanTri: {
      setValue(v: {
        userName: string;
        email: string;
        fullName: string;
        tempPassword: string;
        goLaiMa: string;
      }): void;
    };
  };
  guiKhoiPhuc(): void;
  guiTaoQuanTri(): void;
}

describe('DanhSachDonViPage — CORE.TENANT.NOT_FOUND tải lại danh sách (Screen 20, "Mã lỗi → chỗ hiện")', () => {
  let fixture: ComponentFixture<DanhSachDonViPage>;
  let service: jasmine.SpyObj<DonViService>;
  let page: PageTest;

  beforeEach(async () => {
    service = jasmine.createSpyObj<DonViService>('DonViService', [
      'danhSach',
      'datTrangThai',
      'khoiPhucQuanTri',
      'taoQuanTriMoi',
      'tao',
    ]);
    service.danhSach.and.returnValue(of(TRANG));

    await TestBed.configureTestingModule({
      imports: [DanhSachDonViPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DanhSachDonViPage);
    page = fixture.componentInstance as unknown as PageTest;
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  });

  async function choTaiLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  }

  it('điểm xuất phát: danh sách được tải đúng một lần', () => {
    expect(service.danhSach).toHaveBeenCalledTimes(1);
  });

  it('bật lại → lỗi: toast mang traceId của envelope (fe-api-client.md §1.1) VÀ tải lại danh sách', async () => {
    const toast = TestBed.inject(ToastService);
    service.datTrangThai.and.returnValue(throwError(() => loiApi('CORE.TENANT.NOT_FOUND')));
    (page as unknown as { onChonMenu(k: string, dv: DonVi): void }).onChonMenu('bat-lai', DON_VI);
    await choTaiLai();

    expect(toast.latest()?.severity).toBe('error');
    expect(toast.latest()?.traceId).toBe('t');
    expect(service.danhSach).toHaveBeenCalledTimes(2);
  });

  it('bật lại → mất kết nối (không có envelope): toast không có traceId', async () => {
    const toast = TestBed.inject(ToastService);
    service.datTrangThai.and.returnValue(throwError(() => new ApiFailureError(null)));
    (page as unknown as { onChonMenu(k: string, dv: DonVi): void }).onChonMenu('bat-lai', DON_VI);
    await choTaiLai();

    expect(toast.latest()?.severity).toBe('error');
    expect(toast.latest()?.traceId).toBeNull();
  });

  it('ngưng hoạt động → NOT_FOUND: banner trong hộp xác nhận VÀ tải lại danh sách', async () => {
    service.datTrangThai.and.returnValue(throwError(() => loiApi('CORE.TENANT.NOT_FOUND')));
    page.moXacNhanNgung(DON_VI);
    page.xacNhanNgung();
    await choTaiLai();

    expect(page.loiXacNhanNgung()).not.toBeNull();
    expect(service.danhSach).toHaveBeenCalledTimes(2);
  });

  it('ngưng hoạt động → lỗi khác (SYSTEM_IMMUTABLE): banner nhưng KHÔNG tải lại danh sách', async () => {
    service.datTrangThai.and.returnValue(throwError(() => loiApi('CORE.TENANT.SYSTEM_IMMUTABLE')));
    page.moXacNhanNgung(DON_VI);
    page.xacNhanNgung();
    await choTaiLai();

    expect(page.loiXacNhanNgung()).not.toBeNull();
    expect(service.danhSach).toHaveBeenCalledTimes(1);
  });

  it('khôi phục quản trị → NOT_FOUND: tải lại danh sách', async () => {
    service.khoiPhucQuanTri.and.returnValue(throwError(() => loiApi('CORE.TENANT.NOT_FOUND')));
    page.quanTriHop.moKhoiPhuc(DON_VI);
    page.quanTriHop.formKhoiPhuc.setValue({
      userName: 'admin',
      tempPassword: 'Tam-Pass-1',
      goLaiMa: 'ABC',
    });
    page.guiKhoiPhuc();
    await choTaiLai();

    expect(service.danhSach).toHaveBeenCalledTimes(2);
  });

  it('tạo quản trị mới → NOT_FOUND: tải lại danh sách', async () => {
    service.taoQuanTriMoi.and.returnValue(throwError(() => loiApi('CORE.TENANT.NOT_FOUND')));
    page.quanTriHop.moTaoQuanTri(DON_VI);
    page.quanTriHop.formTaoQuanTri.setValue({
      userName: 'admin2',
      email: 'a@b.vn',
      fullName: 'Quản Trị',
      tempPassword: 'Tam-Pass-1',
      goLaiMa: 'ABC',
    });
    page.guiTaoQuanTri();
    await choTaiLai();

    expect(service.danhSach).toHaveBeenCalledTimes(2);
  });
});

/**
 * fe-ui-conventions.md §6.2 — hộp xác nhận ngưng không có form: banner `danger` trong `ConfirmDialog`
 * lấy câu từ `fieldErrorsText`, page không tự chọn câu của mã gốc. tenants.md (PATCH trạng thái, mã
 * dùng chung) trả `CORE.VALIDATION.FAILED` kèm `fieldErrors["IsActive"]`. Ca "bật lại" hiện ở Toast —
 * ngoài §6.2, không kiểm ở đây.
 */
describe('DanhSachDonViPage — câu của banner lỗi trong hộp ngưng hoạt động (fe-ui-conventions.md §6.2)', () => {
  const CAU_GOC_VALIDATION = 'Kiểm tra lại các trường được đánh dấu.';
  const CAU_REQUIRED = 'Trường này là bắt buộc.';
  const CAU_SYSTEM_IMMUTABLE = 'Không thể ngưng hoạt động đơn vị hệ thống.';

  class CauLoiLoader extends TranslateLoader {
    getTranslation(): Observable<TranslationObject> {
      return of({
        loi: {
          CORE: {
            VALIDATION: { FAILED: CAU_GOC_VALIDATION, REQUIRED: CAU_REQUIRED },
            TENANT: { SYSTEM_IMMUTABLE: CAU_SYSTEM_IMMUTABLE },
          },
        },
      });
    }
  }

  function loi(code: string, fieldErrors: ApiFailure['error']['fieldErrors']): ApiFailureError {
    return new ApiFailureError({
      success: false,
      data: null,
      error: { code, type: 'Validation', message: `msg:${code}`, messageParams: null, fieldErrors },
      traceId: 't',
    });
  }

  let service: jasmine.SpyObj<DonViService>;
  let fixture: ComponentFixture<DanhSachDonViPage>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<DonViService>('DonViService', ['danhSach', 'datTrangThai']);
    service.danhSach.and.returnValue(of(TRANG));
    await TestBed.configureTestingModule({
      imports: [DanhSachDonViPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(CauLoiLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DanhSachDonViPage);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  /** Xác nhận ngưng với lỗi cho trước, rồi đọc thân banner trong `ConfirmDialog` — thứ người dùng thấy. */
  async function ngungHong(err: ApiFailureError): Promise<string | undefined> {
    service.datTrangThai.and.returnValue(throwError(() => err));
    const page = fixture.componentInstance as unknown as PageTest;
    page.moXacNhanNgung(DON_VI);
    page.xacNhanNgung();
    fixture.detectChanges();
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement)
      .querySelector('app-confirm-dialog .notice-banner__than')
      ?.textContent?.trim();
  }

  it('VALIDATION.FAILED kèm fieldErrors["IsActive"] → banner trong hộp hiện câu của MÃ CON, không phải câu mã gốc', async () => {
    const cau = await ngungHong(
      loi('CORE.VALIDATION.FAILED', {
        IsActive: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
      }),
    );

    expect(cau).toBe(CAU_REQUIRED);
    expect(cau).not.toContain(CAU_GOC_VALIDATION);
  });

  it('không kèm fieldErrors (SYSTEM_IMMUTABLE) → câu của mã gốc như cũ', async () => {
    expect(await ngungHong(loi('CORE.TENANT.SYSTEM_IMMUTABLE', null))).toBe(CAU_SYSTEM_IMMUTABLE);
  });
});

/**
 * Nối page ↔ store ↔ component hộp bằng DOM thật (ADR-0049 điều 4): component hộp là dumb, nên chỉ có
 * chỗ này chứng minh page truyền ĐÚNG giá trị xuống và nối ĐÚNG output về hàm của store.
 */
describe('DanhSachDonViPage — nối store với component hộp (component dumb)', () => {
  let fixture: ComponentFixture<DanhSachDonViPage>;
  let service: jasmine.SpyObj<DonViService>;

  interface NoiTest {
    taoHop: { hienThi(): boolean; loiChung(): string | null; dangLuu(): boolean };
    quanTriHop: { dangMo(): string | null; moKhoiPhuc(dv: DonVi): void };
    moHopTao(): void;
  }
  let page: NoiTest;

  beforeEach(async () => {
    service = jasmine.createSpyObj<DonViService>('DonViService', ['danhSach', 'tao']);
    service.danhSach.and.returnValue(of(TRANG));
    await TestBed.configureTestingModule({
      imports: [DanhSachDonViPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DanhSachDonViPage);
    page = fixture.componentInstance as unknown as NoiTest;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  }

  it('mở hộp Tạo (store) → component hộp hiện form; bấm gửi form trống → store nhận submit, ô sai đầu tiên có focus và hiện lỗi', async () => {
    expect(document.getElementById('dv-tao-ma')).toBeNull();

    page.moHopTao();
    await veLai();
    expect(document.getElementById('dv-tao-ma')).not.toBeNull();

    document.querySelector('form.don-vi-form-nhom')!.dispatchEvent(new Event('submit'));
    await veLai();

    expect(service.tao).not.toHaveBeenCalled();
    expect(document.activeElement?.id).toBe('dv-tao-ma');
    expect(document.getElementById('dv-tao-ma-error')).not.toBeNull();
  });

  it('dialog phát closed → page gọi dongHop của store → hộp đóng', async () => {
    page.moHopTao();
    await veLai();
    expect(page.taoHop.hienThi()).toBeTrue();

    // Nút Hủy ở footer là app-button đầu tiên trong slot footer của hộp.
    (document.querySelector('[slot="footer"] button') as HTMLButtonElement).click();
    await veLai();

    expect(page.taoHop.hienThi()).toBeFalse();
  });

  it('mở hộp khôi phục từ store → component hộp quản trị hiện ô "gõ lại mã" của hộp đó', async () => {
    page.quanTriHop.moKhoiPhuc(DON_VI);
    await veLai();
    expect(document.getElementById('dv-kp-go-lai-ma')).not.toBeNull();
    expect(document.getElementById('dv-tq-go-lai-ma')).toBeNull();
  });
});

/**
 * V-3 — logic đóng menu hàng. `p-menu` của PrimeNG phát `onHide` khi hoạt ảnh ẩn BẮT ĐẦU, tức SAU
 * handler `click` của nút hàng khác. Bấm nút hàng B khi menu hàng A đang mở: `onMoMenu` đặt B, rồi
 * `openChange(false)` MUỘN của A tới — nếu nó xoá `mucMoMenuId` vô điều kiện thì nó xoá luôn giá trị
 * vừa đặt cho B, và menu B đóng ngay khi vừa mở. Spec kiểm LOGIC của page (không dựng overlay thật:
 * trong Karma, PrimeNG tự ẩn menu khi cuộn nên DOM thật cho kết quả nhiễu).
 */
describe('DanhSachDonViPage — menu hàng: openChange(false) muộn của hàng A không được xoá menu của hàng B', () => {
  const DON_VI_B: DonVi = { ...DON_VI, id: 'dv-2', code: 'XYZ', name: 'Đơn vị XYZ' };
  interface MenuTest {
    mucMoMenuId(): string | null;
    onMoMenu(evt: Event, dv: DonVi): void;
    onDongMenu(id: string): void;
  }
  let page: MenuTest;
  const evt = new Event('click');

  beforeEach(async () => {
    const service = jasmine.createSpyObj<DonViService>('DonViService', ['danhSach']);
    service.danhSach.and.returnValue(of(TRANG));
    await TestBed.configureTestingModule({
      imports: [DanhSachDonViPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    page = TestBed.createComponent(DanhSachDonViPage).componentInstance as unknown as MenuTest;
  });

  it('mở A, mở B, rồi onHide muộn của A → menu B vẫn mở', () => {
    page.onMoMenu(evt, DON_VI);
    expect(page.mucMoMenuId()).toBe('dv-1');
    page.onMoMenu(evt, DON_VI_B);
    expect(page.mucMoMenuId()).toBe('dv-2');

    page.onDongMenu('dv-1');

    expect(page.mucMoMenuId()).toBe('dv-2');
  });

  it('onHide của CHÍNH menu đang mở → đóng', () => {
    page.onMoMenu(evt, DON_VI);
    page.onDongMenu('dv-1');
    expect(page.mucMoMenuId()).toBeNull();
  });

  it('bấm lại nút hàng đang mở → đóng (bật/tắt giữ nguyên)', () => {
    page.onMoMenu(evt, DON_VI);
    page.onMoMenu(evt, DON_VI);
    expect(page.mucMoMenuId()).toBeNull();
  });
});

/** fe-architecture.md §2.7 luật 2 + DataTable.md: cột thêm nối sau cột dữ liệu của Core, cột hành động vẫn đứng cuối. */
describe('DanhSachDonViPage — seam CORE_SCREEN_EXT khoá "tenants"', () => {
  class DuAnTranslateLoader extends TranslateLoader {
    getTranslation(): Observable<TranslationObject> {
      return of({ duAn: { cot: { maRieng: 'Mã riêng' } } });
    }
  }

  async function cauHinh(cot: readonly ScreenExtColumn<DonVi>[]): Promise<void> {
    const service = jasmine.createSpyObj<DonViService>('DonViService', ['danhSach']);
    service.danhSach.and.returnValue(of(TRANG));
    await TestBed.configureTestingModule({
      imports: [DanhSachDonViPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(DuAnTranslateLoader),
        }),
        provideCoreScreenExt(() => ({ tenants: { columns: cot } })),
      ],
    }).compileComponents();
  }

  it('dự án khai cột → cột hiện sau "donVi.cot.ngayTao", trước cột hành động; tiêu đề đã dịch, ô là value(row)', async () => {
    await cauHinh([{ key: 'maRieng', headerKey: 'duAn.cot.maRieng', value: (r) => `EXT-${r.id}` }]);

    const fixture = TestBed.createComponent(DanhSachDonViPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    const cot = Array.from(el.querySelectorAll('thead th')).map((th) => th.textContent?.trim());
    expect(cot.slice(-3)).toEqual(['donVi.cot.ngayTao', 'Mã riêng', 'donVi.cot.hanhDong']);
    expect(el.querySelector('tbody')?.textContent).toContain('EXT-dv-1');
  });

  // V-03 — fe-architecture.md §2.7 luật 6 (ADR-0084). Tập khoá lấy từ chính bảng đã dựng (kể cả cột
  // hành động nối sau cột dự án), không chép tay: Core thêm cột mà quên đưa vào phép kiểm thì đỏ.
  it('V-03: cột dự án trùng BẤT KỲ khoá cột Core nào đang vẽ (kể cả "hanhDong") → dựng màn ném Error nêu "tenants" và khoá', async () => {
    await cauHinh([]);
    const goc = TestBed.createComponent(DanhSachDonViPage);
    goc.detectChanges();
    const cacKhoa = (goc.componentInstance as unknown as { columns(): readonly { key: string }[] })
      .columns()
      .map((c) => c.key);
    expect(cacKhoa).toContain('hanhDong');

    for (const khoa of cacKhoa) {
      TestBed.resetTestingModule();
      await cauHinh([{ key: khoa, headerKey: 'duAn.cot.maRieng', value: () => 'x' }]);
      expect(() => TestBed.createComponent(DanhSachDonViPage))
        .withContext(khoa)
        .toThrowMatching(
          (e) =>
            e instanceof Error &&
            e.message.includes('"tenants"') &&
            e.message.includes(`"${khoa}"`),
        );
    }
  });
});
