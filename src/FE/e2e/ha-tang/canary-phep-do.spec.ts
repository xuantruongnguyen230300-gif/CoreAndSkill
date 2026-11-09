// Canary của chính các phép đo lớp A và bộ lọc ngoại lệ axe (ADR-0117 T3; 06-testing-strategy.md §8: một test phải
// TỪNG ĐỎ). Mỗi phép đo có ít nhất MỘT ca đỏ — dựng lỗi thật bằng cách cắm CSS/DOM lỗi vào trang đang chạy (hoặc
// vào một trang giả tối thiểu) rồi khẳng định phép đo bắt đúng — và MỘT ca xanh đối chứng (đỏ vì lý do chung chung
// thì không phải canary). Không sửa code sản phẩm: lỗi được cắm ở phía trình duyệt, trong test.
// Phép đo không bắt được lỗi cắm vào là cổng xanh giả — báo, không sửa phép đo cho khớp (test-engineer §🛑 mục 5).
import type { Page } from '@playwright/test';

import { baoLoiClient, meChuaDangNhap, tokenXsrf } from '../du-lieu-gia/khoi-dong';
import type { ApiGia } from './api-gia';
import { apDungNgoaiLe, dinhDangAxe, quetAxe } from './axe';
import type { ViPhamAxe } from './axe';
import { expect, test } from './fixture';
import {
  a1MotH1,
  a2ChuHienThi,
  a3KhongCuon,
  a4HoFont,
  a5ONhap,
  a6KheGiuaVung,
  a6KheTieuDeMoTa,
  a6KheTrongForm,
  a7TuDien,
  a8CoYKhongCo,
  a9AuthCardResponsive,
  a10OMatKhauDemPhai,
  a11ToastKhongCuopFocus,
  a12FocusOLoiDau,
  KHONG_CO_DANG_NHAP_THUONG,
} from './lop-a';
import { loiHeThongKhongLuong } from '../du-lieu-gia/chung';
import { danhSachNguoiDungLoiTai } from '../du-lieu-gia/nguoi-dung';
import { moManVaoH1 } from '../man/ca-trong-khung';
import { hoSoDayDu } from '../du-lieu-gia/ho-so';
import { kichBanQuanTriDonVi } from '../du-lieu-gia/kich-ban';
import { dinhDangViPham } from './lop-a/chung';
import type { NgoaiLeAxe } from './ngoai-le-axe';

// ---------------------------------------------------------------------------------------------------------------
// Trang giả tối thiểu — cùng origin app, dựng bằng `page.route`, không cần phụ thuộc màn nào của sản phẩm.
async function moHtml(page: Page, html: string): Promise<void> {
  await page.route('**/__canary-lop-a', (route) =>
    route.fulfill({ contentType: 'text/html', body: html }),
  );
  await page.goto('/__canary-lop-a');
}

const TRANG = (than: string, dau = '') =>
  `<!doctype html><html lang="vi"><head><meta charset="utf-8"><title>canary</title>${dau}</head><body>${than}</body></html>`;

async function moDangNhap(page: Page, apiGia: ApiGia, duongDan = '/dang-nhap'): Promise<void> {
  apiGia.khaiBao(tokenXsrf, baoLoiClient, meChuaDangNhap);
  await page.goto(duongDan);
  await expect(page.locator('h1')).toBeVisible();
}

const cam = (page: Page, css: string) => page.addStyleTag({ content: css });
const nutGui = (page: Page) => page.locator('main form button[type=submit]');

function chua(viPham: readonly string[], ...manh: (string | RegExp)[]): void {
  const gop = viPham.join('\n');
  expect(gop, 'phép đo phải ĐỎ').not.toBe('');
  for (const m of manh)
    expect(gop).toMatch(
      m instanceof RegExp ? m : new RegExp(m.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')),
    );
}

// ---------------------------------------------------------------------------------------------------------------
test.describe('A1 — một <h1>', () => {
  test('đỏ: trang không có h1 (trang trắng)', async ({ page }) => {
    await moHtml(page, TRANG('<main><p>trống</p></main>'));
    chua((await a1MotH1.do(page)).viPham, '0 thẻ <h1>');
  });
  test('đỏ: hai h1', async ({ page }) => {
    await moHtml(page, TRANG('<main><h1>a</h1><h1>b</h1></main>'));
    chua((await a1MotH1.do(page)).viPham, '2 thẻ <h1>');
  });
  test('đỏ: h1 có trong DOM nhưng ẩn', async ({ page }) => {
    await moHtml(page, TRANG('<main><h1 style="display:none">a</h1></main>'));
    chua((await a1MotH1.do(page)).viPham, 'không hiện');
  });
  test('đỏ: màn xác thực mà h1 ở ngoài <main>', async ({ page }) => {
    await moHtml(page, TRANG('<h1>a</h1><main></main>'));
    chua((await a1MotH1.do(page, { trongMain: true })).viPham, 'không nằm trong <main>');
  });
  test('xanh: một h1 hiện trong main', async ({ page }) => {
    await moHtml(page, TRANG('<main><h1>a</h1></main>'));
    expect((await a1MotH1.do(page, { trongMain: true })).viPham).toEqual([]);
  });
});

test.describe('A2 — chữ hiển thị', () => {
  const GOC = ['xacThuc', 'loi', 'chung'];
  test('đỏ: placeholder "null" (lỗi thật 05/10 của AuthField)', async ({ page }) => {
    await moHtml(page, TRANG('<main><input placeholder="null"></main>'));
    chua((await a2ChuHienThi.do(page, GOC)).viPham, /thuộc tính placeholder.*"null"/);
  });
  test('đỏ: chữ undefined, NaN, [object Object] trong nội dung', async ({ page }) => {
    await moHtml(
      page,
      TRANG('<main><p>undefined</p><p>Tổng NaN đồng</p><p>[object Object]</p></main>'),
    );
    const vp = (await a2ChuHienThi.do(page, GOC)).viPham;
    chua(vp, '"undefined"', 'Tổng NaN đồng', '[object Object]');
  });
  test('đỏ: aria-label, title, alt mang null', async ({ page }) => {
    await moHtml(
      page,
      TRANG(
        '<main><button aria-label="null">x</button><span title="undefined">y</span><img alt="NaN" src="data:,"></main>',
      ),
    );
    chua((await a2ChuHienThi.do(page, GOC)).viPham, 'aria-label', 'title', 'alt');
  });
  test('đỏ: khoá dịch hiện ra thành chữ, trong nội dung và trong thuộc tính', async ({ page }) => {
    await moHtml(
      page,
      TRANG(
        '<main><h1>xacThuc.dangNhap.tieuDe</h1><input placeholder="loi.CORE.AUTH.LOCKED_OUT"></main>',
      ),
    );
    chua(
      (await a2ChuHienThi.do(page, GOC)).viPham,
      'xacThuc.dangNhap.tieuDe',
      'loi.CORE.AUTH.LOCKED_OUT',
    );
  });
  test('đỏ: tiêu đề tab là khoá dịch', async ({ page }) => {
    await moHtml(
      page,
      TRANG('<main></main>').replace(
        '<title>canary</title>',
        '<title>xacThuc.dangNhap.tieuDe</title>',
      ),
    );
    chua((await a2ChuHienThi.do(page, GOC)).viPham, 'document.title');
  });
  test('xanh: chữ thường, tên đăng nhập có dấu chấm, từ chứa "null" trong từ khác, placeholder vắng', async ({
    page,
  }) => {
    await moHtml(
      page,
      TRANG(
        '<main><h1>Đăng nhập</h1><p>an.nv</p><p>nullable</p><p>Tài khoản an.nv đang dùng mật khẩu tạm.</p><input></main>',
      ),
    );
    expect((await a2ChuHienThi.do(page, GOC)).viPham).toEqual([]);
  });
  test('đỏ: chưa có tệp dịch nào thì phép đo ném lỗi, không xanh rỗng', async () => {
    const { docGocKhoaDich } = await import('./lop-a/a2-chu-hien-thi');
    expect(() => docGocKhoaDich(__dirname)).toThrow(/Không đọc được khoá dịch nào/);
    expect(docGocKhoaDich().length).toBeGreaterThan(3);
  });
});

test.describe('A3 — không cuộn khi đủ chỗ', () => {
  const CSS_COT = (minHeightMain: string) =>
    `body{margin:0}.cot{display:flex;flex-direction:column;min-height:100dvh}main{${minHeightMain};padding:16px;display:flex;align-items:center;justify-content:center}main>div{height:300px}footer{height:40px}`;
  test('đỏ: bản lỗi cũ — vùng bọc main tự khai min-height 100dvh rồi Footer xếp sau (lỗi thật 05/10)', async ({
    page,
  }) => {
    await moHtml(
      page,
      TRANG(
        '<div class="cot"><main><div></div></main><footer>f</footer></div>',
        `<style>${CSS_COT('min-height:100dvh')}</style>`,
      ),
    );
    chua((await a3KhongCuon.do(page)).viPham, 'trang cuộn dọc');
  });
  test('đỏ: Footer không sát đáy khi còn dư chỗ', async ({ page }) => {
    await moHtml(
      page,
      TRANG(
        '<div class="cot"><main style="flex:none"><div></div></main><footer>f</footer></div>',
        `<style>${CSS_COT('flex:none')}</style>`,
      ),
    );
    chua((await a3KhongCuon.do(page)).viPham, 'Footer không sát đáy');
  });
  test('xanh: cột min-height 100dvh, main chiếm phần còn lại, Footer sát đáy', async ({ page }) => {
    await moHtml(
      page,
      TRANG(
        '<div class="cot"><main style="flex:1 1 auto"><div></div></main><footer>f</footer></div>',
        `<style>${CSS_COT('flex:1 1 auto')}</style>`,
      ),
    );
    expect((await a3KhongCuon.do(page)).viPham).toEqual([]);
  });
  test('xanh có ghi chú: khung nhìn thấp hơn nội dung thì không đo, nói rõ lý do', async ({
    page,
  }) => {
    await moHtml(
      page,
      TRANG(
        '<div class="cot"><main><div style="height:2000px"></div></main><footer>f</footer></div>',
        `<style>${CSS_COT('flex:1 1 auto')}</style>`,
      ),
    );
    const kq = await a3KhongCuon.do(page);
    expect(kq.viPham).toEqual([]);
    expect(kq.ghiChu).toMatch(/thấp hơn nội dung/);
  });
  test('đỏ trên app thật: cắm lại min-height 100dvh cho vùng bọc khung ở trang đăng nhập', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    await cam(
      page,
      'app-auth-card .auth-card-vung{min-height:100dvh !important}app-auth-page{min-height:0 !important}',
    );
    chua((await a3KhongCuon.do(page)).viPham, 'trang cuộn dọc');
  });
});

test.describe('A4 — họ font', () => {
  test('đỏ trên app thật: nhãn nút chính rơi về Arial (lỗi thật 05/10 của Button)', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    await cam(page, `main form button{font-family:Arial !important}`);
    chua((await a4HoFont.do(page)).viPham, /họ font "Arial", spec đòi "Be Vietnam Pro"/);
  });
  test('đỏ trên app thật: ô nhập không kế thừa — font hệ thống', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, `input{font-family:monospace !important}`);
    chua((await a4HoFont.do(page)).viPham, /<input.*họ font "monospace"/);
  });
  test('đỏ: font đúng tên nhưng không có @font-face nào — chưa nạp', async ({ page }) => {
    await moHtml(
      page,
      TRANG(
        '<main><p style="font-family:var(--font-sans)">chữ</p></main>',
        `<style>:root{--font-sans:'Khong Ton Tai',Arial}</style>`,
      ),
    );
    chua((await a4HoFont.do(page)).viPham, 'không có @font-face');
  });
  test('đỏ: không có token --font-sans', async ({ page }) => {
    await moHtml(page, TRANG('<main><p>chữ</p></main>'));
    chua((await a4HoFont.do(page)).viPham, '--font-sans');
  });
  test('xanh: nút chữ, ô nhập dùng --font-sans; nút chỉ có icon không bị tính là có chữ', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    expect((await a4HoFont.do(page)).viPham).toEqual([]);
  });
});

test.describe('A5 — ô nhập chữ', () => {
  test('đỏ: hai đường song song khi focus — outline xanh cách 2px (lỗi thật 05/10)', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    await cam(
      page,
      `.auth-field__input:focus-visible{outline:2px solid #1a56b8 !important;outline-offset:2px !important}`,
    );
    chua((await a5ONhap.do(page, nutGui(page))).viPham, /hợp lệ · focus: có vòng ngoài \(outline/);
  });
  test('đỏ: ô lỗi nhận focus đỏ trong, xanh ngoài (lỗi thật 05/10)', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    await cam(
      page,
      `.auth-field__khung--loi .auth-field__input:focus-visible{outline:2px solid #1a56b8 !important;outline-offset:2px !important}`,
    );
    chua((await a5ONhap.do(page, nutGui(page))).viPham, /lỗi · focus: có vòng ngoài/);
  });
  test('đỏ: ô lỗi mất màu lỗi khi focus', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    await cam(
      page,
      `.auth-field__khung--loi .auth-field__input:focus-visible{border-color:var(--color-brand) !important}`,
    );
    chua((await a5ONhap.do(page, nutGui(page))).viPham, /lỗi · focus: viền màu/);
  });
  test('đỏ: hover đè viền lỗi (lỗi thật 06/10, Bối cảnh #7)', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    await cam(page, `.auth-field__input:hover{border-color:var(--color-border) !important}`);
    chua(
      (await a5ONhap.do(page, nutGui(page))).viPham,
      /lỗi · hover.*viền màu/,
      /hợp lệ · hover.*viền màu/,
    );
  });
  test('đỏ: ô hợp lệ đậm thêm khi hover (hover không đổi viền)', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    await cam(page, `.auth-field__input:hover{--auth-field-vien-them:1px !important}`);
    chua(
      (await a5ONhap.do(page, nutGui(page))).viPham,
      /hợp lệ · hover \(không focus\): đường biên dày/,
    );
  });
  test('đỏ: viền đổi border-width khi focus — chữ lệch và kích thước đổi', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    await cam(page, `.auth-field__input:focus-visible{border-width:2px !important}`);
    chua((await a5ONhap.do(page, nutGui(page))).viPham, /chữ lệch|kích thước/);
  });
  test('xanh: app hôm nay — đúng một đường biên ở mọi trạng thái', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    expect((await a5ONhap.do(page, nutGui(page))).viPham).toEqual([]);
  });
  test('đỏ: form không chặn ô rỗng nên không dựng được trạng thái lỗi — phép đo nói rõ, không xanh im lặng', async ({
    page,
  }) => {
    await moHtml(
      page,
      TRANG(
        '<main><form onsubmit="return false"><input id="o1"><button type="submit">gui</button></form></main>',
        '<style>:root{--border-w:1px;--border-w-strong:2px;--border-w-accent:3px;--color-border-strong:#667;--color-text:#111;--color-danger-border:#c00}</style>',
      ),
    );
    chua((await a5ONhap.do(page, nutGui(page))).viPham, 'không có aria-invalid="true"');
  });
});

test.describe('A6 — khe liền kề', () => {
  test('đỏ: khe trong form về 0 — nút dính ô mật khẩu, nhãn dính ô (lỗi thật 05/10 của AuthCard)', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    await cam(page, `main form{gap:0 !important}main form>app-button{margin-top:0 !important}`);
    chua(
      (await a6KheTrongForm.do(page)).viPham,
      /→ nút chính: khe 0\.0px, spec đòi 20px/,
      /→ nhãn trường.*khe 0\.0px/,
    );
  });
  test('đỏ: khe nhãn → ô sai bậc', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, `.auth-field__nhan{margin-bottom:0 !important}`);
    chua((await a6KheTrongForm.do(page)).viPham, /nhãn → ô.*khe 0\.0px, spec đòi 8px/);
  });
  test('đỏ: khe giữa hai vùng sai bậc', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, `.auth-card{gap:0 !important}`);
    chua((await a6KheGiuaVung.do(page)).viPham, /khe 0\.0px, spec đòi 24px/);
  });
  test('đỏ: khôi phục lỗi cũ của AuthCard (kéo lại --sp-3 thay vì --sp-3 − --sp-8) → khe 18px, spec đòi 6px', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    await cam(page, `.auth-card__mo-ta{margin-top:calc(var(--sp-3) * -1) !important}`);
    chua(
      (await a6KheTieuDeMoTa.do(page)).viPham,
      /tiêu đề <h1> → mô tả: khe 18\.0px, spec đòi 6px \(--sp-3\)/,
    );
  });
  test('xanh trên app thật: khe tiêu đề → mô tả đúng --sp-3', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia, '/dang-nhap?cheDo=van-hanh');
    expect((await a6KheTieuDeMoTa.do(page)).viPham).toEqual([]);
  });
  test('đỏ: màn không có mô tả mà người gọi đòi A6c', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    chua((await a6KheTieuDeMoTa.do(page)).viPham, 'phải có mô tả');
  });
  test('xanh: khe trong form và giữa vùng đúng bậc, kể cả khi dòng lỗi đang hiện', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    expect((await a6KheTrongForm.do(page)).viPham).toEqual([]);
    expect((await a6KheGiuaVung.do(page)).viPham).toEqual([]);
    await nutGui(page).click();
    await expect(page.locator('main form input[aria-invalid="true"]').first()).toBeVisible();
    expect((await a6KheTrongForm.do(page)).viPham).toEqual([]);
  });
});

test.describe('A7 — ô tự điền', () => {
  test('đỏ: nền tự điền xanh nhạt của Chrome lộ ra (lỗi thật 05/10)', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, `.auth-field__input:-webkit-autofill{box-shadow:none !important}`);
    chua(
      (await a7TuDien.do(page)).viPham,
      /tự điền: nền thật rgb\(232, 240, 254\), spec đòi rgb\(255, 255, 255\)/,
    );
  });
  test('đỏ: chữ tự điền đổi màu', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(
      page,
      `.auth-field__input:-webkit-autofill{-webkit-text-fill-color:rgb(255,0,0) !important}`,
    );
    chua((await a7TuDien.do(page)).viPham, /tự điền: chữ rgb\(255, 0, 0\)/);
  });
  test('đỏ: ô đã tự điền rồi bị khoá không mang nền disabled', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(
      page,
      `.auth-field__input:disabled:-webkit-autofill{box-shadow:inset 0 0 0 100vmax rgb(255,255,255) !important}`,
    );
    chua((await a7TuDien.do(page)).viPham, /tự điền \+ disabled: nền thật/);
  });
  test('xanh: app hôm nay giữ nền và chữ của trạng thái nghỉ, kể cả theme tối', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    expect((await a7TuDien.do(page)).viPham).toEqual([]);
    await page.evaluate(() => document.documentElement.setAttribute('data-theme', 'dark'));
    expect((await a7TuDien.do(page)).viPham).toEqual([]);
  });
});

test.describe('A8 — cố ý không có', () => {
  test('xanh: chế độ thường không có phần tử nào bị cấm', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    expect((await a8CoYKhongCo.do(page, KHONG_CO_DANG_NHAP_THUONG)).viPham).toEqual([]);
  });
  const cacCaDo: [string, string, RegExp][] = [
    ['ô mã đơn vị', `<input autocomplete="organization">`, /ô mã đơn vị ở chế độ thường.*:124/],
    ['ảnh logo', `<img alt="" src="data:,">`, /ảnh logo trong khung.*:127/],
    ['ô ghi nhớ đăng nhập', `<input type="checkbox">`, /ô ghi nhớ đăng nhập.*:128/],
    ['liên kết quên mật khẩu', `<a href="/quen-mat-khau">x</a>`, /liên kết quên mật khẩu.*:129/],
    [
      'LanguageSwitcher',
      `<app-language-switcher></app-language-switcher>`,
      /LanguageSwitcher.*:130/,
    ],
  ];
  for (const [ten, html, mong] of cacCaDo) {
    test(`đỏ: cắm ${ten} vào khung đăng nhập chế độ thường`, async ({ page, apiGia }) => {
      await moDangNhap(page, apiGia);
      await page.evaluate(
        (h) => document.querySelector('main')!.insertAdjacentHTML('beforeend', h),
        html,
      );
      chua((await a8CoYKhongCo.do(page, KHONG_CO_DANG_NHAP_THUONG)).viPham, mong);
    });
  }
  test('đỏ: ô mã đơn vị hiện sau khi đăng nhập trượt vẫn bị bắt (mọi trạng thái)', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    await nutGui(page).click();
    await page.evaluate(() =>
      document
        .querySelector('main form')!
        .insertAdjacentHTML('afterbegin', '<input autocomplete="organization">'),
    );
    chua((await a8CoYKhongCo.do(page, KHONG_CO_DANG_NHAP_THUONG)).viPham, 'ô mã đơn vị');
  });
});

test.describe('axe — quét WCAG A/AA và ngoại lệ có tên', () => {
  const vp = (id: string, ...target: string[]): ViPhamAxe => ({
    id,
    impact: 'serious',
    help: 'h',
    helpUrl: 'u',
    nodes: target.map((t) => ({ target: [t], html: `<x id="${t}">` })),
  });
  const ngoaiLe = (
    quyTac: string,
    boChon: string,
    extra: Partial<NgoaiLeAxe> = {},
  ): NgoaiLeAxe => ({
    quyTac,
    boChon,
    man: 'man-x',
    lyDo: 'lý do',
    ngay: '2026-10-06',
    canCu: 'canary',
    ...extra,
  });

  test('đỏ trên app thật: ảnh thiếu alt bị axe bắt (image-alt)', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await page.evaluate(() =>
      document
        .querySelector('main')!
        .insertAdjacentHTML(
          'beforeend',
          '<img id="anh-xau" src="data:image/gif;base64,R0lGODlhAQABAAAAACw=">',
        ),
    );
    await expect(quetAxe(page, 'man-canary', 'rong-sang')).rejects.toThrow(
      /image-alt[\s\S]*#anh-xau/,
    );
  });
  test('đỏ trên app thật: chữ tương phản thấp bị axe bắt (color-contrast)', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    await page.evaluate(() =>
      document
        .querySelector('main')!
        .insertAdjacentHTML(
          'beforeend',
          '<p id="chu-nhat" style="color:#ddd;background:#fff">chữ nhạt</p>',
        ),
    );
    await expect(quetAxe(page, 'man-canary', 'rong-sang')).rejects.toThrow(
      /color-contrast[\s\S]*#chu-nhat/,
    );
  });
  test('xanh: trang đăng nhập sạch axe, không ngoại lệ nào', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await quetAxe(page, 'man-canary', 'rong-sang');
  });
  test('lọc: ngoại lệ khớp cả quy tắc lẫn phần tử thì gỡ vi phạm đó, không gỡ vi phạm khác', () => {
    const kq = apDungNgoaiLe(
      [vp('color-contrast', '#a', '#b'), vp('image-alt', '#a')],
      'man-x',
      'rong-sang',
      [ngoaiLe('color-contrast', '#a')],
    );
    expect(kq.moCoi).toEqual([]);
    expect(kq.conLai.map((v) => [v.id, v.nodes.map((n) => n.target[0])])).toEqual([
      ['color-contrast', ['#b']],
      ['image-alt', ['#a']],
    ]);
  });
  test('đỏ: ngoại lệ mồ côi — không khớp vi phạm nào — bị nêu tên', () => {
    const kq = apDungNgoaiLe([], 'man-x', 'rong-sang', [ngoaiLe('color-contrast', '#a')]);
    expect(kq.moCoi).toHaveLength(1);
    expect(dinhDangAxe(kq.conLai, kq.moCoi)).toMatch(/ngoại lệ mồ côi: color-contrast × #a/);
  });
  test('đỏ: cùng phần tử nhưng khác quy tắc thì ngoại lệ không che, và mồ côi', () => {
    const kq = apDungNgoaiLe([vp('image-alt', '#a')], 'man-x', 'rong-sang', [
      ngoaiLe('color-contrast', '#a'),
    ]);
    expect(kq.conLai).toHaveLength(1);
    expect(kq.moCoi).toHaveLength(1);
  });
  test('ngoại lệ của màn hay project khác không áp, không bị tính mồ côi', () => {
    const kq = apDungNgoaiLe([], 'man-x', 'rong-sang', [
      ngoaiLe('a', '#a', { man: 'man-khac' }),
      ngoaiLe('a', '#a', { project: ['hep-sang'] }),
    ]);
    expect(kq.moCoi).toEqual([]);
    expect(kq.conLai).toEqual([]);
  });
});

test.describe('khuôn thông điệp đỏ', () => {
  test('thông điệp đỏ nêu mã, tên và từng dòng spec nguồn tệp:dòng', async ({ page }) => {
    await moHtml(page, TRANG('<main></main>'));
    await expect(a1MotH1.kiem(page)).rejects.toThrow(
      /\[A1\][\s\S]*PageHeader\.md:111[\s\S]*AuthCard\.md:145/,
    );
  });
  test('mỗi phép đo mang ít nhất một nguồn dạng docs/Design/...:dòng', () => {
    for (const p of [
      a1MotH1,
      a2ChuHienThi,
      a3KhongCuon,
      a4HoFont,
      a5ONhap,
      a6KheTrongForm,
      a6KheGiuaVung,
      a6KheTieuDeMoTa,
      a7TuDien,
      a8CoYKhongCo,
      a9AuthCardResponsive,
      a10OMatKhauDemPhai,
      a11ToastKhongCuopFocus,
      a12FocusOLoiDau,
    ]) {
      expect(p.thongTin.nguon.length, p.thongTin.ma).toBeGreaterThan(0);
      for (const n of p.thongTin.nguon) expect(n, p.thongTin.ma).toMatch(/^docs\/Design\/\S+:\d+/);
    }
    expect(dinhDangViPham(a1MotH1.thongTin, ['x'])).toContain('docs/Design/');
  });
});

// ---------------------------------------------------------------------------------------------------------------
// Bốn phép đo từ lượt soát trình duyệt 2026-10-06 (A9–A12). Mỗi phép đo: ít nhất một ca ĐỎ tiêm lại lỗi cũ ở phía
// trình duyệt (CSS hay script cắm vào trang đang chạy, không đụng code sản phẩm) và một ca XANH trên app thật.
test.describe('A9 — AuthCard theo bề rộng', () => {
  test('xanh trên app thật: dưới $bp-xs trải hết, không viền/bo góc, đệm --sp-6, căn trên; giữa $bp-xs và $bp-md đệm --sp-8', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    expect((await a9AuthCardResponsive.do(page)).viPham).toEqual([]);
  });
  test('trả khung nhìn về cỡ cũ sau khi đo — kể cả khi phép đo đỏ', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, '.auth-card{border:2px solid red !important}');
    const truoc = page.viewportSize();
    expect((await a9AuthCardResponsive.do(page)).viPham.length).toBeGreaterThan(0);
    expect(page.viewportSize()).toEqual(truoc);
  });
  test('đỏ: khung còn viền và bo góc dưới $bp-xs (lỗi thật 06/10)', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, '.auth-card{border:2px solid red !important;border-radius:12px !important}');
    chua((await a9AuthCardResponsive.do(page)).viPham, /còn viền trên dày 2px/, /còn bo góc/);
  });
  test('đỏ: khung không trải hết bề rộng dưới $bp-xs', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, '.auth-card{max-width:300px !important}');
    chua((await a9AuthCardResponsive.do(page)).viPham, /không trải hết bề rộng/);
  });
  test('đỏ: đệm sai bậc dưới $bp-xs (--sp-8 thay vì --sp-6)', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, '.auth-card{padding:var(--sp-8) !important}');
    chua(
      (await a9AuthCardResponsive.do(page)).viPham,
      /390px: đệm trên 24px, spec đòi 16px \(--sp-6\)/,
    );
  });
  test('đỏ: căn giữa dọc dưới $bp-xs thay vì căn trên', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, '.auth-card-vung{align-items:center !important}');
    chua((await a9AuthCardResponsive.do(page)).viPham, /căn giữa dọc; spec đòi căn trên/);
  });
  test('đỏ: đệm sai bậc ở giữa $bp-xs và $bp-md (--sp-6 thay vì --sp-8)', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    await cam(page, '@media (min-width: 480px){.auth-card{padding:var(--sp-6) !important}}');
    chua(
      (await a9AuthCardResponsive.do(page)).viPham,
      /700px: đệm trên 16px, spec đòi 24px \(--sp-8\)/,
    );
  });
  test('đỏ: không có khung xác thực (trang trống) — phép đo nói rõ, không xanh rỗng', async ({
    page,
  }) => {
    await moHtml(page, TRANG('<div>không có main</div>'));
    chua((await a9AuthCardResponsive.do(page)).viPham, /không tìm thấy <main> có khung/);
  });
});

test.describe('A10 — ô mật khẩu chừa chỗ cho nút hiện/ẩn', () => {
  test('xanh trên app thật: AuthField password ở màn đăng nhập', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    expect((await a10OMatKhauDemPhai.do(page)).viPham).toEqual([]);
  });
  test('đỏ: đệm phải của AuthField về 0 — chữ chạy dưới nút (lỗi thật 06/10)', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    await cam(page, '.auth-field__input{padding-right:0 !important}');
    chua(
      (await a10OMatKhauDemPhai.do(page)).viPham,
      /vùng chữ kéo tới .* nút <button> bắt đầu ở .* chữ dài chạy xuống dưới nút/,
    );
  });
  test('đỏ: đệm phải chỉ đủ một nửa nút', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    await cam(page, '.auth-field__input{padding-right:calc(var(--sp-4) + 14px) !important}');
    chua((await a10OMatKhauDemPhai.do(page)).viPham, /chạy xuống dưới nút/);
  });
  test('đỏ trên Input: cắm đệm phải của Input password không chừa nút', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), hoSoDayDu());
    await page.goto('/ho-so');
    await expect(page.locator('h1')).toBeVisible();
    await cam(page, 'form.form-mat-khau .input{padding-right:var(--sp-4) !important}');
    chua(
      (await a10OMatKhauDemPhai.do(page, 'form.form-mat-khau')).viPham,
      /ô <input#ho-so-mat-khau-hien-tai>/,
    );
  });
  test('xanh trên app thật: Input password ở màn hồ sơ chừa chỗ cho nút', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), hoSoDayDu());
    await page.goto('/ho-so');
    await expect(page.locator('h1')).toBeVisible();
    expect((await a10OMatKhauDemPhai.do(page, 'form.form-mat-khau')).viPham).toEqual([]);
  });
  test('đỏ: không có ô nào mang nút hiện/ẩn — phép đo nói rõ, không xanh rỗng', async ({
    page,
  }) => {
    await moHtml(
      page,
      TRANG('<main><form><input id="o"><button type="submit">gui</button></form></main>'),
    );
    chua((await a10OMatKhauDemPhai.do(page)).viPham, /không có ô nhập nào .* mang nút hiện\/ẩn/);
  });
  test('nút nằm ngoài hộp ô thì không tính là nút của ô — chữ không thể chạy dưới nó', async ({
    page,
  }) => {
    await moHtml(
      page,
      TRANG(
        '<main><div style="display:flex"><input id="o" style="width:200px;padding-right:0"><button type="button">x</button></div></main>',
      ),
    );
    chua((await a10OMatKhauDemPhai.do(page)).viPham, /không có ô nhập nào/);
  });
});

test.describe('A11 — toast lỗi không cướp focus', () => {
  const batToast500 = async (page: Page, apiGia: ApiGia) => {
    await moDangNhap(page, apiGia);
    apiGia.khaiBao({
      phuongThuc: 'POST',
      duongDan: '/core/auth/login',
      phanHoi: loiHeThongKhongLuong(),
      loiCoChuY: true,
    });
    await page.locator('#dang-nhap-ten-dang-nhap').fill('nguoi-dung');
    await page.locator('#dang-nhap-mat-khau').fill('mat-khau-dang-go');
  };
  const gay = (page: Page) => () => page.locator('#dang-nhap-mat-khau').press('Enter');

  test('xanh trên app thật: sau lỗi 500 CORE.SYSTEM.UNEXPECTED toast hiện mà focus không ở trong toast', async ({
    page,
    apiGia,
  }) => {
    await batToast500(page, apiGia);
    expect((await a11ToastKhongCuopFocus.do(page, gay(page))).viPham).toEqual([]);
  });
  test('đỏ: tiêm lại lỗi cũ — nút đóng của toast nhận focus ngay sau khi chèn (autofocus của PrimeNG)', async ({
    page,
    apiGia,
  }) => {
    await page.addInitScript(() => {
      new MutationObserver((ms) => {
        for (const m of ms) {
          m.addedNodes.forEach((n) => {
            if (!(n instanceof Element)) return;
            const nut = n.matches('.p-toast-close-button')
              ? n
              : n.querySelector('.p-toast-close-button');
            if (nut instanceof HTMLElement) {
              requestAnimationFrame(() =>
                requestAnimationFrame(() => requestAnimationFrame(() => nut.focus())),
              );
            }
          });
        }
      }).observe(document, { childList: true, subtree: true });
    });
    await batToast500(page, apiGia);
    chua(
      (await a11ToastKhongCuopFocus.do(page, gay(page))).viPham,
      /focus nằm ở <button.*trong vùng toast — toast đã cướp focus/,
    );
  });
  test('đỏ: không có toast nào hiện — phép đo nói rõ, không xanh rỗng', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    chua(
      (await a11ToastKhongCuopFocus.do(page, async () => undefined)).viPham,
      /không thấy toast nào/,
    );
  });
});

test.describe('A12 — gửi form còn lỗi: focus về ô lỗi đầu tiên', () => {
  const guiRong = async (page: Page) => {
    await nutGui(page).click();
    await expect(page.locator('main form input[aria-invalid="true"]').first()).toBeVisible();
  };
  test('xanh trên app thật: gửi rỗng ở màn đăng nhập, focus ở ô lỗi đầu', async ({
    page,
    apiGia,
  }) => {
    await moDangNhap(page, apiGia);
    await guiRong(page);
    expect((await a12FocusOLoiDau.do(page)).viPham).toEqual([]);
  });
  test('đỏ: tiêm lại lỗi cũ — ứng dụng không chuyển focus, focus ở lại nút gửi', async ({
    page,
    apiGia,
  }) => {
    await page.addInitScript(() => {
      HTMLElement.prototype.focus = () => undefined;
    });
    await moDangNhap(page, apiGia);
    await guiRong(page);
    chua(
      (await a12FocusOLoiDau.do(page)).viPham,
      /focus đang ở <button.*spec đòi ô lỗi đầu tiên <input#dang-nhap-ten-dang-nhap>/,
    );
  });
  test('đỏ: focus nhảy tới ô cuối của form thay vì ô lỗi đầu', async ({ page, apiGia }) => {
    await page.addInitScript(() => {
      const goc = HTMLElement.prototype.focus;
      HTMLElement.prototype.focus = function (
        this: HTMLElement,
        ...doi: Parameters<HTMLElement['focus']>
      ) {
        const cung = this.closest('form')?.querySelectorAll('input:not([type=hidden])');
        return goc.apply(cung && cung.length ? (cung[cung.length - 1] as HTMLElement) : this, doi);
      };
    });
    await moDangNhap(page, apiGia);
    await guiRong(page);
    chua(
      (await a12FocusOLoiDau.do(page)).viPham,
      /focus đang ở <input#dang-nhap-mat-khau>, spec đòi ô lỗi đầu tiên <input#dang-nhap-ten-dang-nhap>/,
    );
  });
  test('đỏ: chưa có ô lỗi nào — phép đo nói rõ, không xanh rỗng', async ({ page, apiGia }) => {
    await moDangNhap(page, apiGia);
    chua((await a12FocusOLoiDau.do(page)).viPham, /không có ô lỗi nào/);
  });
});

// Bộ dựng trạng thái của màn trong khung (`man/ca-trong-khung.ts`): trạng thái phải đúng là trạng thái nó mang tên.
test.describe('moManVaoH1 — trạng thái dựng đúng chỗ', () => {
  test('xanh: màn gọi đúng ca lỗi đã khai, URL giữ nguyên', async ({ page, apiGia }) => {
    await moManVaoH1('/quan-tri/nguoi-dung', [...kichBanQuanTriDonVi(), danhSachNguoiDungLoiTai()])(
      page,
      apiGia,
    );
  });
  test('đỏ: ca lỗi có chủ ý đã khai mà màn không gọi tới — "trạng thái lỗi" chỉ là màn mặc định', async ({
    page,
    apiGia,
  }) => {
    await expect(
      moManVaoH1('/trang-chu', [...kichBanQuanTriDonVi(), danhSachNguoiDungLoiTai()])(page, apiGia),
    ).rejects.toThrow(/GET \/core\/users chưa được màn gọi/);
  });
  test('đỏ: URL bị chuyển hướng khỏi đường dẫn đã mở', async ({ page, apiGia }) => {
    await expect(moManVaoH1('/', kichBanQuanTriDonVi())(page, apiGia)).rejects.toThrow(
      /chuyển hướng khỏi đường dẫn/,
    );
  });
});

// A5, A7 trên `Input` (màn hồ sơ) — cùng phép đo, chọn ô bằng `vung`/`boChonO`; mỗi ca phải ĐỎ khi tiêm lại lỗi cũ.
test.describe('A5, A7 trên Input (màn hồ sơ)', () => {
  const moHoSo = async (page: Page, apiGia: ApiGia) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), hoSoDayDu());
    await page.goto('/ho-so');
    await expect(page.locator('h1')).toBeVisible();
  };
  const nutLuu = (page: Page) => page.locator('form.form-thong-tin button[type=submit]');
  const oHoTen = 'form.form-thong-tin input[autocomplete="name"]';

  test('xanh: Input text (họ tên) đúng một đường biên ở mọi trạng thái', async ({
    page,
    apiGia,
  }) => {
    await moHoSo(page, apiGia);
    expect((await a5ONhap.do(page, nutLuu(page), 'form.form-thong-tin', oHoTen)).viPham).toEqual(
      [],
    );
  });
  test('đỏ trên Input: ô lỗi mất màu lỗi khi focus', async ({ page, apiGia }) => {
    await moHoSo(page, apiGia);
    await cam(page, '.input--error:focus-visible{border-color:var(--color-brand) !important}');
    chua(
      (await a5ONhap.do(page, nutLuu(page), 'form.form-thong-tin', oHoTen)).viPham,
      /ô #ho-so-ho-ten · lỗi · focus: viền màu/,
    );
  });
  test('đỏ trên Input: hai đường song song khi focus — outline xanh cách 2px', async ({
    page,
    apiGia,
  }) => {
    await moHoSo(page, apiGia);
    await cam(
      page,
      '.input:focus-visible{outline:2px solid #1a56b8 !important;outline-offset:2px !important}',
    );
    chua(
      (await a5ONhap.do(page, nutLuu(page), 'form.form-thong-tin', oHoTen)).viPham,
      /ô #ho-so-ho-ten · hợp lệ · focus: có vòng ngoài/,
    );
  });
  test('đỏ: boChonO không khớp ô nào — phép đo nói rõ, không xanh rỗng', async ({
    page,
    apiGia,
  }) => {
    await moHoSo(page, apiGia);
    chua(
      (
        await a5ONhap.do(
          page,
          nutLuu(page),
          'form.form-thong-tin',
          'form.form-thong-tin input.khong-co',
        )
      ).viPham,
      /không có ô nhập nào/,
    );
  });
  test('xanh: Input password giữ nền và chữ khi tự điền', async ({ page, apiGia }) => {
    await moHoSo(page, apiGia);
    expect((await a7TuDien.do(page, 'form.form-mat-khau')).viPham).toEqual([]);
  });
  test('đỏ trên Input: nền tự điền xanh nhạt của Chrome lộ ra', async ({ page, apiGia }) => {
    await moHoSo(page, apiGia);
    await cam(page, '.input:-webkit-autofill{box-shadow:none !important}');
    chua(
      (await a7TuDien.do(page, 'form.form-mat-khau')).viPham,
      /tự điền: nền thật rgb\(232, 240, 254\)/,
    );
  });
});
