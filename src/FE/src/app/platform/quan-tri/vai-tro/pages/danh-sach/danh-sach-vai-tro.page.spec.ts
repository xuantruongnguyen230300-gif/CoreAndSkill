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
import { Observable, Subject, of, throwError } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { ApiFailure, ApiFailureError } from '../../../../../core/http/api-result.model';
import { PagedList } from '../../../../../core/http/paged.model';
import { ScreenExtColumn, provideCoreScreenExt } from '../../../../config/core-screen-ext';
import { VaiTro } from '../../models/vai-tro.model';
import { VaiTroService } from '../../services/vai-tro.service';
import { HopVaiTroStore } from '../../state/hop-vai-tro.store';
import { DanhSachVaiTroPage } from './danh-sach-vai-tro.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/**
 * Cùng logic với `DanhSachDonViPage` (V-3): `p-menu` phát `openChange(false)` khi hoạt ảnh ẩn BẮT
 * ĐẦU — có thể tới sau khi nút hàng khác đã đặt `mucMoMenuId`. Spec kiểm LOGIC của page, không dựng
 * overlay PrimeNG thật (trong Karma PrimeNG tự ẩn menu khi cuộn, cho kết quả nhiễu).
 */
describe('DanhSachVaiTroPage — menu hàng: openChange(false) muộn của hàng A không được xoá menu của hàng B', () => {
  interface MenuTest {
    mucMoMenuId(): string | null;
    onMoMenu(evt: Event, vt: VaiTro): void;
    onDongMenu(id: string): void;
  }
  const A = { id: 'vt-1' } as VaiTro;
  const B = { id: 'vt-2' } as VaiTro;
  const evt = new Event('click');
  let page: MenuTest;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DanhSachVaiTroPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        {
          provide: VaiTroService,
          useValue: jasmine.createSpyObj<VaiTroService>('VaiTroService', ['danhSach']),
        },
        { provide: AuthService, useValue: {} },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    page = TestBed.createComponent(DanhSachVaiTroPage).componentInstance as unknown as MenuTest;
  });

  it('mở A, mở B, rồi onHide muộn của A → menu B vẫn mở', () => {
    page.onMoMenu(evt, A);
    page.onMoMenu(evt, B);
    expect(page.mucMoMenuId()).toBe('vt-2');

    page.onDongMenu('vt-1');

    expect(page.mucMoMenuId()).toBe('vt-2');
  });

  it('onHide của CHÍNH menu đang mở → đóng', () => {
    page.onMoMenu(evt, A);
    page.onDongMenu('vt-1');
    expect(page.mucMoMenuId()).toBeNull();
  });
});

/**
 * `VaiTroService` tắt toast (BO_QUA_TOAST_LOI) nên hộp là nơi DUY NHẤT hiện lỗi tạo/đổi tên vai trò:
 * 422 mà không có `fieldErrors` dùng được mà hộp im lặng thì người dùng chỉ thấy nút quay xong rồi thôi.
 */
describe('DanhSachVaiTroPage — lỗi 422 của hộp tạo vai trò không gắn được vào ô nào vẫn phải HIỆN', () => {
  interface FormTest {
    hop: HopVaiTroStore;
    xongHop: (daDoi: VaiTro | null) => void;
  }
  let service: jasmine.SpyObj<VaiTroService>;
  let page: FormTest;

  function loiApi(code: string, fieldErrors: ApiFailure['error']['fieldErrors']): ApiFailureError {
    return new ApiFailureError({
      success: false,
      data: null,
      error: { code, type: 'Validation', message: `msg:${code}`, messageParams: null, fieldErrors },
      traceId: 't',
    });
  }

  beforeEach(async () => {
    service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['danhSach', 'tao']);
    service.danhSach.and.returnValue(
      of({ items: [], page: 1, pageSize: 20, totalCount: 0 } as PagedList<VaiTro>),
    );
    await TestBed.configureTestingModule({
      imports: [DanhSachVaiTroPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: VaiTroService, useValue: service },
        { provide: AuthService, useValue: { coQuyen: () => true } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    page = TestBed.createComponent(DanhSachVaiTroPage).componentInstance as unknown as FormTest;
    page.hop.moHopTao();
    page.hop.form.setValue({ name: 'Kế toán' });
  });

  // Hộp này chỉ có MỘT ô nhập, nút gửi nằm ngoài <form> → Enter gửi ngầm, Button không chặn được.
  // Enter hai lần → hai POST tạo cùng tên: lần hai nhận NAME_DUPLICATE giả dù lần một đã thành công.
  it('chống gửi hai lần: guiForm() lần hai khi lần một chưa xong → chỉ MỘT request', () => {
    service.tao.and.returnValue(new Subject<{ id: string }>());

    page.hop.gui(page.xongHop);
    page.hop.gui(page.xongHop);

    expect(service.tao).toHaveBeenCalledTimes(1);
  });

  for (const fieldErrors of [null, {}]) {
    it(`VALIDATION.FAILED với fieldErrors = ${JSON.stringify(fieldErrors)} → banner của hộp mang câu của mã`, () => {
      service.tao.and.returnValue(throwError(() => loiApi('CORE.VALIDATION.FAILED', fieldErrors)));

      page.hop.gui(page.xongHop);

      expect(page.hop.loi()).toBe('msg:CORE.VALIDATION.FAILED');
    });
  }
});

/**
 * roles.md §3: PUT mang `version` của lần GET gần nhất, trả vai trò đã đổi kèm `version` MỚI.
 * 409 có hai mã — rẽ nhánh theo `code`, không theo HTTP status. Khuôn xung đột theo màn người dùng và
 * hồ sơ: banner `warning` trong hộp + nút Tải lại, không tự gửi lại; ô giữ tên đang nhập cho tới
 * khi bấm Tải lại, bấm rồi thì nạp tên mới từ máy chủ (Screens/11 §Trạng thái).
 */
describe('DanhSachVaiTroPage — đổi tên vai trò mang token đồng thời', () => {
  interface DoiTenTest {
    rows(): readonly VaiTro[];
    hop: HopVaiTroStore;
    xongHop: (daDoi: VaiTro | null) => void;
    taiLaiDanhSach: () => void;
  }
  const CU: VaiTro = {
    id: 'r1',
    name: 'Kế toán',
    isSystem: false,
    userCount: 2,
    createdAt: null,
    version: 'v-cu',
  };
  let service: jasmine.SpyObj<VaiTroService>;
  let page: DoiTenTest;
  let fixture: ComponentFixture<DanhSachVaiTroPage>;

  function loi409(code: string): ApiFailureError {
    return new ApiFailureError({
      success: false,
      data: null,
      error: {
        code,
        type: 'Conflict',
        message: `msg:${code}`,
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 't',
    });
  }

  beforeEach(async () => {
    service = jasmine.createSpyObj<VaiTroService>('VaiTroService', [
      'danhSach',
      'doiTen',
      'chiTiet',
    ]);
    service.danhSach.and.returnValue(
      of({ items: [CU], page: 1, pageSize: 20, totalCount: 1 } as PagedList<VaiTro>),
    );
    await TestBed.configureTestingModule({
      imports: [DanhSachVaiTroPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: VaiTroService, useValue: service },
        { provide: AuthService, useValue: { coQuyen: () => true, nguoiDung: () => null } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DanhSachVaiTroPage);
    page = fixture.componentInstance as unknown as DoiTenTest;
    await fixture.whenStable();
    page.hop.moHopSua(CU);
    // Mô phỏng người dùng gõ: giá trị đổi VÀ form bẩn.
    page.hop.form.setValue({ name: 'Kế toán trưởng' });
    page.hop.form.markAsDirty();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  }

  it('gửi PUT kèm version của dòng đang mở', () => {
    service.doiTen.and.returnValue(new Subject<VaiTro>());
    page.hop.gui(page.xongHop);
    expect(service.doiTen).toHaveBeenCalledOnceWith('r1', 'Kế toán trưởng', 'v-cu');
  });

  it('thành công → dòng trong danh sách mang version MỚI ngay, không chờ GET lại', () => {
    const moi: VaiTro = { ...CU, name: 'Kế toán trưởng', version: 'v-moi' };
    service.doiTen.and.returnValue(of(moi));
    // Lần tải lại sau khi đổi chưa về — dòng cũ vẫn nằm trong danh sách của store.
    service.danhSach.and.returnValue(new Subject<PagedList<VaiTro>>());

    page.hop.gui(page.xongHop);

    expect(page.hop.hienThi()).toBeFalse();
    expect(page.rows()).toEqual([moi]);
  });

  it('409 CORE.CONCURRENCY.CONFLICT → bật xung đột, KHÔNG tự gửi lại, giữ tên đang nhập, hộp giữ mở', () => {
    service.doiTen.and.returnValue(throwError(() => loi409('CORE.CONCURRENCY.CONFLICT')));

    page.hop.gui(page.xongHop);

    expect(page.hop.xungDot()).toBeTrue();
    expect(page.hop.loi()).toBeNull();
    expect(service.doiTen).toHaveBeenCalledTimes(1);
    expect(page.hop.hienThi()).toBeTrue();
    expect(page.hop.form.getRawValue()).toEqual({ name: 'Kế toán trưởng' });
  });

  it('409 CORE.ROLE.NAME_DUPLICATE → lỗi ở ô tên, KHÔNG phải xung đột (rẽ theo code, không theo status)', () => {
    service.doiTen.and.returnValue(throwError(() => loi409('CORE.ROLE.NAME_DUPLICATE')));

    page.hop.gui(page.xongHop);

    expect(page.hop.xungDot()).toBeFalse();
    expect(page.hop.form.controls.name.errors?.['server']).toBeDefined();
  });

  // Screens/11 §Trạng thái, dòng xung đột: Tải lại NẠP tên mới từ máy chủ, bỏ tên đang nhập — giữ tên
  // cũ kèm `version` mới thì một cú Lưu ghi đè thay đổi của người kia mà người dùng chưa từng thấy.
  it('Tải lại sau xung đột → GET lấy bản mới, ô nhận tên từ máy chủ, form hết dirty; lần Lưu kế gửi version mới', () => {
    service.doiTen.and.returnValue(throwError(() => loi409('CORE.CONCURRENCY.CONFLICT')));
    page.hop.gui(page.xongHop);
    service.chiTiet.and.returnValue(
      of({ ...CU, name: 'Kế toán (người khác đổi)', version: 'v-2' }),
    );

    page.hop.taiLaiSauXungDot(page.taiLaiDanhSach);

    expect(service.chiTiet).toHaveBeenCalledOnceWith('r1');
    expect(page.hop.xungDot()).toBeFalse();
    expect(page.hop.dangSua()?.version).toBe('v-2');
    expect(page.hop.form.getRawValue()).toEqual({ name: 'Kế toán (người khác đổi)' });
    expect(page.hop.form.dirty).withContext('form về trạng thái chưa sửa').toBeFalse();

    page.hop.form.setValue({ name: 'Kế toán tổng hợp' });
    service.doiTen.and.returnValue(new Subject<VaiTro>());
    page.hop.gui(page.xongHop);
    expect(service.doiTen).toHaveBeenCalledWith('r1', 'Kế toán tổng hợp', 'v-2');
  });

  it('Tải lại xong → focus về ô "Tên vai trò" (nút Tải lại đã rời đi cùng banner)', async () => {
    await veLai();
    service.doiTen.and.returnValue(throwError(() => loi409('CORE.CONCURRENCY.CONFLICT')));
    page.hop.gui(page.xongHop);
    await veLai();
    (document.activeElement as HTMLElement | null)?.blur();
    service.chiTiet.and.returnValue(of({ ...CU, name: 'Kế toán mới', version: 'v-2' }));

    page.hop.taiLaiSauXungDot(page.taiLaiDanhSach);
    await veLai();

    const o = document.activeElement as HTMLInputElement | null;
    expect(o?.tagName).toBe('INPUT');
    expect(o?.id).toBe('vai-tro-ten');
    expect(o?.value).toBe('Kế toán mới');
  });

  it('Tải lại hỏng → ô giữ nguyên giá trị đang có, lỗi hiện ở banner hộp', () => {
    service.doiTen.and.returnValue(throwError(() => loi409('CORE.CONCURRENCY.CONFLICT')));
    page.hop.gui(page.xongHop);
    service.chiTiet.and.returnValue(throwError(() => new Error('mất mạng')));

    page.hop.taiLaiSauXungDot(page.taiLaiDanhSach);

    expect(page.hop.form.getRawValue()).toEqual({ name: 'Kế toán trưởng' });
    expect(page.hop.form.dirty).withContext('vẫn là thay đổi chưa lưu').toBeTrue();
    expect(page.hop.dangSua()?.version).withContext('không có bản mới').toBe('v-cu');
    expect(page.hop.loi()).toBe('loi.CORE.CLIENT.NO_CONNECTION');
  });

  it('Tải lại hỏng → câu lỗi của mã vào banner hộp, xung đột tắt', () => {
    service.doiTen.and.returnValue(throwError(() => loi409('CORE.CONCURRENCY.CONFLICT')));
    page.hop.gui(page.xongHop);
    service.chiTiet.and.returnValue(throwError(() => loi409('CORE.ROLE.NOT_FOUND')));

    page.hop.taiLaiSauXungDot(page.taiLaiDanhSach);

    expect(page.hop.xungDot()).toBeFalse();
    expect(page.hop.loi()).toBe('msg:CORE.ROLE.NOT_FOUND');
  });

  it('mở lại hộp sau xung đột → không mang dấu xung đột cũ', () => {
    service.doiTen.and.returnValue(throwError(() => loi409('CORE.CONCURRENCY.CONFLICT')));
    page.hop.gui(page.xongHop);

    page.hop.moHopSua(CU);

    expect(page.hop.xungDot()).toBeFalse();
  });

  // fe-api-client.md §6.1: lượt ĐỌC của màn chết cùng màn. `HopVaiTroStore` cấp ở `providers` của
  // page nên chết cùng page — GET của nút Tải lại không được sống tiếp.
  it('rời màn khi GET của nút Tải lại đang chạy → request bị huỷ', () => {
    service.doiTen.and.returnValue(throwError(() => loi409('CORE.CONCURRENCY.CONFLICT')));
    page.hop.gui(page.xongHop);
    const nguon = new Subject<VaiTro>();
    service.chiTiet.and.returnValue(nguon);
    page.hop.taiLaiSauXungDot(page.taiLaiDanhSach);
    expect(nguon.observed).toBeTrue();

    fixture.destroy();

    expect(nguon.observed).toBeFalse();
  });
});

/**
 * fe-ui-conventions.md §6.2 — hộp xác nhận xoá không có form: banner `danger` trong `ConfirmDialog`
 * (Screens/11, bảng mã lỗi) lấy câu từ `fieldErrorsText`, page không tự chọn câu của mã gốc. roles.md
 * §4 không khai `fieldErrors` cho DELETE, nên ca mã con ở đây ghim LUẬT của khu lỗi chung, không phải
 * một ca hợp đồng; ca không `fieldErrors` là đường đi thật (IN_USE mang `Count`).
 */
describe('DanhSachVaiTroPage — câu của banner lỗi trong hộp xoá (fe-ui-conventions.md §6.2)', () => {
  const CAU_GOC_VALIDATION = 'Kiểm tra lại các trường được đánh dấu.';
  const CAU_REQUIRED = 'Trường này là bắt buộc.';
  const VT: VaiTro = {
    id: 'r1',
    name: 'Kế toán',
    isSystem: false,
    userCount: 0,
    createdAt: null,
    version: 'v1',
  };

  class CauLoiLoader extends TranslateLoader {
    getTranslation(): Observable<TranslationObject> {
      return of({
        loi: {
          CORE: {
            VALIDATION: { FAILED: CAU_GOC_VALIDATION, REQUIRED: CAU_REQUIRED },
            ROLE: { IN_USE: 'Vai trò còn {{Count}} người dùng.' },
          },
        },
      });
    }
  }

  interface XoaTest {
    moHopXoa(vt: VaiTro): void;
    xacNhanXoa(): void;
  }
  let service: jasmine.SpyObj<VaiTroService>;
  let fixture: ComponentFixture<DanhSachVaiTroPage>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['danhSach', 'xoa']);
    service.danhSach.and.returnValue(
      of({ items: [VT], page: 1, pageSize: 20, totalCount: 1 } as PagedList<VaiTro>),
    );
    await TestBed.configureTestingModule({
      imports: [DanhSachVaiTroPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: VaiTroService, useValue: service },
        { provide: AuthService, useValue: { coQuyen: () => true, nguoiDung: () => null } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(CauLoiLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DanhSachVaiTroPage);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  /** Xác nhận xoá với lỗi cho trước, rồi đọc thân banner trong `ConfirmDialog` — thứ người dùng thấy. */
  async function xoaHong(loi: ApiFailureError): Promise<string | undefined> {
    service.xoa.and.returnValue(throwError(() => loi));
    const page = fixture.componentInstance as unknown as XoaTest;
    page.moHopXoa(VT);
    page.xacNhanXoa();
    fixture.detectChanges();
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement)
      .querySelector('app-confirm-dialog .notice-banner__than')
      ?.textContent?.trim();
  }

  function loi(
    code: string,
    fieldErrors: ApiFailure['error']['fieldErrors'],
    messageParams: Record<string, string> | null = null,
  ): ApiFailureError {
    return new ApiFailureError({
      success: false,
      data: null,
      error: { code, type: 'Validation', message: `msg:${code}`, messageParams, fieldErrors },
      traceId: 't',
    });
  }

  it('fieldErrors mang mã con → banner trong hộp hiện câu của MÃ CON, không phải câu mã gốc', async () => {
    const cau = await xoaHong(
      loi('CORE.VALIDATION.FAILED', {
        Id: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
      }),
    );

    expect(cau).toBe(CAU_REQUIRED);
    expect(cau).not.toContain(CAU_GOC_VALIDATION);
  });

  it('không kèm fieldErrors (IN_USE) → câu của mã gốc như cũ, kèm messageParams của máy chủ', async () => {
    expect(await xoaHong(loi('CORE.ROLE.IN_USE', null, { Count: '3' }))).toBe(
      'Vai trò còn 3 người dùng.',
    );
  });
});

/** fe-architecture.md §2.7 luật 2 + DataTable.md: cột thêm nối sau cột dữ liệu của Core, cột hành động vẫn đứng cuối. */
describe('DanhSachVaiTroPage — seam CORE_SCREEN_EXT khoá "roles"', () => {
  class DuAnTranslateLoader extends TranslateLoader {
    getTranslation(): Observable<TranslationObject> {
      return of({ duAn: { cot: { maRieng: 'Mã riêng' } } });
    }
  }

  async function cauHinh(cot: readonly ScreenExtColumn<VaiTro>[]): Promise<void> {
    const service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['danhSach']);
    service.danhSach.and.returnValue(
      of({
        items: [
          {
            id: 'r1',
            name: 'Kế toán',
            isSystem: false,
            userCount: 0,
            createdAt: null,
            version: 'v',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
      } as PagedList<VaiTro>),
    );
    await TestBed.configureTestingModule({
      imports: [DanhSachVaiTroPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: VaiTroService, useValue: service },
        { provide: AuthService, useValue: { nguoiDung: () => null, coQuyen: () => true } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(DuAnTranslateLoader),
        }),
        provideCoreScreenExt(() => ({ roles: { columns: cot } })),
      ],
    }).compileComponents();
  }

  it('dự án khai cột → cột hiện sau "vaiTro.cot.ngayTao", trước cột hành động; tiêu đề đã dịch, ô là value(row)', async () => {
    await cauHinh([{ key: 'maRieng', headerKey: 'duAn.cot.maRieng', value: (r) => `EXT-${r.id}` }]);

    const fixture = TestBed.createComponent(DanhSachVaiTroPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    const cot = Array.from(el.querySelectorAll('thead th')).map((th) => th.textContent?.trim());
    expect(cot.slice(-3)).toEqual(['vaiTro.cot.ngayTao', 'Mã riêng', 'vaiTro.cot.hanhDong']);
    expect(el.querySelector('tbody')?.textContent).toContain('EXT-r1');
  });

  // V-03 — fe-architecture.md §2.7 luật 6 (ADR-0084). Tập khoá lấy từ chính bảng đã dựng (kể cả cột
  // hành động nối sau cột dự án), không chép tay: Core thêm cột mà quên đưa vào phép kiểm thì đỏ.
  it('V-03: cột dự án trùng BẤT KỲ khoá cột Core nào đang vẽ (kể cả "hanhDong") → dựng màn ném Error nêu "roles" và khoá', async () => {
    await cauHinh([]);
    const goc = TestBed.createComponent(DanhSachVaiTroPage);
    goc.detectChanges();
    const cacKhoa = (goc.componentInstance as unknown as { columns(): readonly { key: string }[] })
      .columns()
      .map((c) => c.key);
    expect(cacKhoa).toContain('hanhDong');

    for (const khoa of cacKhoa) {
      TestBed.resetTestingModule();
      await cauHinh([{ key: khoa, headerKey: 'duAn.cot.maRieng', value: () => 'x' }]);
      expect(() => TestBed.createComponent(DanhSachVaiTroPage))
        .withContext(khoa)
        .toThrowMatching(
          (e) =>
            e instanceof Error && e.message.includes('"roles"') && e.message.includes(`"${khoa}"`),
        );
    }
  });
});
