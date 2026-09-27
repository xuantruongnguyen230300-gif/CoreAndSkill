import { signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, catchError, map, of, switchMap } from 'rxjs';

import { ApiFailureError } from '../../../../core/http/api-result.model';
import { dichLoiChoMan } from '../../../../core/http/dich-loi';
import { Option } from '../../../../shared/ui/autocomplete/autocomplete.component';
import { VaiTroService } from '../services/vai-tro.service';
import { VaiTro } from '../models/vai-tro.model';

/** Phần của `VaiTro` mà ô chọn thật sự dùng — để bản ghi rút gọn (`NguoiDung.roles`) seed được không cần ép kiểu. */
export type VaiTroTraCuu = Pick<VaiTro, 'id' | 'name' | 'isSystem'>;

interface KetQuaTim {
  readonly items: readonly VaiTro[];
  /** Câu lỗi của lần tìm, `null` khi tìm được — xem `cauLoi`. */
  readonly loi: string | null;
}

/** Yêu cầu đi qua `tuKhoa$`: một lần tìm, hoặc lệnh đặt lại (`'reset'`) — cùng một dòng để `switchMap` huỷ request đang bay. */
type YeuCau = { readonly q: string; readonly boLoc?: (vt: VaiTro) => boolean } | 'reset';

/**
 * Hạ tầng tìm vai trò cho ô Autocomplete — dùng LẠI ở ba nơi (ô lọc danh sách người dùng, hộp tạo
 * người dùng, hộp gán vai trò) thay vì chép ba lần cùng một khối tín hiệu + bảo vệ request cũ tới
 * muộn (Autocomplete.md "Một luật thi công không được bỏ").
 *
 * LÝ DO KHÔNG phải `@Injectable()` cấp ở `providers` (ADR-0049 điều 2, ngoại lệ duy nhất): MỘT
 * page cần NHIỀU thể hiện độc lập của lớp này — `danh-sach-nguoi-dung.page` giữ một cho ô lọc theo vai
 * trò, `TaoNguoiDungStore` giữ một cho hộp tạo, `GanVaiTroChonStore` giữ một cho hộp gán. Một thể hiện cấp
 * bằng DI thì page chỉ có MỘT; hai ô sẽ chia sẻ NHẦM một trạng thái tìm kiếm. Nơi dùng tự `new`
 * và truyền `VaiTroService` + `TranslateService` từ chính `inject()` của mình.
 *
 * `Subject` + `switchMap` theo khuôn đã chốt ở fe-api-client.md §6.1: HUỶ
 * THẬT request cũ, không chỉ bỏ kết quả tới muộn. `catchError` nằm TRONG `switchMap` — bắt buộc,
 * nếu không một lỗi mạng sẽ làm `Subject` complete và mọi lần gõ sau đó không còn gọi được nữa.
 * Không cần huỷ đăng ký tay lúc trang bị huỷ: `Subject` là field riêng của chính thực thể này —
 * không có gì bên ngoài (route, service `providedIn: 'root'`…) giữ tham chiếu tới nó, nên khi
 * trang bị huỷ và không còn gì trỏ tới thực thể `TraCuuVaiTroStore`, toàn bộ cụm (Subject, subscription,
 * chính nó) rơi vào diện thu dọn cùng lúc — khác hẳn việc subscribe vào một stream sống bên ngoài
 * component (Router, service toàn cục) mà quên `takeUntilDestroyed` mới thực sự rò rỉ.
 */
export class TraCuuVaiTroStore {
  readonly options = signal<readonly Option[]>([]);
  readonly loading = signal(false);
  /**
   * Câu lỗi của lần tìm gần nhất cho lớp nổi (`errorTemplate`), `null` khi không lỗi. GIỮ đúng lỗi, không
   * nuốt thành một cờ: Design/Screens/10-nguoi-dung.md, bảng mã lỗi dòng "tìm vai trò (ô chọn)".
   */
  readonly error = signal<string | null>(null);

  private readonly tuKhoa$ = new Subject<YeuCau>();

  constructor(
    private readonly service: VaiTroService,
    private readonly translate: TranslateService,
  ) {
    this.tuKhoa$
      .pipe(
        switchMap((yeuCau): Observable<KetQuaTim | null> => {
          // Đặt lại: `switchMap` huỷ request đang bay, rồi phát `null` ngay để xoá trạng thái.
          if (yeuCau === 'reset') return of(null);
          const { q, boLoc } = yeuCau;
          this.loading.set(true);
          this.error.set(null);
          return this.service.timKiem(q).pipe(
            map<{ items: readonly VaiTro[] }, KetQuaTim>((trang) => ({
              items: boLoc ? trang.items.filter(boLoc) : trang.items,
              loi: null,
            })),
            catchError((err: unknown) => of<KetQuaTim>({ items: [], loi: this.cauLoi(err) })),
          );
        }),
      )
      .subscribe((ketQua) => {
        this.loading.set(false);
        if (ketQua === null) {
          this.error.set(null);
          this.options.set([]);
          return;
        }
        const { items, loi } = ketQua;
        if (loi !== null) {
          this.error.set(loi);
          return;
        }
        this.options.set(items.map((vt) => this.sangOption(vt)));
      });
  }

  /** `boLoc` dùng để loại vai trò hệ thống đang giữ ngoài ô chọn khi xem chính mình (users.md §2). */
  timKiem(tuKhoa: string, boLoc?: (vt: VaiTro) => boolean): void {
    this.tuKhoa$.next({ q: tuKhoa, boLoc });
  }

  /**
   * Mở lại hộp: xoá danh sách, câu lỗi mạng và trạng thái chờ của lần trước, và HUỶ request đang bay —
   * nếu không, kết quả tới muộn của lần mở trước ghi đè danh sách vừa đặt lại.
   */
  reset(): void {
    this.tuKhoa$.next('reset');
  }

  /** Seed nhãn ban đầu (vai trò đã có sẵn của bản ghi) trước khi người dùng gõ tìm gì. */
  seed(vaiTros: readonly VaiTroTraCuu[]): void {
    this.options.set(vaiTros.map((vt) => this.sangOption(vt)));
  }

  /**
   * Lỗi thường: câu `dichLoiChoMan` (theo mã; không phản hồi → câu mất kết nối). Lớp lỗi xuyên suốt
   * (5xx, 403 `CORE.AUTH.*`) đã được interceptor toast kèm traceId dù request tắt toast — lớp nổi chỉ
   * nói ngắn `vaiTro.tim.loi`, không lặp chi tiết.
   */
  private cauLoi(err: unknown): string {
    if (!(err instanceof ApiFailureError)) {
      return this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION');
    }
    return dichLoiChoMan(this.translate, err) ?? this.translate.instant('vaiTro.tim.loi');
  }

  private sangOption(vt: VaiTroTraCuu): Option {
    return {
      key: vt.id,
      label: vt.name,
      hint: vt.isSystem ? this.translate.instant('vaiTro.loai.heThong') : undefined,
    };
  }
}
