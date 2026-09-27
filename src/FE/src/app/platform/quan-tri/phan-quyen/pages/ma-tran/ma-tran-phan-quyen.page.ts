import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  TemplateRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { ApiFailureError } from '../../../../../core/http/api-result.model';
import { dichLoiChoMan } from '../../../../../core/http/dich-loi';
import { UnsavedChangesService } from '../../../../../core/unsaved-changes/unsaved-changes.service';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { fieldErrorsText } from '../../../../../shared/forms/field-errors-text';
import { NoticeBannerComponent } from '../../../../../shared/components/notice-banner/notice-banner.component';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import {
  TableColumnDef,
  TableComponent,
} from '../../../../../shared/components/table/table.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { CheckComponent } from '../../../../../shared/ui/check/check.component';
import { TooltipComponent } from '../../../../../shared/ui/tooltip/tooltip.component';
import { ToastService } from '../../../../../core/toast/toast.service';
import { PhanQuyenService } from '../../services/phan-quyen.service';
import { MaTranHang, MaTranPhanQuyen } from '../../models/phan-quyen.model';

interface HangHienThi extends MaTranHang {
  readonly resourceName: string;
}

/** Design/Screens/12-ma-tran-phan-quyen.md. Guard: permissionGuard('core.permission.read'). */
@Component({
  selector: 'app-ma-tran-phan-quyen',
  standalone: true,
  imports: [
    TranslatePipe,
    ConfirmDialogComponent,
    EmptyStateComponent,
    NoticeBannerComponent,
    PageHeaderComponent,
    TableComponent,
    ButtonComponent,
    CheckComponent,
    TooltipComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ma-tran-phan-quyen.page.html',
  styleUrl: './ma-tran-phan-quyen.page.scss',
})
export class MaTranPhanQuyenPage {
  private readonly service = inject(PhanQuyenService);
  protected readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);
  private readonly unsavedChanges = inject(UnsavedChangesService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly coQuyenGhi = computed(() => this.auth.coQuyen('core.permission.write'));

  protected readonly dangTai = signal(true);
  /**
   * Tải lần đầu hỏng → trạng thái `error` của `Table`: tiêu đề cố định LUÔN có khi khác `null`; `than`
   * là câu `dichLoiChoMan` — `null` với lớp lỗi xuyên suốt, interceptor đã toast nó kèm traceId
   * (Design/Screens/12 §Trạng thái).
   */
  protected readonly loiTai = signal<{ readonly than: string | null } | null>(null);
  protected readonly maTran = signal<MaTranPhanQuyen | null>(null);
  /** Tải lại TRÊN dữ liệu cũ đang hiển thị (sau lưu, sau 409) — RIÊNG với `dangTai` (tải lần đầu,
   *  chưa có gì để giữ lại). `Table` phân biệt hai ca này qua chính `[loading]` gộp cả hai cờ; ô
   *  tick phải khoá suốt thời gian này để không ai tick thêm trong lúc GET đang chạy rồi bị
   *  `apDuLieuMoi()` áp nhầm coi như đã lưu. */
  protected readonly dangTaiLaiMaTran = signal(false);

  protected readonly dangLuu = signal(false);
  protected readonly loiLuu = signal<string | null>(null);
  protected readonly xungDotVersion = signal(false);

  /** permissionId → tập roleId đang cấp (bản đang sửa trên màn). */
  protected readonly capHienTai = signal<ReadonlyMap<string, ReadonlySet<string>>>(new Map());
  /** Mốc của "đã đổi": tập máy chủ đang giữ — lần GET gần nhất, hoặc tập vừa PUT 200 trong lúc chờ
   *  GET sau lưu. Signal, để cờ thay đổi và dải "đã đổi" tính lại cả khi RIÊNG mốc dời. */
  private readonly goc = signal<ReadonlyMap<string, ReadonlySet<string>>>(new Map());
  /** PUT vừa 200 mà chưa GET nào mang `version` mới về: `version` trong tay đã cũ, gửi lại là nhận
   *  409 — khoá Lưu tới khi `apDuLieuMoi()` chạy. */
  protected readonly choVersionMoi = signal(false);

  constructor() {
    this.taiMaTran();
    const coThayDoiChuaLuu = (): boolean => this.coDoi();
    this.unsavedChanges.dangKy(coThayDoiChuaLuu);
    this.destroyRef.onDestroy(() => this.unsavedChanges.huyDangKy(coThayDoiChuaLuu));
  }

  protected taiMaTran(): void {
    this.dangTai.set(true);
    this.loiTai.set(null);
    this.service
      .layMaTran()
      // Lượt ĐỌC chết cùng màn (fe-api-client.md §6.1); lệnh lưu (`guiLuu`) cố ý chạy tới cùng.
      .pipe(
        finalize(() => this.dangTai.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (mt) => this.apDuLieuMoi(mt),
        error: (err: unknown) => this.loiTai.set({ than: this.cauLoiTai(err) }),
      });
  }

  private cauLoiTai(err: unknown): string | null {
    return err instanceof ApiFailureError
      ? dichLoiChoMan(this.translate, err)
      : this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION');
  }

  /** `(retry)` của hàng lỗi `Table`. Chưa có ma trận → tải lần đầu; đã có (GET sau lưu hỏng) → tải lại
   *  giữ dữ liệu cũ, khoá ô như mọi lần tải lại (Design/Screens/12 §Trạng thái "đang tải"). */
  protected thuLaiTai(): void {
    if (this.maTran()) this.taiMaTranSauLuu();
    else this.taiMaTran();
  }

  /** Áp dữ liệu mới TỪ SERVER (Design/Screens/12 mục "Áp lại sau 409") — KHÔNG bỏ thay đổi, KHÔNG tự
   *  lưu. Giá trị đích chỉ là ô mà bản đang sửa khác mốc CŨ; ô không chạm lấy giá trị máy chủ mới. Sau
   *  PUT 200 mốc cũ là tập vừa gửi, nên cùng luật cho cả GET sau lưu lẫn tải lại sau 409. */
  private apDuLieuMoi(mt: MaTranPhanQuyen): void {
    const gocMoi = new Map(
      mt.rows.map((r) => [r.permissionId, new Set(r.grantedRoleIds) as ReadonlySet<string>]),
    );
    const gocCu = this.goc();
    const dangSua = this.capHienTai();
    const idVaiTroMoi = new Set(mt.roles.map((r) => r.id));
    const idVaiTroHeThong = new Set(mt.roles.filter((r) => r.isSystem).map((r) => r.id));

    const ketQua = new Map<string, ReadonlySet<string>>();
    for (const [permissionId, tapMoi] of gocMoi) {
      const tap = new Set(tapMoi);
      const sua = dangSua.get(permissionId);
      const moc = gocCu.get(permissionId);
      if (sua && moc) {
        const hangGhiQuyen = this.laHangGhiQuyen(permissionId, mt);
        for (const roleId of new Set([...sua, ...moc])) {
          if (sua.has(roleId) === moc.has(roleId)) continue; // không chạm → giá trị máy chủ
          if (!idVaiTroMoi.has(roleId)) continue; // cột không còn → thay đổi rơi
          if (hangGhiQuyen && idVaiTroHeThong.has(roleId)) continue; // ô khoá bất biến → máy chủ
          if (sua.has(roleId)) tap.add(roleId);
          else tap.delete(roleId);
        }
      }
      // Hàng không còn trong `gocMoi` thì thay đổi của nó rơi theo. Đích trùng giá trị mới thì ô bằng
      // mốc mới — dấu "đã đổi" tự bỏ.
      ketQua.set(permissionId, tap);
    }

    this.goc.set(gocMoi);
    this.choVersionMoi.set(false);
    this.capHienTai.set(ketQua);
    this.maTran.set(mt);
  }

  private laHangGhiQuyen(permissionId: string, mt: MaTranPhanQuyen): boolean {
    return mt.rows.find((r) => r.permissionId === permissionId)?.code === 'core.permission.write';
  }

  protected readonly hangHienThi = computed<readonly HangHienThi[]>(() => {
    const mt = this.maTran();
    if (!mt) return [];
    // `Table` không xếp lại `rows` (Table.md §Gom dòng theo nhóm) và hợp đồng không bảo đảm thứ tự:
    // gom theo `resourceKey` — nhóm theo thứ tự xuất hiện đầu tiên, hàng trong nhóm giữ thứ tự
    // response (Design/Screens/12 §Sơ đồ bố cục).
    const theoNhom = new Map<string, HangHienThi[]>();
    for (const r of mt.rows) {
      let nhom = theoNhom.get(r.resourceKey);
      if (!nhom) {
        nhom = [];
        theoNhom.set(r.resourceKey, nhom);
      }
      nhom.push({ ...r, resourceName: this.dichHoacKhoa(r.resourceNameKey, r.resourceKey) });
    }
    return [...theoNhom.values()].flat();
  });

  private dichHoacKhoa(khoa: string, duPhong: string): string {
    const dich = this.translate.instant(khoa);
    return dich === khoa ? duPhong : dich;
  }

  // ---- Cột: "Quyền" cố định + một cột mỗi vai trò ----
  private readonly colQuyen =
    viewChild.required<
      TemplateRef<{ $implicit: HangHienThi; column: TableColumnDef<HangHienThi> }>
    >('colQuyen');
  private readonly colVaiTro =
    viewChild.required<
      TemplateRef<{ $implicit: HangHienThi; column: TableColumnDef<HangHienThi> }>
    >('colVaiTro');

  protected readonly columns = computed<TableColumnDef<HangHienThi>[]>(() => {
    const cols: TableColumnDef<HangHienThi>[] = [
      {
        key: 'quyen',
        header: this.translate.instant('phanQuyen.cot.quyen'),
        cell: this.colQuyen(),
      },
    ];
    // Chưa có ma trận (đang tải lần đầu, tải hỏng): cột "Quyền" vẫn đứng — `<thead>` giữ nguyên và hàng
    // chờ / hàng lỗi có một cột thật để `colspan` bám vào (Table.md §Trạng thái).
    const mt = this.maTran();
    if (!mt) return cols;
    for (const vt of mt.roles) {
      const header = vt.isSystem
        ? `${vt.name} · ${this.translate.instant('vaiTro.loai.heThong')}`
        : vt.name;
      cols.push({ key: vt.id, header, cell: this.colVaiTro() });
    }
    return cols;
  });

  // ---- Tick ô ----
  protected daCap(permissionId: string, roleId: string): boolean {
    return this.capHienTai().get(permissionId)?.has(roleId) ?? false;
  }

  protected daDoiO(permissionId: string, roleId: string): boolean {
    return (
      this.daCap(permissionId, roleId) !== (this.goc().get(permissionId)?.has(roleId) ?? false)
    );
  }

  protected oBiKhoa(hang: HangHienThi, roleId: string): boolean {
    if (this.dangTaiLaiMaTran()) return true;
    if (!this.coQuyenGhi()) return true;
    const vt = this.maTran()?.roles.find((r) => r.id === roleId);
    return hang.code === 'core.permission.write' && (vt?.isSystem ?? false);
  }

  protected toggleO(permissionId: string, roleId: string, hang: HangHienThi): void {
    if (this.oBiKhoa(hang, roleId)) return;
    this.capHienTai.update((cu) => {
      const moi = new Map(cu);
      const tap = new Set(moi.get(permissionId) ?? []);
      if (tap.has(roleId)) tap.delete(roleId);
      else tap.add(roleId);
      moi.set(permissionId, tap);
      return moi;
    });
  }

  protected tenVaiTro(roleId: string): string {
    return this.maTran()?.roles.find((r) => r.id === roleId)?.name ?? '';
  }

  protected dichTenQuyen(hang: HangHienThi): string {
    return this.dichHoacKhoa(hang.nameKey, hang.code);
  }

  // ---- Huỷ thay đổi / lưu ----
  protected readonly coDoi = computed(() => {
    const goc = this.goc();
    for (const [permissionId, tap] of this.capHienTai()) {
      const g = goc.get(permissionId) ?? new Set();
      if (tap.size !== g.size || [...tap].some((id) => !g.has(id))) return true;
    }
    return false;
  });

  private readonly soOGoQuyen = computed(() => {
    const goc = this.goc();
    let dem = 0;
    for (const [permissionId, tap] of this.capHienTai()) {
      const g = goc.get(permissionId) ?? new Set();
      for (const roleId of g) {
        if (!tap.has(roleId)) dem++;
      }
    }
    return dem;
  });

  protected huyThayDoi(): void {
    this.capHienTai.set(this.goc());
  }

  protected readonly hienThiXacNhanLuu = signal(false);

  protected bamLuu(): void {
    if (this.choVersionMoi()) return;
    if (this.soOGoQuyen() > 0) {
      this.hienThiXacNhanLuu.set(true);
    } else {
      this.guiLuu();
    }
  }

  protected huyXacNhanLuu(): void {
    this.hienThiXacNhanLuu.set(false);
  }

  protected xacNhanLuu(): void {
    this.hienThiXacNhanLuu.set(false);
    this.guiLuu();
  }

  protected soLuongGo(): number {
    return this.soOGoQuyen();
  }

  private guiLuu(): void {
    const mt = this.maTran();
    if (!mt) return;
    this.dangLuu.set(true);
    this.loiLuu.set(null);
    this.xungDotVersion.set(false);
    const entries = mt.rows.map((r) => ({
      permissionId: r.permissionId,
      roleIds: [...(this.capHienTai().get(r.permissionId) ?? [])],
    }));
    this.service
      .luuMaTran({ version: mt.version, entries })
      .pipe(finalize(() => this.dangLuu.set(false)))
      .subscribe({
        next: () => {
          // Máy chủ đã nhận đúng tập vừa gửi: mốc dời về nó NGAY, không đợi GET — GET có thể hỏng. Ô
          // tick thêm trong lúc PUT chạy vẫn là thay đổi chưa lưu.
          this.goc.set(
            new Map<string, ReadonlySet<string>>(
              entries.map((e) => [e.permissionId, new Set(e.roleIds)]),
            ),
          );
          this.choVersionMoi.set(true);
          this.toast.thanhCong(this.translate.instant('phanQuyen.thongBao.luuThanhCong'));
          this.taiMaTranSauLuu();
        },
        error: (err: unknown) => this.xuLyLoiLuu(err),
      });
  }

  private taiMaTranSauLuu(): void {
    // Tải lại từ server sau khi lưu — version cho lần lưu kế lấy từ GET này, KHÔNG dùng version
    // trong response của PUT (Design/Screens/12 §Quyết định). Hỏng → trạng thái `error` của `Table`
    // như tải lần đầu hỏng (§Trạng thái "lỗi"): một chỗ hiện lỗi, vì service tắt toast.
    this.loiTai.set(null);
    this.taiLaiGiuDuLieuCu(
      (mt) => this.apDuLieuMoi(mt),
      (err: unknown) => this.loiTai.set({ than: this.cauLoiTai(err) }),
    );
  }

  /** Dùng chung bởi `taiMaTranSauLuu()` và `taiLaiSauXungDot()` — cả hai đều "tải lại trong khi
   *  dữ liệu cũ còn hiển thị", nên cùng một cờ khoá ô (`dangTaiLaiMaTran`), tách khỏi `dangTai`
   *  (chỉ dành cho tải lần đầu, chưa có gì để giữ lại). */
  private taiLaiGiuDuLieuCu(
    onSuccess: (mt: MaTranPhanQuyen) => void,
    onError: (err: unknown) => void,
  ): void {
    this.dangTaiLaiMaTran.set(true);
    // Lượt ĐỌC chết cùng màn — kể cả khi gọi từ `next` của một lần lưu xong SAU khi đã rời màn: màn
    // đã chết thì không mở GET nào.
    this.service
      .layMaTran()
      .pipe(
        finalize(() => this.dangTaiLaiMaTran.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({ next: onSuccess, error: onError });
  }

  private xuLyLoiLuu(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiLuu.set(this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'));
      return;
    }
    const ma = err.body?.error.code;
    if (ma === 'CORE.PERMISSION.VERSION_MISMATCH') {
      this.xungDotVersion.set(true);
      return;
    }
    // Màn không có form: câu của khu lỗi chung do fe-ui-conventions.md §6.2 quyết — mã con trong
    // `fieldErrors` (vd `Entries[i].RoleIds`) thắng mã gốc; không có mã con thì lùi về câu mã gốc.
    this.loiLuu.set(fieldErrorsText(this.translate, err));
  }

  protected taiLaiSauXungDot(): void {
    this.xungDotVersion.set(false);
    this.loiLuu.set(null);
    this.taiLaiGiuDuLieuCu(
      (mt) => this.apDuLieuMoi(mt),
      (err: unknown) => {
        // Cùng khu lỗi với `xuLyLoiLuu` — cùng hàm: lớp lỗi xuyên suốt → `null` (interceptor đã toast).
        this.loiLuu.set(
          err instanceof ApiFailureError
            ? fieldErrorsText(this.translate, err)
            : this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'),
        );
      },
    );
  }
}
