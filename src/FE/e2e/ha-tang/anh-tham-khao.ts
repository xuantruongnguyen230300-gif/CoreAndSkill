// Ảnh chụp THAM KHẢO (ADR-0117 QC4): đính vào báo cáo của test, thư mục kết quả bị `.gitignore` loại.
// Không so với ảnh nào, không chặn, không commit, không phải tài liệu — ảnh và spec Design khác nhau thì spec đúng.
// Không có `toHaveScreenshot`, không ảnh chuẩn.
import type { Page, TestInfo } from '@playwright/test';

export async function chupThamKhao(
  page: Page,
  testInfo: TestInfo,
  trangThai: string,
): Promise<void> {
  const ten = `${testInfo.project.name}--${trangThai}`.replace(/[^\w.-]+/g, '-');
  await testInfo.attach(`anh-tham-khao--${ten}`, {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png',
  });
}
