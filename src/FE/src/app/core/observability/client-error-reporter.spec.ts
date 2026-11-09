import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { CLIENT_ERROR_OUTPUT, ClientErrorOutput } from './client-error-output';
import { ClientErrorReport } from './client-error-report';
import { ClientErrorReporter } from './client-error-reporter';

const CUA_SO_MS = 60_000;

class DauRaGhi implements ClientErrorOutput {
  readonly daGui: { baoCao: ClientErrorReport; traceIdTruoc: readonly string[] }[] = [];
  khiGui: () => void = () => undefined;

  send(baoCao: ClientErrorReport, traceIdTruoc: readonly string[]): void {
    this.daGui.push({ baoCao, traceIdTruoc });
    this.khiGui();
  }
}

describe('ClientErrorReporter', () => {
  let dauRa: DauRaGhi;
  let reporter: ClientErrorReporter;

  beforeEach(() => {
    dauRa = new DauRaGhi();
    TestBed.configureTestingModule({
      providers: [
        { provide: CLIENT_ERROR_OUTPUT, useValue: dauRa },
        {
          provide: DOCUMENT,
          useValue: {
            location: {
              pathname: '/phieu/danh-sach',
              search: '?hoTen=bimat',
              href: 'x?hoTen=bimat',
            },
          },
        },
      ],
    });
    reporter = TestBed.inject(ClientErrorReporter);
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date(2026, 10, 6, 9, 0, 0));
  });

  afterEach(() => jasmine.clock().uninstall());

  describe('báo cáo gửi đi', () => {
    it('mang đường dẫn hiện tại, không query', () => {
      reporter.capture(new TypeError('hỏng'));

      expect(dauRa.daGui.length).toBe(1);
      expect(dauRa.daGui[0].baoCao.duongDan).toBe('/phieu/danh-sach');
      expect(JSON.stringify(dauRa.daGui[0].baoCao)).not.toContain('bimat');
    });

    it('phienBanApp không rỗng kể cả khi bản dựng không có mã commit', () => {
      reporter.capture(new Error('x'));

      expect(dauRa.daGui[0].baoCao.phienBanApp).not.toBe('');
    });
  });

  describe('§4.1 ràng buộc 2 — giới hạn tần suất', () => {
    function baoNhieuLoiKhacNhau(soLoi: number): void {
      for (let i = 0; i < soLoi; i++) {
        reporter.capture(new Error(`lỗi khác nhau số ${i}`));
      }
    }

    it('một lỗi lặp nghìn lần (vòng lặp vẽ lại) chỉ ra MỘT báo cáo', () => {
      for (let i = 0; i < 1000; i++) {
        reporter.capture(new TypeError('đọc thuộc tính của undefined'));
      }

      expect(dauRa.daGui.length).toBe(1);
    });

    it('lỗi nghìn lần KHÔNG tốn thêm hạn mức: vẫn còn chỗ cho các lỗi khác tới trần', () => {
      for (let i = 0; i < 1000; i++) {
        reporter.capture(new Error('lặp'));
      }
      baoNhieuLoiKhacNhau(10);

      // 1 (lỗi lặp) + 4 lỗi khác = 5 báo cáo = đúng trần; phần còn lại bị chặn.
      expect(dauRa.daGui.length).toBe(5);
    });

    it('nghìn lỗi KHÁC NHAU cũng bị chặn ở trần, không vô hạn', () => {
      baoNhieuLoiKhacNhau(1000);

      expect(dauRa.daGui.length).toBe(5);
    });

    it('hết một cửa sổ thì báo lại được — lỗi vẫn còn thì vẫn đáng biết', () => {
      baoNhieuLoiKhacNhau(1000);
      expect(dauRa.daGui.length).toBe(5);

      jasmine.clock().tick(CUA_SO_MS - 1);
      reporter.capture(new Error('lỗi khác nhau số 0'));
      expect(dauRa.daGui.length).toBe(5);

      jasmine.clock().tick(1);
      reporter.capture(new Error('lỗi khác nhau số 0'));
      expect(dauRa.daGui.length).toBe(6);
    });
  });

  describe('§4.1 ràng buộc 3 — bọc kín, không vòng lặp', () => {
    it('đầu ra ném lỗi → capture không ném, và lần sau vẫn báo được', () => {
      dauRa.khiGui = () => {
        throw new Error('đầu ra hỏng');
      };

      expect(() => reporter.capture(new Error('một'))).not.toThrow();

      dauRa.khiGui = () => undefined;
      reporter.capture(new Error('hai'));
      expect(dauRa.daGui.map((g) => g.baoCao.message)).toEqual(['một', 'hai']);
    });

    it('việc báo lỗi tự sinh lỗi rồi quay lại capture → lần vào lại bị bỏ, không vòng lặp', () => {
      dauRa.khiGui = () => reporter.capture(new Error('lỗi sinh ra từ chính việc báo lỗi'));

      reporter.capture(new Error('lỗi gốc'));

      expect(dauRa.daGui.map((g) => g.baoCao.message)).toEqual(['lỗi gốc']);
    });

    it('lỗi tự sinh ra TRONG LÚC dựng báo cáo (getter ném) → không ném, không báo, lần sau vẫn chạy', () => {
      const loiHoc = new Error('gốc');
      Object.defineProperty(loiHoc, 'message', {
        get: () => {
          throw new Error('getter ném');
        },
      });

      expect(() => reporter.capture(loiHoc)).not.toThrow();
      expect(dauRa.daGui.length).toBe(0);

      reporter.capture(new Error('bình thường'));
      expect(dauRa.daGui.length).toBe(1);
    });
  });

  describe('§5.2 — traceId của request gần nhất', () => {
    const phanHoi = (traceId: string): unknown => ({ success: true, data: null, traceId });

    it('báo cáo mang traceId của phản hồi cuối cùng; các traceId trước đi riêng', () => {
      reporter.ghiNhanTuPhanHoi(phanHoi('t1'));
      reporter.ghiNhanTuPhanHoi(phanHoi('t2'));
      reporter.ghiNhanTuPhanHoi(phanHoi('t3'));

      reporter.capture(new Error('x'));

      expect(dauRa.daGui[0].baoCao.traceId).toBe('t3');
      expect(dauRa.daGui[0].traceIdTruoc).toEqual(['t1', 't2']);
    });

    it('chưa có phản hồi nào → báo cáo không có traceId', () => {
      reporter.capture(new Error('x'));

      expect('traceId' in dauRa.daGui[0].baoCao).toBeFalse();
      expect(dauRa.daGui[0].traceIdTruoc).toEqual([]);
    });

    it('chỉ giữ một số ít traceId gần nhất', () => {
      for (let i = 1; i <= 20; i++) {
        reporter.ghiNhanTuPhanHoi(phanHoi(`t${i}`));
      }

      const dangGiu = reporter.traceIdGanDay();
      expect(dangGiu.length).toBeLessThan(20);
      expect(dangGiu.at(-1)).toBe('t20');
    });

    it('cùng traceId liên tiếp chỉ tính một lần', () => {
      reporter.ghiNhanTuPhanHoi(phanHoi('t1'));
      reporter.ghiNhanTuPhanHoi(phanHoi('t1'));

      expect(reporter.traceIdGanDay()).toEqual(['t1']);
    });

    it('thân không phải envelope (null, chuỗi, Blob, thiếu traceId, traceId rỗng) → bỏ qua, không ném', () => {
      const than = [
        null,
        undefined,
        'html',
        new Blob(['x']),
        { success: true },
        { traceId: '' },
        { traceId: 7 },
      ];
      for (const mot of than) {
        expect(() => reporter.ghiNhanTuPhanHoi(mot)).not.toThrow();
      }

      expect(reporter.traceIdGanDay()).toEqual([]);
    });

    it('thân là Proxy ném khi đọc → không ném ra ngoài', () => {
      const than = new Proxy(
        {},
        {
          get: () => {
            throw new Error('proxy ném');
          },
        },
      );

      expect(() => reporter.ghiNhanTuPhanHoi(than)).not.toThrow();
    });
  });
});
