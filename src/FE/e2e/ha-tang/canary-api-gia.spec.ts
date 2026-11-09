// Canary hạ tầng API giả (ADR-0117 QC2, việc T2): chứng minh hạ tầng BẮT ĐƯỢC cái nó hứa bắt.
// Ca xanh chạy thẳng; ca đỏ chạy THẬT trong một lượt Playwright con (`canary/ca-do.canary.ts`) rồi
// khẳng định từng ca đỏ đúng chỗ — đỏ vì lý do khác (ví dụ trình duyệt không mở được) thì canary này đỏ.
import { spawnSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

import { thanhCong, thatBai, TRACE_ID_GIA } from './envelope';
import { expect, test } from './fixture';
import { docDanhMucMaLoi, kiemTraMaLoi } from './ma-loi-hop-dong';
import { API, goi, JSON_XSRF, moTrangCanary } from './canary/trang-canary';

test.describe('xanh — hạ tầng phục vụ đúng cái đã khai', () => {
  test('POST JSON khác nguồn kèm header XSRF nhận đúng dữ liệu giả (preflight qua được)', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao({
      phuongThuc: 'POST',
      duongDan: '/core/ghi-thu',
      phanHoi: thanhCong({ dau: 'vao' }),
    });
    await moTrangCanary(page);
    const guiDi = page.waitForRequest(
      (r) => r.method() === 'POST' && r.url() === `${API}/core/ghi-thu`,
    );

    const kq = await goi(page, `${API}/core/ghi-thu`, {
      method: 'POST',
      headers: JSON_XSRF,
      body: '{"a":1}',
    });

    expect(kq).toEqual({
      status: 200,
      body: { success: true, data: { dau: 'vao' }, error: null, traceId: TRACE_ID_GIA },
    });
    expect((await guiDi).headers()['x-xsrf-token']).toBe('xsrf-gia');
  });

  test('phản hồi lỗi mang envelope ApiFailure với mã lấy từ card, kèm lỗi theo ô', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao({
      phuongThuc: 'POST',
      duongDan: '/core/auth/login',
      loiCoChuY: true,
      phanHoi: thatBai('CORE.VALIDATION.FAILED', {
        type: 'Validation',
        status: 400,
        fieldErrors: { UserName: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }] },
      }),
    });
    await moTrangCanary(page);

    const kq = await goi(page, `${API}/core/auth/login`, {
      method: 'POST',
      headers: JSON_XSRF,
      body: '{}',
    });

    expect(kq.status).toBe(400);
    expect(kq.body).toMatchObject({
      success: false,
      data: null,
      traceId: TRACE_ID_GIA,
      error: {
        code: 'CORE.VALIDATION.FAILED',
        type: 'Validation',
        fieldErrors: { UserName: [{ code: 'CORE.VALIDATION.REQUIRED' }] },
      },
    });
  });
});

test.describe('đỏ — mã lỗi phải có ở docs/contracts/', () => {
  test('mã bịa bị từ chối', () => {
    expect(() => thatBai('Auth.Unauthenticated', { type: 'Unauthorized', status: 401 })).toThrow(
      /"Auth\.Unauthenticated" không có trong docs\/contracts/,
    );
  });

  test('mã bịa trong fieldErrors bị từ chối, nêu tên ô', () => {
    expect(() =>
      thatBai('CORE.VALIDATION.FAILED', {
        type: 'Validation',
        status: 400,
        fieldErrors: { Password: [{ code: 'CORE.VALIDATION.BIA_RA', messageParams: null }] },
      }),
    ).toThrow(/fieldErrors\["Password"\].*CORE\.VALIDATION\.BIA_RA/);
  });

  test('mã có thật nhưng type/HTTP lệch bảng lỗi của card bị từ chối', () => {
    expect(() =>
      thatBai('CORE.AUTH.NOT_AUTHENTICATED', { type: 'Unauthorized', status: 403 }),
    ).toThrow(/khai Unauthorized\/401.*ghi Unauthorized\/403/);
  });

  test('thư mục không có card nào chứa mã thì ĐỎ, không xanh im lặng', () => {
    const thuMuc = mkdtempSync(join(tmpdir(), 'canary-contracts-'));
    try {
      writeFileSync(
        join(thuMuc, 'khong-ma.md'),
        '# Card không có mã lỗi nào\n\nChỉ chữ thường `code`.\n',
      );
      expect(() => docDanhMucMaLoi(thuMuc)).toThrow(/Trích được 0 mã lỗi/);
      expect(() =>
        kiemTraMaLoi('CORE.AUTH.NOT_AUTHENTICATED', 'Unauthorized', 401, docDanhMucMaLoi(thuMuc)),
      ).toThrow();
    } finally {
      rmSync(thuMuc, { recursive: true, force: true });
    }
  });

  test('thư mục rỗng cũng ĐỎ', () => {
    const thuMuc = mkdtempSync(join(tmpdir(), 'canary-contracts-'));
    try {
      mkdirSync(join(thuMuc, 'con'));
      expect(() => docDanhMucMaLoi(thuMuc)).toThrow(/Trích được 0 mã lỗi/);
    } finally {
      rmSync(thuMuc, { recursive: true, force: true });
    }
  });

  test('thư mục card thật cho ra mã (chặn trích 0 mã trên dữ liệu thật)', () => {
    expect(docDanhMucMaLoi().ma.size).toBeGreaterThan(20);
  });
});

interface KetQuaCon {
  readonly ok: boolean;
  readonly thongDiep: string;
}

/** Chạy lượt con và gom kết quả theo tên test. */
function chayCa(): Map<string, KetQuaCon> {
  const cli = require.resolve('@playwright/test/cli');
  const cauHinh = resolve(__dirname, 'canary/playwright.canary.config.ts');
  // Lượt con không được thừa hưởng trạng thái của worker đang chạy.
  const env = Object.fromEntries(
    Object.entries(process.env).filter(
      ([k]) => !k.startsWith('PW_') && !k.startsWith('PLAYWRIGHT_'),
    ),
  );
  const r = spawnSync(process.execPath, [cli, 'test', '-c', cauHinh], {
    encoding: 'utf8',
    env: { ...env, CI: '' },
    cwd: resolve(__dirname, '../..'),
    maxBuffer: 64 * 1024 * 1024,
  });
  const bao = JSON.parse(r.stdout) as { suites: Suite[] };
  const kq = new Map<string, KetQuaCon>();
  const duyet = (s: Suite) => {
    for (const sp of s.specs ?? []) {
      const loi = sp.tests.flatMap((t) => t.results.flatMap((x) => x.errors ?? []));
      // eslint-disable-next-line no-control-regex
      kq.set(sp.title, {
        ok: sp.ok,
        thongDiep: loi
          .map((e) => e.message)
          .join('\n')
          .replace(/\u001b\[[0-9;]*m/g, ''),
      });
    }
    (s.suites ?? []).forEach(duyet);
  };
  bao.suites.forEach(duyet);
  return kq;
}
interface Suite {
  specs?: {
    title: string;
    ok: boolean;
    tests: { results: { errors?: { message: string }[] }[] }[];
  }[];
  suites?: Suite[];
}

test.describe('đỏ — ca chạy thật phải đỏ đúng chỗ, nêu đúng URL', () => {
  let ca: Map<string, KetQuaCon>;
  test.beforeAll(() => {
    ca = chayCa();
  });

  const doDung = (ten: string, ...phaiCo: (string | RegExp)[]) =>
    test(`ca "${ten}" đỏ, thông điệp nêu rõ`, () => {
      const c = ca.get(ten);
      expect(c, `lượt con không chạy ca "${ten}"`).toBeDefined();
      expect(c!.ok, 'ca phải ĐỎ').toBe(false);
      for (const p of phaiCo)
        expect(c!.thongDiep).toMatch(
          p instanceof RegExp ? p : new RegExp(p.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')),
        );
    });

  doDung('DO request GET chưa khai báo', 'H2', `GET ${API}/core/khong-co-ca`);
  doDung('DO request POST JSON kèm XSRF chưa khai báo', 'H2', `POST ${API}/core/khong-co-ca-ghi`);
  doDung(
    'DO request ra ngoài origin app và ngoài tiền tố API',
    'H2',
    'GET https://ngoai-pham-vi.invalid/x',
  );
  doDung(
    'DO phản hồi lỗi đã khai nhưng không đánh dấu loiCoChuY',
    'H1 — console.error ngoài lỗi có chủ ý',
    `${API}/core/auth/me`,
  );
  doDung('DO console.error của trang dù đã có ca lỗi chủ ý', 'loi-tu-ghi-cua-trang');
  doDung('DO ngoại lệ trang', 'H1 — ngoại lệ trang', 'ngoai-le-co-chu-y-cua-canary');

  test('ca đỏ do console.error của trang KHÔNG kéo theo dòng "Failed to load resource" của ca lỗi chủ ý', () => {
    expect(ca.get('DO console.error của trang dù đã có ca lỗi chủ ý')!.thongDiep).not.toContain(
      'Failed to load resource',
    );
  });

  test('ca đối chứng XANH thật sự xanh (lỗi chủ ý + POST JSON khác nguồn)', () => {
    const c = ca.get('XANH lỗi có chủ ý đã khai + POST JSON khác nguồn kèm XSRF nhận dữ liệu giả');
    expect(c, 'lượt con không chạy ca đối chứng').toBeDefined();
    expect(c!.thongDiep).toBe('');
    expect(c!.ok).toBe(true);
  });
});
