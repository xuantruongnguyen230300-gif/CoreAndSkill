import {
  Component,
  TemplateRef,
  provideZonelessChangeDetection,
  signal,
  viewChild,
} from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { DataColumnDef, DataTableComponent, DataTableViewState } from './data-table.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

interface Hang {
  readonly id: number;
  readonly ten: string;
}

@Component({
  standalone: true,
  imports: [DataTableComponent],
  template: `
    <ng-template #oTen let-hang>{{ hang.ten }}</ng-template>
    <ng-template #slotLoi><span class="slot-loi">slot-loi</span></ng-template>
    <ng-template #slotTai><span class="slot-tai">slot-tai</span></ng-template>
    <ng-template #slotRong let-s
      ><span class="slot-rong">{{ s }}</span></ng-template
    >
    <app-data-table
      [rows]="rows()"
      [columns]="[{ key: 'ten', header: 'Tên', cell: oTen }]"
      caption="Bảng thử"
      rowKey="id"
      paginationAriaLabel="Phân trang"
      [state]="state()"
      [totalRecords]="totalRecords()"
      [page]="page()"
      [errorTemplate]="slotLoi"
      [loadingTemplate]="slotTai"
      [emptyTemplate]="slotRong"
    />
  `,
})
class HostComponent {
  readonly rows = signal<readonly Hang[]>([]);
  readonly state = signal<DataTableViewState>('idle');
  readonly totalRecords = signal(0);
  readonly page = signal(1);
}

@Component({
  standalone: true,
  imports: [DataTableComponent],
  template: `
    <ng-template #oTen let-hang>{{ hang.ten }}</ng-template>
    <app-data-table
      [rows]="[{ id: 1, ten: 'a' }]"
      [columns]="[{ key: 'ten', header: 'Tên', sortable: true, cell: oTen }]"
      caption="Bảng sắp xếp"
      rowKey="id"
      paginationAriaLabel="Phân trang"
      [sortBy]="sortBy()"
      [sortDescending]="giam()"
    />
  `,
})
class SapXepHostComponent {
  readonly sortBy = signal<string | null>(null);
  readonly giam = signal(false);
}

/** Cột `value` (fe-ui-conventions.md §9): chuỗi đã định dạng, đọc signal thì ô tự cập nhật. */
@Component({
  standalone: true,
  imports: [DataTableComponent],
  template: `
    <ng-template #oTen let-hang>{{ hang.ten }}</ng-template>
    <app-data-table
      [rows]="[{ id: 1, ten: 'an' }]"
      [columns]="columns()"
      caption="Bảng value"
      rowKey="id"
      paginationAriaLabel="Phân trang"
    />
  `,
})
class ValueHostComponent {
  readonly hauTo = signal('A');
  readonly columns = signal<readonly DataColumnDef<Hang>[]>([]);
  readonly oTen = viewChild.required<TemplateRef<{ $implicit: Hang }>>('oTen');
}

/** Ô có nút hành động thật — để đo được focus có sống qua một lần tải lại hay không. */
@Component({
  standalone: true,
  imports: [DataTableComponent],
  template: `
    <ng-template #oHanhDong let-hang>
      <button type="button" class="nut-hanh-dong" [attr.data-id]="hang.id">{{ hang.ten }}</button>
    </ng-template>
    <app-data-table
      [rows]="rows()"
      [columns]="[{ key: 'hanhDong', header: 'Hành động', cell: oHanhDong }]"
      caption="Bảng track"
      rowKey="id"
      paginationAriaLabel="Phân trang"
      [state]="state()"
    />
  `,
})
class TrackHostComponent {
  readonly rows = signal<readonly Hang[]>([]);
  readonly state = signal<DataTableViewState>('idle');
}

function trang1(): readonly Hang[] {
  return Array.from({ length: 20 }, (_, i) => ({ id: i + 1, ten: `dong-${i + 1}` }));
}

/**
 * DataTable.md §API dòng `rowKey` và §Luật "Luôn truyền `rowKey`": dòng track theo khoá, không theo
 * tham chiếu. `list.reload()` sau tạo/sửa/xoá cho về object MỚI (mapper dựng lại), nên track theo
 * tham chiếu — mặc định của `p-table` — dựng lại cả `<tbody>` và làm mất focus của nút trên dòng.
 */
describe('DataTableComponent — track dòng theo `rowKey`', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideNoopAnimations()],
    });
  });

  async function dung(): Promise<{
    el: HTMLElement;
    host: TrackHostComponent;
    lai: () => Promise<void>;
  }> {
    const fixture = TestBed.createComponent(TrackHostComponent);
    const host = fixture.componentInstance;
    host.rows.set(trang1());
    fixture.detectChanges();
    await fixture.whenStable();
    return {
      el: fixture.nativeElement as HTMLElement,
      host,
      lai: async () => {
        fixture.detectChanges();
        await fixture.whenStable();
      },
    };
  }

  function dongThan(el: HTMLElement): HTMLTableRowElement[] {
    return Array.from(el.querySelectorAll<HTMLTableRowElement>('tbody tr'));
  }

  function nutCuaDong(el: HTMLElement, id: number): HTMLButtonElement {
    const nut = el.querySelector<HTMLButtonElement>(`.nut-hanh-dong[data-id="${id}"]`);
    if (nut === null) throw new Error(`không thấy nút của dòng ${id}`);
    return nut;
  }

  it('tải lại trả object MỚI cùng khoá → mọi <tr> giữ nguyên phần tử DOM, nút đang focus vẫn giữ focus', async () => {
    const { el, host, lai } = await dung();
    const truoc = dongThan(el);
    expect(truoc.length).toBe(20);
    const nut = nutCuaDong(el, 3);
    nut.focus();
    expect(document.activeElement).withContext('tiền đề: nút nhận được focus').toBe(nut);

    // Mô phỏng list.reload(): đang tải giữ dòng cũ, rồi mapper trả object MỚI cùng id, nội dung đổi.
    host.state.set('loading');
    await lai();
    host.rows.set(trang1().map((h) => ({ ...h, ten: `${h.ten}-moi` })));
    host.state.set('idle');
    await lai();

    const sau = dongThan(el);
    expect(sau.length).toBe(20);
    sau.forEach((tr, i) =>
      expect(tr).withContext(`<tr> thứ ${i} phải là CÙNG phần tử qua lần tải lại`).toBe(truoc[i]),
    );
    expect(nutCuaDong(el, 3)).toBe(nut);
    expect(nut.textContent?.trim()).withContext('dữ liệu mới vẫn vẽ vào ô').toBe('dong-3-moi');
    expect(document.activeElement).withContext('focus không rơi về <body>').toBe(nut);
  });

  it('xoá một dòng rồi tải lại → chỉ <tr> của dòng đó rời DOM; dòng sau nó giữ phần tử (track theo khoá, không theo vị trí)', async () => {
    const { el, host, lai } = await dung();
    const truoc = dongThan(el);
    const nut = nutCuaDong(el, 3);
    nut.focus();

    host.rows.set(trang1().filter((h) => h.id !== 2));
    await lai();

    const sau = dongThan(el);
    expect(sau.length).toBe(19);
    expect(sau).not.toContain(truoc[1]);
    expect(sau[0]).toBe(truoc[0]);
    // Track theo vị trí sẽ tái dùng <tr> thứ 2 cũ cho dòng id 3 — khẳng định dưới đây đỏ ở ca đó.
    expect(sau[1]).withContext('dòng id 3 giữ đúng <tr> của nó').toBe(truoc[2]);
    expect(document.activeElement).toBe(nut);
  });
});

describe('DataTableComponent — cột `value` (fe-ui-conventions.md §9)', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideNoopAnimations()],
    });
  });

  async function dung(
    cot: (host: ValueHostComponent) => readonly DataColumnDef<Hang>[],
  ): Promise<{ el: HTMLElement; host: ValueHostComponent; lai: () => Promise<void> }> {
    const fixture = TestBed.createComponent(ValueHostComponent);
    const host = fixture.componentInstance;
    fixture.detectChanges();
    host.columns.set(cot(host));
    fixture.detectChanges();
    await fixture.whenStable();
    return {
      el: fixture.nativeElement as HTMLElement,
      host,
      lai: async () => {
        fixture.detectChanges();
        await fixture.whenStable();
      },
    };
  }

  it('cột có `value` (không `cell`) → ô vẽ chuỗi value(row) trả về, tiêu đề là header', async () => {
    const { el } = await dung(() => [{ key: 'ma', header: 'Mã NV', value: (r) => `MNV-${r.ten}` }]);

    expect(el.querySelector('thead')?.textContent).toContain('Mã NV');
    expect(el.querySelector('tbody')?.textContent).toContain('MNV-an');
  });

  it('`value` đọc signal → đổi signal thì ô tự cập nhật (view OnPush vẫn vẽ lại)', async () => {
    const { el, host, lai } = await dung((h) => [
      { key: 'ma', header: 'Mã', value: (r) => `${r.ten}-${h.hauTo()}` },
    ]);
    expect(el.querySelector('tbody')?.textContent).toContain('an-A');

    host.hauTo.set('B');
    await lai();

    expect(el.querySelector('tbody')?.textContent).toContain('an-B');
    expect(el.querySelector('tbody')?.textContent).not.toContain('an-A');
  });

  it('cột có `cell` vẫn vẽ bằng template; cột thiếu CẢ `cell` lẫn `value` → ném lỗi lúc dựng cột', async () => {
    const { el } = await dung((h) => [{ key: 'ten', header: 'Tên', cell: h.oTen() }]);
    expect(el.querySelector('tbody')?.textContent).toContain('an');

    const fixture = TestBed.createComponent(ValueHostComponent);
    fixture.detectChanges();
    fixture.componentInstance.columns.set([
      { key: 'rong', header: 'Rỗng' } as unknown as DataColumnDef<Hang>,
    ]);
    expect(() => fixture.detectChanges()).toThrowError(/rong/);
  });

  it('cột có CẢ `cell` lẫn `value` → ném lỗi: mỗi cột đúng một trong hai', async () => {
    const fixture = TestBed.createComponent(ValueHostComponent);
    fixture.detectChanges();
    fixture.componentInstance.columns.set([
      { key: 'ca-hai', header: 'X', cell: fixture.componentInstance.oTen(), value: () => 'x' },
    ]);
    expect(() => fixture.detectChanges()).toThrowError(/ca-hai/);
  });
});

describe('DataTableComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideNoopAnimations(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    });
  });

  async function dung(
    rows: readonly Hang[],
    state: DataTableViewState,
  ): Promise<{ el: HTMLElement; host: HostComponent; lai: () => Promise<void> }> {
    const fixture = TestBed.createComponent(HostComponent);
    const host = fixture.componentInstance;
    host.rows.set(rows);
    host.state.set(state);
    host.totalRecords.set(rows.length > 0 ? 45 : 0);
    fixture.detectChanges();
    await fixture.whenStable();
    return {
      el: fixture.nativeElement as HTMLElement,
      host,
      lai: async () => {
        fixture.detectChanges();
        await fixture.whenStable();
      },
    };
  }

  it('idle có dòng → vẽ thân bảng, không slot nào', async () => {
    const { el } = await dung(trang1(), 'idle');

    expect(el.textContent).toContain('dong-1');
    expect(el.querySelector('.slot-loi')).toBeNull();
  });

  it('F2 — đã có dòng trang 1, sang trang 2 API lỗi → thân bảng là slot lỗi, KHÔNG còn dòng trang 1', async () => {
    const { el, host, lai } = await dung(trang1(), 'idle');
    expect(el.textContent).toContain('dong-1');

    // ListStateStore giữ rows cũ khi lỗi; `state` là nguồn duy nhất (fe-architecture.md §2.8 luật 6).
    host.page.set(2);
    host.state.set('loading');
    await lai();
    host.state.set('error');
    await lai();

    expect(el.querySelector('.slot-loi'))
      .withContext('DataTable.md §Trạng thái — error: thân bảng hiện slot lỗi kèm Thử lại')
      .not.toBeNull();
    expect(el.textContent).not.toContain('dong-1');
    expect(el.textContent).withContext('tiêu đề cột giữ nguyên').toContain('Tên');
  });

  it('error khi chưa có dòng nào → slot lỗi', async () => {
    const { el } = await dung([], 'error');
    expect(el.querySelector('.slot-loi')).not.toBeNull();
  });

  it('loading lần đầu (chưa có dòng) → slot đang tải', async () => {
    const { el } = await dung([], 'loading');
    expect(el.querySelector('.slot-tai')).not.toBeNull();
  });

  it('loading khi đã có dòng → giữ dòng cũ, không slot đang tải', async () => {
    const { el } = await dung(trang1(), 'loading');
    expect(el.textContent).toContain('dong-1');
    expect(el.querySelector('.slot-tai')).toBeNull();
  });

  it('empty-filtered → slot trống nhận đúng state', async () => {
    const { el } = await dung([], 'empty-filtered');
    expect(el.querySelector('.slot-rong')?.textContent).toBe('empty-filtered');
  });

  // Design/Icons.md §5: chưa chọn `pi-sort-alt`, tăng `pi-sort-amount-up-alt`, giảm `pi-sort-amount-down`.
  it('icon sắp xếp theo đúng bảng Icons.md §5 cho ba trạng thái', async () => {
    const fixture = TestBed.createComponent(SapXepHostComponent);
    const icon = async (): Promise<string> => {
      fixture.detectChanges();
      await fixture.whenStable();
      const i = (fixture.nativeElement as HTMLElement).querySelector('.data-table__icon-sap-xep');
      return Array.from(i?.classList ?? [])
        .filter((c) => c.startsWith('pi-'))
        .join(' ');
    };
    expect(await icon()).toBe('pi-sort-alt');
    fixture.componentInstance.sortBy.set('ten');
    expect(await icon()).toBe('pi-sort-amount-up-alt');
    fixture.componentInstance.giam.set(true);
    expect(await icon()).toBe('pi-sort-amount-down');
  });

  // PrimeNG vẽ `#caption` trong một <div> NGOÀI <table>, nên <caption> đặt ở đó không đặt tên cho
  // bảng. Tên truy cập phải nằm trên chính <table> (DataTable.md §Accessibility, dòng "Tiêu đề bảng").
  it('<table> mang tên truy cập bằng chính caption, không có <caption> lạc ngoài <table>', async () => {
    const { el } = await dung(trang1(), 'idle');
    const bang = el.querySelector('table');
    expect(bang).not.toBeNull();
    expect(bang?.getAttribute('aria-label')).toBe('Bảng thử');
    for (const c of Array.from(el.querySelectorAll('caption'))) {
      expect(c.parentElement?.tagName)
        .withContext('<caption> chỉ hợp lệ khi là con trực tiếp của <table>')
        .toBe('TABLE');
    }
  });
});
