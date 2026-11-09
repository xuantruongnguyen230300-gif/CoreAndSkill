// ADR-0117 QC2 — API giả ĐÓNG MẶC ĐỊNH.
//
// - Mọi request tới tiền tố `apiBaseUrl` (đọc từ `environment.e2e.ts`) đều đi qua đây. Request không
//   có ca giả khai báo bị huỷ VÀ ghi lại; test kết thúc mà còn bản ghi là ĐỎ, nêu phương thức và URL (H2).
// - Request ra ngoài origin của app và ngoài tiền tố API cũng bị huỷ và ghi lại (H2).
// - Request cùng origin với app (dev server) đi tiếp bình thường.
// - API khác origin với app (ADR-0015): hạ tầng gắn header CORS có credentials vào mỗi phản hồi. Preflight:
//   đo ngày 2026-10-06 với Playwright 1.63.0 + Chromium kèm theo, `page.route` KHÔNG thấy request OPTIONS —
//   Playwright tự xử lý nên POST JSON kèm X-XSRF-TOKEN tới được ca giả (canary xanh chứng minh). Hạ tầng
//   không tự viết nhánh preflight; nếu một bản sau để OPTIONS lọt vào đây thì nó rơi vào "chưa khai báo"
//   và test ĐỎ nêu `OPTIONS <url>` — ồn ào, không im lặng.
//
// H1 (không `console.error`, không ngoại lệ trang) cũng chốt ở đây vì nó phải biết ca nào là lỗi CÓ CHỦ Ý:
// Chromium tự ghi một dòng `console.error` "Failed to load resource" cho mọi phản hồi 4xx/5xx. ADR-0117
// không nói cách đối xử với dòng đó, nên chọn cách HẸP NHẤT — miễn đúng request đã khai `loiCoChuY: true`:
//   * khớp theo URL gây ra dòng log (location().url), KHÔNG theo chữ của dòng log, KHÔNG theo miền;
//   * mỗi lần ca được phục vụ miễn đúng MỘT dòng — dòng thứ hai từ cùng URL là đỏ;
//   * ca >= 400 không khai `loiCoChuY` thì dòng log của nó là đỏ — buộc người viết nói rõ "lỗi này là chủ ý".
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Page, Route } from '@playwright/test';

import type { PhanHoi } from './envelope';

export type PhuongThuc = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export interface CaGia {
  readonly phuongThuc: PhuongThuc;
  /** Đường dẫn SAU `apiBaseUrl`, bắt đầu bằng `/`, không query — ví dụ `/core/auth/me`. */
  readonly duongDan: string;
  readonly phanHoi: PhanHoi;
  /** Chỉ cho phản hồi >= 400: lỗi này là chủ ý của kịch bản, dòng console của Chromium cho nó được miễn (H1). */
  readonly loiCoChuY?: true;
  /** Giữ phản hồi cho tới khi promise này xong — dựng trạng thái "đang tải" (request treo) mà không ngủ chờ. Spec PHẢI giải phóng trước khi test kết thúc. */
  readonly cho?: Promise<unknown>;
}

const TEP_MOI_TRUONG = resolve(__dirname, '../../src/environments/environment.e2e.ts');

/** `apiBaseUrl` của cấu hình e2e. Đọc văn bản tệp để không nạp mã Angular lúc chạy. */
export function docApiBaseUrl(tep: string = TEP_MOI_TRUONG): string {
  const m = /apiBaseUrl:\s*'([^']+)'/.exec(readFileSync(tep, 'utf8'));
  if (!m) throw new Error(`Không đọc được apiBaseUrl trong ${tep}`);
  return m[1]!.replace(/\/+$/, '');
}

const khoa = (phuongThuc: string, url: string) => `${phuongThuc} ${url}`;

export class ApiGia {
  private readonly ca = new Map<string, CaGia>();
  private readonly chuaKhai: string[] = [];
  private readonly ngoaiPhamVi: string[] = [];
  private readonly loiConsole: { text: string; url: string }[] = [];
  private readonly ngoaiLeTrang: string[] = [];
  private readonly phucVuLoiChuY = new Map<string, number>();
  private readonly daPhucVu = new Set<string>();
  private readonly mienTheoChu: { chuoi: string; con: number }[] = [];

  private constructor(
    private readonly apiBaseUrl: string,
    private readonly originApp: string,
  ) {}

  static async dung(
    page: Page,
    originApp: string,
    apiBaseUrl: string = docApiBaseUrl(),
  ): Promise<ApiGia> {
    const api = new ApiGia(apiBaseUrl, new URL(originApp).origin);
    page.on('console', (m) => {
      if (m.type() === 'error') api.loiConsole.push({ text: m.text(), url: m.location().url });
    });
    page.on('pageerror', (e) => api.ngoaiLeTrang.push(String(e)));
    await page.route('**/*', (route) => api.xuLy(route));
    return api;
  }

  /** Khai báo ca giả. Khai hai lần cùng phương thức + đường dẫn thì ca sau thay ca trước. */
  khaiBao(...cas: readonly CaGia[]): void {
    for (const c of cas) {
      if (!c.duongDan.startsWith('/'))
        throw new Error(`duongDan phải bắt đầu bằng "/": ${c.duongDan}`);
      if (c.loiCoChuY && c.phanHoi.status < 400) {
        throw new Error(`loiCoChuY chỉ dành cho phản hồi >= 400: ${c.phuongThuc} ${c.duongDan}`);
      }
      this.ca.set(khoa(c.phuongThuc, this.apiBaseUrl + c.duongDan), c);
    }
  }

  /**
   * Ca đã được phục vụ ít nhất một lần chưa. Dùng để kiểm một trạng thái dựng bằng ca lỗi có chủ ý thật sự đã đi
   * qua ca đó — màn không gọi endpoint thì "trạng thái lỗi" chỉ là màn mặc định mang tên khác.
   */
  daPhucVuCa(c: CaGia): boolean {
    return this.daPhucVu.has(khoa(c.phuongThuc, this.apiBaseUrl + c.duongDan));
  }

  /**
   * Miễn dòng `console.error` mà Chromium ghi khi MỘT tệp tĩnh bị huỷ có chủ ý (`route.abort()`), mỗi lần huỷ miễn đúng một
   * dòng theo URL — cùng khuôn với `loiCoChuY` của ca API. Dùng cho ca "không tải được phần mã" (02-trang-loi).
   */
  mienLoiTaiTep(url: string): void {
    this.phucVuLoiChuY.set(url, (this.phucVuLoiChuY.get(url) ?? 0) + 1);
  }

  /**
   * Miễn tối đa `soLan` dòng `console.error` có chứa `chuoi` — cho dòng `ERROR ...` mà Angular tự ghi khi lần điều hướng
   * hỏng vì phần mã không tải được (promise điều hướng bị từ chối, không ai bắt). Vị trí của dòng đó là một chunk của
   * framework, không đoán được, nên khớp theo chữ — HẸP: chuỗi cụ thể, có số lần, và chỉ ca chặn chunk mới gọi.
   */
  mienLoiConsoleChua(chuoi: string, soLan: number): void {
    this.mienTheoChu.push({ chuoi, con: soLan });
  }

  /** Mọi vi phạm H1 + H2 tích tới lúc gọi. Rỗng là sạch. */
  viPham(): string[] {
    const kq: string[] = [];
    if (this.chuaKhai.length) {
      kq.push(`H2 — request API chưa có ca giả khai báo:\n  ${this.chuaKhai.join('\n  ')}`);
    }
    if (this.ngoaiPhamVi.length) {
      kq.push(
        `H2 — request ra ngoài origin của app và ngoài tiền tố API:\n  ${this.ngoaiPhamVi.join('\n  ')}`,
      );
    }
    const conLai = new Map(this.phucVuLoiChuY);
    const mienChu = this.mienTheoChu.map((m) => ({ ...m }));
    const loiThat: string[] = [];
    for (const l of this.loiConsole) {
      const con = conLai.get(l.url) ?? 0;
      const mien = mienChu.find((m) => m.con > 0 && l.text.includes(m.chuoi));
      if (con > 0) conLai.set(l.url, con - 1);
      else if (mien) mien.con -= 1;
      else loiThat.push(`${l.text} (${l.url || 'không rõ nguồn'})`);
    }
    if (loiThat.length)
      kq.push(`H1 — console.error ngoài lỗi có chủ ý:\n  ${loiThat.join('\n  ')}`);
    if (this.ngoaiLeTrang.length)
      kq.push(`H1 — ngoại lệ trang:\n  ${this.ngoaiLeTrang.join('\n  ')}`);
    return kq;
  }

  /** Gọi ở cuối test; ném lỗi nếu còn vi phạm. */
  kiemTraCuoiTest(): void {
    const vp = this.viPham();
    if (vp.length) throw new Error(`Lưới giao diện — lượt chạy không hợp lệ:\n${vp.join('\n')}`);
  }

  private cors(requestHeaders?: string): Record<string, string> {
    return {
      'access-control-allow-origin': this.originApp,
      'access-control-allow-credentials': 'true',
      'access-control-allow-headers': requestHeaders ?? '*',
      'access-control-expose-headers': 'Retry-After, Allow',
      vary: 'Origin',
    };
  }

  private async xuLy(route: Route): Promise<void> {
    const req = route.request();
    const url = req.url();
    if (new URL(url).origin === this.originApp) return route.continue();

    if (!url.startsWith(this.apiBaseUrl + '/')) {
      this.ngoaiPhamVi.push(`${req.method()} ${url}`);
      return route.abort();
    }

    const duongDanKhongQuery = url.split('?')[0]!;
    const ca = this.ca.get(khoa(req.method(), duongDanKhongQuery));
    if (!ca) {
      this.chuaKhai.push(`${req.method()} ${url}`);
      return route.abort();
    }
    if (ca.cho) await ca.cho;
    this.daPhucVu.add(khoa(req.method(), duongDanKhongQuery));
    if (ca.loiCoChuY) this.phucVuLoiChuY.set(url, (this.phucVuLoiChuY.get(url) ?? 0) + 1);
    return route.fulfill({
      status: ca.phanHoi.status,
      headers: { ...this.cors(), ...ca.phanHoi.headers },
      contentType: 'application/json',
      body: JSON.stringify(ca.phanHoi.body),
    });
  }
}
