import { Injectable, computed, signal } from '@angular/core';

/** Đếm request HTTP đang chạy — KHÔNG phải cờ boolean, vì nhiều request có thể chạy song song. */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly dangChay = signal(0);
  readonly hienThi = computed(() => this.dangChay() > 0);

  batDau(): void {
    this.dangChay.update((n) => n + 1);
  }

  ketThuc(): void {
    // Không bao giờ để âm — hai lần gọi ketThuc() cho cùng một request là lỗi lập trình, không phải lý do để đếm âm.
    this.dangChay.update((n) => Math.max(0, n - 1));
  }
}
