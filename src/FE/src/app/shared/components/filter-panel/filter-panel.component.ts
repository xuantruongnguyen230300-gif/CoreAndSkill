import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnChanges,
  SimpleChanges,
  computed,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormRecord, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { map } from 'rxjs';

import { AutocompleteComponent, Option } from '../../ui/autocomplete/autocomplete.component';
import { DrawerComponent } from '../../ui/drawer/drawer.component';
import { UiTreeNode } from '../../ui/types';
import { ButtonComponent } from '../button/button.component';
import { FormRowComponent } from '../form-row/form-row.component';
import { InputComponent, SegmentOption } from '../input/input.component';

/** Một trường trong FilterPanel — chữ ký đầy đủ ở quy-uoc/fe-ui-conventions.md §9. */
export interface FilterField {
  readonly key: string;
  /** Câu ĐÃ DỊCH, hiện thành `<label>` thật. */
  readonly label: string;
  readonly kind:
    | 'text'
    | 'number'
    | 'date'
    | 'daterange'
    | 'select'
    | 'multiselect'
    | 'tree'
    | 'check'
    | 'autocomplete';
  readonly options?: readonly SegmentOption[];
  readonly nodes?: readonly UiTreeNode[];
  /** Bắt buộc với `autocomplete` — input bắt buộc của `Autocomplete`. */
  readonly placeholder?: string;
  readonly group?: string;
}

/** Kết quả tìm gần nhất của MỘT trường `autocomplete` (FilterPanel.md §API, `searchStates`). */
export interface FilterSearchState {
  readonly options: readonly Option[];
  readonly loading: boolean;
  /** Câu ĐÃ DỊCH; `null` khi không lỗi. */
  readonly error: string | null;
}

const NHAN_RONG: Readonly<Record<string, string>> = {};

/** Giá trị rỗng của một điều kiện: vắng, `null` hoặc chuỗi rỗng. */
function laRong(v: unknown): boolean {
  return v === null || v === undefined || v === '';
}

function chuoi(v: unknown): string {
  return laRong(v) ? '' : String(v);
}

/** Kind đã có ô vẽ — kind khác không có ô nên điều kiện đang áp của nó đi qua nguyên vẹn khi áp dụng. */
const KIND_DA_VE: readonly FilterField['kind'][] = ['text', 'select', 'autocomplete'];

/**
 * Design/Components/FilterPanel.md, biến thể `drawer`. Tự dựng; component **dumb** (luật F11): không
 * inject service, không gọi API — danh mục tải theo yêu cầu vào qua `searchStates`, tên của giá trị
 * đang áp qua `selectedLabels`, trang cha nghe `fieldSearch` rồi gọi API.
 *
 * Tự đặt một `Drawer` `edit` cỡ `lg` (`dirty` luôn `false` → đóng chưa áp KHÔNG hỏi xác nhận, phần
 * chưa áp bị bỏ). Áp dụng → phát `applied` rồi `closed`; Xoá hết → phát `cleared`, panel GIỮ MỞ và
 * focus về trường đầu. Nút Áp dụng nằm ở chân `Drawer` (ngoài thẻ form) nên gắn vào form bằng
 * thuộc tính `form`; `Enter` trong một trường vì thế áp bộ lọc theo cơ chế gửi form gốc.
 *
 * 🚧 Kind đã dựng: `text`, `select`, `autocomplete`. Kind còn lại (`number`, `date`, `daterange`,
 * `multiselect`, `tree`, `check`) CHƯA dựng — `DatePicker`, ô chọn nhiều, cây… chưa có component
 * Core; trường mang kind đó không vẽ gì. Cũng chưa dựng: `<fieldset>` cho `group` (chỉ cần khi có `daterange`).
 */
@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    AutocompleteComponent,
    ButtonComponent,
    DrawerComponent,
    FormRowComponent,
    InputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './filter-panel.component.html',
  styleUrl: './filter-panel.component.scss',
})
export class FilterPanelComponent implements OnChanges {
  readonly fields = input<readonly FilterField[]>([]);
  /** Điều kiện đang áp, khoá theo `key` của trường. */
  readonly value = input<Readonly<Record<string, unknown>>>({});
  readonly open = input(false);
  /** Câu ĐÃ DỊCH nói lọc cái gì ("Lọc người dùng"). */
  readonly title = input.required<string>();
  /** `id` phần tử gốc của `Drawer` — cùng chuỗi trang truyền vào `filterPanelId` của `Toolbar`. */
  readonly panelId = input.required<string>();
  readonly disabled = input(false);
  readonly searchStates = input<Readonly<Record<string, FilterSearchState>>>({});
  readonly selectedLabels = input<Readonly<Record<string, string>>>(NHAN_RONG);

  /** Bấm Áp dụng: giá trị MỌI trường, trường trống là `null`. Không phát ở mỗi lần đổi một trường. */
  readonly applied = output<Record<string, unknown>>();
  /** Bấm Xoá hết: điều kiện của panel đã về trống và áp ngay; panel giữ mở. */
  readonly cleared = output<void>();
  /** Mọi lần panel đóng. Trang cha đặt `open` về `false`. */
  readonly closed = output<void>();
  /** Chuyển tiếp lần tìm của ô `autocomplete`; "Thử lại" phát lại với chuỗi gần nhất. */
  readonly fieldSearch = output<{ key: string; text: string }>();

  private readonly bieuMau = viewChild<ElementRef<HTMLFormElement>>('bieuMau');

  /**
   * Bản nháp các trường — phần chưa áp. Mỗi trường của `fields` một `FormControl<string>`, rỗng là
   * chuỗi rỗng. `FormRecord` vì tập khoá do trang cha quyết định lúc chạy.
   */
  protected readonly form = new FormRecord<FormControl<string>>({});
  /** Giá trị mọi ô của `form` dưới dạng signal — nguồn của các `computed` bên dưới. */
  private readonly nhap = toSignal(
    this.form.valueChanges.pipe(map(() => this.form.getRawValue())),
    {
      initialValue: {} as Readonly<Record<string, string>>,
    },
  );
  /** Nhãn của mục `autocomplete` người dùng vừa chọn trong lần mở này. */
  private readonly nhanNhap = signal<Readonly<Record<string, string>>>({});
  private readonly chuoiTimGanNhat = new Map<string, string>();

  protected readonly idForm = computed(() => `${this.panelId()}-form`);

  protected readonly coThayDoi = computed(() => {
    const nhap = this.nhap();
    const daAp = this.value();
    return this.fields().some((f) => chuoi(nhap[f.key]) !== chuoi(daAp[f.key]));
  });
  protected readonly khongCoDieuKien = computed(() => {
    const nhap = this.nhap();
    return this.fields().every((f) => laRong(nhap[f.key]));
  });

  /**
   * Điều kiện đang áp đổi (hoặc panel mở lại) → nháp về đúng `value`: lần mở sau các trường hiện
   * lại đúng thứ đang áp, phần chưa áp của lần trước bị bỏ. Đổi `fields` chỉ thêm/bớt ô, giữ phần
   * đang gõ. `disabled` đổi → khoá/mở cả form. Lifecycle hook, không `effect()`: mỗi việc ở đây
   * ghi vào `FormControl` — thứ ngoài signal graph (quy-uoc/fe-ui-conventions.md §6).
   */
  ngOnChanges(thayDoi: SimpleChanges): void {
    const datLai = !!thayDoi['value'] || (!!thayDoi['open'] && this.open());
    if (datLai || thayDoi['fields']) {
      this.dongBoNhap(datLai);
    }
    if (this.disabled()) {
      this.form.disable({ emitEvent: false });
    } else {
      this.form.enable({ emitEvent: false });
    }
  }

  /** Cho `form` có đúng một ô mỗi trường; `datLai` → mọi ô và nhãn vừa chọn về đúng điều kiện đang áp. */
  private dongBoNhap(datLai: boolean): void {
    const daAp = this.value();
    const khoa = this.fields().map((f) => f.key);
    for (const k of Object.keys(this.form.controls)) {
      if (!khoa.includes(k)) {
        this.form.removeControl(k, { emitEvent: false });
      }
    }
    for (const k of khoa) {
      if (!Object.hasOwn(this.form.controls, k)) {
        this.form.addControl(k, new FormControl(chuoi(daAp[k]), { nonNullable: true }), {
          emitEvent: false,
        });
      } else if (datLai) {
        this.form.controls[k].setValue(chuoi(daAp[k]), { emitEvent: false });
      }
    }
    if (datLai) {
      this.nhanNhap.set({});
    }
    this.form.updateValueAndValidity();
  }

  protected idTruong(f: FilterField): string {
    return `${this.panelId()}-truong-${f.key}`;
  }

  // ---- Trường `autocomplete` ----
  protected trangThaiTim(f: FilterField): FilterSearchState | null {
    return this.searchStates()[f.key] ?? null;
  }

  /** Nhãn của giá trị đang chọn; `undefined` khi chưa biết tên (ô để trống, không hiện khoá thô). */
  private nhanDangChon(f: FilterField): string | undefined {
    const v = chuoi(this.nhap()[f.key]);
    if (v === '') {
      return undefined;
    }
    const nhanVuaChon = this.nhanNhap()[f.key];
    if (nhanVuaChon !== undefined) {
      return nhanVuaChon;
    }
    if (v === chuoi(this.value()[f.key])) {
      return this.selectedLabels()[f.key];
    }
    return this.searchStates()[f.key]?.options.find((o) => o.key === v)?.label;
  }

  /** Khoá truyền cho `Autocomplete`: `null` khi chưa có nhãn → ô trống. */
  protected khoaAutocomplete(f: FilterField): string | null {
    return this.nhanDangChon(f) === undefined ? null : chuoi(this.nhap()[f.key]);
  }

  /** Gợi ý của ô: kết quả tìm, cộng mục đang chọn nếu chưa nằm trong đó (để ô biết nhãn của nó). */
  protected goiYAutocomplete(f: FilterField): readonly Option[] {
    const ds = this.searchStates()[f.key]?.options ?? [];
    const nhan = this.nhanDangChon(f);
    const khoa = chuoi(this.nhap()[f.key]);
    return nhan !== undefined && !ds.some((o) => o.key === khoa)
      ? [...ds, { key: khoa, label: nhan }]
      : ds;
  }

  protected onChonAutocomplete(f: FilterField, gt: string | readonly string[]): void {
    const khoa = typeof gt === 'string' ? gt : '';
    const nhan = this.searchStates()[f.key]?.options.find((o) => o.key === khoa)?.label;
    this.nhanNhap.update((cu) => {
      const moi = { ...cu };
      if (nhan !== undefined) {
        moi[f.key] = nhan;
      } else {
        delete moi[f.key];
      }
      return moi;
    });
    this.form.controls[f.key].setValue(khoa);
  }

  protected onTim(key: string, text: string): void {
    this.chuoiTimGanNhat.set(key, text);
    this.fieldSearch.emit({ key, text });
  }

  protected thuLaiTim(key: string): void {
    this.fieldSearch.emit({ key, text: this.chuoiTimGanNhat.get(key) ?? '' });
  }

  // ---- Hành động ----
  protected apDung(su: Event): void {
    su.preventDefault();
    if (this.disabled() || !this.coThayDoi()) {
      return;
    }
    const nhap = this.nhap();
    const daAp = this.value();
    const ketQua: Record<string, unknown> = {};
    for (const f of this.fields()) {
      const gt = KIND_DA_VE.includes(f.kind) ? nhap[f.key] : daAp[f.key];
      ketQua[f.key] = nhap[f.key] === '' || laRong(gt) ? null : gt;
    }
    this.applied.emit(ketQua);
    this.closed.emit();
  }

  protected xoaHet(): void {
    this.form.setValue(Object.fromEntries(this.fields().map((f) => [f.key, ''])));
    this.nhanNhap.set({});
    // Nút vừa bấm sắp `disabled`: đưa focus đi trước, không để rơi về <body>.
    this.bieuMau()
      ?.nativeElement.querySelector<HTMLElement>(
        ':is(input, select, textarea):not([disabled]):not([type="hidden"])',
      )
      ?.focus();
    this.cleared.emit();
  }

  protected khiDrawerDong(): void {
    this.dongBoNhap(true);
    this.closed.emit();
  }
}
