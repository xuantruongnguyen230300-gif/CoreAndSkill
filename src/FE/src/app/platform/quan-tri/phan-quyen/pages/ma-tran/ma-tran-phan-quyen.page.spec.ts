import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { ApiFailure, ApiFailureError } from '../../../../../core/http/api-result.model';
import { UnsavedChangesService } from '../../../../../core/unsaved-changes/unsaved-changes.service';
import { MaTranHang, MaTranPhanQuyen } from '../../models/phan-quyen.model';
import { PhanQuyenService } from '../../services/phan-quyen.service';
import { MaTranPhanQuyenPage } from './ma-tran-phan-quyen.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const MA_TRAN: MaTranPhanQuyen = {
  roles: [{ id: 'r1', name: 'Kế toán', isSystem: false }],
  rows: [
    {
      permissionId: 'p1',
      code: 'core.user.read',
      resourceKey: 'user',
      resourceNameKey: 'phanQuyen.taiNguyen.user',
      nameKey: 'phanQuyen.quyen.core.user.read',
      grantedRoleIds: ['r1'],
    },
  ],
  version: 'v1',
};

interface PageTest {
  bamLuu(): void;
  taiLaiSauXungDot(): void;
}

/**
 * fe-api-client.md §6.1: lượt ĐỌC của màn chết cùng màn — rời màn thì GET đang chạy bị huỷ. Lượt GHI
 * thì KHÔNG: huỷ không dừng được việc ghi ở máy chủ, chỉ bỏ dở phần FE xử lý kết quả của nó.
 */
describe('MaTranPhanQuyenPage — huỷ request khi rời màn', () => {
  let service: jasmine.SpyObj<PhanQuyenService>;
  let fixture: ComponentFixture<MaTranPhanQuyenPage>;

  async function dung(): Promise<PageTest> {
    await TestBed.configureTestingModule({
      imports: [MaTranPhanQuyenPage],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PhanQuyenService, useValue: service },
        { provide: AuthService, useValue: { coQuyen: () => true } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(MaTranPhanQuyenPage);
    return fixture.componentInstance as unknown as PageTest;
  }

  beforeEach(() => {
    service = jasmine.createSpyObj<PhanQuyenService>('PhanQuyenService', [
      'layMaTran',
      'luuMaTran',
    ]);
  });

  it('rời màn khi GET ma trận lần đầu đang chạy → request bị huỷ', async () => {
    const nguon = new Subject<MaTranPhanQuyen>();
    service.layMaTran.and.returnValue(nguon);
    await dung();
    expect(nguon.observed).toBeTrue();

    fixture.destroy();

    expect(nguon.observed).toBeFalse();
  });

  it('rời màn khi GET tải lại sau xung đột đang chạy → request bị huỷ', async () => {
    service.layMaTran.and.returnValue(of(MA_TRAN));
    const page = await dung();
    const nguon = new Subject<MaTranPhanQuyen>();
    service.layMaTran.and.returnValue(nguon);
    page.taiLaiSauXungDot();
    expect(nguon.observed).toBeTrue();

    fixture.destroy();

    expect(nguon.observed).toBeFalse();
  });

  it('LƯU đang chạy khi rời màn → KHÔNG bị huỷ; lưu xong sau đó không mở GET tải lại cho màn đã chết', async () => {
    service.layMaTran.and.returnValue(of(MA_TRAN));
    const page = await dung();
    const nguonLuu = new Subject<string>();
    service.luuMaTran.and.returnValue(nguonLuu);
    page.bamLuu();

    fixture.destroy();
    expect(nguonLuu.observed).withContext('lệnh ghi vẫn chạy tới cùng').toBeTrue();

    const nguonTaiLai = new Subject<MaTranPhanQuyen>();
    service.layMaTran.and.returnValue(nguonTaiLai);
    nguonLuu.next('v2');
    nguonLuu.complete();

    expect(nguonTaiLai.observed).toBeFalse();
  });
});

/**
 * fe-ui-conventions.md §6.2 — màn không có form: banner lỗi lưu lấy câu từ `fieldErrorsText`, page
 * không tự chọn câu của mã gốc. Mã gốc không nói thay mã con: permissions.md (mã dùng chung) trả 400
 * `CORE.VALIDATION.FAILED` kèm `fieldErrors["Entries[i].RoleIds"]` mã `CORE.VALIDATION.REQUIRED`;
 * banner hiện "kiểm tra lại các trường được đánh dấu" là sai vì màn không có ô nào được đánh dấu.
 */
describe('MaTranPhanQuyenPage — câu của banner lỗi lưu (fe-ui-conventions.md §6.2)', () => {
  const CAU_GOC_VALIDATION = 'Kiểm tra lại các trường được đánh dấu.';
  const CAU_REQUIRED = 'Trường này là bắt buộc.';
  const CAU_SYSTEM_ROLE = 'Vai trò hệ thống luôn giữ quyền sửa phân quyền.';

  class CauLoiLoader extends TranslateLoader {
    getTranslation(): Observable<TranslationObject> {
      return of({
        loi: {
          CORE: {
            VALIDATION: { FAILED: CAU_GOC_VALIDATION, REQUIRED: CAU_REQUIRED },
            PERMISSION: { SYSTEM_ROLE_CANNOT_LOSE_WRITE: CAU_SYSTEM_ROLE },
          },
        },
      });
    }
  }

  function loiLuu(
    code: string,
    fieldErrors: ApiFailure['error']['fieldErrors'],
    type = 'Validation',
  ): ApiFailureError {
    return new ApiFailureError({
      success: false,
      data: null,
      error: { code, type, message: `msg:${code}`, messageParams: null, fieldErrors },
      traceId: 't',
    });
  }

  let service: jasmine.SpyObj<PhanQuyenService>;
  let fixture: ComponentFixture<MaTranPhanQuyenPage>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<PhanQuyenService>('PhanQuyenService', [
      'layMaTran',
      'luuMaTran',
    ]);
    service.layMaTran.and.returnValue(of(MA_TRAN));
    await TestBed.configureTestingModule({
      imports: [MaTranPhanQuyenPage],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PhanQuyenService, useValue: service },
        { provide: AuthService, useValue: { coQuyen: () => true } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(CauLoiLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(MaTranPhanQuyenPage);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  /** Bấm lưu với lỗi cho trước, rồi đọc thân banner `danger` trên DOM — thứ người dùng thấy. */
  async function luuHong(loi: ApiFailureError): Promise<string | undefined> {
    service.luuMaTran.and.returnValue(throwError(() => loi));
    (fixture.componentInstance as unknown as PageTest).bamLuu();
    fixture.detectChanges();
    await fixture.whenStable();
    const than = (fixture.nativeElement as HTMLElement).querySelectorAll(
      '.notice-banner--danger .notice-banner__than',
    );
    expect(than.length).withContext('đúng một banner lỗi lưu').toBe(1);
    return than[0]?.textContent?.trim();
  }

  it('400 VALIDATION.FAILED kèm fieldErrors["Entries[3].RoleIds"] → banner hiện câu của MÃ CON, không phải câu mã gốc', async () => {
    const cau = await luuHong(
      loiLuu('CORE.VALIDATION.FAILED', {
        'Entries[3].RoleIds': [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
      }),
    );

    expect(cau).toBe(CAU_REQUIRED);
    expect(cau).not.toContain(CAU_GOC_VALIDATION);
  });

  for (const fieldErrors of [null, {}]) {
    it(`VALIDATION.FAILED với fieldErrors = ${JSON.stringify(fieldErrors)} → đường lùi: câu của mã gốc`, async () => {
      expect(await luuHong(loiLuu('CORE.VALIDATION.FAILED', fieldErrors))).toBe(CAU_GOC_VALIDATION);
    });
  }

  it('422 không kèm fieldErrors (SYSTEM_ROLE_CANNOT_LOSE_WRITE) → banner hiện câu của mã gốc như cũ', async () => {
    expect(
      await luuHong(loiLuu('CORE.PERMISSION.SYSTEM_ROLE_CANNOT_LOSE_WRITE', null, 'BusinessRule')),
    ).toBe(CAU_SYSTEM_ROLE);
  });
});

/**
 * Design/Screens/12-ma-tran-phan-quyen.md §Trạng thái "lỗi": tải hỏng → trạng thái `error` của
 * `Table` (hàng `colspan` chứa `NoticeBanner` `danger` + thử lại), tiêu đề `phanQuyen.loi.taiThatBai`
 * LUÔN có, thân là câu `dichLoiChoMan` — lớp lỗi xuyên suốt thì không thân (chi tiết và `traceId` ở
 * toast). KHÔNG thay cả bảng bằng `EmptyState`: `<thead>` giữ nguyên (Table.md §Trạng thái).
 */
describe('MaTranPhanQuyenPage — tải ma trận hỏng', () => {
  let service: jasmine.SpyObj<PhanQuyenService>;
  let fixture: ComponentFixture<MaTranPhanQuyenPage>;

  function loiTai(code: string, status: number): ApiFailureError {
    return new ApiFailureError(
      {
        success: false,
        data: null,
        error: { code, type: 'X', message: `msg:${code}`, messageParams: null, fieldErrors: null },
        traceId: 't',
      },
      status,
    );
  }

  async function dung(loi: ApiFailureError): Promise<HTMLElement> {
    service = jasmine.createSpyObj<PhanQuyenService>('PhanQuyenService', [
      'layMaTran',
      'luuMaTran',
    ]);
    service.layMaTran.and.returnValue(throwError(() => loi));
    await TestBed.configureTestingModule({
      imports: [MaTranPhanQuyenPage],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PhanQuyenService, useValue: service },
        { provide: AuthService, useValue: { coQuyen: () => true } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(MaTranPhanQuyenPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('lỗi thường (422) → hàng lỗi của Table: tiêu đề cố định + thân là câu dịch theo mã; không EmptyState', async () => {
    const goc = await dung(loiTai('CORE.PERMISSION.MA_THU', 422));

    expect(goc.querySelector('app-empty-state'))
      .withContext('không thay bảng bằng EmptyState')
      .toBeNull();
    const banner = goc.querySelector('app-table tbody app-notice-banner');
    expect(banner).withContext('hàng lỗi nằm TRONG Table').not.toBeNull();
    expect(banner?.querySelector('.notice-banner__tieu-de')?.textContent?.trim()).toBe(
      'phanQuyen.loi.taiThatBai',
    );
    expect(banner?.querySelector('.notice-banner__than')?.textContent?.trim()).toBe(
      'msg:CORE.PERMISSION.MA_THU',
    );
    expect(goc.querySelector('app-table thead th'))
      .withContext('<thead> giữ nguyên')
      .not.toBeNull();
  });

  it('lớp xuyên suốt (500) → tiêu đề vẫn có, KHÔNG thân (câu và traceId ở toast)', async () => {
    const goc = await dung(loiTai('CORE.SYSTEM.UNEXPECTED', 500));

    const banner = goc.querySelector('app-table tbody app-notice-banner');
    expect(banner?.querySelector('.notice-banner__tieu-de')?.textContent?.trim()).toBe(
      'phanQuyen.loi.taiThatBai',
    );
    expect(banner?.querySelector('.notice-banner__than')?.textContent?.trim()).toBe('');
  });

  it('bấm Thử lại ở hàng lỗi → gọi lại GET ma trận', async () => {
    const goc = await dung(loiTai('CORE.SYSTEM.UNEXPECTED', 500));
    service.layMaTran.and.returnValue(of(MA_TRAN));

    goc.querySelector<HTMLButtonElement>('app-table tbody app-button button')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(service.layMaTran).toHaveBeenCalledTimes(2);
    expect(goc.querySelector('app-table tbody app-notice-banner')).toBeNull();
  });
});

/** Dựng màn với ma trận cho trước; trả trang gốc và hàm vẽ lại. */
async function dungMaTran(
  service: jasmine.SpyObj<PhanQuyenService>,
  maTran: MaTranPhanQuyen,
): Promise<{ fixture: ComponentFixture<MaTranPhanQuyenPage>; veLai: () => Promise<void> }> {
  service.layMaTran.and.returnValue(of(maTran));
  await TestBed.configureTestingModule({
    imports: [MaTranPhanQuyenPage],
    providers: [
      provideZonelessChangeDetection(),
      { provide: PhanQuyenService, useValue: service },
      { provide: AuthService, useValue: { coQuyen: () => true } },
      provideTranslateService({
        lang: 'vi',
        fallbackLang: 'vi',
        loader: provideTranslateLoader(FakeTranslateLoader),
      }),
    ],
  }).compileComponents();
  const fixture = TestBed.createComponent(MaTranPhanQuyenPage);
  const veLai = async (): Promise<void> => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  };
  await veLai();
  return { fixture, veLai };
}

/**
 * Design/Screens/12 luồng lưu: "200 → Toast → GET lại → bỏ mọi đánh dấu đã đổi". Máy chủ đã nhận đúng
 * tập vừa gửi ngay khi PUT trả 200 — mốc "đã đổi" dời về tập đó NGAY, không đợi GET. GET sau lưu hỏng
 * thì hiện ở trạng thái `error` của `Table` (§Trạng thái "lỗi": tải hỏng, có Thử lại) — một chỗ, vì
 * `PhanQuyenService` tắt toast (fe-api-client.md §3). `version` trong tay đã cũ, nên Lưu khoá tới khi
 * một lần GET mang version mới về — gửi version cũ là nhận 409.
 */
describe('MaTranPhanQuyenPage — PUT 200 rồi GET tải lại sau lưu hỏng', () => {
  interface LuuTest {
    toggleO(permissionId: string, roleId: string, hang: Pick<MaTranHang, 'code'>): void;
    bamLuu(): void;
    huyThayDoi(): void;
    daCap(permissionId: string, roleId: string): boolean;
  }

  const HANG: Omit<MaTranHang, 'grantedRoleIds'> = {
    permissionId: 'p1',
    code: 'core.user.read',
    resourceKey: 'user',
    resourceNameKey: 'phanQuyen.taiNguyen.user',
    nameKey: 'phanQuyen.quyen.core.user.read',
  };

  function maTran(grantedRoleIds: string[], version: string): MaTranPhanQuyen {
    return {
      roles: [
        { id: 'r1', name: 'Kế toán', isSystem: false },
        { id: 'r2', name: 'Thủ kho', isSystem: false },
      ],
      rows: [{ ...HANG, grantedRoleIds }],
      version,
    };
  }

  let service: jasmine.SpyObj<PhanQuyenService>;
  let fixture: ComponentFixture<MaTranPhanQuyenPage>;
  let veLai: () => Promise<void>;
  let page: LuuTest;
  let thayDoi: UnsavedChangesService;

  beforeEach(async () => {
    service = jasmine.createSpyObj<PhanQuyenService>('PhanQuyenService', [
      'layMaTran',
      'luuMaTran',
    ]);
    ({ fixture, veLai } = await dungMaTran(service, maTran([], 'v1')));
    page = fixture.componentInstance as unknown as LuuTest;
    thayDoi = TestBed.inject(UnsavedChangesService);
  });

  function trang(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  /** [Huỷ thay đổi, Lưu ma trận] ở `PageHeader`. */
  function nutHeader(): HTMLButtonElement[] {
    return [...trang().querySelectorAll<HTMLButtonElement>('app-page-header app-button button')];
  }

  const LOI_GET_SAU_LUU: readonly [string, ApiFailureError, string][] = [
    ['mất kết nối (status 0)', new ApiFailureError(null, 0), 'loi.CORE.CLIENT.NO_CONNECTION'],
    [
      '429',
      new ApiFailureError(
        {
          success: false,
          data: null,
          error: {
            code: 'CORE.RATE_LIMIT.EXCEEDED',
            type: 'RateLimit',
            message: 'msg:CORE.RATE_LIMIT.EXCEEDED',
            messageParams: { RetryAfterSeconds: '30' },
            fieldErrors: null,
          },
          traceId: 't',
        },
        429,
      ),
      'msg:CORE.RATE_LIMIT.EXCEEDED',
    ],
  ];

  for (const [ten, loi, cau] of LOI_GET_SAU_LUU) {
    it(`GET sau lưu hỏng (${ten}) → cờ thay đổi về false, Lưu/Huỷ khoá, guard không hỏi; lỗi hiện ĐÚNG MỘT chỗ ở hàng lỗi của Table`, async () => {
      page.toggleO('p1', 'r1', HANG);
      service.luuMaTran.and.returnValue(of('v2'));
      service.layMaTran.and.returnValue(throwError(() => loi));

      page.bamLuu();
      await veLai();

      expect(thayDoi.coThayDoiChuaLuu()).withContext('rời trang không bị hỏi').toBeFalse();
      const [nutHuy, nutLuu] = nutHeader();
      expect(nutHuy?.disabled).withContext('Huỷ thay đổi khoá').toBeTrue();
      expect(nutLuu?.disabled).withContext('Lưu khoá').toBeTrue();

      expect(trang().querySelectorAll('.notice-banner--danger').length)
        .withContext('lỗi hiện đúng một chỗ')
        .toBe(1);
      const banner = trang().querySelector('app-table tbody app-notice-banner');
      expect(banner?.querySelector('.notice-banner__tieu-de')?.textContent?.trim()).toBe(
        'phanQuyen.loi.taiThatBai',
      );
      expect(banner?.querySelector('.notice-banner__than')?.textContent?.trim()).toBe(cau);

      page.huyThayDoi();
      expect(page.daCap('p1', 'r1'))
        .withContext('"Huỷ thay đổi" về đúng tập máy chủ vừa nhận')
        .toBeTrue();
    });
  }

  it('Lưu khoá tới khi có version mới; Thử lại ở hàng lỗi → GET mang version mới → lần lưu kế gửi version đó', async () => {
    page.toggleO('p1', 'r1', HANG);
    const put = new Subject<string>();
    service.luuMaTran.and.returnValue(put);
    page.bamLuu();
    // Tick thêm trong lúc PUT đang chạy: chưa lưu, guard phải còn thấy.
    page.toggleO('p1', 'r2', HANG);
    service.layMaTran.and.returnValue(throwError(() => new ApiFailureError(null, 0)));
    put.next('v2');
    put.complete();
    await veLai();

    expect(thayDoi.coThayDoiChuaLuu()).withContext('ô r2 chưa lưu').toBeTrue();
    expect(nutHeader()[1]?.disabled).withContext('version trong tay đã cũ').toBeTrue();
    page.bamLuu();
    expect(service.luuMaTran).withContext('không gửi PUT với version cũ').toHaveBeenCalledTimes(1);

    service.layMaTran.and.returnValue(of(maTran(['r1'], 'v3')));
    const nutThuLai = trang().querySelector<HTMLButtonElement>('app-table tbody app-button button');
    expect(nutThuLai).withContext('hàng lỗi có Thử lại').not.toBeNull();
    nutThuLai?.click();
    await veLai();

    expect(trang().querySelector('app-table tbody app-notice-banner')).toBeNull();
    expect(nutHeader()[1]?.disabled).toBeFalse();
    service.luuMaTran.and.returnValue(new Subject<string>());
    page.bamLuu();
    expect(service.luuMaTran.calls.mostRecent().args[0]).toEqual({
      version: 'v3',
      entries: [{ permissionId: 'p1', roleIds: ['r1', 'r2'] }],
    });
  });
});

/**
 * Design/Screens/12 §Sơ đồ bố cục: nhóm theo thứ tự xuất hiện đầu tiên trong `rows`, hàng trong nhóm
 * giữ thứ tự response; `Table` KHÔNG xếp lại (Table.md §Gom dòng theo nhóm luật 1), nên màn truyền
 * `rows` đã xếp. Hợp đồng không bảo đảm các hàng cùng `resourceKey` nằm liền nhau.
 */
describe('MaTranPhanQuyenPage — rows xen kẽ hai resourceKey', () => {
  function hang(permissionId: string, resourceKey: string, code: string): MaTranHang {
    return {
      permissionId,
      code,
      resourceKey,
      resourceNameKey: `resource.${resourceKey}`,
      nameKey: `permission.${code}`,
      grantedRoleIds: [],
    };
  }

  it('mỗi nhóm hiện một lần, theo thứ tự xuất hiện đầu tiên; hàng trong nhóm giữ thứ tự response', async () => {
    const service = jasmine.createSpyObj<PhanQuyenService>('PhanQuyenService', [
      'layMaTran',
      'luuMaTran',
    ]);
    const { fixture } = await dungMaTran(service, {
      roles: [{ id: 'r1', name: 'Kế toán', isSystem: false }],
      rows: [
        hang('p1', 'user', 'core.user.read'),
        hang('p2', 'role', 'core.role.read'),
        hang('p3', 'user', 'core.user.write'),
        hang('p4', 'role', 'core.role.write'),
      ],
      version: 'v1',
    });
    const goc = fixture.nativeElement as HTMLElement;

    const nhom = [...goc.querySelectorAll('app-table .table__hang-nhom th')].map((th) =>
      th.textContent?.trim(),
    );
    expect(nhom).toEqual(['user', 'role']);
    const thuTu = [...goc.querySelectorAll('app-table tbody tr:not(.table__hang-nhom)')].map((tr) =>
      tr.querySelector('td span:not(.sr-only)')?.textContent?.trim(),
    );
    expect(thuTu).toEqual([
      'core.user.read',
      'core.user.write',
      'core.role.read',
      'core.role.write',
    ]);
  });
});

/**
 * Design/Screens/12 "Áp lại sau 409" — mỗi ô ĐÃ ĐỔI là một giá trị đích; ô người dùng không chạm lấy
 * giá trị máy chủ mới. Cùng luật cho GET sau lưu: mốc lúc đó là tập vừa PUT 200, nên đích chỉ còn các
 * ô tick thêm trong lúc PUT chạy.
 */
describe('MaTranPhanQuyenPage — áp dữ liệu mới: chỉ ô đã đổi là giá trị đích', () => {
  interface ApTest {
    toggleO(permissionId: string, roleId: string, hang: Pick<MaTranHang, 'code'>): void;
    bamLuu(): void;
    taiLaiSauXungDot(): void;
    daCap(permissionId: string, roleId: string): boolean;
    daDoiO(permissionId: string, roleId: string): boolean;
    coDoi(): boolean;
  }

  const VAI_TRO = [
    { id: 'r1', name: 'Kế toán', isSystem: false },
    { id: 'r2', name: 'Thủ kho', isSystem: false },
  ];
  const HANG_DOC = { code: 'core.user.read' };

  function hang(permissionId: string, grantedRoleIds: string[]): MaTranHang {
    return {
      permissionId,
      code: `core.${permissionId}.read`,
      resourceKey: 'user',
      resourceNameKey: 'resource.user',
      nameKey: `permission.${permissionId}`,
      grantedRoleIds,
    };
  }

  const LOI_409 = new ApiFailureError(
    {
      success: false,
      data: null,
      error: {
        code: 'CORE.PERMISSION.VERSION_MISMATCH',
        type: 'Conflict',
        message: 'msg',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 't',
    },
    409,
  );

  let service: jasmine.SpyObj<PhanQuyenService>;
  let veLai: () => Promise<void>;
  let page: ApTest;
  let thayDoi: UnsavedChangesService;

  beforeEach(async () => {
    service = jasmine.createSpyObj<PhanQuyenService>('PhanQuyenService', [
      'layMaTran',
      'luuMaTran',
    ]);
    let fixture: ComponentFixture<MaTranPhanQuyenPage>;
    ({ fixture, veLai } = await dungMaTran(service, {
      roles: VAI_TRO,
      rows: [hang('p1', []), hang('p2', [])],
      version: 'v1',
    }));
    page = fixture.componentInstance as unknown as ApTest;
    thayDoi = TestBed.inject(UnsavedChangesService);
  });

  /** Người dùng tick p1×r1, bấm Lưu, nhận 409, rồi bấm "Tải lại ma trận" với dữ liệu `moi`. */
  async function tick409RoiTaiLai(moi: MaTranPhanQuyen): Promise<void> {
    page.toggleO('p1', 'r1', HANG_DOC);
    service.luuMaTran.and.returnValue(throwError(() => LOI_409));
    page.bamLuu();
    await veLai();
    service.layMaTran.and.returnValue(of(moi));
    page.taiLaiSauXungDot();
    await veLai();
  }

  it('(a) sau 409, ô người khác vừa đổi mà người dùng không chạm → giá trị máy chủ, không dấu "đã đổi", PUT kế tiếp không mang giá trị cũ', async () => {
    await tick409RoiTaiLai({
      roles: VAI_TRO,
      rows: [hang('p1', []), hang('p2', ['r1'])],
      version: 'v2',
    });

    expect(page.daCap('p2', 'r1')).withContext('giá trị máy chủ mới').toBeTrue();
    expect(page.daDoiO('p2', 'r1')).withContext('ô không chạm không mang dấu').toBeFalse();
    expect(page.daCap('p1', 'r1')).withContext('ô đã đổi giữ giá trị đích').toBeTrue();
    expect(page.daDoiO('p1', 'r1')).toBeTrue();

    service.luuMaTran.and.returnValue(new Subject<string>());
    page.bamLuu();
    expect(service.luuMaTran.calls.mostRecent().args[0]).toEqual({
      version: 'v2',
      entries: [
        { permissionId: 'p1', roleIds: ['r1'] },
        { permissionId: 'p2', roleIds: ['r1'] },
      ],
    });
  });

  it('(b) ô đích đã bằng giá trị mới → bỏ dấu; người khác đổi thêm một ô cùng hàng → ô đó theo máy chủ; không còn thay đổi nào', async () => {
    await tick409RoiTaiLai({
      roles: VAI_TRO,
      rows: [hang('p1', ['r1', 'r2']), hang('p2', [])],
      version: 'v2',
    });

    expect(page.daCap('p1', 'r1')).toBeTrue();
    expect(page.daDoiO('p1', 'r1')).withContext('đích trùng giá trị mới → bỏ dấu').toBeFalse();
    expect(page.daCap('p1', 'r2')).withContext('ô không chạm theo máy chủ').toBeTrue();
    expect(page.daDoiO('p1', 'r2')).toBeFalse();
    expect(page.coDoi()).toBeFalse();
    expect(thayDoi.coThayDoiChuaLuu()).toBeFalse();
  });

  it('hàng quyền hoặc cột vai trò không còn → thay đổi đó rơi', async () => {
    page.toggleO('p2', 'r2', HANG_DOC);
    await tick409RoiTaiLai({
      roles: [VAI_TRO[0]],
      rows: [hang('p2', [])],
      version: 'v2',
    });

    expect(page.daCap('p2', 'r2')).toBeFalse();
    expect(page.coDoi()).toBeFalse();
  });

  it('(c) GET sau lưu mang một thay đổi của người khác → hiện giá trị máy chủ, coDoi() false', async () => {
    page.toggleO('p1', 'r1', HANG_DOC);
    service.luuMaTran.and.returnValue(of('v2'));
    service.layMaTran.and.returnValue(
      of({ roles: VAI_TRO, rows: [hang('p1', ['r1']), hang('p2', ['r2'])], version: 'v3' }),
    );

    page.bamLuu();
    await veLai();

    expect(page.daCap('p2', 'r2')).withContext('thay đổi của người khác hiện ra').toBeTrue();
    expect(page.daDoiO('p2', 'r2')).toBeFalse();
    expect(page.coDoi()).toBeFalse();
    expect(thayDoi.coThayDoiChuaLuu()).toBeFalse();
  });
});
