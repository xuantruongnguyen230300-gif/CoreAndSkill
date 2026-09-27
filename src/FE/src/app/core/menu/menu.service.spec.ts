import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { BO_QUA_TOAST_LOI } from '../http/api-result.model';
import { MenuItemDto } from './menu.dto';
import { MenuNode } from './menu.model';
import { MenuService } from './menu.service';

describe('MenuService', () => {
  let service: MenuService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(MenuService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('layMenu() gọi ĐÚNG đường dẫn ngắn và bóc data ra khỏi envelope', () => {
    let ketQua: unknown;
    service.layMenu().subscribe((d) => (ketQua = d));

    const req = httpMock.expectOne('/core/meta/menu');
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [], error: null, traceId: 't-1' });

    expect(ketQua).toEqual([]);
  });

  // fe-api-client.md §4.1 (luật F10): service là nơi map DTO → model — store chỉ nhận cây MenuNode.
  it('layMenu() trả CÂY MenuNode đã map, không phải danh sách phẳng của dây', () => {
    let ketQua: readonly MenuNode[] | undefined;
    service.layMenu().subscribe((d) => (ketQua = d));

    const phang: readonly MenuItemDto[] = [
      {
        id: 'p',
        parentId: null,
        code: 'quan-tri',
        labelKey: 'menu.quan-tri',
        icon: 'pi-cog',
        route: null,
        displayOrder: 90,
      },
      {
        id: 'c',
        parentId: 'p',
        code: 'quan-tri-nguoi-dung',
        labelKey: 'menu.quan-tri-nguoi-dung',
        icon: 'pi-users',
        route: '/quan-tri/nguoi-dung',
        displayOrder: 10,
      },
    ];
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: phang, error: null, traceId: 't-1' });

    expect(ketQua?.length).withContext('mục con vào children, không đứng ở gốc').toBe(1);
    expect(ketQua?.[0].children.map((n) => n.code)).toEqual(['quan-tri-nguoi-dung']);
    const khoa = Object.keys(ketQua?.[0] ?? {});
    expect(khoa).withContext('hình dạng dây không lọt ra model').not.toContain('parentId');
    expect(khoa).not.toContain('displayOrder');
  });

  it('layMenu() tắt toast chung — Sidebar tự hiện lỗi kèm Thử lại (Design/Screens/00, "Tải menu hỏng")', () => {
    service.layMenu().subscribe({ error: () => undefined });

    const req = httpMock.expectOne('/core/meta/menu');
    expect(req.request.context.get(BO_QUA_TOAST_LOI)).toBeTrue();
    req.flush({ success: true, data: [], error: null, traceId: 't-1' });
  });
});
