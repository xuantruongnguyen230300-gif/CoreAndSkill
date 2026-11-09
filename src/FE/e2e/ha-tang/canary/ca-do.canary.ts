// Các ca ĐỎ có chủ ý của canary hạ tầng API giả (ADR-0117 T2). Không phải spec của lưới: tên tệp
// `.canary.ts` nên `playwright.config.ts` KHÔNG chạy; chỉ `canary-api-gia.spec.ts` chạy chúng qua
// `playwright.canary.config.ts` và khẳng định từng ca ĐỎ đúng chỗ, với thông điệp nêu đúng URL.
// Tiền tố tên: `DO` — phải đỏ; `XANH` — phải xanh (ca đối chứng: đỏ vì lý do chung chung thì không phải canary).
import { thanhCong, thatBai } from '../envelope';
import { expect, test } from '../fixture';
import { API, goi, JSON_XSRF, moTrangCanary } from './trang-canary';

test('DO request GET chưa khai báo', async ({ page }) => {
  await moTrangCanary(page);
  await goi(page, `${API}/core/khong-co-ca`);
});

test('DO request POST JSON kèm XSRF chưa khai báo', async ({ page }) => {
  await moTrangCanary(page);
  await goi(page, `${API}/core/khong-co-ca-ghi`, {
    method: 'POST',
    headers: JSON_XSRF,
    body: '{}',
  });
});

test('DO request ra ngoài origin app và ngoài tiền tố API', async ({ page }) => {
  await moTrangCanary(page);
  await goi(page, 'https://ngoai-pham-vi.invalid/x');
});

test('DO phản hồi lỗi đã khai nhưng không đánh dấu loiCoChuY', async ({ page, apiGia }) => {
  apiGia.khaiBao({
    phuongThuc: 'GET',
    duongDan: '/core/auth/me',
    phanHoi: thatBai('CORE.AUTH.NOT_AUTHENTICATED', { type: 'Unauthorized', status: 401 }),
  });
  await moTrangCanary(page);
  await goi(page, `${API}/core/auth/me`);
});

test('DO console.error của trang dù đã có ca lỗi chủ ý', async ({ page, apiGia }) => {
  apiGia.khaiBao({
    phuongThuc: 'GET',
    duongDan: '/core/auth/me',
    phanHoi: thatBai('CORE.AUTH.NOT_AUTHENTICATED', { type: 'Unauthorized', status: 401 }),
    loiCoChuY: true,
  });
  await moTrangCanary(page);
  await goi(page, `${API}/core/auth/me`);
  await page.evaluate(() => console.error('loi-tu-ghi-cua-trang'));
});

test('DO ngoại lệ trang', async ({ page }) => {
  await moTrangCanary(page);
  await page.evaluate(() => {
    setTimeout(() => {
      throw new Error('ngoai-le-co-chu-y-cua-canary');
    }, 0);
  });
  await page.waitForTimeout(100);
});

test('XANH lỗi có chủ ý đã khai + POST JSON khác nguồn kèm XSRF nhận dữ liệu giả', async ({
  page,
  apiGia,
}) => {
  apiGia.khaiBao(
    {
      phuongThuc: 'GET',
      duongDan: '/core/auth/me',
      phanHoi: thatBai('CORE.AUTH.NOT_AUTHENTICATED', { type: 'Unauthorized', status: 401 }),
      loiCoChuY: true,
    },
    { phuongThuc: 'POST', duongDan: '/core/ghi-thu', phanHoi: thanhCong({ ok: 1 }) },
  );
  await moTrangCanary(page);
  expect((await goi(page, `${API}/core/auth/me`)).status).toBe(401);
  expect(
    (await goi(page, `${API}/core/ghi-thu`, { method: 'POST', headers: JSON_XSRF, body: '{}' }))
      .status,
  ).toBe(200);
});
