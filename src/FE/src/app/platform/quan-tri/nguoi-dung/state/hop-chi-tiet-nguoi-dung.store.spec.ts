import { Type, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  TranslateLoader,
  TranslateService,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject, firstValueFrom, from, of, throwError } from 'rxjs';

import { ApiFailure, ApiFailureError, ApiFieldError } from '../../../../core/http/api-result.model';
import { ToastService } from '../../../../core/toast/toast.service';
import { NguoiDung } from '../models/nguoi-dung.model';
import { NguoiDungService } from '../services/nguoi-dung.service';
import { HopChiTietNguoiDungStore } from './hop-chi-tiet-nguoi-dung.store';

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

const BAT_BUOC: readonly ApiFieldError[] = [
  { code: 'CORE.VALIDATION.REQUIRED', messageParams: null },
];

describe('HopChiTietNguoiDungStore', () => {
  let service: jasmine.SpyObj<NguoiDungService>;
  let toast: jasmine.SpyObj<ToastService>;
  let store: HopChiTietNguoiDungStore;
  let xong: jasmine.Spy;

  beforeEach(() => {
    service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', [
      'suaThongTin',
      'datLaiMatKhau',
      'ganVaiTro',
      'khoa',
      'moKhoa',
    ]);
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['thanhCong', 'canhBao', 'loi']);
    const translate = {
      instant: (khoa: string, tham?: Record<string, string>) =>
        tham && Object.keys(tham).length > 0 ? `[${khoa}:${JSON.stringify(tham)}]` : `[${khoa}]`,
    } as unknown as TranslateService;
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        HopChiTietNguoiDungStore,
        { provide: NguoiDungService, useValue: service },
        { provide: ToastService, useValue: toast },
        { provide: TranslateService, useValue: translate },
      ],
    });
    store = TestBed.inject(HopChiTietNguoiDungStore);
    xong = jasmine.createSpy('xong');
  });

  describe('Sửa', () => {
    beforeEach(() => store.moHopSua(NGUOI_DUNG));

    it('moHopSua() điền form từ người dùng (email null → chuỗi rỗng) và mở đúng hộp', () => {
      expect(store.hopDangMo()).toBe('sua');
      expect(store.formSua.getRawValue()).toEqual({ email: 'an@b.vn', fullName: 'Nguyễn An' });

      store.moHopSua({ ...NGUOI_DUNG, email: null });
      expect(store.formSua.controls.email.value).toBe('');
    });

    it('THÀNH CÔNG → đóng hộp, toast, gọi xong(); gửi kèm version của bản ghi', () => {
      service.suaThongTin.and.returnValue(of(undefined));
      store.formSua.setValue({ email: 'moi@b.vn', fullName: 'An Mới' });

      store.guiSua(NGUOI_DUNG, xong);

      expect(service.suaThongTin).toHaveBeenCalledOnceWith('u1', {
        email: 'moi@b.vn',
        fullName: 'An Mới',
        version: 'v-7',
      });
      expect(store.hopDangMo()).toBeNull();
      expect(store.dangGui()).toBeFalse();
      expect(toast.thanhCong).toHaveBeenCalledOnceWith('[nguoiDung.thongBao.suaThanhCong]');
      expect(xong).toHaveBeenCalledTimes(1);
    });

    it('form không hợp lệ → không gọi service, hộp giữ mở, daGui bật, lanGuiSai tăng (đưa focus về ô sai)', () => {
      store.formSua.controls.email.setValue('abc');
      store.guiSua(NGUOI_DUNG, xong);
      expect(service.suaThongTin).not.toHaveBeenCalled();
      expect(store.hopDangMo()).toBe('sua');
      expect(store.daGui()).toBeTrue();
      expect(store.lanGuiSai()).toBe(1);
      expect(xong).not.toHaveBeenCalled();

      store.guiSua(NGUOI_DUNG, xong);
      expect(store.lanGuiSai()).toBe(2);
    });

    it('form hợp lệ → lanGuiSai KHÔNG tăng', () => {
      service.suaThongTin.and.returnValue(of(undefined));
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.lanGuiSai()).toBe(0);
    });

    it('đang gửi → dangGui() bật và dong() bị khoá', () => {
      service.suaThongTin.and.returnValue(new Subject<void>());
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.dangGui()).toBeTrue();
      store.dong();
      expect(store.hopDangMo()).toBe('sua');
    });

    it('EMAIL_DUPLICATED → lỗi vào ô email, không banner, hộp giữ mở', () => {
      service.suaThongTin.and.returnValue(throwError(() => loiApi('CORE.USER.EMAIL_DUPLICATED')));
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.formSua.controls.email.errors?.['server']).toEqual([
        '[loi.CORE.USER.EMAIL_DUPLICATED]',
      ]);
      expect(store.loiHop()).toBeNull();
      expect(store.hopDangMo()).toBe('sua');
      expect(store.dangGui()).toBeFalse();
      expect(xong).not.toHaveBeenCalled();
    });

    // Mã trùng xuống ô Email, nhưng mã con đi kèm vẫn theo luật chung — không bị nuốt.
    it('EMAIL_DUPLICATED kèm fieldErrors khoá khớp ô khác → CẢ HAI ô mang lỗi, không banner', () => {
      service.suaThongTin.and.returnValue(
        throwError(() => loiApi('CORE.USER.EMAIL_DUPLICATED', { FullName: BAT_BUOC })),
      );
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.formSua.controls.email.errors?.['server']).toEqual([
        '[loi.CORE.USER.EMAIL_DUPLICATED]',
      ]);
      expect(store.formSua.controls.fullName.errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
      expect(store.loiHop()).toBeNull();
    });

    // Bảng mã gốc → ô thuộc hộp Sửa: hộp Đặt lại không có ô Email, mã đó lên banner của hộp.
    it('EMAIL_DUPLICATED ở hộp Đặt lại mật khẩu → banner mang câu của mã, không ô nào bị đặt lỗi', () => {
      store.moHopDatLai();
      store.formDatLai.setValue({ tempPassword: 'Tam-Pass-1' });
      service.datLaiMatKhau.and.returnValue(throwError(() => loiApi('CORE.USER.EMAIL_DUPLICATED')));
      store.guiDatLai(NGUOI_DUNG, xong);
      expect(store.loiHop()).toBe('[loi.CORE.USER.EMAIL_DUPLICATED]');
      expect(store.formDatLai.controls.tempPassword.errors).toBeNull();
    });

    it('VALIDATION.FAILED có khoá khớp → lỗi vào ô; chỉ khoá lạ → banner mang câu của MÃ CON (§6.2)', () => {
      service.suaThongTin.and.returnValue(
        throwError(() => loiApi('CORE.VALIDATION.FAILED', { FullName: BAT_BUOC })),
      );
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.formSua.controls.fullName.errors?.['server']).toBeDefined();
      expect(store.loiHop()).toBeNull();

      // Lỗi server nằm lại trên ô tới khi giá trị đổi (form invalid) — mở lại để có form sạch.
      store.moHopSua(NGUOI_DUNG);
      service.suaThongTin.and.returnValue(
        throwError(() => loiApi('CORE.USER.UPDATE_FAILED', { KhoaLa: BAT_BUOC })),
      );
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.loiHop()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    // Service tắt toast (BO_QUA_TOAST_LOI) nên hộp là nơi DUY NHẤT hiện lỗi — không được im (§6.2).
    it('UPDATE_FAILED với fieldErrors = null hoặc {} → banner mang câu của mã, không im', () => {
      for (const fieldErrors of [null, {}]) {
        store.moHopSua(NGUOI_DUNG);
        service.suaThongTin.and.returnValue(
          throwError(() => loiApi('CORE.USER.UPDATE_FAILED', fieldErrors)),
        );
        store.guiSua(NGUOI_DUNG, xong);
        expect(store.loiHop())
          .withContext(`fieldErrors = ${JSON.stringify(fieldErrors)}`)
          .toBe('[loi.CORE.USER.UPDATE_FAILED]');
      }
    });

    it('CONCURRENCY.CONFLICT → bật xungDot, không banner lỗi', () => {
      service.suaThongTin.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.xungDot()).toBeTrue();
      expect(store.loiHop()).toBeNull();
    });

    // fe-ui-conventions.md §6.2: store không chọn câu theo một danh sách mã chép từ card. BE thêm
    // `fieldErrors` cho một mã chưa từng có trong danh sách đó thì câu vẫn phải là của MÃ CON.
    it('mã NGOÀI danh sách cũ kèm fieldErrors: khoá khớp → lỗi vào ô; chỉ khoá lạ → banner mang câu của MÃ CON', () => {
      service.suaThongTin.and.returnValue(
        throwError(() => loiApi('CORE.USER.MA_NGOAI_DANH_SACH', { FullName: BAT_BUOC })),
      );
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.formSua.controls.fullName.errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
      expect(store.loiHop()).toBeNull();

      store.moHopSua(NGUOI_DUNG);
      service.suaThongTin.and.returnValue(
        throwError(() => loiApi('CORE.USER.MA_NGOAI_DANH_SACH', { KhoaLa: BAT_BUOC })),
      );
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.loiHop()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('mã khác → banner chung theo mã; không phải ApiFailureError → câu dự phòng', () => {
      service.suaThongTin.and.returnValue(throwError(() => loiApi('CORE.USER.SOMETHING')));
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.loiHop()).toBe('[loi.CORE.USER.SOMETHING]');

      service.suaThongTin.and.returnValue(throwError(() => new Error('boom')));
      store.guiSua(NGUOI_DUNG, xong);
      expect(store.loiHop()).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
    });

    it('loiSua — ô sai được chạm thì hiện câu lỗi; sửa đúng thì hết (tính lại theo sự kiện của form)', () => {
      store.formSua.controls.email.setValue('');
      expect(store.loiSua().email).toBeNull();
      store.formSua.controls.email.markAsTouched();
      expect(store.loiSua().email).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
      store.formSua.controls.email.setValue('a@b.vn');
      expect(store.loiSua().email).toBeNull();
    });
  });

  describe('Đặt lại mật khẩu', () => {
    beforeEach(() => store.moHopDatLai());

    it('moHopDatLai() reset ô mật khẩu và mở đúng hộp', () => {
      store.formDatLai.controls.tempPassword.setValue('cu');
      store.moHopDatLai();
      expect(store.formDatLai.controls.tempPassword.value).toBe('');
      expect(store.hopDangMo()).toBe('dat-lai');
    });

    it('THÀNH CÔNG → đóng hộp, toast kèm tên đăng nhập, gửi kèm version', () => {
      service.datLaiMatKhau.and.returnValue(of(undefined));
      store.formDatLai.setValue({ tempPassword: 'Tam-Pass-1' });

      store.guiDatLai(NGUOI_DUNG, xong);

      expect(service.datLaiMatKhau).toHaveBeenCalledOnceWith('u1', {
        tempPassword: 'Tam-Pass-1',
        version: 'v-7',
      });
      expect(store.hopDangMo()).toBeNull();
      expect(toast.thanhCong).toHaveBeenCalledOnceWith(
        '[nguoiDung.thongBao.datLaiThanhCong:{"tenDangNhap":"an.nguyen"}]',
      );
      expect(xong).toHaveBeenCalledTimes(1);
    });

    it('ô trống → không gọi service, ô hiện lỗi sau khi bấm gửi, lanGuiSai tăng', () => {
      store.guiDatLai(NGUOI_DUNG, xong);
      expect(service.datLaiMatKhau).not.toHaveBeenCalled();
      expect(store.loiMatKhau()).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
      expect(store.lanGuiSai()).toBe(1);
    });

    it('RESET_PASSWORD_FAILED có khoá khớp → lỗi vào ô; chỉ khoá lạ → banner mang câu của MÃ CON (§6.2)', () => {
      store.formDatLai.setValue({ tempPassword: 'x' });
      service.datLaiMatKhau.and.returnValue(
        throwError(() => loiApi('CORE.USER.RESET_PASSWORD_FAILED', { TempPassword: BAT_BUOC })),
      );
      store.guiDatLai(NGUOI_DUNG, xong);
      expect(store.formDatLai.controls.tempPassword.errors?.['server']).toBeDefined();
      expect(store.loiHop()).toBeNull();

      store.moHopDatLai();
      store.formDatLai.setValue({ tempPassword: 'x' });
      service.datLaiMatKhau.and.returnValue(
        throwError(() => loiApi('CORE.USER.RESET_PASSWORD_FAILED', { KhoaLa: BAT_BUOC })),
      );
      store.guiDatLai(NGUOI_DUNG, xong);
      expect(store.loiHop()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('RESET_PASSWORD_FAILED với fieldErrors = null hoặc {} → banner mang câu của mã, không im', () => {
      for (const fieldErrors of [null, {}]) {
        store.moHopDatLai();
        store.formDatLai.setValue({ tempPassword: 'x' });
        service.datLaiMatKhau.and.returnValue(
          throwError(() => loiApi('CORE.USER.RESET_PASSWORD_FAILED', fieldErrors)),
        );
        store.guiDatLai(NGUOI_DUNG, xong);
        expect(store.loiHop())
          .withContext(`fieldErrors = ${JSON.stringify(fieldErrors)}`)
          .toBe('[loi.CORE.USER.RESET_PASSWORD_FAILED]');
      }
    });

    it('mã NGOÀI danh sách cũ kèm fieldErrors: khoá khớp → lỗi vào ô; chỉ khoá lạ → banner mang câu của MÃ CON', () => {
      store.formDatLai.setValue({ tempPassword: 'x' });
      service.datLaiMatKhau.and.returnValue(
        throwError(() => loiApi('CORE.USER.MA_NGOAI_DANH_SACH', { TempPassword: BAT_BUOC })),
      );
      store.guiDatLai(NGUOI_DUNG, xong);
      expect(store.formDatLai.controls.tempPassword.errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
      expect(store.loiHop()).toBeNull();

      store.moHopDatLai();
      store.formDatLai.setValue({ tempPassword: 'x' });
      service.datLaiMatKhau.and.returnValue(
        throwError(() => loiApi('CORE.USER.MA_NGOAI_DANH_SACH', { KhoaLa: BAT_BUOC })),
      );
      store.guiDatLai(NGUOI_DUNG, xong);
      expect(store.loiHop()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    // users.md §9: đích là tài khoản vận hành hệ thống cũng nhận mã này (403 nghiệp vụ, không toast).
    it('403 RESET_PASSWORD_TARGET_FORBIDDEN → banner trong hộp mang câu của mã, hộp giữ mở', () => {
      store.formDatLai.setValue({ tempPassword: 'Tam-Pass-1' });
      service.datLaiMatKhau.and.returnValue(
        throwError(() => loiApi('CORE.USER.RESET_PASSWORD_TARGET_FORBIDDEN')),
      );

      store.guiDatLai(NGUOI_DUNG, xong);

      expect(store.loiHop()).toBe('[loi.CORE.USER.RESET_PASSWORD_TARGET_FORBIDDEN]');
      expect(store.hopDangMo()).toBe('dat-lai');
      expect(toast.loi).not.toHaveBeenCalled();
      expect(xong).not.toHaveBeenCalled();
    });
  });

  describe('xuLyLoi dùng chung (cả cho hộp không phải form: khoá, gán vai trò)', () => {
    it('hop = null: CONFLICT vẫn bật xungDot; mã khác → banner; không có ô nào bị chạm', () => {
      store.xuLyLoi(loiApi('CORE.CONCURRENCY.CONFLICT'), null);
      expect(store.xungDot()).toBeTrue();

      store.xuLyLoi(loiApi('CORE.USER.EMAIL_DUPLICATED'), null);
      expect(store.loiHop()).toBe('[loi.CORE.USER.EMAIL_DUPLICATED]');
      expect(store.formSua.controls.email.errors?.['server']).toBeUndefined();
    });
  });

  describe('Gán vai trò', () => {
    const VT_A = { id: 'a', name: 'A', isSystem: false };
    const VT_B = { id: 'b', name: 'B', isSystem: false };
    const CO_HAI_VAI_TRO: NguoiDung = { ...NGUOI_DUNG, roles: [VT_A, VT_B] };

    beforeEach(() => store.moHopGanVaiTro());

    it('không gỡ vai trò nào → gửi ngay tập đích; thành công đóng hộp, toast, gọi xong()', () => {
      service.ganVaiTro.and.returnValue(of(undefined));

      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a', 'b', 'c'], xong);

      expect(service.ganVaiTro).toHaveBeenCalledOnceWith('u1', {
        roleIds: ['a', 'b', 'c'],
        version: 'v-7',
      });
      expect(store.hopXacNhanGo()).toBeNull();
      expect(store.hopDangMo()).toBeNull();
      expect(toast.thanhCong).toHaveBeenCalledOnceWith(
        '[nguoiDung.thongBao.ganVaiTroThanhCong:{"tenDangNhap":"an.nguyen"}]',
      );
      expect(xong).toHaveBeenCalledTimes(1);
    });

    it('CÓ gỡ vai trò → hỏi xác nhận kèm số vai trò gỡ, CHƯA gửi; huỷ thì không gửi gì', () => {
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a'], xong);
      expect(store.hopXacNhanGo()).toBe(1);
      expect(service.ganVaiTro).not.toHaveBeenCalled();

      store.huyXacNhanGo();

      expect(store.hopXacNhanGo()).toBeNull();
      expect(service.ganVaiTro).not.toHaveBeenCalled();
      expect(store.hopDangMo()).toBe('gan-vai-tro');
    });

    it('xác nhận gỡ → đóng hộp hỏi rồi mới gửi đúng tập đích đã chốt lúc bấm', () => {
      service.ganVaiTro.and.returnValue(of(undefined));
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, [], xong);
      expect(store.hopXacNhanGo()).toBe(2);

      store.xacNhanGoVaiTro();

      expect(store.hopXacNhanGo()).toBeNull();
      expect(service.ganVaiTro).toHaveBeenCalledOnceWith('u1', { roleIds: [], version: 'v-7' });
      expect(xong).toHaveBeenCalledTimes(1);
    });

    // users.md §7 (ADR-0082): `version` bắt buộc — token của TÀI KHOẢN ĐÍCH từ `GET` chi tiết gần
    // nhất, không phải bản store tự giữ từ lần trước.
    it('gửi version của bản ghi truyền vào lần bấm này — sau 409 và tải lại, lần gán kế mang version MỚI', () => {
      service.ganVaiTro.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a', 'b'], xong);
      expect(service.ganVaiTro).toHaveBeenCalledOnceWith('u1', {
        roleIds: ['a', 'b'],
        version: 'v-7',
      });

      service.ganVaiTro.calls.reset();
      service.ganVaiTro.and.returnValue(of(undefined));
      store.yeuCauGanVaiTro({ ...CO_HAI_VAI_TRO, version: 'v-8' }, ['a', 'b'], xong);

      expect(service.ganVaiTro).toHaveBeenCalledOnceWith('u1', {
        roleIds: ['a', 'b'],
        version: 'v-8',
      });
    });

    it('xác nhận gỡ gửi version của bản ghi CHỐT lúc bấm Lưu', () => {
      service.ganVaiTro.and.returnValue(of(undefined));
      store.yeuCauGanVaiTro({ ...CO_HAI_VAI_TRO, version: 'v-9' }, ['a'], xong);

      store.xacNhanGoVaiTro();

      expect(service.ganVaiTro).toHaveBeenCalledOnceWith('u1', { roleIds: ['a'], version: 'v-9' });
    });

    it('xacNhanGoVaiTro() khi chưa có yêu cầu nào → không gửi gì', () => {
      store.xacNhanGoVaiTro();
      expect(service.ganVaiTro).not.toHaveBeenCalled();
    });

    it('CONFLICT → bật xungDot ở hộp gán (có nút Thử lại), hộp giữ mở, không gọi xong()', () => {
      service.ganVaiTro.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a', 'b'], xong);

      expect(store.xungDot()).toBeTrue();
      expect(store.hopDangMo()).toBe('gan-vai-tro');
      expect(store.dangGui()).toBeFalse();
      expect(xong).not.toHaveBeenCalled();
    });

    it('lần gán trước lỗi (banner) hoặc xung đột → yêu cầu mới có gỡ vai trò KHÔNG mang dấu vết cũ sang hộp hỏi', () => {
      service.ganVaiTro.and.returnValue(throwError(() => loiApi('CORE.USER.ROLE_NOT_FOUND')));
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a', 'b'], xong);
      expect(store.loiHop()).toBe('[loi.CORE.USER.ROLE_NOT_FOUND]');
      store.xungDot.set(true);

      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a'], xong);

      expect(store.hopXacNhanGo()).toBe(1);
      expect(store.loiHop()).withContext('loiHop').toBeNull();
      expect(store.xungDot()).withContext('xungDot').toBeFalse();
    });

    // users.md §7: `roleIds` quá 50 phần tử → VALIDATION.FAILED, fieldErrors["RoleIds"] mã
    // CORE.VALIDATION.MAX_ITEMS, messageParams { MaxItems }. Hộp gán không có ô nào để gắn
    // (10-nguoi-dung.md: "không gắn vào ô") → banner phải mang CÂU CỦA LỖI ĐÓ kèm tham số, không
    // phải câu chung "kiểm tra lại các trường được đánh dấu" (09-forms-validation.md §4.3).
    it('VALIDATION.FAILED kèm fieldErrors RoleIds MAX_ITEMS → banner mang câu MAX_ITEMS với tham số MaxItems', () => {
      service.ganVaiTro.and.returnValue(
        throwError(() =>
          loiApi('CORE.VALIDATION.FAILED', {
            RoleIds: [{ code: 'CORE.VALIDATION.MAX_ITEMS', messageParams: { MaxItems: '50' } }],
          }),
        ),
      );
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a', 'b'], xong);

      expect(store.loiHop()).toBe('[loi.CORE.VALIDATION.MAX_ITEMS:{"MaxItems":"50"}]');
      expect(store.hopDangMo()).toBe('gan-vai-tro');
      expect(xong).not.toHaveBeenCalled();
    });

    it('VALIDATION.FAILED KHÔNG kèm fieldErrors → banner mang câu của chính mã', () => {
      service.ganVaiTro.and.returnValue(throwError(() => loiApi('CORE.VALIDATION.FAILED')));
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a', 'b'], xong);
      expect(store.loiHop()).toBe('[loi.CORE.VALIDATION.FAILED]');
    });

    it('mã lỗi khác → banner theo mã', () => {
      service.ganVaiTro.and.returnValue(throwError(() => loiApi('CORE.USER.SOMETHING')));
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a', 'b'], xong);
      expect(store.loiHop()).toBe('[loi.CORE.USER.SOMETHING]');
    });

    it('đang gửi → dangGui() bật', () => {
      service.ganVaiTro.and.returnValue(new Subject<void>());
      store.yeuCauGanVaiTro(CO_HAI_VAI_TRO, ['a', 'b'], xong);
      expect(store.dangGui()).toBeTrue();
    });
  });

  describe('Khoá', () => {
    it('moXacNhanKhoa() mở hộp xác nhận và xoá dấu vết của lần trước', () => {
      store.loiHop.set('lỗi cũ');
      store.xungDot.set(true);

      store.moXacNhanKhoa();

      expect(store.hienThiXacNhanKhoa()).toBeTrue();
      expect(store.loiHop()).toBeNull();
      expect(store.xungDot()).toBeFalse();
    });

    it('THÀNH CÔNG → đóng hộp xác nhận, toast, gọi xong(); gửi kèm version', () => {
      service.khoa.and.returnValue(of(undefined));
      store.moXacNhanKhoa();

      store.xacNhanKhoa(NGUOI_DUNG, xong);

      expect(service.khoa).toHaveBeenCalledOnceWith('u1', { version: 'v-7' });
      expect(store.hienThiXacNhanKhoa()).toBeFalse();
      expect(toast.thanhCong).toHaveBeenCalledOnceWith(
        '[nguoiDung.thongBao.khoaThanhCong:{"tenDangNhap":"an.nguyen"}]',
      );
      expect(xong).toHaveBeenCalledTimes(1);
    });

    // Hộp xác nhận khoá KHÔNG có chỗ hiện `xungDot`, và service tắt toast của interceptor — nên
    // nhánh này là chỗ duy nhất người dùng biết bản ghi đã đổi. Không được im, và không được để
    // gửi lại cùng `version` cũ (xung đột lặp vô tận).
    it('CONFLICT → cảnh báo cho người dùng, đóng hộp, tải lại (xong()) để lấy version mới', () => {
      service.khoa.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));
      store.moXacNhanKhoa();

      store.xacNhanKhoa(NGUOI_DUNG, xong);

      expect(toast.canhBao).toHaveBeenCalledOnceWith('[loi.CORE.CONCURRENCY.CONFLICT]');
      expect(store.hienThiXacNhanKhoa()).toBeFalse();
      expect(store.dangGui()).toBeFalse();
      expect(xong).toHaveBeenCalledTimes(1);
    });

    it('mã lỗi khác → banner trong hộp xác nhận, hộp giữ mở, không tải lại', () => {
      service.khoa.and.returnValue(throwError(() => loiApi('CORE.USER.CANNOT_LOCK_SELF')));
      store.moXacNhanKhoa();

      store.xacNhanKhoa(NGUOI_DUNG, xong);

      expect(store.loiHop()).toBe('[loi.CORE.USER.CANNOT_LOCK_SELF]');
      expect(store.hienThiXacNhanKhoa()).toBeTrue();
      expect(xong).not.toHaveBeenCalled();
    });

    // Hộp xác nhận không có form nào để gắn `fieldErrors` — cùng luật với hộp Gán vai trò (§6.2).
    it('mã kèm fieldErrors → banner trong hộp xác nhận mang câu của MÃ CON, không phải mã gốc', () => {
      service.khoa.and.returnValue(
        throwError(() => loiApi('CORE.USER.MA_NGOAI_DANH_SACH', { KhoaLa: BAT_BUOC })),
      );
      store.moXacNhanKhoa();

      store.xacNhanKhoa(NGUOI_DUNG, xong);

      expect(store.loiHop()).toBe('[loi.CORE.VALIDATION.REQUIRED]');
      expect(store.hienThiXacNhanKhoa()).toBeTrue();
      expect(xong).not.toHaveBeenCalled();
    });

    // users.md §2 luật 4: đích là tài khoản vận hành hệ thống thì luôn chặn, cùng mã (403 nghiệp vụ).
    it('403 SYSTEM_ROLE_LOCK_FORBIDDEN → banner trong hộp xác nhận, hộp giữ mở, không toast, không tải lại', () => {
      service.khoa.and.returnValue(
        throwError(() => loiApi('CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN')),
      );
      store.moXacNhanKhoa();

      store.xacNhanKhoa(NGUOI_DUNG, xong);

      expect(store.loiHop()).toBe('[loi.CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN]');
      expect(store.hienThiXacNhanKhoa()).toBeTrue();
      expect(toast.loi).not.toHaveBeenCalled();
      expect(xong).not.toHaveBeenCalled();
    });

    it('huyXacNhanKhoa() đóng hộp VÀ xoá lỗi để hộp Sửa/Đặt lại mở sau đó không mang câu của lần khoá', () => {
      service.khoa.and.returnValue(throwError(() => loiApi('CORE.USER.CANNOT_LOCK_SELF')));
      store.moXacNhanKhoa();
      store.xacNhanKhoa(NGUOI_DUNG, xong);
      expect(store.loiHop()).not.toBeNull();

      store.huyXacNhanKhoa();

      expect(store.hienThiXacNhanKhoa()).toBeFalse();
      expect(store.loiHop()).withContext('ngay sau khi huỷ').toBeNull();
      expect(store.xungDot()).toBeFalse();

      store.moHopSua(NGUOI_DUNG);
      expect(store.loiHop()).withContext('sau khi mở hộp Sửa').toBeNull();
    });

    it('đang gửi → huyXacNhanKhoa() bị khoá', () => {
      service.khoa.and.returnValue(new Subject<void>());
      store.moXacNhanKhoa();
      store.xacNhanKhoa(NGUOI_DUNG, xong);

      store.huyXacNhanKhoa();

      expect(store.hienThiXacNhanKhoa()).toBeTrue();
    });
  });

  describe('Mở khoá', () => {
    it('THÀNH CÔNG → toast kèm tên đăng nhập, gọi xong(); gửi kèm version', () => {
      service.moKhoa.and.returnValue(of(undefined));

      store.moKhoa(NGUOI_DUNG, xong);

      expect(service.moKhoa).toHaveBeenCalledOnceWith('u1', { version: 'v-7' });
      expect(toast.thanhCong).toHaveBeenCalledOnceWith(
        '[nguoiDung.thongBao.moKhoaThanhCong:{"tenDangNhap":"an.nguyen"}]',
      );
      expect(xong).toHaveBeenCalledTimes(1);
    });

    it('bấm đúp: gọi moKhoa() hai lần liên tiếp → chỉ MỘT request; xong thì hạ cờ và cho gửi lại', () => {
      const nguon = new Subject<void>();
      service.moKhoa.and.returnValue(nguon);

      store.moKhoa(NGUOI_DUNG, xong);
      expect(store.dangGui()).toBeTrue();
      store.moKhoa(NGUOI_DUNG, xong);

      expect(service.moKhoa).toHaveBeenCalledTimes(1);

      nguon.next();
      nguon.complete();
      expect(store.dangGui()).toBeFalse();
      expect(xong).toHaveBeenCalledTimes(1);

      service.moKhoa.and.returnValue(of(undefined));
      store.moKhoa(NGUOI_DUNG, xong);
      expect(service.moKhoa).toHaveBeenCalledTimes(2);
    });

    it('lỗi → cờ đang gửi cũng được hạ (không kẹt nút)', () => {
      service.moKhoa.and.returnValue(throwError(() => loiApi('CORE.USER.SOMETHING')));
      store.moKhoa(NGUOI_DUNG, xong);
      expect(store.dangGui()).toBeFalse();
    });

    it('CONFLICT → cảnh báo và tải lại, không toast lỗi', () => {
      service.moKhoa.and.returnValue(throwError(() => loiApi('CORE.CONCURRENCY.CONFLICT')));

      store.moKhoa(NGUOI_DUNG, xong);

      expect(toast.canhBao).toHaveBeenCalledOnceWith('[loi.CORE.CONCURRENCY.CONFLICT]');
      expect(toast.loi).not.toHaveBeenCalled();
      expect(xong).toHaveBeenCalledTimes(1);
    });

    // Service tắt toast chung nên toast này là thông báo DUY NHẤT — phải mang traceId (fe-api-client.md §1.1).
    it('mã khác → toast lỗi theo mã KÈM traceId; mất mạng → câu dự phòng, traceId null', () => {
      service.moKhoa.and.returnValue(throwError(() => loiApi('CORE.USER.SOMETHING')));
      store.moKhoa(NGUOI_DUNG, xong);
      expect(toast.loi).toHaveBeenCalledWith('[loi.CORE.USER.SOMETHING]', 't');

      service.moKhoa.and.returnValue(throwError(() => new Error('boom')));
      store.moKhoa(NGUOI_DUNG, xong);
      expect(toast.loi).toHaveBeenCalledWith('[loi.CORE.CLIENT.NO_CONNECTION]', null);
      expect(xong).not.toHaveBeenCalled();
    });
  });

  // Hộp Đặt lại mật khẩu chỉ có MỘT ô nhập và nút gửi nằm ngoài <form> → Enter gửi ngầm (ngSubmit
  // là đường sống), và Button chỉ chặn được click. Mọi hàm ghi phải tự chặn khi đang gửi — nếu không,
  // hai POST cùng `version` và POST thứ hai nhận 409 GIẢ sau khi hộp đã đóng.
  describe('chống gửi hai lần — mọi luồng ghi: gọi lần hai khi lần một chưa xong → chỉ MỘT request', () => {
    it('guiSua', () => {
      const nguon = new Subject<void>();
      service.suaThongTin.and.returnValue(nguon);
      store.moHopSua(NGUOI_DUNG);

      store.guiSua(NGUOI_DUNG, xong);
      store.guiSua(NGUOI_DUNG, xong);

      expect(service.suaThongTin).toHaveBeenCalledTimes(1);
      nguon.next();
      nguon.complete();
      expect(store.dangGui()).toBeFalse();
    });

    it('guiDatLai', () => {
      service.datLaiMatKhau.and.returnValue(new Subject<void>());
      store.moHopDatLai();
      store.formDatLai.setValue({ tempPassword: 'Tam-Pass-1' });

      store.guiDatLai(NGUOI_DUNG, xong);
      store.guiDatLai(NGUOI_DUNG, xong);

      expect(service.datLaiMatKhau).toHaveBeenCalledTimes(1);
    });

    it('guiDatLai: lần hai KHÔNG làm hỏng trạng thái của lần một (daGui/loiHop giữ nguyên)', () => {
      service.datLaiMatKhau.and.returnValue(new Subject<void>());
      store.moHopDatLai();
      store.formDatLai.setValue({ tempPassword: 'Tam-Pass-1' });
      store.guiDatLai(NGUOI_DUNG, xong);
      store.loiHop.set('dấu vết');

      store.guiDatLai(NGUOI_DUNG, xong);

      expect(store.loiHop()).toBe('dấu vết');
    });

    it('yeuCauGanVaiTro (không gỡ vai trò) và xacNhanGoVaiTro', () => {
      service.ganVaiTro.and.returnValue(new Subject<void>());
      const co = { ...NGUOI_DUNG, roles: [{ id: 'a', name: 'A', isSystem: false }] };

      store.yeuCauGanVaiTro(co, ['a', 'b'], xong);
      store.yeuCauGanVaiTro(co, ['a', 'b'], xong);
      expect(service.ganVaiTro).toHaveBeenCalledTimes(1);

      // Đang gửi thì cũng không mở hộp hỏi gỡ.
      store.yeuCauGanVaiTro(co, [], xong);
      expect(store.hopXacNhanGo()).toBeNull();
      expect(service.ganVaiTro).toHaveBeenCalledTimes(1);
    });

    it('xacNhanGoVaiTro: xác nhận hai lần liên tiếp → một request', () => {
      service.ganVaiTro.and.returnValue(new Subject<void>());
      const co = { ...NGUOI_DUNG, roles: [{ id: 'a', name: 'A', isSystem: false }] };
      store.yeuCauGanVaiTro(co, [], xong);

      store.xacNhanGoVaiTro();
      store.xacNhanGoVaiTro();

      expect(service.ganVaiTro).toHaveBeenCalledTimes(1);
    });

    it('xacNhanGoVaiTro khi đang có một lần ghi khác chạy (dangGui) → không gửi thêm', () => {
      const co = { ...NGUOI_DUNG, roles: [{ id: 'a', name: 'A', isSystem: false }] };
      store.yeuCauGanVaiTro(co, [], xong);
      expect(store.hopXacNhanGo()).toBe(1);
      store.dangGui.set(true);

      store.xacNhanGoVaiTro();

      expect(service.ganVaiTro).not.toHaveBeenCalled();
    });

    it('xacNhanKhoa', () => {
      service.khoa.and.returnValue(new Subject<void>());
      store.moXacNhanKhoa();

      store.xacNhanKhoa(NGUOI_DUNG, xong);
      store.xacNhanKhoa(NGUOI_DUNG, xong);

      expect(service.khoa).toHaveBeenCalledTimes(1);
    });

    it('xong một lần thì gửi lại được (cờ hạ)', () => {
      service.suaThongTin.and.returnValue(of(undefined));
      store.moHopSua(NGUOI_DUNG);
      store.guiSua(NGUOI_DUNG, xong);
      store.moHopSua(NGUOI_DUNG);
      store.guiSua(NGUOI_DUNG, xong);
      expect(service.suaThongTin).toHaveBeenCalledTimes(2);
    });
  });

  describe('mở hộp — không mang dấu vết của lần trước (ba tín hiệu chung: daGui, loiHop, xungDot)', () => {
    const cachMo: readonly [string, (s: HopChiTietNguoiDungStore) => void][] = [
      ['moHopSua', (s) => s.moHopSua(NGUOI_DUNG)],
      ['moHopDatLai', (s) => s.moHopDatLai()],
      ['moHopGanVaiTro', (s) => s.moHopGanVaiTro()],
    ];

    for (const [ten, mo] of cachMo) {
      it(`${ten}() xoá lỗi/xung đột/cờ đã-gửi còn sót từ lần khoá hoặc lần mở trước`, () => {
        store.daGui.set(true);
        store.loiHop.set('lỗi của lần khoá trước');
        store.xungDot.set(true);

        mo(store);

        expect(store.daGui()).withContext('daGui').toBeFalse();
        expect(store.loiHop()).withContext('loiHop').toBeNull();
        expect(store.xungDot()).withContext('xungDot').toBeFalse();
      });
    }
  });

  describe('đóng / hỏi xác nhận huỷ', () => {
    it('dong() reset toàn bộ trạng thái chung của lần mở trước', () => {
      store.moHopSua(NGUOI_DUNG);
      store.daGui.set(true);
      store.loiHop.set('lỗi cũ');
      store.xungDot.set(true);

      store.dong();

      expect(store.hopDangMo()).toBeNull();
      expect(store.daGui()).toBeFalse();
      expect(store.loiHop()).toBeNull();
      expect(store.xungDot()).toBeFalse();
    });

    it('onDismissAttempted() hỏi; huyXacNhanHuy() giữ hộp; xacNhanHuy() đóng hộp', () => {
      store.moHopSua(NGUOI_DUNG);
      store.onDismissAttempted();
      expect(store.hienThiXacNhanHuy()).toBeTrue();

      store.huyXacNhanHuy();
      expect(store.hienThiXacNhanHuy()).toBeFalse();
      expect(store.hopDangMo()).toBe('sua');

      store.onDismissAttempted();
      store.xacNhanHuy();
      expect(store.hienThiXacNhanHuy()).toBeFalse();
      expect(store.hopDangMo()).toBeNull();
    });
  });
});

/** Nạp ĐÚNG `public/i18n/vi.json` — bản dịch giả ở trên in lại khoá nên không thấy tham số bị bỏ quên. */
class ViJsonThatLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return from(fetch('/i18n/vi.json').then((r) => r.json() as Promise<TranslationObject>));
  }
}

class BangDichRongLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/**
 * `CORE.USER.EMAIL_DUPLICATED` ở hộp Sửa: câu dưới ô Email đi qua bảng dịch THẬT và `messageParams`
 * đúng khoá của card (contracts/users.md: `{ "Email": "…" }`). Thiếu tham số thì người dùng thấy
 * nguyên chữ `{{Email}}`; thiếu khoá dịch thì đường lùi là câu BE gửi kèm (`dichLoi`), không phải khoá.
 */
describe('HopChiTietNguoiDungStore — câu lỗi trùng email qua bảng dịch thật', () => {
  const TRUNG_EMAIL = new ApiFailureError({
    success: false,
    data: null,
    error: {
      code: 'CORE.USER.EMAIL_DUPLICATED',
      type: 'Conflict',
      message: 'Email an@b.vn already exists.',
      messageParams: { Email: 'an@b.vn' },
      fieldErrors: null,
    },
    traceId: 't',
  });

  async function dung(loader: Type<TranslateLoader>): Promise<HopChiTietNguoiDungStore> {
    const service = jasmine.createSpyObj<NguoiDungService>('NguoiDungService', ['suaThongTin']);
    service.suaThongTin.and.returnValue(throwError(() => TRUNG_EMAIL));
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        HopChiTietNguoiDungStore,
        { provide: NguoiDungService, useValue: service },
        {
          provide: ToastService,
          useValue: jasmine.createSpyObj<ToastService>('ToastService', ['thanhCong', 'loi']),
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(loader),
        }),
      ],
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const store = TestBed.inject(HopChiTietNguoiDungStore);
    store.moHopSua(NGUOI_DUNG);
    return store;
  }

  it('câu dưới ô Email mang địa chỉ thật, không còn "{{Email}}"; hộp không có banner', async () => {
    const store = await dung(ViJsonThatLoader);
    store.guiSua(NGUOI_DUNG, () => undefined);

    expect(store.formSua.controls.email.errors?.['server']).toEqual(['Email an@b.vn đã tồn tại.']);
    expect(store.loiSua().email).toBe('Email an@b.vn đã tồn tại.');
    expect(store.loiHop()).toBeNull();
  });

  it('bảng dịch thiếu khoá → câu dưới ô là câu BE gửi kèm, không phải khoá dịch thô', async () => {
    const store = await dung(BangDichRongLoader);
    store.guiSua(NGUOI_DUNG, () => undefined);

    expect(store.formSua.controls.email.errors?.['server']).toEqual([
      'Email an@b.vn already exists.',
    ]);
  });
});
