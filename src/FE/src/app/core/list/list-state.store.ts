import { DestroyRef, Injectable, Injector, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import {
  EMPTY,
  Observable,
  Subject,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  map,
  switchMap,
  tap,
} from 'rxjs';

import { PagedList } from '../http/paged.model';
import { GridQuery } from './grid-query';

type ListViewState = 'idle' | 'loading' | 'error' | 'empty' | 'empty-filtered';

/** Chờ ngừng gõ. Dưới 200ms một từ gõ có dấu vẫn sinh vài lần gọi; trên 500ms ô tìm có cảm giác chậm. */
const NGUNG_GO_MS = 300;

/** Áp khi URL không mang `pageSize` — mặc định của hợp đồng (contracts/README.md §8). */
const PAGE_SIZE_MAC_DINH = 20;

/** Tên dây dùng chung cho mọi danh sách; query param nào khác là bộ lọc. `tab` là tên dùng chung: store bỏ qua (luật 8). */
const TEN_DUNG_CHUNG: readonly string[] = [
  'page',
  'pageSize',
  'sortBy',
  'sortDescending',
  'searchText',
  'tab',
];

/**
 * Định nghĩa gốc: quy-uoc/fe-architecture.md §2.8. MỘT mẫu duy nhất cho mọi màn danh sách — không
 * `providedIn: 'root'` (luật 7): cấp ở `providers` của chính page mà route trỏ tới, MỘT thực thể
 * cho MỘT màn. Tầng này KHÔNG inject `HttpClient` và không biết endpoint — màn truyền hàm tải của
 * service mình qua `connect()`.
 */
@Injectable()
export class ListStateStore<T> {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  private readonly _state = signal<ListViewState>('idle');
  private readonly _result = signal<PagedList<T> | null>(null);
  private readonly reloadTick = signal(0);
  private readonly searchInput = new Subject<string>();
  private connected = false;

  /** Bản ĐỌC của URL, không phải bản thứ hai (luật 1). */
  readonly query = toSignal(this.route.queryParamMap.pipe(map(docQuery)), { requireSync: true });
  readonly state = this._state.asReadonly();
  /** Rỗng KHÔNG mang nghĩa "không có dữ liệu" — `state` mới mang nghĩa đó. */
  readonly rows = computed<readonly T[]>(() => this._result()?.items ?? []);
  readonly totalCount = computed(() => this._result()?.totalCount ?? 0);

  constructor() {
    // Cơ chế 1: chờ ngừng gõ; thay URL, không nhồi lịch sử (luật 5). KHÔNG lọc trùng theo lần gõ:
    // URL có thể đã đổi giữa hai lần gõ cùng một từ — cơ chế 2 so TRUY VẤN, ở connect() (luật 1, 4).
    this.searchInput
      .pipe(
        debounceTime(NGUNG_GO_MS),
        map((s) => s.trim()),
        takeUntilDestroyed(),
      )
      .subscribe((s) => this.ghiUrl({ searchText: s === '' ? null : s, page: null }, true));
  }

  /** Màn gọi ĐÚNG MỘT lần, truyền hàm tải của service mình. Store không biết endpoint. */
  connect(load: (query: GridQuery) => Observable<PagedList<T>>): this {
    if (this.connected) {
      throw new Error('ListStateStore.connect() chỉ được gọi một lần cho mỗi màn');
    }
    this.connected = true;
    combineLatest([
      toObservable(this.query, { injector: this.injector }).pipe(distinctUntilChanged(cungTruyVan)),
      toObservable(this.reloadTick, { injector: this.injector }),
    ])
      .pipe(
        tap(() => this._state.set('loading')),
        // Cơ chế 3: request mới huỷ request cũ.
        switchMap(([q]) =>
          load(q).pipe(
            tap((trang) => {
              if (trang.items.length === 0 && trang.totalCount > 0 && q.page > 1) {
                // Luật 9: vượt trang cuối (URL cũ, vừa xoá dòng cuối của trang cuối) → về trang cuối,
                // thay URL tại chỗ; giữ 'loading' tới lượt tải kế và KHÔNG lưu kết quả rỗng. `page`
                // giảm ngặt (≤ page − 1) nên chuỗi ghi URL dừng chắc chắn, muộn nhất ở trang 1.
                const cuoi = Math.min(q.page - 1, Math.ceil(trang.totalCount / q.pageSize));
                this.ghiUrl({ page: cuoi === 1 ? null : cuoi }, true);
                return;
              }
              this._result.set(trang);
              this._state.set(
                trang.totalCount > 0 ? 'idle' : coDieuKien(q) ? 'empty-filtered' : 'empty',
              );
            }),
            // errorInterceptor đã hiển thị lỗi; ở đây chỉ đổi trạng thái.
            catchError(() => {
              this._state.set('error');
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
    return this;
  }

  setPage(page: number, pageSize?: number): void {
    const patch: Params = { page: page === 1 ? null : page };
    if (pageSize !== undefined) {
      patch['pageSize'] = pageSize === PAGE_SIZE_MAC_DINH ? null : pageSize;
    }
    this.ghiUrl(patch, false);
  }

  setSort(sortBy: string | null, sortDescending: boolean): void {
    this.ghiUrl({ sortBy, sortDescending: sortBy !== null && sortDescending ? true : null }, false);
  }

  setSearchText(searchText: string): void {
    this.searchInput.next(searchText);
  }

  /** `null` gỡ điều kiện khỏi URL. Đổi lọc thì về trang 1 (luật 3). */
  setFilters(filters: Readonly<Record<string, string | null>>): void {
    this.ghiUrl({ ...filters, page: null }, false);
  }

  reload(): void {
    this.reloadTick.update((n) => n + 1);
  }

  /** `null` gỡ tham số — giá trị mặc định không nằm trên URL. */
  private ghiUrl(patch: Params, replaceUrl: boolean): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: patch,
      queryParamsHandling: 'merge',
      replaceUrl,
    });
  }
}

function docQuery(p: ParamMap): GridQuery {
  const filters: Record<string, string> = {};
  for (const k of p.keys) {
    const v = p.get(k);
    if (v !== null && !TEN_DUNG_CHUNG.includes(k)) {
      filters[k] = v;
    }
  }
  return {
    page: soNguyenDuong(p.get('page')) ?? 1,
    pageSize: soNguyenDuong(p.get('pageSize')) ?? PAGE_SIZE_MAC_DINH,
    sortBy: p.get('sortBy') ?? undefined,
    sortDescending: p.get('sortDescending') === 'true' ? true : undefined,
    searchText: p.get('searchText') ?? undefined,
    filters,
  };
}

/** Sai dạng thì bỏ (luật 2). */
function soNguyenDuong(raw: string | null): number | undefined {
  const n = raw === null ? Number.NaN : Number(raw);
  return Number.isInteger(n) && n >= 1 ? n : undefined;
}

function cungTruyVan(a: GridQuery, b: GridQuery): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}

function coDieuKien(q: GridQuery): boolean {
  return q.searchText !== undefined || Object.keys(q.filters).length > 0;
}
