import { DOCUMENT } from '@angular/common';
import { Injectable, inject, isDevMode } from '@angular/core';

import { MA_COMMIT } from '../config/build-info';
import { CLIENT_ERROR_OUTPUT } from './client-error-output';
import { ClientErrorReport, dauVanTay, dungBaoCao } from './client-error-report';

/**
 * Giới hạn tần suất (10-observability.md §4.1 ràng buộc 2). Tài liệu đòi giới hạn nhưng KHÔNG nêu con
 * số — hai số dưới là lựa chọn của hiện thực này, đổi ở đây là đổi cho mọi đầu ra:
 * - BE cho 20 báo cáo / 60 giây mỗi IP (`CoreRateLimitOptions.ClientErrorsPermitLimit`); mỗi tab giữ
 *   dưới mức đó để vài tab sau cùng một IP không tự chặn nhau.
 * - Cùng một lỗi (cùng dấu vân tay) chỉ báo một lần trong cửa sổ và KHÔNG tốn hạn mức: vòng lặp vẽ
 *   lại sinh hàng nghìn bản y hệt chỉ tốn một chỗ.
 */
const CUA_SO_GIOI_HAN_MS = 60_000;
const TOI_DA_BAO_CAO_MOI_CUA_SO = 5;

/** Số `traceId` gần nhất giữ lại (10-observability.md §5.2: "request cuối cùng và một số ít trước đó"). */
const SO_TRACE_ID_GIU = 5;

/**
 * Bộ thu lỗi runtime của FE. Nhận một giá trị lỗi bất kỳ, dựng báo cáo đã che (client-error-report.ts),
 * áp giới hạn tần suất, rồi đưa cho đầu ra (`CLIENT_ERROR_OUTPUT`).
 *
 * Bọc kín (§4.1 ràng buộc 3): `capture` và `ghiNhanTuPhanHoi` KHÔNG BAO GIỜ ném. Cờ `dangBao` chặn
 * vòng lặp cổ điển — nếu việc báo lỗi tự gây lỗi và lỗi đó quay lại `capture` (qua `ErrorHandler`,
 * hay qua đầu ra gọi ngược), lần vào lại bị bỏ, không báo.
 */
@Injectable({ providedIn: 'root' })
export class ClientErrorReporter {
  private readonly output = inject(CLIENT_ERROR_OUTPUT);
  private readonly document = inject(DOCUMENT);

  private readonly cacTraceId: string[] = [];
  private readonly lanBaoGanDay = new Map<string, number>();
  private readonly thoiDiemDaBao: number[] = [];
  private dangBao = false;

  capture(loi: unknown): void {
    if (this.dangBao) {
      return;
    }
    this.dangBao = true;
    try {
      const baoCao = dungBaoCao(loi, {
        duongDan: this.document.location?.pathname ?? '/',
        traceId: this.cacTraceId.at(-1) ?? null,
        phienBanApp: MA_COMMIT ?? '',
      });
      if (this.choPhepBao(baoCao)) {
        this.output.send(baoCao, this.cacTraceId.slice(0, -1));
      }
    } catch (loiCuaBoThu) {
      this.canhBaoKhiPhatTrien(loiCuaBoThu);
    } finally {
      this.dangBao = false;
    }
  }

  /**
   * Ghi `traceId` của một phản hồi HTTP đã hoàn tất — thành công lẫn thất bại (§5.2). Nhận cả thân
   * phản hồi chứ không nhận `traceId` rời: nơi gọi (interceptor) không phải biết envelope trông thế
   * nào, và thân không phải envelope (tệp tải về, HTML của proxy) tự bị bỏ qua.
   */
  ghiNhanTuPhanHoi(thanPhanHoi: unknown): void {
    try {
      const traceId =
        typeof thanPhanHoi === 'object' && thanPhanHoi !== null
          ? (thanPhanHoi as { traceId?: unknown }).traceId
          : undefined;
      if (typeof traceId !== 'string' || traceId === '' || this.cacTraceId.at(-1) === traceId) {
        return;
      }
      this.cacTraceId.push(traceId);
      if (this.cacTraceId.length > SO_TRACE_ID_GIU) {
        this.cacTraceId.shift();
      }
    } catch (loiCuaBoThu) {
      this.canhBaoKhiPhatTrien(loiCuaBoThu);
    }
  }

  /** Các `traceId` đang giữ, cũ nhất trước — request cuối cùng ở cuối. Dùng cho test và log phát triển. */
  traceIdGanDay(): readonly string[] {
    return [...this.cacTraceId];
  }

  private choPhepBao(baoCao: ClientErrorReport): boolean {
    const bay = Date.now();
    this.donCuaSo(bay);

    const dau = dauVanTay(baoCao);
    if (this.lanBaoGanDay.has(dau)) {
      return false;
    }
    if (this.thoiDiemDaBao.length >= TOI_DA_BAO_CAO_MOI_CUA_SO) {
      return false;
    }
    this.lanBaoGanDay.set(dau, bay);
    this.thoiDiemDaBao.push(bay);
    return true;
  }

  /** Bỏ mọi dấu vết cũ hơn một cửa sổ — kích cỡ cả hai cấu trúc bị chặn bởi `TOI_DA_BAO_CAO_MOI_CUA_SO`. */
  private donCuaSo(bay: number): void {
    while (this.thoiDiemDaBao.length > 0 && bay - this.thoiDiemDaBao[0] >= CUA_SO_GIOI_HAN_MS) {
      this.thoiDiemDaBao.shift();
    }
    for (const [dau, luc] of this.lanBaoGanDay) {
      if (bay - luc >= CUA_SO_GIOI_HAN_MS) {
        this.lanBaoGanDay.delete(dau);
      }
    }
  }

  /** Bộ thu lỗi tự hỏng thì im lặng với người dùng — nhưng không im với người phát triển. */
  private canhBaoKhiPhatTrien(loi: unknown): void {
    if (isDevMode()) {
      console.warn('[ClientErrorReporter] bộ thu lỗi tự gặp lỗi, bỏ qua báo cáo này:', loi);
    }
  }
}
