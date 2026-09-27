import { Injectable, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

import { TraCuuVaiTroStore, VaiTroTraCuu } from '../../vai-tro/state/tra-cuu-vai-tro.store';
import { VaiTroService } from '../../vai-tro/services/vai-tro.service';
import { VaiTroRutGon } from '../models/nguoi-dung.model';

/**
 * Trạng thái ô chọn vai trò của hộp "Gán vai trò" (chi-tiet-nguoi-dung.page) — tách khỏi page để
 * giữ *.page.ts dưới ngưỡng dòng (fe-architecture.md §5, luật F22). Store cấp ở `providers` của
 * page (ADR-0049) nên chết cùng màn; `moHop()` reset lại trạng thái mỗi lần mở hộp.
 */
@Injectable()
export class GanVaiTroChonStore {
  private readonly vaiTroService = inject(VaiTroService);
  private readonly translate = inject(TranslateService);

  /** Một thể hiện riêng của hộp này — `TraCuuVaiTroStore` không cấp bằng DI (lý do ở đầu tệp đó). */
  readonly timVaiTro = new TraCuuVaiTroStore(this.vaiTroService, this.translate);
  readonly dangChon = signal<readonly string[]>([]);
  /** Vai trò hệ thống đứng NGOÀI ô chọn khi xem chính mình (users.md §2 luật 2). */
  readonly heThongCuaMinh = signal<readonly VaiTroTraCuu[]>([]);
  /** Ảnh chụp lúc mở hộp — so với `dangChon()` để biết "còn thay đổi chưa lưu" (Dialog `dirty`). */
  private banDau: readonly string[] = [];

  readonly dirty = computed(() => {
    const hienTai = this.dangChon();
    if (hienTai.length !== this.banDau.length) return true;
    const tapBanDau = new Set(this.banDau);
    return hienTai.some((id) => !tapBanDau.has(id));
  });

  moHop(rolesHienCo: readonly VaiTroRutGon[], laChinhMinh: boolean): void {
    const heThongCuaMinh = laChinhMinh ? rolesHienCo.filter((r) => r.isSystem) : [];
    const idHeThongCuaMinh = new Set(heThongCuaMinh.map((r) => r.id));
    this.heThongCuaMinh.set(heThongCuaMinh);
    const conLai = rolesHienCo.filter((r) => !idHeThongCuaMinh.has(r.id));
    const idsConLai = conLai.map((r) => r.id);
    this.banDau = idsConLai;
    this.dangChon.set(idsConLai);
    this.timVaiTro.reset();
    this.timVaiTro.seed(conLai);
  }

  timKiem(tuKhoa: string): void {
    const idHeThongCuaMinh = new Set(this.heThongCuaMinh().map((r) => r.id));
    this.timVaiTro.timKiem(tuKhoa, (vt) => !idHeThongCuaMinh.has(vt.id));
  }

  chon(gt: string | readonly string[]): void {
    this.dangChon.set(Array.isArray(gt) ? gt : []);
  }

  /** Tập vai trò đích khi gửi lên server — ô chọn CỘNG vai trò hệ thống đứng ngoài (users.md §7). */
  tapDich(): readonly string[] {
    return [...this.dangChon(), ...this.heThongCuaMinh().map((r) => r.id)];
  }
}
