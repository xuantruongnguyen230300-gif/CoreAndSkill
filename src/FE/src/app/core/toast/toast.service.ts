import { Injectable, signal } from '@angular/core';

export type ToastSeverity = 'success' | 'error' | 'info' | 'warn';

export interface ToastMessage {
  /** Tăng dần — phân biệt hai thông điệp trùng nội dung là hai lần hiện khác nhau. */
  readonly id: number;
  readonly severity: ToastSeverity;
  readonly summary: string;
  readonly traceId: string | null;
}

/**
 * Nơi DUY NHẤT hiện thông báo chung. Ở `core/` nên KHÔNG được biết thư viện UI (luật F5) —
 * component hiển thị thật nằm ở `shared/ui/toast/`, đọc `hangDoi()` rồi lấy hết bằng `layHet()`.
 *
 * Hàng đợi, không phải một ô: signal gộp các lần ghi trước khi effect chạy, nên hai thông báo liên
 * tiếp trong cùng một nhịp mà chỉ giữ ô cuối thì thông báo đầu mất im lặng.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  private readonly _latest = signal<ToastMessage | null>(null);
  private readonly _hangDoi = signal<readonly ToastMessage[]>([]);
  /** Thông báo gần nhất — chỉ để đọc/kiểm, KHÔNG phải nguồn để vẽ (xem `hangDoi`). */
  readonly latest = this._latest.asReadonly();
  /** Các thông báo chưa được vẽ, theo thứ tự đẩy vào. */
  readonly hangDoi = this._hangDoi.asReadonly();

  /** Lấy và xoá mọi thông báo đang chờ — mỗi thông báo được vẽ đúng một lần. */
  layHet(): readonly ToastMessage[] {
    const ds = this._hangDoi();
    if (ds.length > 0) {
      this._hangDoi.set([]);
    }
    return ds;
  }

  loi(summary: string, traceId: string | null = null): void {
    this.push('error', summary, traceId);
  }

  thanhCong(summary: string): void {
    this.push('success', summary, null);
  }

  thongTin(summary: string): void {
    this.push('info', summary, null);
  }

  canhBao(summary: string): void {
    this.push('warn', summary, null);
  }

  private push(severity: ToastSeverity, summary: string, traceId: string | null): void {
    const message: ToastMessage = { id: ++this.seq, severity, summary, traceId };
    this._latest.set(message);
    this._hangDoi.update((ds) => [...ds, message]);
  }
}
