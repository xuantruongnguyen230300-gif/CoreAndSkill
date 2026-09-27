import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject, defer, of, throwError } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import {
  ApiFailure,
  ApiFailureError,
  ApiFieldError,
} from '../../../../../core/http/api-result.model';
import { ToastService } from '../../../../../core/toast/toast.service';
import { VaiTroService } from '../../../vai-tro/services/vai-tro.service';
import { NguoiDung } from '../../models/nguoi-dung.model';
import { HopSuaNguoiDungComponent } from '../../components/hop-sua-nguoi-dung/hop-sua-nguoi-dung.component';
import { NguoiDungService } from '../../services/nguoi-dung.service';
import { ChiTietNguoiDungPage } from './chi-tiet-nguoi-dung.page';

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

function loiApi(
  code: string,
  fieldErrors: Record<string, readonly ApiFieldError[]> | null = null,
): ApiFailureError {
  const body: ApiFailure = {
    success: false,
    data: null,
    error: { code, type: 'BusinessRule', message: `msg:${code}`, messageParams: null, fieldErrors },
    traceId: 't',
  };
  return new ApiFailureError(body);
}

/** Bề mặt protected của page mà spec cần chạm. */
interface PageTest {
  moHopSua(): void;
  moHopGanVaiTro(): void;
  guiSua(): void;
  guiDatLai(): void;
  guiGanVaiTro(): void;
  ganVaiTro: { chon(v: readonly string[]): void };
  hop: { moHopDatLai(): void; formDatLai: { setValue(v: { tempPassword: string }): void } };
}

/**
 * Page nối store ↔ component hộp qua `input()` trên các component OnPush. Đây là chỗ một lỗi CHỈ
 * hiện ra khi chạy nối cả chuỗi: lỗi server gắn vào control (không phải signal) sau khi gửi —
 * nếu chuỗi nối làm giá trị câu lỗi không tới được component, ô không hiện gì dù store đúng.
 */
describe('ChiTietNguoiDungPage — lỗi của hộp thoại phải HIỆN trên màn (store → page → component OnPush)', () => {
  let fixture: ComponentFixture<ChiTietNguoiDungPage>;
  let service: jasmine.SpyObj<NguoiDungService>;
  let page: PageTest;
  let toast: ToastService;

  beforeEach(async () => {
    service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', [
      'chiTiet',
      'suaThongTin',
      'datLaiMatKhau',
      'ganVaiTro',
      'khoa',
      'moKhoa',
    ]);
    service.chiTiet.and.returnValue(of(NGUOI_DUNG));
    const auth = {
      nguoiDung: signal({ id: 'nguoi-khac' }),
      coQuyen: () => true,
    };

    await TestBed.configureTestingModule({
      imports: [ChiTietNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: NguoiDungService, useValue: service },
        { provide: AuthService, useValue: auth },
        {
          provide: VaiTroService,
          useValue: jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']),
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'u1' }) } },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();

    toast = TestBed.inject(ToastService);
    fixture = TestBed.createComponent(ChiTietNguoiDungPage);
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

  it('điểm xuất phát: chi tiết được tải và hộp Sửa đóng', () => {
    expect(service.chiTiet).toHaveBeenCalledTimes(1);
    expect(document.getElementById('ndsua-email')).toBeNull();
  });

  it('Sửa → EMAIL_DUPLICATED: câu lỗi hiện ở ô email của hộp, hộp giữ mở', async () => {
    service.suaThongTin.and.returnValue(throwError(() => loiApi('CORE.USER.EMAIL_DUPLICATED')));
    page.moHopSua();
    await veLai();

    expect(document.getElementById('ndsua-email')).not.toBeNull();

    page.guiSua();
    await veLai();

    // Bảng dịch rỗng → câu là đường lùi của `dichLoi`: câu BE gửi kèm, không phải khoá dịch thô.
    expect(document.body.textContent).toContain('msg:CORE.USER.EMAIL_DUPLICATED');
    expect(document.body.textContent).not.toContain('loi.CORE.USER.EMAIL_DUPLICATED');
  });

  it('mở hộp Gán vai trò → component hiện ô chọn vai trò và chỉ hộp đó mở', async () => {
    page.moHopGanVaiTro();
    await veLai();

    expect(document.getElementById('ndgan-vai-tro')).not.toBeNull();
    expect(document.getElementById('ndsua-email')).toBeNull();
    expect(document.getElementById('nddat-mk')).toBeNull();
  });

  it('Sửa → xung đột đồng thời: banner xung đột với nút Thử lại hiện trong hộp', async () => {
    service.suaThongTin.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
    page.moHopSua();
    await veLai();

    page.guiSua();
    await veLai();

    expect(document.body.textContent).toContain('loi.CORE.CONCURRENCY.CONFLICT');
    expect(document.body.textContent).toContain('chung.thuLai');
  });

  it('Đặt lại mật khẩu → RESET_PASSWORD_FAILED (khoá khớp): câu lỗi hiện ở ô mật khẩu tạm', async () => {
    service.datLaiMatKhau.and.returnValue(
      throwError(() =>
        loiApi('CORE.USER.RESET_PASSWORD_FAILED', {
          TempPassword: [{ code: 'CORE.USER.PASSWORD_TOO_WEAK', messageParams: null }],
        }),
      ),
    );
    page.hop.moHopDatLai();
    await veLai();
    page.hop.formDatLai.setValue({ tempPassword: 'yeu' });

    page.guiDatLai();
    await veLai();

    expect(document.body.textContent).toContain('loi.CORE.USER.PASSWORD_TOO_WEAK');
  });

  // ---- Nối (binding) giữa template và store: gỡ một dòng binding thì ca tương ứng đỏ ----

  /** Nút thật trong DOM theo nhãn (khoá dịch — loader giả trả nguyên khoá). */
  function nut(nhan: string, goc: ParentNode = document): HTMLButtonElement {
    const thay = Array.from(goc.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === nhan,
    );
    if (!thay) throw new Error(`Không có nút "${nhan}" trong DOM`);
    return thay;
  }

  /** Dựng lại trang với dữ liệu chi tiết khác — bỏ bản dựng của beforeEach để DOM không bị đôi. */
  function dungLai(): void {
    fixture.destroy();
    service.chiTiet.calls.reset();
    fixture = TestBed.createComponent(ChiTietNguoiDungPage);
    page = fixture.componentInstance as unknown as PageTest;
  }

  const hopXacNhan = (): HTMLElement | null => document.querySelector('.confirm-dialog');

  it('Đặt lại mật khẩu (form MỘT ô): Enter hai lần liên tiếp = hai sự kiện submit → chỉ MỘT request', async () => {
    service.datLaiMatKhau.and.returnValue(new Subject<void>());
    page.hop.moHopDatLai();
    await veLai();
    page.hop.formDatLai.setValue({ tempPassword: 'Tam-Pass-1' });
    const form = document.getElementById('nddat-mk')?.closest('form');
    expect(form).withContext('form của hộp Đặt lại').toBeTruthy();

    form!.dispatchEvent(new Event('submit', { cancelable: true }));
    form!.dispatchEvent(new Event('submit', { cancelable: true }));

    expect(service.datLaiMatKhau).toHaveBeenCalledTimes(1);
  });

  it('Sửa → xung đột → bấm nút "Thử lại" thật: tải lại chi tiết và đóng hộp (nối (thuLai))', async () => {
    service.suaThongTin.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
    page.moHopSua();
    await veLai();
    page.guiSua();
    await veLai();
    expect(service.chiTiet).toHaveBeenCalledTimes(1);

    nut('chung.thuLai').click();
    await veLai();

    expect(service.chiTiet).toHaveBeenCalledTimes(2);
    expect(document.getElementById('ndsua-email')).toBeNull();
  });

  it('Khoá → 409: người dùng THẤY cảnh báo, hộp xác nhận đóng, chi tiết được tải lại (không im, không lặp version cũ)', async () => {
    service.khoa.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
    nut('nguoiDung.hanhDong.khoa').click();
    await veLai();
    expect(hopXacNhan()?.textContent).toContain('nguoiDung.khoa.moTa');

    nut('nguoiDung.khoa.xacNhan', hopXacNhan()!).click();
    await veLai();

    expect(service.khoa).toHaveBeenCalledOnceWith('u1', { version: 'v-7' });
    expect(toast.latest()?.severity).toBe('warn');
    expect(toast.latest()?.summary).toBe('loi.CORE.CONCURRENCY.CONFLICT');
    expect(hopXacNhan()).toBeNull();
    expect(service.chiTiet).toHaveBeenCalledTimes(2);
  });

  it('Khoá lỗi mã khác → câu lỗi hiện TRONG hộp xác nhận; huỷ rồi mở Sửa → hộp Sửa không mang câu đó', async () => {
    service.khoa.and.returnValue(throwError(() => loiApi('CORE.USER.LOCK_FAILED')));
    nut('nguoiDung.hanhDong.khoa').click();
    await veLai();
    nut('nguoiDung.khoa.xacNhan', hopXacNhan()!).click();
    await veLai();
    expect(hopXacNhan()?.textContent).toContain('msg:CORE.USER.LOCK_FAILED');

    nut('chung.huy', hopXacNhan()!).click();
    await veLai();
    expect(hopXacNhan()).toBeNull();

    nut('nguoiDung.hanhDong.sua').click();
    await veLai();

    expect(document.getElementById('ndsua-email')).not.toBeNull();
    expect(document.body.textContent).not.toContain('msg:CORE.USER.LOCK_FAILED');
  });

  it('Mở hộp Sửa rồi cố đóng khi còn thay đổi → hỏi xác nhận huỷ; xác nhận thì đóng hộp (nối (dismissAttempted))', async () => {
    page.moHopSua();
    await veLai();
    const hop = fixture.debugElement.query(By.directive(HopSuaNguoiDungComponent));

    hop.componentInstance.dismissAttempted.emit();
    await veLai();
    expect(hopXacNhan()?.textContent).toContain('chung.roiTrang.tieuDe');

    nut('chung.roiTrang.xacNhan', hopXacNhan()!).click();
    await veLai();

    expect(hopXacNhan()).toBeNull();
    expect(document.getElementById('ndsua-email')).toBeNull();
  });

  describe('Gán vai trò — gỡ bớt vai trò phải hỏi xác nhận', () => {
    const VT_A = { id: 'a', name: 'A', isSystem: false };
    const VT_B = { id: 'b', name: 'B', isSystem: false };

    beforeEach(async () => {
      service.chiTiet.and.returnValue(of({ ...NGUOI_DUNG, roles: [VT_A, VT_B] }));
      dungLai();
      await veLai();
    });

    it('bỏ một vai trò rồi bấm Lưu → hộp xác nhận hiện; xác nhận mới gửi đúng tập còn lại (nối (confirmed))', async () => {
      service.ganVaiTro.and.returnValue(of(undefined));
      page.moHopGanVaiTro();
      await veLai();
      page.ganVaiTro.chon(['a']);

      page.guiGanVaiTro();
      await veLai();
      expect(hopXacNhan()?.textContent).toContain('nguoiDung.goVaiTro.moTa');
      expect(service.ganVaiTro).not.toHaveBeenCalled();

      nut('nguoiDung.goVaiTro.xacNhan', hopXacNhan()!).click();
      await veLai();

      expect(service.ganVaiTro).toHaveBeenCalledOnceWith('u1', { roleIds: ['a'], version: 'v-7' });
      expect(hopXacNhan()).toBeNull();
    });

    it('bỏ một vai trò rồi bấm Huỷ ở hộp xác nhận → không gửi gì (nối (cancelled))', async () => {
      page.moHopGanVaiTro();
      await veLai();
      page.ganVaiTro.chon(['a']);
      page.guiGanVaiTro();
      await veLai();

      nut('chung.huy', hopXacNhan()!).click();
      await veLai();

      expect(service.ganVaiTro).not.toHaveBeenCalled();
      expect(hopXacNhan()).toBeNull();
    });

    it('gán lỗi → sửa lại (vẫn có gỡ) → bấm Lưu: hộp xác nhận gỡ KHÔNG mang câu lỗi của lần trước', async () => {
      service.ganVaiTro.and.returnValue(throwError(() => loiApi('CORE.USER.ROLE_NOT_FOUND')));
      page.moHopGanVaiTro();
      await veLai();
      page.ganVaiTro.chon(['a']);
      page.guiGanVaiTro();
      await veLai();
      nut('nguoiDung.goVaiTro.xacNhan', hopXacNhan()!).click();
      await veLai();
      expect(document.body.textContent).toContain('msg:CORE.USER.ROLE_NOT_FOUND');
      expect(hopXacNhan()).toBeNull();

      page.ganVaiTro.chon(['b']);
      page.guiGanVaiTro();
      await veLai();

      expect(hopXacNhan()?.textContent).toContain('nguoiDung.goVaiTro.moTa');
      expect(hopXacNhan()?.textContent).not.toContain('msg:CORE.USER.ROLE_NOT_FOUND');
    });

    it('gán vai trò → 409: hộp gán hiện banner xung đột kèm nút Thử lại', async () => {
      service.ganVaiTro.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
      page.moHopGanVaiTro();
      await veLai();

      page.guiGanVaiTro();
      await veLai();

      expect(document.body.textContent).toContain('loi.CORE.CONCURRENCY.CONFLICT');
      expect(document.body.textContent).toContain('chung.thuLai');
    });

    // users.md §7 (ADR-0082) — cùng nhánh 409 của hộp Sửa: Thử lại = đóng hộp + GET chi tiết; lần gán
    // kế tiếp phải mang `version` của GET đó, không lặp `version` cũ (409 mãi).
    it('gán vai trò → 409 → bấm Thử lại thật: tải lại chi tiết; lần gán kế gửi version của GET mới', async () => {
      service.ganVaiTro.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
      page.moHopGanVaiTro();
      await veLai();
      page.guiGanVaiTro();
      await veLai();
      expect(service.ganVaiTro).toHaveBeenCalledOnceWith('u1', {
        roleIds: ['a', 'b'],
        version: 'v-7',
      });

      const soLanTai = service.chiTiet.calls.count();
      service.chiTiet.and.returnValue(of({ ...NGUOI_DUNG, roles: [VT_A, VT_B], version: 'v-8' }));
      nut('chung.thuLai').click();
      await veLai();
      expect(service.chiTiet).toHaveBeenCalledTimes(soLanTai + 1);

      service.ganVaiTro.calls.reset();
      service.ganVaiTro.and.returnValue(of(undefined));
      page.moHopGanVaiTro();
      await veLai();
      page.guiGanVaiTro();
      await veLai();

      expect(service.ganVaiTro).toHaveBeenCalledOnceWith('u1', {
        roleIds: ['a', 'b'],
        version: 'v-8',
      });
    });
  });

  it('Mở khoá: đang gửi thì nút chuyển sang trạng thái tải và bấm thêm không phát request thứ hai', async () => {
    service.chiTiet.and.returnValue(of({ ...NGUOI_DUNG, isLocked: true }));
    service.moKhoa.and.returnValue(new Subject<void>());
    dungLai();
    await veLai();

    nut('nguoiDung.hanhDong.moKhoa').click();
    await veLai();

    const nutMoKhoa = Array.from(document.querySelectorAll('button')).find(
      (b) => b.className.includes('nut') && b.querySelector('.pi-spinner'),
    );
    expect(nutMoKhoa).withContext('nút mở khoá phải ở trạng thái loading').toBeDefined();
    nutMoKhoa?.click();
    expect(service.moKhoa).toHaveBeenCalledTimes(1);
  });

  // fe-api-client.md §6.1 — component tự subscribe thì phải huỷ khi bị huỷ.
  describe('rời màn — GET chi tiết không sống quá màn', () => {
    it('GET chi tiết đang bay khi rời màn → request bị huỷ', async () => {
      let daHuy = false;
      service.chiTiet.and.returnValue(new Observable<NguoiDung>(() => () => (daHuy = true)));
      dungLai();
      await veLai();
      expect(daHuy).withContext('GET còn đang bay trước khi rời màn').toBeFalse();

      fixture.destroy();

      expect(daHuy).toBeTrue();
    });

    it('PUT (Sửa) xong SAU khi đã rời màn → không bắn GET chi tiết nào', async () => {
      const put = new Subject<void>();
      service.suaThongTin.and.returnValue(put);
      page.moHopSua();
      await veLai();
      page.guiSua();
      expect(service.suaThongTin).toHaveBeenCalledTimes(1);

      let soLanGet = 0;
      service.chiTiet.and.returnValue(
        defer(() => {
          soLanGet++;
          return of(NGUOI_DUNG);
        }),
      );
      fixture.destroy();
      put.next();
      put.complete();

      // Ca không rỗng: nhánh thành công của PUT (nơi gọi `xong` → tải lại chi tiết) đã thật sự chạy.
      expect(toast.latest()?.summary).toBe('nguoiDung.thongBao.suaThanhCong');
      expect(soLanGet).withContext('GET bắn ra sau khi màn đã huỷ').toBe(0);
    });
  });

  it('Mở khoá → 409: cảnh báo và tải lại (mở khoá đối xứng với khoá)', async () => {
    service.chiTiet.and.returnValue(of({ ...NGUOI_DUNG, isLocked: true }));
    service.moKhoa.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
    dungLai();
    await veLai();

    nut('nguoiDung.hanhDong.moKhoa').click();
    await veLai();

    expect(toast.latest()?.severity).toBe('warn');
    expect(service.chiTiet).toHaveBeenCalledTimes(2); // lần dựng lại + lần tải lại sau xung đột
  });
});

/**
 * Design/Screens/10-nguoi-dung.md §Trạng thái màn chi tiết, dòng "lỗi": tải hỏng lý do khác
 * `NOT_FOUND` → `Card` `error` (`NoticeBanner` `danger` + thử lại) cho CẢ HAI thẻ; tiêu đề
 * `nguoiDung.chiTietTrang.loiTai` luôn có; thân là câu `dichLoiChoMan` — lớp lỗi xuyên suốt thì
 * không thân (chi tiết và `traceId` ở toast); `PageHeader` vẫn hiện để quay lại được.
 */
describe('ChiTietNguoiDungPage — tải chi tiết hỏng', () => {
  let fixture: ComponentFixture<ChiTietNguoiDungPage>;
  let service: jasmine.SpyObj<NguoiDungService>;

  function loiTai(code: string, status: number): ApiFailureError {
    const body: ApiFailure = {
      success: false,
      data: null,
      error: { code, type: 'X', message: `msg:${code}`, messageParams: null, fieldErrors: null },
      traceId: 't',
    };
    return new ApiFailureError(body, status);
  }

  async function dung(loi: ApiFailureError): Promise<HTMLElement> {
    service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['chiTiet']);
    service.chiTiet.and.returnValue(throwError(() => loi));
    await TestBed.configureTestingModule({
      imports: [ChiTietNguoiDungPage],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: NguoiDungService, useValue: service },
        {
          provide: AuthService,
          useValue: { nguoiDung: signal({ id: 'khac' }), coQuyen: () => true },
        },
        {
          provide: VaiTroService,
          useValue: jasmine.createSpyObj<VaiTroService>('VaiTroService', ['timKiem']),
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'u1' }) } },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ChiTietNguoiDungPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function thePhanLoi(goc: HTMLElement): HTMLElement[] {
    return Array.from(goc.querySelectorAll<HTMLElement>('app-card')).filter(
      (the) => the.querySelector('.notice-banner--danger') !== null,
    );
  }

  it('lớp xuyên suốt (500) → CẢ HAI thẻ ở trạng thái error: tiêu đề cố định, không thân, có Thử lại', async () => {
    const goc = await dung(loiTai('CORE.SYSTEM.UNEXPECTED', 500));

    const the = thePhanLoi(goc);
    expect(the.length).withContext('hai thẻ, mỗi thẻ một khối lỗi').toBe(2);
    expect(the.map((t) => t.querySelector('.card__tieu-de')?.textContent?.trim())).toEqual([
      'nguoiDung.chiTietTrang.theThongTin',
      'nguoiDung.chiTietTrang.theVaiTro',
    ]);
    for (const t of the) {
      expect(t.querySelector('.notice-banner__tieu-de')?.textContent?.trim()).toBe(
        'nguoiDung.chiTietTrang.loiTai',
      );
      expect(t.querySelector('.notice-banner__than')?.textContent).not.toContain('msg:');
      expect(t.querySelector('app-button')?.textContent?.trim()).toBe('chung.thuLai');
    }
    expect(goc.querySelector('app-page-header')).withContext('PageHeader vẫn hiện').not.toBeNull();
  });

  it('lỗi thường (422, mã khác NOT_FOUND) → thân mang câu dịch theo mã', async () => {
    const goc = await dung(loiTai('CORE.USER.MA_THU', 422));

    for (const t of thePhanLoi(goc)) {
      expect(t.querySelector('.notice-banner__than')?.textContent).toContain(
        'msg:CORE.USER.MA_THU',
      );
    }
    expect(thePhanLoi(goc).length).toBe(2);
  });

  it('bấm Thử lại → gọi lại GET chi tiết; về được thì hai thẻ hiện dữ liệu', async () => {
    const goc = await dung(loiTai('CORE.SYSTEM.UNEXPECTED', 500));
    service.chiTiet.and.returnValue(of(NGUOI_DUNG));

    thePhanLoi(goc)[0]?.querySelector<HTMLButtonElement>('app-button button')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(service.chiTiet).toHaveBeenCalledTimes(2);
    expect(thePhanLoi(goc).length).toBe(0);
    expect(goc.querySelector('app-the-thong-tin-nguoi-dung')).not.toBeNull();
  });
});
