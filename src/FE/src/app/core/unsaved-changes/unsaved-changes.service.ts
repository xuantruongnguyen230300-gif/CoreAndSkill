import { DestroyRef, Injectable, inject, signal } from '@angular/core';

/**
 * Định nghĩa gốc: quy-uoc/fe-routing-guard.md §4.1. "Một chỗ" giữ cả việc HỎI (mở hộp) và việc
 * CHẶN (trả kết quả cho guard/khung ứng dụng) — màn chỉ đăng ký hàm kiểm "còn thay đổi chưa
 * lưu" qua `dangKy`, không tự vẽ hộp thoại, không tự nghe `beforeunload` (luật F30). UI thật
 * (ConfirmDialog) render ở platform/shell/unsaved-changes-dialog/ — service này ở `core/`
 * nên KHÔNG được biết thư viện UI (đối xứng với `core/toast/toast.service.ts`).
 *
 * Chỉ MỘT màn active tại một thời điểm trong ứng dụng này (v1 không có hai form dở tay song
 * song ở hai route khác nhau), nên registry chỉ cần giữ đúng MỘT hàm kiểm.
 */
@Injectable({ providedIn: 'root' })
export class UnsavedChangesService {
  private kiemTra: (() => boolean) | null = null;
  private dangCho: ((roiDuoc: boolean) => void) | null = null;
  private daDongYRoi = false;

  /** `platform/shell/unsaved-changes-dialog` đọc để hiện/ẩn `ConfirmDialog`. */
  readonly dangHoi = signal(false);

  constructor() {
    // Đường "đóng tab/tải lại trang" — khác kỹ thuật với canDeactivate, cùng bắt buộc theo
    // câu cuối fe-routing-guard.md §4.1 ("thiếu một đường là thủng một nửa"). Trình duyệt tự vẽ
    // hộp cảnh báo của NÓ — không gọi ConfirmDialog được ở đây.
    const nghe = (e: BeforeUnloadEvent): void => {
      if (!this.coThayDoiChuaLuu()) {
        return;
      }
      e.preventDefault();
      e.returnValue = '';
    };
    window.addEventListener('beforeunload', nghe);
    inject(DestroyRef).onDestroy(() => window.removeEventListener('beforeunload', nghe));
  }

  /** Màn đang có form gọi (thường trong constructor) để khai "tôi đang có thay đổi chưa lưu". */
  dangKy(kiemTra: () => boolean): void {
    this.kiemTra = kiemTra;
  }

  /** Màn gọi lúc bị destroy — tránh guard/khung hỏi nhầm cho một màn đã rời hẳn. */
  huyDangKy(kiemTra: () => boolean): void {
    if (this.kiemTra === kiemTra) {
      this.kiemTra = null;
    }
  }

  /**
   * Hàm kiểm đang đăng ký — `unsavedChangesGuard` giữ lại lúc người dùng chọn "Rời đi" để sau
   * `NavigationEnd` gỡ ĐÚNG đăng ký của màn đang rời (`huyDangKy` so theo danh tính), không đụng
   * đăng ký của màn đích đã dựng xong trước khi `NavigationEnd` phát.
   */
  kiemTraDangDangKy(): (() => boolean) | null {
    return this.kiemTra;
  }

  coThayDoiChuaLuu(): boolean {
    return this.kiemTra?.() ?? false;
  }

  /**
   * `unsavedChangesGuard` (canDeactivate) VÀ khung ứng dụng (đăng xuất, đổi ngôn ngữ) gọi CÙNG
   * hàm này TRƯỚC khi rời trang hoặc gửi request — một cơ chế phục vụ cả điều hướng trong app
   * lẫn hành động ngoài router, tránh hỏi hai lần theo hai đường khác nhau (ADR-0040).
   * Không còn thay đổi chưa lưu → resolve ngay `true`, không mở hộp.
   */
  xinRoiTrang(): Promise<boolean> {
    if (!this.coThayDoiChuaLuu()) {
      return Promise.resolve(true);
    }
    // Lượt hỏi cũ còn treo (hiếm, ví dụ bấm liên tiếp) → coi là "ở lại" trước khi mở lượt mới.
    this.dangCho?.(false);
    this.dangHoi.set(true);
    return new Promise<boolean>((resolve) => {
      this.dangCho = resolve;
    });
  }

  /**
   * `platform/shell/unsaved-changes-dialog` gọi khi người dùng chọn "Rời đi".
   *
   * KHÔNG gỡ đăng ký ở đây: "Rời đi" mới là lời đồng ý, chưa phải việc rời trang. Khung ứng dụng gọi
   * `xinRoiTrang()` thẳng trước khi gửi đăng xuất — đăng xuất hỏng (mất mạng, 5xx) thì người dùng ở
   * lại màn với đúng dữ liệu đang dở (D6 §4), và lớp bảo vệ — hỏi khi điều hướng, chặn khi đóng tab —
   * phải còn nguyên (fe-routing-guard.md §4.1). Đăng ký chỉ gỡ khi việc rời trang đã chắc chắn:
   * `roiTrangChacChan()`, do `unsavedChangesGuard` gọi.
   */
  xacNhanRoiDi(): void {
    this.dangHoi.set(false);
    this.dangCho?.(true);
    this.dangCho = null;
  }

  /**
   * `unsavedChangesGuard` ghi nhận người dùng đã chọn "Rời đi" cho lần điều hướng đang chạy. Chưa gỡ
   * đăng ký: điều hướng còn có thể hỏng (`NavigationError`, `NavigationCancel`) và khi đó lớp bảo vệ
   * phải còn nguyên. Cờ chỉ để lần Router chạy lại canDeactivate của cùng route — khi một canActivate
   * phía sau chuyển hướng — không hỏi lần hai.
   */
  danhDauDongYRoi(): void {
    this.daDongYRoi = true;
  }

  /** Lời đồng ý đang hiệu lực cho lần điều hướng hiện tại (đọc bởi `unsavedChangesGuard`). */
  daDongYRoiLanNay(): boolean {
    return this.daDongYRoi;
  }

  /** Điều hướng đã đồng ý rời bị hỏng/huỷ: bỏ lời đồng ý, đăng ký giữ nguyên — lần rời sau hỏi lại. */
  huyDongYRoi(): void {
    this.daDongYRoi = false;
  }

  /**
   * Việc rời màn đã chắc chắn xảy ra — chỉ `unsavedChangesGuard` gọi, ở ca:
   *
   * -Phiên đã kết thúc (401, tab khác đăng xuất, đăng xuất chủ động đã xong): màn luôn rời về đăng
   *   nhập, kể cả khi form dở (fe-routing-guard.md §8). Lượt hỏi còn treo (của một điều hướng đã bị
   *   thay, hoặc của khung ứng dụng) nhận "ở lại" và hộp đóng — không để hộp nằm lại trên màn đăng nhập.
   */
  roiTrangChacChan(): void {
    this.huy();
    this.kiemTra = null;
    this.daDongYRoi = false;
  }

  /** `platform/shell/unsaved-changes-dialog` gọi khi chọn "Ở lại"/Escape/bấm ra ngoài. */
  huy(): void {
    this.dangHoi.set(false);
    this.dangCho?.(false);
    this.dangCho = null;
  }
}
