---
kind: luat
scope: core
verified: chua-doi-chieu
---

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Các kiểu dưới đây là một phần của mục §9 ở [`fe-ui-conventions.md`](fe-ui-conventions.md) (kiểu dữ liệu công khai của thư viện UI) nhưng **chưa có tệp thật nào trong `src/FE`** (đối chiếu 2026-09-27, `grep` toàn bộ `src/FE/src` theo tên kiểu — 0 kết quả cho mỗi kiểu). Khối dưới là **chữ ký để dựng khi component tương ứng được thi công**, không phải kiểu đang `import` được hôm nay. Kiểu nào có tệp thật thì chuyển ngược về đúng vị trí cũ trong §9 của file gốc, kèm chuỗi neo.

# Kiểu dữ liệu công khai của thư viện UI — phần chưa thi công

Số mục dưới đây trùng tên tiểu mục của §9 ở [`fe-ui-conventions.md`](fe-ui-conventions.md); mỗi kiểu ghi chú component nào dùng nó.

---

## Mở đầu §9 — `TimelineItem`

```typescript
/** Một mục trong Timeline, dùng cho cả hai biến thể. */
export interface TimelineItem {
  key: string;
  state: 'done' | 'current' | 'rejected' | 'upcoming';
  title: string;
  meta?: string;                        // thời gian tuyệt đối, địa chỉ IP…
  quote?: string;                       // lý do, ghi chú
  change?: { from: string; to: string };
  interactive?: boolean;
}
```

Dùng cho [`Timeline`](../Design/Components/Timeline.md).

---

## Họ kiểu CỘT — phần mở rộng chưa thi công

```typescript
/** EditableGrid thêm ba khả năng của một lưới ghi. */
export interface EditableColumnDef<T = unknown> extends ColumnDef<T> {
  editor?: 'text' | 'number' | 'date' | 'select' | 'check';   // 'date' → DatePicker mode='single'
  computed?: boolean;                      // máy tính ra, không gõ được
  validate?: (value: unknown, row: T) => string | null;  // null = hợp lệ
}

/** Sáu công tắc của DataTable. Khai bằng MỘT object có tên, không phải sáu input boolean rời. */
export interface DataTableSwitches {
  selectable?: boolean;
  frozen?: boolean | 'first' | 'both';   // 'both' = ghim cả cột đầu và cột hành động
  tree?: boolean;
  grouped?: boolean;                     // đi kèm `groupBy`
  expandable?: boolean;                  // đi kèm `rowDetail`
  summary?: boolean;                     // đi kèm `summary` và `summaryScope`
}

/** Dòng tổng của DataTable, khi bật công tắc `summary`. */
export interface SummaryRow {
  label: string;                           // ĐÃ kèm phạm vi: "Tổng cộng 137 bản ghi"
  values: Record<string, string>;          // khoá cột → giá trị ĐÃ định dạng
}
```

**`values` của `SummaryRow` là chuỗi đã định dạng, không phải số**; component không được đoán hộ định dạng.

`EditableColumnDef` mở rộng gốc `ColumnDef` (kiểu gốc, đã có thật — [`fe-ui-conventions.md`](fe-ui-conventions.md) §9); dùng cho [`EditableGrid`](../Design/Components/EditableGrid.md). `DataTableSwitches` và `SummaryRow` dùng cho [`DataTable`](../Design/Components/DataTable.md).

---

## Bảy kiểu danh sách lựa chọn — phần chưa thi công

```typescript
export interface TabItem { key: string; label: string; icon?: string; badge?: number; disabled?: boolean }
export interface StepItem { key: string; label: string; note?: string; state: 'done' | 'current' | 'upcoming' | 'error' }
export interface ChartSeries { key: string; label: string; points: ReadonlyArray<number | null> }
export interface DateRangePreset { key: string; label: string; from: Date; to: Date }   // DatePicker mode='range'
```

**`DateRangePreset` là điểm mở rộng của `DatePicker` ở `mode` `'range'`.** Core khai bộ lối tắt mặc định độc lập nghiệp vụ (hôm nay, 7 ngày qua, tháng này, quý này, năm nay); lối tắt nghiệp vụ do trang truyền vào qua đây. `from`/`to` là ngày đã tính sẵn.

**`StepItem`, không phải `Step`.**

**`points` của `ChartSeries` cho phép `null`**: *không có dữ liệu tại điểm đó*, khác `0` là *có dữ liệu và bằng không*.

Dùng cho [`Tabs`](../Design/Components/Tabs.md), [`Stepper`](../Design/Components/Stepper.md), [`Chart`](../Design/Components/Chart.md), [`DatePicker`](../Design/Components/DatePicker.md) (mode `range`).

---

## Ba kiểu của khung ứng dụng — phần chưa thi công

```typescript
export interface ToastItem {                                // Toast — service cắt còn tối đa ba
  id: string; severity: 'success' | 'warning' | 'danger' | 'info';
  title: string; message?: string; actionLabel?: string; duration?: number;
}
export interface UploadItem {                               // FileUpload
  id: string; name: string; size: number;
  status: 'queued' | 'uploading' | 'done' | 'failed' | 'rejected';
  progress: number | null;                                  // null khi chưa bắt đầu hoặc không đo được
  error?: string;
}
```

`UploadItem.progress` nhận `null` **có chủ đích**: khi đó [`../Design/Components/ProgressBar.md`](../Design/Components/ProgressBar.md) chuyển sang biến thể `indeterminate`, không bịa một con số.

Dùng cho [`Toast`](../Design/Components/Toast.md) — `toast.service.ts` hôm nay tự khai `interface ToastMessage` riêng, không dùng `ToastItem` — và [`FileUpload`](../Design/Components/FileUpload.md).

---

**Ba luật đi kèm ở §9 của file gốc** (tiền tố `Ui`, không lọt kiểu PrimeNG, chuỗi hiển thị đã qua i18n trước khi tới component) áp cho **mọi** kiểu ở tệp này y hệt kiểu đã có tệp thật — chúng không lặp lại ở đây, tránh bản sao.

> 📖 Lý do, bẫy, ví dụ mở rộng (mọi kiểu ở tệp này): [`fe-ui-conventions.md`](../wiki-core/fe/ly-do/fe-ui-conventions.md) §9
