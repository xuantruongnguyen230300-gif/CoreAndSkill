import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { PagedList } from '../../../../../core/http/paged.model';
import { VaiTro } from '../../models/vai-tro.model';
import { VaiTroService } from '../../services/vai-tro.service';
import { DanhSachVaiTroPage } from './danh-sach-vai-tro.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const VAI_TRO: VaiTro = {
  id: 'vt-1',
  name: 'Kế toán',
  isSystem: false,
  userCount: 0,
  createdAt: null,
  version: 'v1',
};

/** Templates/ListScreen.md §2: khối liền, nút của màn ở Toolbar; DataTable.md §"Cột hành động". */
describe('DanhSachVaiTroPage — khối danh sách liền và cột hành động dùng chung', () => {
  let fixture: ComponentFixture<DanhSachVaiTroPage>;
  let el: HTMLElement;

  async function dung(quyen: readonly string[]): Promise<void> {
    const service = jasmine.createSpyObj<VaiTroService>('VaiTroService', ['danhSach']);
    service.danhSach.and.returnValue(
      of({ items: [VAI_TRO], page: 1, pageSize: 20, totalCount: 1 } as PagedList<VaiTro>),
    );
    await TestBed.configureTestingModule({
      imports: [DanhSachVaiTroPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: VaiTroService, useValue: service },
        {
          provide: AuthService,
          useValue: {
            nguoiDung: signal({ id: 'x', tenantName: 'Cty A' }),
            coQuyen: (ma: string) => quyen.includes(ma),
          },
        },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DanhSachVaiTroPage);
    el = fixture.nativeElement as HTMLElement;
    document.body.appendChild(el);
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
  }

  afterEach(() => el.remove());

  it('PageHeader chỉ tiêu đề + mô tả; Toolbar attached trong chỗ cắm đầu khung', async () => {
    await dung(['core.role.write', 'core.permission.read']);
    expect(el.querySelectorAll('app-page-header button, app-page-header a').length).toBe(0);
    expect(el.querySelector('.data-table__dau app-toolbar .toolbar--attached')).not.toBeNull();
  });

  it('liên kết "Ma trận phân quyền" đứng TRƯỚC nút chính trong nhóm hành động; nút chính cuối', async () => {
    await dung(['core.role.write', 'core.permission.read']);
    const nhom = el.querySelector('.toolbar__hanh-dong')!;
    const muc = Array.from(nhom.children);
    expect(muc.length).toBe(2);
    expect(muc[0].tagName).toBe('A');
    expect(muc[0].getAttribute('href')).toContain('/quan-tri/phan-quyen');
    expect(muc[1].querySelector('.pi-plus')).not.toBeNull();
  });

  it('có core.role.write → cột hành động: th ẩn thị giác + nút ⋯ aria-haspopup="menu" (không còn cột thường "hanhDong")', async () => {
    await dung(['core.role.write']);
    expect(el.querySelector('thead th.data-table__o-hanh-dong .sr-only')?.textContent).toContain(
      'vaiTro.cot.hanhDong',
    );
    const nut = el.querySelector<HTMLButtonElement>('tbody td.data-table__o-hanh-dong button')!;
    expect(nut.getAttribute('aria-haspopup')).toBe('menu');
    expect(nut.getAttribute('aria-expanded')).toBe('false');
    expect(nut.querySelector('.pi-ellipsis-v')).not.toBeNull();
  });

  it('không có core.role.write → không cột hành động, kể cả th; không nút "Thêm"', async () => {
    await dung(['core.role.read']);
    expect(el.querySelectorAll('.data-table__o-hanh-dong').length).toBe(0);
    expect(el.querySelectorAll('.toolbar__hanh-dong app-button').length).toBe(0);
  });
});
