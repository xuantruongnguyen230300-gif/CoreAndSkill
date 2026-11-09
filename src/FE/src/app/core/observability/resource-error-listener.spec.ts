import { provideEnvironmentInitializer } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ClientErrorReporter } from './client-error-reporter';
import { dungBaoCao } from './client-error-report';
import { ResourceLoadError, nghePhanTuTaiHong } from './resource-error-listener';

/** Phần tử nằm trong tài liệu thật — sự kiện `error` của nó đi qua giai đoạn capture của `window`. */
function phanTuTrongTaiLieu(
  tag: 'img' | 'script' | 'link',
  thuocTinh: Record<string, string>,
): Element {
  const phanTu = document.createElement(tag);
  for (const [ten, giaTri] of Object.entries(thuocTinh)) {
    phanTu.setAttribute(ten, giaTri);
  }
  document.body.appendChild(phanTu);
  return phanTu;
}

describe('nghePhanTuTaiHong — nguồn lỗi thứ ba của 10-observability.md §3', () => {
  let capture: jasmine.Spy;
  const phanTuTamThoi: Element[] = [];

  function dungPhanTu(tag: 'img' | 'script' | 'link', thuocTinh: Record<string, string>): Element {
    const phanTu = phanTuTrongTaiLieu(tag, thuocTinh);
    phanTuTamThoi.push(phanTu);
    return phanTu;
  }

  function lamHongTaiNguyen(phanTu: Element): void {
    phanTu.dispatchEvent(new Event('error'));
  }

  beforeEach(() => {
    capture = jasmine.createSpy('capture');
    TestBed.configureTestingModule({
      providers: [
        { provide: ClientErrorReporter, useValue: { capture } },
        provideEnvironmentInitializer(nghePhanTuTaiHong),
      ],
    });
    TestBed.inject(ClientErrorReporter); // dựng injector, chạy initializer
  });

  afterEach(() => {
    for (const phanTu of phanTuTamThoi.splice(0)) {
      phanTu.remove();
    }
  });

  it('ảnh tải hỏng → báo ResourceLoadError, tên thẻ và URL, KHÔNG query/fragment', () => {
    const anh = dungPhanTu('img', { src: 'https://cdn.test/anh/a.png?token=bimat&hoTen=Nguyen#x' });

    lamHongTaiNguyen(anh);

    expect(capture).toHaveBeenCalledTimes(1);
    const loi = capture.calls.mostRecent().args[0] as ResourceLoadError;
    expect(loi).toBeInstanceOf(ResourceLoadError);
    expect(loi.message).toBe('Không tải được tài nguyên <img> https://cdn.test/anh/a.png');
    expect(loi.message).not.toContain('bimat');
  });

  it('script tải hỏng → báo, đường dẫn tương đối được giải theo tài liệu', () => {
    const script = dungPhanTu('script', { src: '/assets/phu-tro.js?v=3' });

    lamHongTaiNguyen(script);

    const loi = capture.calls.mostRecent().args[0] as ResourceLoadError;
    expect(loi.message).toBe(
      `Không tải được tài nguyên <script> ${location.origin}/assets/phu-tro.js`,
    );
  });

  it('stylesheet (href) tải hỏng → báo', () => {
    const link = dungPhanTu('link', { rel: 'stylesheet', href: 'https://cdn.test/s.css' });

    lamHongTaiNguyen(link);

    expect(capture).toHaveBeenCalledTimes(1);
  });

  it('data: và blob: chỉ giữ tên scheme — nội dung có thể là tệp người dùng chọn', () => {
    const anh = dungPhanTu('img', { src: 'data:image/png;base64,QUJDREVGRw==' });

    lamHongTaiNguyen(anh);

    const loi = capture.calls.mostRecent().args[0] as ResourceLoadError;
    expect(loi.message).toBe('Không tải được tài nguyên <img> data:');
    expect(loi.message).not.toContain('QUJD');
  });

  it('báo cáo của lỗi tải tài nguyên không có stack', () => {
    const baoCao = dungBaoCao(new ResourceLoadError('x'), {
      duongDan: '/',
      traceId: null,
      phienBanApp: 'v',
    });

    expect(baoCao.kind).toBe('ResourceLoadError');
    expect('stack' in baoCao).toBeFalse();
  });

  it('sự kiện error có đích KHÔNG phải phần tử (đã đến ErrorHandler bằng đường khác) → bỏ qua, không báo hai lần', () => {
    document.dispatchEvent(new Event('error'));

    expect(capture).not.toHaveBeenCalled();
  });

  it('bộ thu lỗi ném → bộ nghe nuốt, không thành lỗi window (vòng lặp)', () => {
    capture.and.throwError('bộ thu lỗi hỏng');
    const anh = dungPhanTu('img', { src: 'https://cdn.test/b.png' });

    expect(() => lamHongTaiNguyen(anh)).not.toThrow();
    // Một lỗi bộ nghe ném sẽ bị Jasmine bắt ở window.onerror và làm hỏng spec này.
  });

  it('gỡ bộ nghe khi injector bị huỷ', () => {
    TestBed.resetTestingModule();
    const anh = dungPhanTu('img', { src: 'https://cdn.test/c.png' });

    lamHongTaiNguyen(anh);

    expect(capture).not.toHaveBeenCalled();
  });
});
