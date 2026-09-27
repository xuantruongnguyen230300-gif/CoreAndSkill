import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { danhSachTrang, theoId } from './crud';
import { PagedList } from './paged.model';

interface DongDto {
  readonly id: string;
  readonly createdAt: string;
}

interface Dong {
  readonly id: string;
  readonly createdAt: Date;
}

const sangDong = (dto: DongDto): Dong => ({ id: dto.id, createdAt: new Date(dto.createdAt) });

/**
 * fe-api-client.md §5.2 — hàm dùng chung của mọi service danh sách. Kiểm qua request THẬT đi ra
 * (`HttpTestingController`), vì hợp đồng là chuỗi query string trên dây (contracts/README.md §8),
 * không phải hàm nội bộ dựng nó.
 */
describe('crud — danhSachTrang() / theoId()', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  const TRANG_RONG: PagedList<DongDto> = { items: [], page: 1, pageSize: 20, totalCount: 0 };

  it('tham số chung đi lên dây ĐÚNG tên của PageQuery — `page`, không phải `pageNumber`', () => {
    danhSachTrang<DongDto, Dong>(
      http,
      '/core/users',
      { page: 2, pageSize: 50, sortBy: 'fullName', sortDescending: true, searchText: 'an' },
      sangDong,
    ).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/core/users');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('sortBy')).toBe('fullName');
    expect(req.request.params.get('sortDescending')).toBe('true');
    expect(req.request.params.get('searchText')).toBe('an');
    req.flush({ success: true, data: TRANG_RONG, error: null, traceId: 't' });
  });

  it('`filters` được TRẢI thành tham số rời theo từng khoá — không có tham số tên `filters`', () => {
    danhSachTrang<DongDto, Dong>(
      http,
      '/core/users',
      { page: 1, pageSize: 20, filters: { status: 'Locked', roleId: 'r-1' } },
      sangDong,
    ).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/core/users');
    expect(req.request.params.get('status'))
      .withContext('bộ lọc mất thì lưới hiện dữ liệu CHƯA lọc trong khi chip lọc vẫn hiện')
      .toBe('Locked');
    expect(req.request.params.get('roleId')).toBe('r-1');
    expect(req.request.params.has('filters')).toBeFalse();
    req.flush({ success: true, data: TRANG_RONG, error: null, traceId: 't' });
  });

  it("bỏ qua giá trị `''` và `undefined` — ở cả tham số chung lẫn trong `filters`", () => {
    danhSachTrang<DongDto, Dong>(
      http,
      '/core/users',
      {
        page: 1,
        pageSize: 20,
        sortBy: undefined,
        searchText: '',
        filters: { status: '', roleId: 'r-1' },
      },
      sangDong,
    ).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/core/users');
    expect(req.request.params.has('sortBy')).withContext('undefined').toBeFalse();
    expect(req.request.params.has('searchText')).withContext("'' ở tham số chung").toBeFalse();
    expect(req.request.params.has('status')).withContext("'' trong filters").toBeFalse();
    expect(req.request.params.get('roleId')).withContext('ô có giá trị vẫn đi').toBe('r-1');
    expect(req.request.params.keys().sort()).toEqual(['page', 'pageSize', 'roleId']);
    req.flush({ success: true, data: TRANG_RONG, error: null, traceId: 't' });
  });

  it('`sortDescending: false` vẫn đi lên dây — chỉ `undefined` và chuỗi rỗng mới bị bỏ', () => {
    danhSachTrang<DongDto, Dong>(
      http,
      '/core/users',
      { page: 1, pageSize: 20, sortBy: 'createdAt', sortDescending: false },
      sangDong,
    ).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/core/users');
    expect(req.request.params.get('sortDescending')).toBe('false');
    req.flush({ success: true, data: TRANG_RONG, error: null, traceId: 't' });
  });

  it('bóc envelope và map TỪNG item qua mapper; giữ page/pageSize/totalCount của máy chủ', () => {
    let ketQua: PagedList<Dong> | undefined;
    danhSachTrang<DongDto, Dong>(
      http,
      '/core/users',
      { page: 3, pageSize: 10 },
      sangDong,
    ).subscribe((t) => (ketQua = t));

    httpMock
      .expectOne((r) => r.url === '/core/users')
      .flush({
        success: true,
        data: {
          items: [{ id: 'a', createdAt: '2026-01-02T03:04:05Z' }],
          page: 3,
          pageSize: 10,
          totalCount: 21,
        },
        error: null,
        traceId: 't',
      });

    expect(ketQua?.items[0].createdAt).toEqual(new Date('2026-01-02T03:04:05Z'));
    expect(ketQua?.page).toBe(3);
    expect(ketQua?.pageSize).toBe(10);
    expect(ketQua?.totalCount).toBe(21);
  });

  it('theoId() gọi `<duongDan>/<id>`, bóc envelope rồi map qua mapper', () => {
    let ketQua: Dong | undefined;
    theoId<DongDto, Dong>(http, '/core/users', 'u-9', sangDong).subscribe((d) => (ketQua = d));

    const req = httpMock.expectOne('/core/users/u-9');
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: { id: 'u-9', createdAt: '2026-05-06T00:00:00Z' },
      error: null,
      traceId: 't',
    });

    expect(ketQua).toEqual({ id: 'u-9', createdAt: new Date('2026-05-06T00:00:00Z') });
  });
});
