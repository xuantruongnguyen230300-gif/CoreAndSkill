import { definePreset } from '@primeuix/themes';
import AuraAutocomplete from '@primeuix/themes/aura/autocomplete';
import AuraBase from '@primeuix/themes/aura/base';
import AuraButton from '@primeuix/themes/aura/button';
import AuraCheckbox from '@primeuix/themes/aura/checkbox';
import AuraChip from '@primeuix/themes/aura/chip';
import AuraDatatable from '@primeuix/themes/aura/datatable';
import AuraDialog from '@primeuix/themes/aura/dialog';
import AuraInputtext from '@primeuix/themes/aura/inputtext';
import AuraMenu from '@primeuix/themes/aura/menu';
import AuraPaginator from '@primeuix/themes/aura/paginator';
import AuraRadiobutton from '@primeuix/themes/aura/radiobutton';
import AuraSelect from '@primeuix/themes/aura/select';
import AuraToast from '@primeuix/themes/aura/toast';
import AuraTooltip from '@primeuix/themes/aura/tooltip';

/**
 * Bảy nhóm semantic được ghi đè theo ĐÚNG danh sách ở
 * docs/wiki-core/fe/04-design-token-system.md §7: primary, highlight, formField, content,
 * overlay, text (đều nằm dưới `colorScheme.light`/`colorScheme.dark`) và `focusRing` (nằm ngoài
 * `colorScheme` — dùng chung cho cả hai chế độ). Thang màu gốc 50–950 và các nhóm còn lại của
 * Aura (mask, list, navigation…) KHÔNG bị đụng tới về MÀU — chưa có component nào cần tới chúng.
 *
 * Ngoài bảy nhóm màu trên, `definePreset` ở cuối tệp còn ghi đè hai khoá semantic KHÔNG phải màu:
 * `transitionDuration` và `mask.transitionDuration` (ADR-0073). Chúng khai thẳng ở đó, không nằm
 * trong object này — `colorScheme` chỉ giữ thứ đổi theo chế độ sáng/tối.
 *
 * `light` và `dark` trỏ CÙNG một object: bản thân biến `var(--color-*)` đã đổi giá trị theo
 * `data-theme` (src/styles/_tokens.scss); PrimeNG không cần một cơ chế đổi màu thứ hai
 * (`darkModeSelector: false` ở app.config.ts).
 */
const colorScheme = {
  primary: {
    color: 'var(--color-brand)',
    contrastColor: 'var(--color-text-on-brand)',
    hoverColor: 'var(--color-brand-hover)',
    activeColor: 'var(--color-brand-active)',
  },
  highlight: {
    background: 'var(--color-brand-subtle)',
    focusBackground: 'var(--color-brand-subtle)',
    color: 'var(--color-brand-on-subtle)',
    focusColor: 'var(--color-brand-on-subtle)',
  },
  formField: {
    background: 'var(--color-surface)',
    disabledBackground: 'var(--color-surface-3)',
    filledBackground: 'var(--color-surface-2)',
    filledHoverBackground: 'var(--color-surface-2)',
    filledFocusBackground: 'var(--color-surface-2)',
    borderColor: 'var(--color-border-strong)',
    hoverBorderColor: 'var(--color-border-strong)',
    focusBorderColor: 'var(--color-focus)',
    invalidBorderColor: 'var(--color-danger-border)',
    color: 'var(--color-text)',
    disabledColor: 'var(--color-text-disabled)',
    placeholderColor: 'var(--color-text-muted)',
    invalidPlaceholderColor: 'var(--color-danger)',
    floatLabelColor: 'var(--color-text-muted)',
    floatLabelFocusColor: 'var(--color-brand)',
    floatLabelActiveColor: 'var(--color-text-muted)',
    floatLabelInvalidColor: 'var(--color-danger)',
    iconColor: 'var(--color-text-muted)',
  },
  content: {
    background: 'var(--color-surface)',
    hoverBackground: 'var(--color-surface-2)',
    borderColor: 'var(--color-border)',
    color: 'var(--color-text)',
    hoverColor: 'var(--color-text)',
  },
  overlay: {
    select: {
      background: 'var(--color-surface)',
      borderColor: 'var(--color-border)',
      color: 'var(--color-text)',
    },
    popover: {
      background: 'var(--color-surface)',
      borderColor: 'var(--color-border)',
      color: 'var(--color-text)',
    },
    modal: {
      background: 'var(--color-surface)',
      borderColor: 'var(--color-border)',
      color: 'var(--color-text)',
    },
  },
  text: {
    color: 'var(--color-text)',
    hoverColor: 'var(--color-text)',
    mutedColor: 'var(--color-text-muted)',
    hoverMutedColor: 'var(--color-text-muted)',
  },
};

/**
 * Preset gốc ghép từ TỪNG sub-preset Aura — KHÔNG import object `Aura` gộp của
 * `@primeuix/themes/aura` (kéo theo ~90 sub-preset của mọi component PrimeNG, phần lớn Core
 * không dùng tới; xem ADR-0038 — `docs/adr/0038-preset-primeng-tung-sub-theo-phan-dung.md`).
 *
 * Chỉ ghép đúng sub-preset của những component PrimeNG mà `src/FE` THẬT import trực tiếp hôm nay
 * (xác nhận bằng `grep "from 'primeng/" -r src/FE/src`):
 *   - `base`  — token gốc (`primitive` + `semantic`) mà mọi sub-preset component khác tham chiếu
 *     tới qua cú pháp `{ten.token}`; luôn cần bất kể component nào được dùng.
 *   - `toast` — component PrimeNG (`primeng/toast`, dùng ở `shared/ui/toast/toast.component.ts`).
 *     `MessageService` (`primeng/api`) và `providePrimeNG` (`primeng/config`) không phải component
 *     — không cần sub-preset (luật F29, `docs/RULES.md`).
 *   - `datatable` — `primeng/table`, dùng ở `shared/ui/data-table/` (F3, luật F3-04f — DataTable).
 *   - `paginator` — `primeng/paginator`, dùng ở `shared/ui/pagination/` (F3 — DataTable ghép nó BÊN TRONG).
 *   - `dialog` — `primeng/dialog`, dùng ở `shared/ui/dialog/` (F3).
 *   - `checkbox` — `primeng/checkbox`, dùng ở `shared/ui/check/` (F3 — ma trận phân quyền).
 *   - `radiobutton` — `primeng/radiobutton`, dùng ở `shared/ui/check/` biến thể `radio` (nhánh
 *     `@if (type() === 'radio')` của chính template — vẫn compile tĩnh vào bundle bất kể runtime
 *     có màn nào dùng biến thể đó hay không, nên sub-preset vẫn bắt buộc theo đúng luật F29).
 *   - `autocomplete` — `primeng/autocomplete`, dùng ở `shared/ui/autocomplete/` (F3 — chọn vai trò).
 *   - `menu` — `primeng/menu`, dùng ở `shared/ui/menu/` (F3 — hành động hàng của màn vai trò).
 *   - `tooltip` — `primeng/tooltip`, dùng ở `shared/ui/tooltip/` (F3 — lý do khoá ô/mục menu).
 *
 * Cộng thêm sub-preset của component CON mà PrimeNG tự dựng bên trong các component trên, theo đúng
 * cấu hình lớp bọc đang dùng — không import trực tiếp nhưng vẫn vẽ ra, và thiếu sub-preset thì biến
 * `--p-<con>-*` rỗng (prime-preset.spec.ts dựng lại và canh việc này):
 *   - `button` — nút đóng của `p-dialog` (`closable`).
 *   - `select` — ô số dòng của `p-paginator` (`rowsPerPageOptions`).
 *   - `chip` — mục đã chọn của `p-autoComplete` (`multiple`).
 *   - `inputtext` — ô nhập của `p-autoComplete` biến thể đơn (`pInputText`).
 *
 * Thêm một component PrimeNG mới (`primeng/<x>`) thì thêm sub-preset `<x>` tương ứng vào
 * `AURA_SUBSET.components` — thiếu thì component đó vẫn CHẠY nhưng SAI THEME (không lỗi biên
 * dịch, không test nào bắt cho tới khi có cổng F29).
 */
const AURA_SUBSET = {
  ...AuraBase,
  components: {
    toast: AuraToast,
    datatable: AuraDatatable,
    paginator: AuraPaginator,
    dialog: AuraDialog,
    checkbox: AuraCheckbox,
    radiobutton: AuraRadiobutton,
    autocomplete: AuraAutocomplete,
    menu: AuraMenu,
    tooltip: AuraTooltip,
    // Component con — xem khối chú thích trên.
    button: AuraButton,
    select: AuraSelect,
    chip: AuraChip,
    inputtext: AuraInputtext,
  },
};

/**
 * Preset styled của Core — dựng từ `AURA_SUBSET` bằng `definePreset`, đăng ký một lần ở
 * `app.config.ts` (`providePrimeNG`). Preset KHÔNG giữ mã màu nào, chỉ trỏ `var(--color-*)` —
 * đổi bảng màu là sửa `src/styles/_tokens.scss`, không sửa tệp này.
 */
export const CORE_PRIME_PRESET = definePreset(AURA_SUBSET, {
  semantic: {
    /**
     * Thời lượng của MỌI chuyển tiếp nhỏ trong thư viện — ADR-0073 quyết định 1.
     *
     * ĐÚNG MỘT khoá, không phải một khoá cho mỗi component. Sub-preset của từng component trỏ tới
     * một trong hai tên: `{transition.duration}` (datatable, paginator, menu, toast…) hoặc
     * `{form.field.transition.duration}` (checkbox, autocomplete…) — nhưng Aura khai
     * `semantic.formField.transitionDuration = '{transition.duration}'`, tức nhánh thứ hai CŨNG
     * đổ về khoá này. Sửa ở gốc thì mọi lá theo, kể cả lá chưa mọc.
     *
     * 🛑 KHÔNG khai thêm `formField.transitionDuration` cho "dễ đọc": nó đã trỏ về đây rồi, khai
     * lại là dựng nguồn thứ hai cho cùng một giá trị, và nguồn thứ hai sẽ không được sửa cùng lúc
     * (ADR-0073, phương án B của vế 1).
     *
     * ⚠️ Tầm ảnh hưởng rộng hơn danh sách component đã khảo sát: cùng khoá này nuôi `button`,
     * `select`, `inputtext`, `chip`, `tooltip` và mọi sub-preset thêm về sau. Aura cho nó `0.2s`;
     * `--dur-fast` là `120ms`, nên mọi chuyển tiếp nhỏ nhanh lên. Chỗ nào vì thế thành giật thì
     * báo `design-expert` — KHÔNG khai riêng một khoá ở đây để chữa.
     */
    transitionDuration: 'var(--dur-fast)',
    /**
     * Backdrop của lớp nổi dạng modal — ADR-0073 quyết định 7.
     *
     * Backdrop đi đường khác hẳn hộp thoại: `@primeuix/styles/base` chạy
     * `animation: p-overlay-mask-enter-animation dt('mask.transition.duration') forwards`, tức nó
     * là CSS thuần và token TỚI ĐƯỢC — khác hộp, nơi tham số là chuỗi hoạt ảnh Angular không giải
     * `var()` (xem `DIALOG_THOI_LUONG_MS` ở shared/ui/dialog/dialog.component.ts).
     *
     * 🛑 Giá trị ở đây phải là CÙNG token với hằng số thời lượng của `Dialog` — `--dur-base`, chứ
     * KHÔNG phải `var(--dur-fast)` của khoá trên. Design/Components/Dialog.md đòi backdrop mờ dần
     * CÙNG LÚC với hộp; nối lẻ backdrop về một token khác làm khoảng lệch rộng ra, không hẹp lại.
     * Aura mặc định cho khoá này `0.3s` riêng — nó KHÔNG đi theo `transitionDuration` ở trên, nên
     * bỏ khoá này là để backdrop trôi một mình.
     *
     * Đổi token của `Dialog` thì đổi cả hai chỗ: hằng số kia và dòng này. `prime-preset.spec.ts`
     * canh dòng này bằng `--dur-base`; cổng F37 canh hằng số kia bằng cùng token — hai nửa gặp
     * nhau ở tên token, vì luật F1 cấm `core/` import `shared/`.
     */
    mask: {
      transitionDuration: 'var(--dur-base)',
    },
    // Vòng focus của MỌI control thư viện — cùng cơ chế outline + offset với DESIGN.md §2.6.
    focusRing: {
      width: 'var(--border-w-strong)',
      style: 'solid',
      color: 'var(--color-focus)',
      offset: '2px',
      shadow: 'none',
    },
    colorScheme: {
      light: colorScheme,
      dark: colorScheme,
    },
  },
});
