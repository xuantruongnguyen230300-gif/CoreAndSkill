import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgTemplateOutlet } from '@angular/common';
import { AutoCompleteCompleteEvent, AutoCompleteModule } from 'primeng/autocomplete';

/** Chữ ký đầy đủ ở quy-uoc/fe-ui-conventions.md §9. */
export interface Option {
  readonly key: string;
  readonly label: string;
  readonly hint?: string;
}

/**
 * Design/Components/Autocomplete.md. Nơi DUY NHẤT trong ứng dụng được import `primeng/autocomplete`.
 * Định vị lớp nổi khi cuộn/tràn viewport là hành vi "khó" (Design/COMPONENTS.md §4).
 *
 * 🚧 F3 tối giản: hai biến thể, ba cỡ, `minChars`/`debounceMs` (map thẳng sang `minQueryLength`/
 * `delay` của PrimeNG — component KHÔNG tự debounce lần hai), `search`/`valueChange`. Component
 * là dumb: KHÔNG tự gọi HTTP, KHÔNG tự bỏ kết quả request cũ — trang cha giữ số thứ tự request và
 * bỏ kết quả đến muộn (Autocomplete.md "Một luật thi công không được bỏ"). `allowCreate` /
 * `createRequested` CHƯA dựng — không màn F3 nào cho tạo nhanh từ ô chọn vai trò.
 *
 * `value`/`options` chỉ mang KHOÁ — PrimeNG cần đối tượng `Option` đầy đủ để hiển thị nhãn. Cache
 * nhãn (`nhanDaBiet`) gom dần từ mọi `options()` từng thấy, để một khoá đã chọn KHÔNG mất nhãn khi
 * kết quả tìm kiếm đổi sang trang khác (Autocomplete "quên nhãn" là lỗi UX thật, không phải giả
 * định) — trang gọi vẫn nên seed `options()` bằng các lựa chọn ban đầu khi mở form sửa.
 */
@Component({
  selector: 'app-autocomplete',
  standalone: true,
  imports: [NgTemplateOutlet, FormsModule, AutoCompleteModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './autocomplete.component.html',
  styleUrl: './autocomplete.component.scss',
})
export class AutocompleteComponent {
  readonly options = input<readonly Option[]>([]);
  readonly value = input<string | readonly string[] | null>(null);
  readonly variant = input<'single' | 'multiple'>('single');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly minChars = input(2);
  readonly debounceMs = input(300);
  /** `p-autoComplete` (bản cài) không có input `loading` công khai (tự quản lý nội bộ khi đang
   *  gọi `completeMethod`) — cờ này giữ đúng API dự kiến của Autocomplete.md cho trang gọi, nhưng
   *  KHÔNG có chỗ nối trực quan vào spinner của thư viện ở bản này; trang vẫn dùng nó để khoá nút
   *  gửi form nếu cần. Biết vẫn còn thiếu — không giấu. */
  readonly loading = input(false);
  readonly errorTemplate = input<TemplateRef<unknown> | null>(null);
  readonly disabled = input(false);
  readonly placeholder = input.required<string>();
  readonly inputId = input<string | null>(null);
  readonly describedBy = input<string | null>(null);

  // `search` là tên trong API "định nghĩa gốc" của Autocomplete.md §API dự kiến — trùng tên sự
  // kiện DOM gốc trên input search, nhưng đây là output CUSTOM của component, không phải native
  // event passthrough. Rule không nằm trong danh sách cấm tắt (fe-architecture.md §4.5).
  // eslint-disable-next-line @angular-eslint/no-output-native
  readonly search = output<string>();
  readonly valueChange = output<string | readonly string[]>();

  private readonly nhanDaBiet = signal<ReadonlyMap<string, string>>(new Map());

  /** `[suggestions]` của PrimeNG đòi mảng CÓ THỂ SỬA (`any[]`), `options()` là readonly — bản sao rẻ. */
  protected readonly dsGoiY = computed(() => [...this.options()]);

  /** Giá trị đưa vào `[ngModel]` của PrimeNG — object đầy đủ, không phải khoá trần. */
  protected readonly gtHienTai = computed<Option | Option[] | null>(() => {
    const v = this.value();
    const nhan = this.nhanDaBiet();
    const dungOption = (k: string): Option => ({ key: k, label: nhan.get(k) ?? k });
    // `typeof v === 'string'`, không `Array.isArray`, để tránh giới hạn thu hẹp kiểu của TS với
    // hợp `readonly string[]` ở nhánh phủ định (loại mảng ra khỏi hợp không luôn thu hẹp đúng).
    if (this.variant() === 'multiple') {
      return typeof v === 'string' || v === null ? [] : v.map(dungOption);
    }
    return typeof v === 'string' ? dungOption(v) : null;
  });

  constructor() {
    effect(() => {
      const opts = this.options();
      if (opts.length === 0) {
        return;
      }
      this.nhanDaBiet.update((cu) => {
        let doi = false;
        const moi = new Map(cu);
        for (const o of opts) {
          if (moi.get(o.key) !== o.label) {
            moi.set(o.key, o.label);
            doi = true;
          }
        }
        return doi ? moi : cu;
      });
    });
  }

  protected onComplete(evt: AutoCompleteCompleteEvent): void {
    this.search.emit(evt.query);
  }

  protected onModelChange(gt: Option | Option[] | null): void {
    if (this.variant() === 'multiple') {
      const ds = Array.isArray(gt) ? gt : [];
      this.valueChange.emit(ds.map((o) => o.key));
      return;
    }
    this.valueChange.emit(gt === null || Array.isArray(gt) ? '' : gt.key);
  }
}
