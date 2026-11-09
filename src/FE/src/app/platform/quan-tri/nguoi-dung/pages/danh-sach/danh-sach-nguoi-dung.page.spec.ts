import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { inject, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslateService,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { ApiFailure, ApiFailureError } from '../../../../../core/http/api-result.model';
import { PagedList } from '../../../../../core/http/paged.model';
import { AutocompleteComponent } from '../../../../../shared/ui/autocomplete/autocomplete.component';
import {
  CoreScreenExtensions,
  ScreenExtColumn,
  provideCoreScreenExt,
} from '../../../../config/core-screen-ext';
import { VaiTro } from '../../../vai-tro/models/vai-tro.model';
import { VaiTroService } from '../../../vai-tro/services/vai-tro.service';
import { NguoiDung } from '../../models/nguoi-dung.model';
import { NguoiDungService } from '../../services/nguoi-dung.service';
import { DanhSachNguoiDungPage } from './danh-sach-nguoi-dung.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const NGUOI_DUNG: NguoiDung = {
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
const TRANG: PagedList<NguoiDung> = { items: [NGUOI_DUNG], page: 1, pageSize: 20, totalCount: 1 };

function loiApi(code: string, status = 0): ApiFailureError {
  const body: ApiFailure = {
    success: false,
    data: null,
    error: {
      code,
      type: 'BusinessRule',
      message: `msg:${code}`,
      messageParams: null,
      fieldErrors: null,
    },
    traceId: 't',
  };
  return new ApiFailureError(body, status);
}

/** Bề mặt protected của page mà spec cần chạm. */
interface PageTest {
  tao: {
    moHop(): void;
    form: {
      setValue(v: {
        userName: string;
        email: string;
        fullName: string;
        tempPassword: string;
        roleIds: readonly string[];
      }): void;
    };
  };
  guiTao(): void;
}

/**
 * Nối page ↔ TaoNguoiDungStore ↔ HopTaoNguoiDungComponent bằng DOM thật (ADR-0049 điều 4): component
 * hộp là dumb và OnPush, nên chỉ có chỗ này chứng minh page truyền ĐÚNG giá trị xuống, và lỗi server
 * gắn vào control sau khi gửi thật sự HIỆN trên màn.
 */
describe('DanhSachNguoiDungPage — hộp "Thêm người dùng" nối store với component dumb', () => {
  let fixture: ComponentFixture<DanhSachNguoiDungPage>;
  let service: jasmine.SpyObj<NguoiDungService>;
  let page: PageTest;

  beforeEach(async () => {
    service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['danhSach', 'tao']);
    service.danhSach.and.returnValue(of(TRANG));
    const auth = {
      nguoiDung: signal({ id: 'nguoi-khac', tenantName: 'Cty A' }),
      coQuyen: () => true,
    };

    await TestBed.configureTestingModule({
      imports: [DanhSachNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: NguoiDungService, useValue: service },
        { provide: AuthService, useValue: auth },
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
    page = fixture.componentInstance as unknown as PageTest;
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  }

  function dienFormHopLe(): void {
    page.tao.form.setValue({
      userName: 'an.nguyen',
      email: 'an@b.vn',
      fullName: 'Nguyễn An',
      tempPassword: 'Tam-Pass-1',
      roleIds: [],
    });
  }

  it('điểm xuất phát: danh sách được tải một lần, hộp đóng', () => {
    expect(service.danhSach).toHaveBeenCalledTimes(1);
    expect(document.getElementById('nd-tao-ten')).toBeNull();
  });

  it('mở hộp → component hiện form; USERNAME_DUPLICATED → câu lỗi hiện ở ô tên đăng nhập', async () => {
    service.tao.and.returnValue(throwError(() => loiApi('CORE.USER.USERNAME_DUPLICATED')));
    page.tao.moHop();
    await veLai();
    expect(document.getElementById('nd-tao-ten')).not.toBeNull();

    dienFormHopLe();
    page.guiTao();
    await veLai();

    // Bảng dịch rỗng → câu là đường lùi của `dichLoi`: câu BE gửi kèm, không phải khoá dịch thô.
    expect(document.body.textContent).toContain('msg:CORE.USER.USERNAME_DUPLICATED');
    expect(document.body.textContent).not.toContain('loi.CORE.USER.USERNAME_DUPLICATED');
  });

  it('bấm gửi khi form trống → service không được gọi, ô sai hiện lỗi bắt buộc', async () => {
    page.tao.moHop();
    await veLai();

    page.guiTao();
    await veLai();

    expect(service.tao).not.toHaveBeenCalled();
    expect(document.body.textContent).toContain('loi.CORE.CLIENT.VALIDATION_REQUIRED');
  });

  it('tạo thành công → hộp đóng và danh sách được tải lại', async () => {
    service.tao.and.returnValue(of({ id: 'u2' }));
    page.tao.moHop();
    await veLai();
    dienFormHopLe();

    page.guiTao();
    await veLai();

    expect(service.tao).toHaveBeenCalledTimes(1);
    expect(service.danhSach).toHaveBeenCalledTimes(2);
  });
});

/**
 * fe-architecture.md §2.7 (ADR-0057) luật 2, 4 và Design/Screens/10-nguoi-dung.md §Điểm mở rộng.
 * Dự án khai ở composition root qua `provideCoreScreenExt`: tiêu đề là KHOÁ i18n (màn dịch), ô là
 * `value(row)` — không TemplateRef, không chuỗi đã dịch; `factory` chạy trong ngữ cảnh tiêm.
 */
describe('DanhSachNguoiDungPage — seam CORE_SCREEN_EXT khoá "users"', () => {
  /** Bảng dịch có khoá của dự án — chứng minh màn DỊCH `headerKey`, không in khoá thô. */
  class DuAnTranslateLoader extends TranslateLoader {
    getTranslation(): Observable<TranslationObject> {
      return of({ duAn: { cot: { maNhanVien: 'Mã nhân viên' } } });
    }
  }

  async function dung(khai: (() => CoreScreenExtensions) | null): Promise<HTMLElement> {
    const service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['danhSach']);
    service.danhSach.and.returnValue(of(TRANG));
    await TestBed.configureTestingModule({
      imports: [DanhSachNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: NguoiDungService, useValue: service },
        {
          provide: AuthService,
          useValue: { nguoiDung: signal({ id: 'x', tenantName: 'Cty A' }), coQuyen: () => true },
        },
        {
          provide: VaiTroService,
          useValue: jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem', 'chiTiet']),
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(DuAnTranslateLoader),
        }),
        ...(khai ? [provideCoreScreenExt(khai)] : []),
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(DanhSachNguoiDungPage);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function tieuDeCot(el: HTMLElement): string[] {
    return Array.from(el.querySelectorAll('thead th:not(.data-table__o-hanh-dong)')).map(
      (th) => th.textContent?.trim() ?? '',
    );
  }

  const cotMaNv: ScreenExtColumn<NguoiDung> = {
    key: 'maNhanVien',
    headerKey: 'duAn.cot.maNhanVien',
    value: (nd) => `MNV-${nd.userName}`,
  };

  it('dự án khai cột cho "users" → cột hiện SAU "Ngày tạo", tiêu đề ĐÃ DỊCH từ headerKey, ô là value(row)', async () => {
    const el = await dung(() => ({ users: { columns: [cotMaNv] } }));

    const cot = tieuDeCot(el);
    expect(cot.length).toBe(7);
    expect(cot[6]).toBe('Mã nhân viên');
    expect(cot[5]).toBe('nguoiDung.cot.ngayTao');
    expect(el.querySelector('tbody')?.textContent).toContain('MNV-an.nguyen');
  });

  it('cột của dự án không sắp xếp được — <th> của nó không có nút sắp xếp', async () => {
    const el = await dung(() => ({ users: { columns: [cotMaNv] } }));

    const th = el.querySelectorAll('thead th')[6];
    expect(th.querySelector('button')).toBeNull();
    expect(th.getAttribute('aria-sort')).toBeNull();
  });

  it('factory chạy trong ngữ cảnh tiêm → value(row) đọc được service của dự án qua inject()', async () => {
    const el = await dung(() => {
      const dich = inject(TranslateService);
      return {
        users: {
          columns: [{ ...cotMaNv, value: (nd) => `${dich.currentLang()}:${nd.userName}` }],
        },
      };
    });
    expect(el.querySelector('tbody')?.textContent).toContain('vi:an.nguyen');
  });

  it('khai cho màn KHÁC ("roles") → màn người dùng giữ nguyên sáu cột gốc', async () => {
    const el = await dung(() => ({
      roles: { columns: [{ key: 'x', headerKey: 'duAn.cot.maNhanVien', value: () => 'x' }] },
    }));
    expect(tieuDeCot(el).length).toBe(6);
    expect(tieuDeCot(el)).not.toContain('Mã nhân viên');
  });

  it('không khai CORE_SCREEN_EXT → màn chạy như cũ, sáu cột gốc, không lỗi', async () => {
    const el = await dung(null);
    expect(tieuDeCot(el).length).toBe(6);
  });

  /**
   * V-03 — fe-architecture.md §2.7 luật 6 (ADR-0084): khoá cột trùng là lỗi cấu hình, màn ném `Error`
   * lúc DỰNG, ở dev lẫn production — không bỏ qua cột, không đè cột Core, không để tới DataTable.
   */
  it('V-03: cột dự án trùng khoá cột Core ("email") → dựng màn ném Error nêu mã màn và khoá', async () => {
    await expectAsync(
      dung(() => ({ users: { columns: [{ ...cotMaNv, key: 'email' }] } })),
    ).toBeRejectedWithError(Error, /"users"[\s\S]*"email"/);
  });

  it('V-03: hai cột dự án trùng khoá nhau → dựng màn ném Error nêu mã màn và khoá', async () => {
    await expectAsync(
      dung(() => ({ users: { columns: [cotMaNv, { ...cotMaNv, headerKey: 'duAn.cot.khac' }] } })),
    ).toBeRejectedWithError(Error, /"users"[\s\S]*"maNhanVien"/);
  });

  it('V-03: mọi khoá cột Core ĐANG VẼ đều nằm trong phép kiểm — trùng khoá nào cũng ném', async () => {
    // Tập khoá lấy từ chính bảng đã dựng, không chép tay: Core thêm cột mà quên đưa vào phép kiểm
    // thì ca này đỏ (đường "Core nâng cấp thêm cột" của ADR-0084).
    await dung(null);
    const fixture = TestBed.createComponent(DanhSachNguoiDungPage);
    fixture.detectChanges();
    const trang = fixture.componentInstance as unknown as { columns(): readonly { key: string }[] };
    const cacKhoa = trang.columns().map((c) => c.key);
    expect(cacKhoa.length).withContext('sáu cột gốc — không khai seam').toBe(6);

    for (const khoa of cacKhoa) {
      TestBed.resetTestingModule();
      await expectAsync(dung(() => ({ users: { columns: [{ ...cotMaNv, key: khoa }] } })))
        .withContext(khoa)
        .toBeRejectedWithError(Error, new RegExp(`"${khoa}"`));
    }
  });
});

/**
 * fe-architecture.md §2.8 luật 1 (URL là nguồn sự thật) và luật 5 (đổi bộ lọc thêm một bước lịch
 * sử). Router TÁI DÙNG page khi chỉ query param đổi, nên Back/Forward không dựng lại màn: ô lọc và
 * chip phải theo URL MỖI LẦN nó đổi, không nạp một lần lúc dựng. Back/Forward đi qua `Location` của
 * Router (popstate giả của `provideLocationMocks`), không gọi tay `navigateByUrl` — nên các ca này
 * khoá luôn luật 5: đổi lọc mà thay URL tại chỗ thì Back không về được giá trị lọc trước.
 */
describe('DanhSachNguoiDungPage — Back/Forward giữa hai giá trị lọc', () => {
  const KE_TOAN: VaiTro = {
    id: 'r-ke-toan',
    name: 'Kế toán',
    isSystem: false,
    userCount: 1,
    createdAt: null,
    version: 'v-1',
  };
  const THU_KHO: VaiTro = { ...KE_TOAN, id: 'r-thu-kho', name: 'Thủ kho' };

  let fixture: ComponentFixture<DanhSachNguoiDungPage>;
  let location: Location;
  let service: jasmine.SpyObj<NguoiDungService>;
  let vaiTroService: jasmine.SpyObj<VaiTroService>;

  async function dung(
    urlDau: string,
    coQuyen: (ma: string) => boolean = () => true,
  ): Promise<void> {
    service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['danhSach']);
    service.danhSach.and.returnValue(of(TRANG));
    vaiTroService = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem', 'chiTiet']);
    vaiTroService.chiTiet.and.callFake((id: string) => {
      const vt = [KE_TOAN, THU_KHO].find((v) => v.id === id);
      return vt ? of(vt) : throwError(() => loiApi('CORE.ROLE.NOT_FOUND'));
    });

    TestBed.configureTestingModule({
      imports: [DanhSachNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideLocationMocks(),
        { provide: NguoiDungService, useValue: service },
        {
          provide: AuthService,
          useValue: { nguoiDung: signal({ id: 'x', tenantName: 'Cty A' }), coQuyen },
        },
        { provide: VaiTroService, useValue: vaiTroService },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    });
    const router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    // Ngoài TestBed, bootstrap của app gắn listener này; thiếu nó thì popstate không tới Router.
    router.setUpLocationChangeListener();
    await router.navigateByUrl(urlDau);
    fixture = TestBed.createComponent(DanhSachNguoiDungPage);
    await onDinh();
  }

  /** Popstate tới Router sau một nhịp `setTimeout`; điều hướng là pending task nên `whenStable` chờ nó. */
  async function onDinh(): Promise<void> {
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function quayLai(): Promise<void> {
    location.back();
    await onDinh();
  }

  async function diToi(): Promise<void> {
    location.forward();
    await onDinh();
  }

  // Panel lọc là Drawer gắn vào <body>: mặt nạ chỉ gỡ khi hoạt ảnh rời kết thúc, nên fixture bị huỷ
  // lúc panel còn mở/đang đóng sẽ để mặt nạ rò sang spec sau (chặn `elementFromPoint` của chúng).
  afterEach(() => {
    fixture?.destroy();
    document.querySelectorAll('.p-drawer-mask').forEach((m) => m.remove());
  });

  const ID_PANEL = 'nguoi-dung-bang-loc';

  /** Panel lọc nằm trong Drawer gắn vào <body>, không nằm dưới `fixture.nativeElement`. */
  function trongPanel<T extends HTMLElement>(chon: string): T | null {
    return document.body.querySelector<T>(`#${ID_PANEL} ${chon}`);
  }

  async function moPanel(): Promise<void> {
    if (document.getElementById(ID_PANEL)) return;
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('.toolbar__nut-loc button')!
      .click();
    await onDinh();
  }

  async function dongPanel(): Promise<void> {
    document.body.querySelector<HTMLButtonElement>('.p-drawer-close-button button')?.click();
    await onDinh();
  }

  async function apDungPanel(): Promise<void> {
    document.body
      .querySelector<HTMLButtonElement>(`button[type=submit][form="${ID_PANEL}-form"]`)!
      .click();
    await onDinh();
  }

  /** Giá trị ô "Trạng thái" như người dùng thấy khi mở panel; đóng panel lại sau khi đọc. */
  async function docTrangThai(): Promise<string> {
    await moPanel();
    const gt = trongPanel<HTMLSelectElement>('select')?.value ?? '';
    await dongPanel();
    return gt;
  }

  async function chonTrangThai(gt: string): Promise<void> {
    await moPanel();
    const o = trongPanel<HTMLSelectElement>('select');
    if (!o) throw new Error('không thấy ô lọc trạng thái');
    o.value = gt;
    o.dispatchEvent(new Event('change'));
    await onDinh();
    await apDungPanel();
  }

  /** Gõ tìm rồi chọn một mục trong panel — hai output của Autocomplete, rồi bấm Áp dụng. */
  async function chonVaiTro(vt: VaiTro): Promise<void> {
    await moPanel();
    vaiTroService.timKiem.and.returnValue(
      of({ items: [vt], page: 1, pageSize: 20, totalCount: 1 }),
    );
    const o = fixture.debugElement.query(By.directive(AutocompleteComponent))
      .componentInstance as AutocompleteComponent;
    o.search.emit(vt.name);
    await onDinh();
    o.valueChange.emit(vt.id);
    await onDinh();
    await apDungPanel();
  }

  /** Ô nhập mà PrimeNG vẽ trong Autocomplete — nơi người dùng đọc nhãn của vai trò đang lọc. */
  async function docLocVaiTro(): Promise<string> {
    await moPanel();
    const o = trongPanel<HTMLInputElement>('app-autocomplete input');
    if (!o) throw new Error('không thấy ô lọc vai trò');
    const gt = o.value;
    await dongPanel();
    return gt;
  }

  function chuChip(): string {
    return (
      (fixture.nativeElement as HTMLElement).querySelector('.toolbar__chips')?.textContent ?? ''
    );
  }

  function locLanTaiCuoi(): Readonly<Record<string, string>> {
    return service.danhSach.calls.mostRecent().args[0].filters ?? {};
  }

  it('vai trò: lọc "Kế toán" rồi "Thủ kho", Back → lưới VÀ chip cùng là Kế toán; Forward → Thủ kho', async () => {
    await dung('/');
    await chonVaiTro(KE_TOAN);
    await chonVaiTro(THU_KHO);
    expect(location.path()).toBe('/?roleId=r-thu-kho');
    expect(chuChip()).toContain('Thủ kho');

    await quayLai();
    expect(location.path())
      .withContext('luật 5 — đổi lọc là một bước lịch sử')
      .toBe('/?roleId=r-ke-toan');
    expect(locLanTaiCuoi()['roleId']).toBe('r-ke-toan');
    expect(chuChip()).toContain('Kế toán');
    expect(chuChip()).not.toContain('Thủ kho');

    await diToi();
    expect(location.path()).toBe('/?roleId=r-thu-kho');
    expect(chuChip()).toContain('Thủ kho');
    expect(chuChip()).not.toContain('Kế toán');

    await quayLai();
    await quayLai();
    expect(location.path()).toBe('/');
    expect(locLanTaiCuoi()['roleId']).toBeUndefined();
    expect(chuChip()).not.toContain('Kế toán');
  });

  /**
   * Design/Screens/10-nguoi-dung.md, dòng "Nhãn chip lọc Vai trò": URL mở sẵn `roleId` thì tên tra
   * được dùng cho chip VÀ ô lọc. Autocomplete chỉ biết nhãn của khoá từng có trong `options()`, nên
   * thiếu bước nạp tên thì ô lọc hiện id thô.
   */
  it('vai trò: mở liên kết có sẵn roleId → ô lọc và chip cùng hiện TÊN, không hiện id thô; Back giữ tên', async () => {
    await dung('/?roleId=r-ke-toan');
    expect(await docLocVaiTro()).toBe('Kế toán');
    expect(chuChip()).toContain('Kế toán');

    await chonVaiTro(THU_KHO);
    expect(await docLocVaiTro()).toBe('Thủ kho');

    await quayLai();
    expect(location.path()).toBe('/?roleId=r-ke-toan');
    expect(await docLocVaiTro()).toBe('Kế toán');
    expect(chuChip()).toContain('Kế toán');
  });

  it('vai trò: Back tới roleId mà tra tên hỏng → không vẽ chip, tên của lần lọc sau KHÔNG ở lại', async () => {
    await dung('/');
    await chonVaiTro(KE_TOAN);
    await chonVaiTro(THU_KHO);
    vaiTroService.chiTiet.and.returnValue(throwError(() => loiApi('CORE.ROLE.NOT_FOUND')));

    await quayLai();
    expect(location.path()).toBe('/?roleId=r-ke-toan');
    expect(chuChip()).not.toContain('Thủ kho');
    expect(chuChip()).not.toContain('Kế toán');
  });

  it('trạng thái: "Hoạt động" rồi "Đã khoá", Back → ô chọn ghi đúng giá trị lưới đang lọc; Forward còn nguyên', async () => {
    await dung('/');
    await chonTrangThai('active');
    await chonTrangThai('locked');
    expect(location.path()).toBe('/?status=locked');

    await quayLai();
    expect(location.path())
      .withContext('luật 5 — đổi lọc là một bước lịch sử')
      .toBe('/?status=active');
    expect(locLanTaiCuoi()['status']).toBe('active');
    expect(await docTrangThai()).toBe('active');

    await quayLai();
    expect(location.path()).toBe('/');
    expect(locLanTaiCuoi()['status']).toBeUndefined();
    expect(await docTrangThai()).toBe('');

    // Đồng bộ ô chọn theo URL không được tự ghi URL lần nữa — nếu có, nhánh Forward đã bị cắt.
    await diToi();
    expect(location.path()).toBe('/?status=active');
    expect(await docTrangThai()).toBe('active');
  });

  it('trạng thái: chọn "Đã khoá", Back về "Tất cả", chọn lại "Đã khoá" → lưới lọc lại theo Đã khoá', async () => {
    await dung('/');
    await chonTrangThai('locked');
    await quayLai();
    expect(await docTrangThai()).toBe('');

    await chonTrangThai('locked');
    expect(location.path()).toBe('/?status=locked');
    expect(locLanTaiCuoi()['status']).toBe('locked');
    expect(await docTrangThai()).toBe('locked');
  });

  it('thiếu core.role.read: roleId trên URL bị gỡ khi vào; Back về URL đó → roleId vẫn KHÔNG gửi lên', async () => {
    await dung('/?roleId=r-ke-toan', (ma) => ma !== 'core.role.read');
    expect(location.path()).toBe('/');

    await quayLai();
    expect(location.path()).toBe('/?roleId=r-ke-toan');
    const coGuiRoleId = service.danhSach.calls
      .allArgs()
      .some(([q]) => q.filters?.['roleId'] !== undefined);
    expect(coGuiRoleId)
      .withContext('Design/Screens/10-nguoi-dung.md — thiếu quyền thì "không gửi lên"')
      .toBeFalse();
    expect(vaiTroService.chiTiet).not.toHaveBeenCalled();
  });
});

/**
 * fe-architecture.md §2.8 luật 10 và Design/Screens/10-nguoi-dung.md, dòng errorTemplate: nút của
 * khối lỗi theo `list.errorCode()`, KHÔNG theo `roleId` đang gửi — đang lọc vai trò mà lỗi là mất
 * mạng hay 5xx vẫn phải ra "Thử lại", chỉ CORE.USER.ROLE_NOT_FOUND mới ra "Xoá bộ lọc".
 */
describe('DanhSachNguoiDungPage — khối lỗi tải: nút theo errorCode (luật 10)', () => {
  let fixture: ComponentFixture<DanhSachNguoiDungPage>;
  let service: jasmine.SpyObj<NguoiDungService>;

  async function dung(urlDau: string, loi: ApiFailureError): Promise<void> {
    service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['danhSach']);
    service.danhSach.and.returnValue(throwError(() => loi));
    const vaiTroService = jasmine.createSpyObj<VaiTroService>('VaiTroService', [
      'timKiem',
      'chiTiet',
    ]);
    vaiTroService.chiTiet.and.returnValue(throwError(() => loiApi('CORE.ROLE.NOT_FOUND')));

    await TestBed.configureTestingModule({
      imports: [DanhSachNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: NguoiDungService, useValue: service },
        {
          provide: AuthService,
          useValue: { nguoiDung: signal({ id: 'x', tenantName: 'Cty A' }), coQuyen: () => true },
        },
        { provide: VaiTroService, useValue: vaiTroService },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();

    const router = TestBed.inject(Router);
    await router.navigateByUrl(urlDau);
    fixture = TestBed.createComponent(DanhSachNguoiDungPage);
    await onDinh();
  }

  /** Cùng nhịp chờ với describe Back/Forward — popstate/điều hướng là pending task. */
  async function onDinh(): Promise<void> {
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  }

  function nutHanhDong(): HTMLButtonElement {
    const b = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.empty-state button.nut',
    );
    if (!b) throw new Error('không thấy nút hành động của khối lỗi');
    return b;
  }

  it('đang lọc vai trò, nhận CORE.USER.ROLE_NOT_FOUND → nút "Xoá bộ lọc", bấm gỡ roleId khỏi truy vấn', async () => {
    await dung('/?roleId=r-ke-toan', loiApi('CORE.USER.ROLE_NOT_FOUND'));

    expect(nutHanhDong().textContent).toContain('chung.xoaBoLoc');
    expect(nutHanhDong().textContent).not.toContain('chung.thuLai');

    nutHanhDong().click();
    await onDinh();

    const goiCuoi = service.danhSach.calls.mostRecent().args[0];
    expect(goiCuoi.filters?.['roleId']).toBeUndefined();
  });

  it('đang lọc vai trò, nhận lỗi mất kết nối (không envelope) → nút vẫn "Thử lại"', async () => {
    await dung('/?roleId=r-ke-toan', new ApiFailureError(null, 0));

    expect(nutHanhDong().textContent).toContain('chung.thuLai');
    expect(nutHanhDong().textContent).not.toContain('chung.xoaBoLoc');
  });

  it('đang lọc vai trò, nhận 5xx mang mã khác ROLE_NOT_FOUND → nút vẫn "Thử lại"', async () => {
    await dung('/?roleId=r-ke-toan', loiApi('CORE.SYSTEM.UNEXPECTED', 500));

    expect(nutHanhDong().textContent).toContain('chung.thuLai');
    expect(nutHanhDong().textContent).not.toContain('chung.xoaBoLoc');
  });
});
