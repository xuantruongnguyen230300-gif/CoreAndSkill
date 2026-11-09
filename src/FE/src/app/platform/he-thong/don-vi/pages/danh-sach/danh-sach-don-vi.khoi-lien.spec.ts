import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { PagedList } from '../../../../../core/http/paged.model';
import { DonVi } from '../../models/don-vi.model';
import { DonViService } from '../../services/don-vi.service';
import { DanhSachDonViPage } from './danh-sach-don-vi.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const DON_VI: DonVi = {
  id: 'dv-1',
  code: 'ABC',
  name: 'Đơn vị ABC',
  isActive: true,
  createdAt: null,
};

/** Templates/ListScreen.md §2: khối liền, nút chính ở Toolbar; DataTable.md §"Cột hành động". */
describe('DanhSachDonViPage — khối danh sách liền và cột hành động dùng chung', () => {
  let fixture: ComponentFixture<DanhSachDonViPage>;
  let el: HTMLElement;

  beforeEach(async () => {
    const service = jasmine.createSpyObj<DonViService>('DonViService', ['danhSach']);
    service.danhSach.and.returnValue(
      of({ items: [DON_VI], page: 1, pageSize: 20, totalCount: 1 } as PagedList<DonVi>),
    );
    await TestBed.configureTestingModule({
      imports: [DanhSachDonViPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: DonViService, useValue: service },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DanhSachDonViPage);
    el = fixture.nativeElement as HTMLElement;
    document.body.appendChild(el);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  });

  afterEach(() => el.remove());

  it('PageHeader chỉ tiêu đề + mô tả; Toolbar attached trong chỗ cắm đầu khung; nút chính "Tạo đơn vị" ở Toolbar', () => {
    expect(el.querySelectorAll('app-page-header button, app-page-header a').length).toBe(0);
    expect(el.querySelector('.data-table__dau app-toolbar .toolbar--attached')).not.toBeNull();
    const nut = el.querySelector('.toolbar__hanh-dong app-button button')!;
    expect(nut.textContent).toContain('donVi.hanhDong.tao');
  });

  it('cột hành động là rowActionsTemplate: th ẩn thị giác + nút ⋯ aria-haspopup="menu", mỗi dòng một ô', () => {
    expect(el.querySelector('thead th.data-table__o-hanh-dong .sr-only')?.textContent).toContain(
      'donVi.cot.hanhDong',
    );
    const nut = el.querySelector<HTMLButtonElement>('tbody td.data-table__o-hanh-dong button')!;
    expect(nut.getAttribute('aria-haspopup')).toBe('menu');
    expect(nut.getAttribute('aria-expanded')).toBe('false');
    expect(nut.getAttribute('aria-label')).toBe('donVi.menu.moMenu');
  });
});
