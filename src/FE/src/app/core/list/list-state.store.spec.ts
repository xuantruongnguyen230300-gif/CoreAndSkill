import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';

import { PagedList } from '../http/paged.model';
import { GridQuery } from './grid-query';
import { ListStateStore } from './list-state.store';

interface Hang {
  readonly id: number;
}

function trang(page: number, soDong: number, totalCount: number): PagedList<Hang> {
  const items = Array.from({ length: soDong }, (_, i) => ({ id: (page - 1) * 20 + i + 1 }));
  return { items, page, pageSize: 20, totalCount };
}

/** Lớn hơn `NGUNG_GO_MS` của store — đợi thật, suite chạy zoneless nên không có fakeAsync. */
const QUA_NGUNG_GO_MS = 350;

function cho(ms = 0): Promise<void> {
  return new Promise((r) => setTimeout(r, ms));
}

describe('ListStateStore', () => {
  let router: Router;
  let store: ListStateStore<Hang>;
  let load: jasmine.Spy<(q: GridQuery) => Observable<PagedList<Hang>>>;

  /** Cho router điều hướng xong và effect của `toObservable` chạy. */
  async function onDinh(): Promise<void> {
    await cho();
    TestBed.tick();
    await cho();
    TestBed.tick();
  }

  function lanGoiCuoi(): GridQuery {
    return load.calls.mostRecent().args[0];
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideRouter([]), ListStateStore],
    });
    router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    store = TestBed.inject<ListStateStore<Hang>>(ListStateStore);
    load = jasmine.createSpy('load');
  });

  it('tải xong trang có dòng → state idle, rows và totalCount theo kết quả', async () => {
    load.and.returnValue(of(trang(1, 20, 45)));
    store.connect(load);
    await onDinh();

    expect(lanGoiCuoi().page).toBe(1);
    expect(lanGoiCuoi().pageSize).toBe(20);
    expect(store.state()).toBe('idle');
    expect(store.rows().length).toBe(20);
    expect(store.totalCount()).toBe(45);
  });

  it('không có bản ghi và không có điều kiện → empty; có từ khoá → empty-filtered', async () => {
    load.and.returnValue(of(trang(1, 0, 0)));
    store.connect(load);
    await onDinh();
    expect(store.state()).toBe('empty');

    await router.navigateByUrl('/?searchText=zz');
    await onDinh();
    expect(store.state()).toBe('empty-filtered');
  });

  it('connect() gọi lần hai → ném lỗi', () => {
    load.and.returnValue(of(trang(1, 0, 0)));
    store.connect(load);
    expect(() => store.connect(load)).toThrowError(/một lần/);
  });

  it('F2 — đang ở trang 1 có dòng, sang trang 2 API lỗi → state error và truy vấn là trang 2', async () => {
    load.and.callFake((q: GridQuery) =>
      q.page === 1 ? of(trang(1, 20, 45)) : throwError(() => new Error('500')),
    );
    store.connect(load);
    await onDinh();
    expect(store.state()).toBe('idle');

    store.setPage(2);
    await onDinh();

    expect(store.query().page).toBe(2);
    expect(store.state())
      .withContext('state là nguồn duy nhất — DataTable quyết hiện slot lỗi theo nó, bất kể rows')
      .toBe('error');
  });

  it('F3 — URL mất searchText (bấm mục menu) rồi gõ lại đúng từ cũ → từ khoá lên URL lại và lưới lọc lại', async () => {
    load.and.returnValue(of(trang(1, 3, 3)));
    store.connect(load);
    await onDinh();

    store.setSearchText('an');
    await cho(QUA_NGUNG_GO_MS);
    await onDinh();
    expect(store.query().searchText).toBe('an');
    expect(lanGoiCuoi().searchText).toBe('an');

    // Bấm mục menu của chính màn đang mở: URL mất searchText, store cũ vẫn sống.
    await router.navigateByUrl('/');
    await onDinh();
    expect(store.query().searchText).toBeUndefined();
    expect(lanGoiCuoi().searchText).toBeUndefined();

    store.setSearchText('an');
    await cho(QUA_NGUNG_GO_MS);
    await onDinh();

    expect(store.query().searchText)
      .withContext('URL là nguồn sự thật — lần gõ này không được bị nuốt')
      .toBe('an');
    expect(lanGoiCuoi().searchText).toBe('an');
  });

  it('gõ liên tục → chỉ một lần ghi URL sau khi ngừng gõ, từ khoá đã trim, page về 1', async () => {
    load.and.returnValue(of(trang(1, 3, 3)));
    await router.navigateByUrl('/?page=3');
    store.connect(load);
    await onDinh();
    const navigate = spyOn(router, 'navigate').and.callThrough();

    store.setSearchText('a');
    store.setSearchText('an');
    store.setSearchText(' an ');
    await cho(QUA_NGUNG_GO_MS);
    await onDinh();

    expect(navigate).toHaveBeenCalledTimes(1);
    expect(store.query().searchText).toBe('an');
    expect(store.query().page).toBe(1);
  });

  it('cùng truy vấn ghi lại lên URL → không gọi API lần hai (bỏ qua truy vấn trùng ở connect)', async () => {
    load.and.returnValue(of(trang(1, 3, 3)));
    store.connect(load);
    store.setSearchText('an');
    await cho(QUA_NGUNG_GO_MS);
    await onDinh();
    const soLan = load.calls.count();

    store.setSearchText('an ');
    await cho(QUA_NGUNG_GO_MS);
    await onDinh();

    expect(load.calls.count()).toBe(soLan);
  });

  it('reload() gọi lại API với cùng truy vấn', async () => {
    load.and.returnValue(of(trang(1, 3, 3)));
    store.connect(load);
    await onDinh();
    const soLan = load.calls.count();

    store.reload();
    await onDinh();

    expect(load.calls.count()).toBe(soLan + 1);
  });

  describe('luật 9 — trang vượt trang cuối thì về trang cuối', () => {
    /** Kho giả: trả đúng lát của `tong` bản ghi cho trang được hỏi — trang vượt thì `items` rỗng. */
    function kho(tong: () => number): (q: GridQuery) => Observable<PagedList<Hang>> {
      return (q) => {
        const soDong = Math.max(0, Math.min(q.pageSize, tong() - (q.page - 1) * q.pageSize));
        return of({ ...trang(q.page, soDong, tong()), pageSize: q.pageSize });
      };
    }

    it('URL ?page=5, 30 bản ghi, pageSize 20 → tự về trang 2 bằng replaceUrl, hiện 10 dòng', async () => {
      load.and.callFake(kho(() => 30));
      await router.navigateByUrl('/?page=5');
      const navigate = spyOn(router, 'navigate').and.callThrough();
      store.connect(load);
      await onDinh();
      await onDinh();

      expect(load.calls.allArgs().map(([q]) => q.page)).toEqual([5, 2]);
      expect(navigate).toHaveBeenCalledTimes(1);
      expect(navigate.calls.mostRecent().args[1]).toEqual(
        jasmine.objectContaining({ queryParams: { page: 2 }, replaceUrl: true }),
      );
      expect(store.query().page).toBe(2);
      expect(router.url).toBe('/?page=2');
      expect(store.state()).toBe('idle');
      expect(store.rows().length).toBe(10);
      expect(store.totalCount()).toBe(30);
    });

    it('trong lúc chờ trang đích: state giữ loading, KHÔNG lưu kết quả rỗng của trang vượt', async () => {
      const trangDich = new Subject<PagedList<Hang>>();
      load.and.callFake((q: GridQuery) => (q.page === 5 ? of(trang(5, 0, 30)) : trangDich));
      await router.navigateByUrl('/?page=5');
      store.connect(load);
      await onDinh();
      await onDinh();

      expect(store.query().page).toBe(2);
      expect(store.state())
        .withContext('không lọt về empty/idle giữa hai lượt tải')
        .toBe('loading');
      expect(store.totalCount())
        .withContext('kết quả rỗng của trang 5 không được lưu — totalCount vẫn là của lúc chưa tải')
        .toBe(0);

      trangDich.next(trang(2, 10, 30));
      TestBed.tick();
      expect(store.state()).toBe('idle');
      expect(store.rows().length).toBe(10);
    });

    it('xoá dòng cuối của trang cuối rồi reload → về trang trước, page gỡ khỏi URL khi là 1', async () => {
      let tong = 21;
      load.and.callFake(kho(() => tong));
      await router.navigateByUrl('/?page=2');
      store.connect(load);
      await onDinh();
      expect(store.rows().length).toBe(1);

      tong = 20;
      const navigate = spyOn(router, 'navigate').and.callThrough();
      store.reload();
      await onDinh();
      await onDinh();

      expect(navigate).toHaveBeenCalledTimes(1);
      expect(navigate.calls.mostRecent().args[1]).toEqual(
        jasmine.objectContaining({ queryParams: { page: null }, replaceUrl: true }),
      );
      expect(router.url).toBe('/');
      expect(store.query().page).toBe(1);
      expect(store.state()).toBe('idle');
      expect(store.rows().length).toBe(20);
      expect(store.totalCount()).toBe(20);
    });

    it('BE trả rỗng ở MỌI trang mà totalCount > 0 → page giảm ngặt, dừng ở trang 1, không lặp', async () => {
      load.and.callFake((q: GridQuery) => of(trang(q.page, 0, 90)));
      await router.navigateByUrl('/?page=9');
      store.connect(load);
      for (let i = 0; i < 6; i++) await onDinh();

      const cacTrang = load.calls.allArgs().map(([q]) => q.page);
      expect(cacTrang).toEqual([9, 5, 4, 3, 2, 1]);
      expect(store.query().page).toBe(1);
      expect(store.state())
        .withContext('trang 1 thì không ghi URL nữa — nhận kết quả như thường')
        .toBe('idle');
    });

    it('trang 1 rỗng mà totalCount > 0 → không ghi URL (page = 1 không vượt được)', async () => {
      load.and.returnValue(of(trang(1, 0, 5)));
      const navigate = spyOn(router, 'navigate').and.callThrough();
      store.connect(load);
      await onDinh();

      expect(navigate).not.toHaveBeenCalled();
      expect(store.state()).toBe('idle');
    });
  });

  it('query param sai dạng thì bỏ; tham số lạ là bộ lọc; tab không phải bộ lọc', async () => {
    await router.navigateByUrl('/?page=abc&pageSize=-1&status=active&tab=x');
    TestBed.tick();

    expect(store.query().page).toBe(1);
    expect(store.query().pageSize).toBe(20);
    expect(store.query().filters).toEqual({ status: 'active' });
  });
});
