import { Type, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import {
  TranslateLoader,
  TranslateService,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, firstValueFrom, from, of } from 'rxjs';

import { ApiFailure, ApiFailureError, ApiFieldError } from '../../core/http/api-result.model';
import { applyFormFailure } from './apply-form-failure';

/**
 * Lời gọi CÓ bảng mã gốc, qua kiểu hàm khai tham số thứ tư là tuỳ chọn: các ca của bảng biên dịch
 * được cả trên bản `applyFormFailure` chưa nhận tham số đó — ở đó chúng phải ĐỎ vì kết quả sai,
 * không phải vì lỗi biên dịch.
 */
const applyCoBang: (
  form: FormGroup,
  loi: ApiFailureError,
  translate: TranslateService,
  maGocVaoO?: Readonly<Record<string, string>>,
) => string | null = applyFormFailure;

const BAT_BUOC: ApiFieldError = { code: 'CORE.VALIDATION.REQUIRED', messageParams: null };
const QUA_DAI: ApiFieldError = {
  code: 'CORE.VALIDATION.MAX_LENGTH',
  messageParams: { MaxLength: '200' },
};
const MAT_KHAU_NGAN: ApiFieldError = {
  code: 'CORE.USER.PASSWORD_TOO_SHORT',
  messageParams: { MinLength: '8' },
};

function thatBai(
  fieldErrors: ApiFailure['error']['fieldErrors'],
  code = 'CORE.TENANT.ADMIN_CREATE_FAILED',
  messageParams: Readonly<Record<string, string>> | null = null,
  message = 'msg',
): ApiFailureError {
  // 422: ngoài lớp lỗi xuyên suốt — các ca dưới kiểm luật của khu lỗi, không phải lớp đó.
  return new ApiFailureError(
    {
      success: false,
      data: null,
      error: { code, type: 'BusinessRule', message, messageParams, fieldErrors },
      traceId: 't',
    },
    422,
  );
}

const TRUNG_TEN = 'CORE.USER.USERNAME_DUPLICATED';
const TRUNG_EMAIL = 'CORE.USER.EMAIL_DUPLICATED';

/**
 * Luật ở quy-uoc/fe-ui-conventions.md §6.2: câu của mã gốc CHỈ là đường lùi khi `fieldErrors` không
 * mang mã nào; còn mã chưa hiện ở ô nào thì banner mang câu của TỪNG mã đó, mỗi mã một dòng.
 */
describe('applyFormFailure()', () => {
  const translate = {
    instant: (khoa: string, tham?: Record<string, string>) =>
      tham && Object.keys(tham).length > 0 ? `[${khoa}:${JSON.stringify(tham)}]` : `[${khoa}]`,
  } as unknown as TranslateService;
  let form: FormGroup;

  beforeEach(() => {
    form = new FormGroup({ userName: new FormControl(''), email: new FormControl('') });
  });

  describe('đường lùi — fieldErrors không mang mã nào → câu của mã gốc', () => {
    it('fieldErrors = null', () => {
      expect(applyFormFailure(form, thatBai(null), translate)).toBe(
        '[loi.CORE.TENANT.ADMIN_CREATE_FAILED]',
      );
    });

    it('fieldErrors = {}', () => {
      expect(applyFormFailure(form, thatBai({}), translate)).toBe(
        '[loi.CORE.TENANT.ADMIN_CREATE_FAILED]',
      );
    });

    it('mọi danh sách rỗng — khoá khớp lẫn khoá lạ', () => {
      expect(applyFormFailure(form, thatBai({ UserName: [], KhoaLa: [] }), translate)).toBe(
        '[loi.CORE.TENANT.ADMIN_CREATE_FAILED]',
      );
    });

    it('body = null (không phải envelope) → câu mất kết nối', () => {
      expect(applyFormFailure(form, new ApiFailureError(null), translate)).toBe(
        '[loi.CORE.CLIENT.NO_CONNECTION]',
      );
    });
  });

  // Lớp lỗi xuyên suốt: interceptor đã toast kèm traceId — khu lỗi của form KHÔNG hiện lần hai,
  // và form không bị đụng.
  describe('lớp lỗi xuyên suốt → null, form không bị đụng', () => {
    function loi(status: number, code: string | null): ApiFailureError {
      return new ApiFailureError(
        code === null
          ? null
          : {
              success: false,
              data: null,
              error: { code, type: 'X', message: 'msg', messageParams: null, fieldErrors: null },
              traceId: 't',
            },
        status,
      );
    }

    for (const [status, code] of [
      [500, 'CORE.SYSTEM.UNEXPECTED'],
      [503, null],
      [403, 'CORE.AUTH.FORBIDDEN'],
      [403, 'CORE.AUTH.ORIGIN_REJECTED'],
    ] as const) {
      it(`${status} ${code ?? '(không envelope)'} → null`, () => {
        expect(applyFormFailure(form, loi(status, code), translate)).toBeNull();
        expect(form.controls['userName'].errors).toBeNull();
        expect(form.controls['email'].errors).toBeNull();
      });
    }

    it('403 CORE.AUTH.PASSWORD_CHANGE_REQUIRED → KHÔNG thuộc lớp: câu của mã như cũ', () => {
      expect(
        applyFormFailure(form, loi(403, 'CORE.AUTH.PASSWORD_CHANGE_REQUIRED'), translate),
      ).toBe('[loi.CORE.AUTH.PASSWORD_CHANGE_REQUIRED]');
    });

    it('mất kết nối (status 0, không envelope) → KHÔNG thuộc lớp: câu mất kết nối như cũ', () => {
      expect(applyFormFailure(form, loi(0, null), translate)).toBe(
        '[loi.CORE.CLIENT.NO_CONNECTION]',
      );
    });
  });

  it('mọi mã đều hiện dưới một ô → null, lỗi nằm ở từng ô', () => {
    expect(applyFormFailure(form, thatBai({ UserName: [BAT_BUOC] }), translate)).toBeNull();
    expect(form.controls['userName'].errors?.['server']).toEqual([
      '[loi.CORE.VALIDATION.REQUIRED]',
    ]);
  });

  it('một ô có mã, một ô mang danh sách rỗng → null: danh sách rỗng không mang mã nào chưa hiện', () => {
    expect(
      applyFormFailure(form, thatBai({ UserName: [BAT_BUOC], Email: [] }), translate),
    ).toBeNull();
    expect(form.controls['userName'].errors?.['server']).toBeDefined();
    expect(form.controls['email'].errors).toBeNull();
  });

  it('khoá khớp mang danh sách RỖNG → ô KHÔNG bị đặt lỗi (không để ô invalid mà không có câu nào để hiện)', () => {
    applyFormFailure(form, thatBai({ UserName: [] }), translate);
    expect(form.controls['userName'].errors).toBeNull();
    expect(form.controls['userName'].valid).toBeTrue();
  });

  describe('mã gốc không nói thay mã con', () => {
    it('toàn khoá lạ → câu của mã con, kèm messageParams', () => {
      expect(applyFormFailure(form, thatBai({ KhoaLa: [MAT_KHAU_NGAN] }), translate)).toBe(
        '[loi.CORE.USER.PASSWORD_TOO_SHORT:{"MinLength":"8"}]',
      );
    });

    it('mã gốc CORE.VALIDATION.FAILED kèm một khoá lạ "" → câu của mã con, không phải "kiểm tra các trường được đánh dấu"', () => {
      expect(
        applyFormFailure(form, thatBai({ '': [BAT_BUOC] }, 'CORE.VALIDATION.FAILED'), translate),
      ).toBe('[loi.CORE.VALIDATION.REQUIRED]');
    });

    it('lẫn khoá khớp và khoá lạ → chỉ câu của mã chưa hiện; không lặp mã đã hiện dưới ô', () => {
      expect(
        applyFormFailure(form, thatBai({ UserName: [BAT_BUOC], KhoaLa: [QUA_DAI] }), translate),
      ).toBe('[loi.CORE.VALIDATION.MAX_LENGTH:{"MaxLength":"200"}]');
      expect(form.controls['userName'].errors?.['server']).toEqual([
        '[loi.CORE.VALIDATION.REQUIRED]',
      ]);
    });

    it('khoá khớp mang danh sách rỗng cạnh khoá lạ có mã → câu của mã ở khoá lạ', () => {
      expect(applyFormFailure(form, thatBai({ UserName: [], KhoaLa: [BAT_BUOC] }), translate)).toBe(
        '[loi.CORE.VALIDATION.REQUIRED]',
      );
    });
  });

  it('nhiều mã chưa hiện → mỗi mã một dòng, đúng thứ tự BE gửi, nối bằng \\n', () => {
    expect(
      applyFormFailure(
        form,
        thatBai({
          KhoaA: [MAT_KHAU_NGAN, BAT_BUOC],
          UserName: [QUA_DAI],
          'DiaChi.Tinh': [BAT_BUOC],
        }),
        translate,
      ),
    ).toBe(
      [
        '[loi.CORE.USER.PASSWORD_TOO_SHORT:{"MinLength":"8"}]',
        '[loi.CORE.VALIDATION.REQUIRED]',
        '[loi.CORE.VALIDATION.REQUIRED]',
      ].join('\n'),
    );
  });

  /**
   * Bảng mã gốc → tên control: mã card KHÔNG khai `fieldErrors` mà screen spec đặt dưới một ô (mã
   * trùng). Mã đã hiện dưới ô thì không lặp ở khu lỗi chung.
   */
  describe('bảng mã gốc → ô', () => {
    const BANG = { [TRUNG_TEN]: 'userName', [TRUNG_EMAIL]: 'email' } as const;

    it('mã gốc có trong bảng, fieldErrors vắng → câu (qua dichLoi, kèm messageParams) dưới ô; khu chung null', () => {
      const body = thatBai(null, TRUNG_TEN, { UserName: 'an' });

      expect(applyCoBang(form, body, translate, BANG)).toBeNull();
      expect(form.controls['userName'].errors?.['server']).toEqual([
        `[loi.${TRUNG_TEN}:{"UserName":"an"}]`,
      ]);
      expect(form.controls['userName'].touched).toBeTrue();
      expect(form.controls['email'].errors).toBeNull();
    });

    it('fieldErrors = {} hay mọi danh sách rỗng → vẫn dưới ô, khu chung null', () => {
      expect(applyCoBang(form, thatBai({}, TRUNG_EMAIL), translate, BANG)).toBeNull();
      expect(form.controls['email'].errors?.['server']).toEqual([`[loi.${TRUNG_EMAIL}]`]);

      form.reset();
      expect(applyCoBang(form, thatBai({ KhoaLa: [] }, TRUNG_EMAIL), translate, BANG)).toBeNull();
      expect(form.controls['email'].errors?.['server']).toEqual([`[loi.${TRUNG_EMAIL}]`]);
    });

    it('kèm mã con ở khoá lạ → mã gốc dưới ô, khu chung chỉ mang câu của mã con (không lặp mã gốc)', () => {
      const body = thatBai({ KhoaLa: [QUA_DAI] }, TRUNG_TEN);

      expect(applyCoBang(form, body, translate, BANG)).toBe(
        '[loi.CORE.VALIDATION.MAX_LENGTH:{"MaxLength":"200"}]',
      );
      expect(form.controls['userName'].errors?.['server']).toEqual([`[loi.${TRUNG_TEN}]`]);
    });

    it('kèm mã con ở một ô KHÁC → cả hai ô mang lỗi, khu chung null', () => {
      expect(
        applyCoBang(form, thatBai({ Email: [BAT_BUOC] }, TRUNG_TEN), translate, BANG),
      ).toBeNull();
      expect(form.controls['userName'].errors?.['server']).toEqual([`[loi.${TRUNG_TEN}]`]);
      expect(form.controls['email'].errors?.['server']).toEqual(['[loi.CORE.VALIDATION.REQUIRED]']);
    });

    it('kèm mã con ở CÙNG ô → câu của mã gốc đứng đầu, mã con theo sau — không mã nào bị đè', () => {
      const body = thatBai({ UserName: [QUA_DAI] }, TRUNG_TEN);

      expect(applyCoBang(form, body, translate, BANG)).toBeNull();
      expect(form.controls['userName'].errors?.['server']).toEqual([
        `[loi.${TRUNG_TEN}]`,
        '[loi.CORE.VALIDATION.MAX_LENGTH:{"MaxLength":"200"}]',
      ]);
    });

    it('ô đang mang lỗi server của lần trước → thay bằng câu của phản hồi này, không cộng dồn', () => {
      form.controls['userName'].setErrors({ server: ['câu cũ'] });

      applyCoBang(form, thatBai(null, TRUNG_TEN), translate, BANG);

      expect(form.controls['userName'].errors?.['server']).toEqual([`[loi.${TRUNG_TEN}]`]);
    });

    it('giữ lỗi validator client đang có trên ô, chỉ thêm khoá server', () => {
      form.controls['email'].setErrors({ email: true });

      applyCoBang(form, thatBai(null, TRUNG_EMAIL), translate, BANG);

      expect(form.controls['email'].errors).toEqual({
        email: true,
        server: [`[loi.${TRUNG_EMAIL}]`],
      });
    });

    it('mã gốc KHÔNG có trong bảng → như không có bảng: câu của mã gốc ở khu chung, không ô nào bị đặt lỗi', () => {
      expect(applyCoBang(form, thatBai(null, 'CORE.USER.CREATE_FAILED'), translate, BANG)).toBe(
        '[loi.CORE.USER.CREATE_FAILED]',
      );
      expect(form.controls['userName'].errors).toBeNull();
      expect(form.controls['email'].errors).toBeNull();
    });

    it('bảng trỏ vào control KHÔNG có trong form → như không có bảng: câu của mã gốc ở khu chung', () => {
      const bangLech = { [TRUNG_TEN]: 'tenDangNhap' };

      expect(applyCoBang(form, thatBai(null, TRUNG_TEN), translate, bangLech)).toBe(
        `[loi.${TRUNG_TEN}]`,
      );
      expect(form.controls['userName'].errors).toBeNull();
    });

    it('body = null → câu mất kết nối, bảng không đụng ô nào', () => {
      expect(applyCoBang(form, new ApiFailureError(null), translate, BANG)).toBe(
        '[loi.CORE.CLIENT.NO_CONNECTION]',
      );
      expect(form.controls['userName'].errors).toBeNull();
    });

    it('tên control trong bảng ràng theo kiểu của form — tên ngoài form là lỗi biên dịch', () => {
      const formCoKieu = new FormGroup({ email: new FormControl('') });
      const bangSai = { [TRUNG_TEN]: 'tenDangNhap' } as const;

      // @ts-expect-error — 'tenDangNhap' không phải control của formCoKieu.
      applyFormFailure(formCoKieu, thatBai(null, TRUNG_TEN), translate, bangSai);

      expect(formCoKieu.controls.email.errors).toBeNull();
    });
  });
});

/** Nạp ĐÚNG `public/i18n/vi.json` — bản dịch giả ở trên in lại khoá nên không thấy tham số. */
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
 * Mã trùng có tham số (contracts/users.md §5: `{ "UserName": "…" }`) qua bảng dịch THẬT: câu dưới ô
 * đi qua `dichLoi` — thiếu tham số thì người dùng thấy nguyên chữ `{{UserName}}`; thiếu khoá dịch thì
 * đường lùi là câu BE gửi kèm, không phải khoá dịch thô.
 */
describe('applyFormFailure() — bảng mã gốc → ô qua bảng dịch thật', () => {
  const BANG = { [TRUNG_TEN]: 'userName' };
  const TRUNG = thatBai(
    null,
    TRUNG_TEN,
    { UserName: 'an.nguyen' },
    'User name an.nguyen already exists.',
  );

  async function dungTranslate(loader: Type<TranslateLoader>): Promise<TranslateService> {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(loader),
        }),
      ],
    });
    const translate = TestBed.inject(TranslateService);
    await firstValueFrom(translate.use('vi'));
    return translate;
  }

  function taoForm(): FormGroup {
    return new FormGroup({ userName: new FormControl(''), email: new FormControl('') });
  }

  it('câu dưới ô Tên đăng nhập mang tên thật; khu chung null', async () => {
    const form = taoForm();
    const cau = applyCoBang(form, TRUNG, await dungTranslate(ViJsonThatLoader), BANG);

    expect(form.controls['userName'].errors?.['server']).toEqual([
      'Tên đăng nhập an.nguyen đã tồn tại.',
    ]);
    expect(cau).toBeNull();
  });

  it('bảng dịch thiếu khoá → câu dưới ô là câu BE gửi kèm', async () => {
    const form = taoForm();
    applyCoBang(form, TRUNG, await dungTranslate(BangDichRongLoader), BANG);

    expect(form.controls['userName'].errors?.['server']).toEqual([
      'User name an.nguyen already exists.',
    ]);
  });
});
