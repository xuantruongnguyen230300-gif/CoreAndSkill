import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AutocompleteComponent } from '../../ui/autocomplete/autocomplete.component';
import { FilterField, FilterPanelComponent, FilterSearchState } from './filter-panel.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({ chung: { loc: { apDung: 'Áp dụng', xoaHet: 'Xoá hết' }, thuLai: 'Thử lại' } });
  }
}

const TRUONG: readonly FilterField[] = [
  {
    key: 'roleId',
    label: 'Vai trò',
    kind: 'autocomplete',
    placeholder: 'Gõ tên vai trò',
  },
  {
    key: 'status',
    label: 'Trạng thái',
    kind: 'select',
    options: [
      { value: '', label: 'Tất cả' },
      { value: 'active', label: 'Hoạt động' },
      { value: 'locked', label: 'Đã khoá' },
    ],
  },
];

@Component({
  standalone: true,
  imports: [FilterPanelComponent],
  template: `
    <main><button id="nut-loc" type="button" (click)="open.set(true)">Bộ lọc</button></main>
    <app-filter-panel
      title="Lọc người dùng"
      panelId="bang-loc"
      [open]="open()"
      [fields]="fields()"
      [value]="value()"
      [disabled]="disabled()"
      [searchStates]="searchStates()"
      [selectedLabels]="selectedLabels()"
      (applied)="daAp.push($event); value.set($event)"
      (cleared)="soLanXoa = soLanXoa + 1; value.set({})"
      (closed)="soLanDong = soLanDong + 1; open.set(false)"
      (fieldSearch)="tim.push($event)"
    />
  `,
})
class HostComponent {
  readonly open = signal(false);
  readonly fields = signal<readonly FilterField[]>(TRUONG);
  readonly value = signal<Record<string, unknown>>({});
  readonly disabled = signal(false);
  readonly searchStates = signal<Record<string, FilterSearchState>>({});
  readonly selectedLabels = signal<Record<string, string>>({});
  daAp: Record<string, unknown>[] = [];
  tim: { key: string; text: string }[] = [];
  soLanXoa = 0;
  soLanDong = 0;
}

describe('FilterPanelComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  /**
   * Chờ điều kiện đúng. PrimeNG 21 thoát phần tử bằng hoạt ảnh CSS thật (lớp `p-leave-to`, gỡ khỏi
   * DOM khi `transitionend`), không còn gỡ đồng bộ như bản 20 — nên không thể đo ngay sau `onDinh`.
   */
  async function choDen(dieuKien: () => boolean, toiDaMs = 2000): Promise<void> {
    const batDau = Date.now();
    while (!dieuKien() && Date.now() - batDau < toiDaMs) {
      await new Promise((xong) => setTimeout(xong, 20));
      fixture.detectChanges();
    }
  }

  async function onDinh(): Promise<void> {
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.tick();
    fixture.detectChanges();
    await new Promise((r) => setTimeout(r));
  }

  function trong<T extends HTMLElement>(chon: string): T | null {
    return document.body.querySelector<T>(`#bang-loc ${chon}`);
  }
  function chanDrawer<T extends HTMLElement>(chon: string): T | null {
    return document.body.querySelector<T>(`#bang-loc .p-drawer-footer ${chon}`);
  }
  const nutApDung = (): HTMLButtonElement =>
    document.body.querySelector<HTMLButtonElement>('button[type=submit][form="bang-loc-form"]')!;
  const nutXoaHet = (): HTMLButtonElement =>
    chanDrawer<HTMLButtonElement>('app-button[variant=ghost] button, button.nut--ghost')!;
  const oTrangThai = (): HTMLSelectElement => trong<HTMLSelectElement>('select')!;
  const oVaiTro = (): HTMLInputElement => trong<HTMLInputElement>('app-autocomplete input')!;
  const autocomplete = (): AutocompleteComponent =>
    fixture.debugElement.query(By.directive(AutocompleteComponent))
      .componentInstance as AutocompleteComponent;

  async function mo(): Promise<void> {
    const nut = document.getElementById('nut-loc')!;
    nut.focus();
    nut.click();
    await onDinh();
  }

  async function chonTrangThai(gt: string): Promise<void> {
    oTrangThai().value = gt;
    oTrangThai().dispatchEvent(new Event('change'));
    await onDinh();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    await onDinh();
  });

  afterEach(() => {
    fixture.destroy();
    document.querySelectorAll('.p-drawer-mask').forEach((m) => m.remove());
  });

  it('mở: Drawer là hộp thoại có tên "Lọc người dùng", form role="search", mỗi trường có <label> thật', async () => {
    await mo();
    const g = document.getElementById('bang-loc')!;
    expect(g.getAttribute('role')).toBe('dialog');
    expect(g.getAttribute('aria-modal')).toBe('true');
    expect(g.classList).toContain('app-drawer--lg');
    expect(trong('form')?.getAttribute('role')).toBe('search');
    const nhan = Array.from(g.querySelectorAll('label')).map((l) => l.textContent?.trim());
    expect(nhan).toEqual(['Vai trò', 'Trạng thái']);
    const forId = g.querySelector('label')!.getAttribute('for')!;
    expect(document.getElementById(forId)).not.toBeNull();
  });

  it('mở: focus vào TRƯỜNG ĐẦU TIÊN, không vào nút đóng', async () => {
    await mo();
    expect(document.activeElement?.id).toBe('bang-loc-truong-roleId');
  });

  it('thứ tự Tab: trường → … → Xoá hết → Áp dụng (Áp dụng đứng cuối)', async () => {
    await mo();
    const g = document.getElementById('bang-loc')!;
    const thuTu = Array.from(
      g.querySelectorAll<HTMLElement>('input, select, button:not([data-p-hidden-focusable])'),
    ).map((e) => e.id || e.textContent?.trim() || e.tagName);
    const chiSo = (s: string): number => thuTu.findIndex((t) => t.includes(s));
    expect(chiSo('bang-loc-truong-roleId')).toBeLessThan(chiSo('bang-loc-truong-status'));
    expect(chiSo('bang-loc-truong-status')).toBeLessThan(chiSo('Xoá hết'));
    expect(chiSo('Xoá hết')).toBeLessThan(chiSo('Áp dụng'));
  });

  describe('Áp dụng', () => {
    it('khoá khi chưa có gì đổi so với value; mở khoá khi đổi một trường', async () => {
      await mo();
      expect(nutApDung().disabled).toBeTrue();
      await chonTrangThai('active');
      expect(nutApDung().disabled).toBeFalse();
      await chonTrangThai('');
      expect(nutApDung().disabled).toBeTrue();
    });

    it('KHÔNG phát applied ở mỗi lần đổi một trường', async () => {
      await mo();
      await chonTrangThai('active');
      await chonTrangThai('locked');
      expect(host.daAp.length).toBe(0);
    });

    it('bấm Áp dụng → applied mang giá trị MỌI trường (trống là null), rồi closed; panel đóng', async () => {
      await mo();
      await chonTrangThai('locked');
      nutApDung().click();
      await onDinh();
      expect(host.daAp).toEqual([{ roleId: null, status: 'locked' }]);
      expect(host.soLanDong).toBe(1);
      await choDen(() => document.getElementById('bang-loc') === null);
      expect(document.getElementById('bang-loc')).toBeNull();
      expect(document.activeElement).toBe(document.getElementById('nut-loc'));
    });

    it('Enter trong một trường (gửi form) cũng áp bộ lọc', async () => {
      await mo();
      await chonTrangThai('active');
      trong<HTMLFormElement>('form')!.requestSubmit(nutApDung());
      await onDinh();
      expect(host.daAp).toEqual([{ roleId: null, status: 'active' }]);
    });

    it('form gửi khi chưa có gì đổi thì không phát applied', async () => {
      await mo();
      trong<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
      await onDinh();
      expect(host.daAp.length).toBe(0);
    });
  });

  describe('Xoá hết', () => {
    beforeEach(async () => {
      host.value.set({ roleId: 'r1', status: 'locked' });
      host.selectedLabels.set({ roleId: 'Kế toán' });
      await onDinh();
      await mo();
    });

    it('phát cleared, đưa mọi trường về trống, panel GIỮ MỞ, focus về trường đầu, nút tự khoá', async () => {
      expect(oTrangThai().value).toBe('locked');
      nutXoaHet().click();
      await onDinh();
      expect(host.soLanXoa).toBe(1);
      expect(host.soLanDong).toBe(0);
      expect(document.getElementById('bang-loc')).not.toBeNull();
      expect(oTrangThai().value).toBe('');
      expect(oVaiTro().value).toBe('');
      expect(document.activeElement?.id).toBe('bang-loc-truong-roleId');
      expect(nutXoaHet().disabled).toBeTrue();
    });
  });

  describe('đóng mà chưa áp', () => {
    it('nút đóng: phát closed, KHÔNG hỏi xác nhận, bỏ phần chưa áp — lần mở sau hiện lại đúng value', async () => {
      host.value.set({ status: 'active' });
      await onDinh();
      await mo();
      await chonTrangThai('locked');
      document.body.querySelector<HTMLButtonElement>('.p-drawer-close-button button')!.click();
      await onDinh();
      expect(host.soLanDong).toBe(1);
      expect(host.daAp.length).toBe(0);
      await choDen(() => document.getElementById('bang-loc') === null);
      expect(document.getElementById('bang-loc')).toBeNull();
      expect(document.activeElement).toBe(document.getElementById('nut-loc'));

      await mo();
      expect(oTrangThai().value).toBe('active');
    });
  });

  describe('trường autocomplete', () => {
    it('vẽ bằng Autocomplete single cỡ md, placeholder từ FilterField', async () => {
      await mo();
      expect(oVaiTro().getAttribute('placeholder')).toBe('Gõ tên vai trò');
      expect(oVaiTro().classList).toContain('app-autocomplete__o--md');
    });

    it('gõ tìm → fieldSearch {key, text}', async () => {
      await mo();
      autocomplete().search.emit('kế');
      expect(host.tim).toEqual([{ key: 'roleId', text: 'kế' }]);
    });

    it('chọn một mục trong searchStates rồi Áp dụng → applied mang KHOÁ của mục', async () => {
      host.searchStates.set({
        roleId: { options: [{ key: 'r-ke-toan', label: 'Kế toán' }], loading: false, error: null },
      });
      await onDinh();
      await mo();
      autocomplete().valueChange.emit('r-ke-toan');
      await onDinh();
      expect(oVaiTro().value).toBe('Kế toán');
      nutApDung().click();
      await onDinh();
      expect(host.daAp).toEqual([{ roleId: 'r-ke-toan', status: null }]);
    });

    it('value có khoá và selectedLabels có nhãn → ô hiện TÊN, không hiện khoá', async () => {
      host.value.set({ roleId: 'r-ke-toan' });
      host.selectedLabels.set({ roleId: 'Kế toán' });
      await onDinh();
      await mo();
      expect(oVaiTro().value).toBe('Kế toán');
    });

    it('value có khoá mà CHƯA có nhãn → ô TRỐNG, không hiện khoá thô; có nhãn sau đó thì hiện tên', async () => {
      host.value.set({ roleId: 'r-ke-toan' });
      await onDinh();
      await mo();
      expect(oVaiTro().value).toBe('');

      host.selectedLabels.set({ roleId: 'Kế toán' });
      await onDinh();
      expect(oVaiTro().value).toBe('Kế toán');
    });

    it('lỗi tải: CHỈ trường đó hiện câu lỗi kèm "Thử lại"; "Thử lại" phát lại fieldSearch với chuỗi gần nhất; panel không đóng', async () => {
      await mo();
      autocomplete().search.emit('thu');
      host.tim = [];
      host.searchStates.set({
        roleId: { options: [], loading: false, error: 'Không tìm được vai trò.' },
      });
      await onDinh();
      const g = document.getElementById('bang-loc')!;
      expect(g.textContent).toContain('Không tìm được vai trò.');
      expect(oTrangThai().disabled).toBeFalse();

      const thuLai = Array.from(g.querySelectorAll<HTMLButtonElement>('button')).find((b) =>
        b.textContent?.includes('Thử lại'),
      )!;
      thuLai.click();
      expect(host.tim).toEqual([{ key: 'roleId', text: 'thu' }]);
      expect(host.soLanDong).toBe(0);
      expect(document.getElementById('bang-loc')).not.toBeNull();
    });
  });

  describe('nháp theo form', () => {
    it('value đổi khi panel đang mở → các ô về đúng value mới, phần đang gõ bị bỏ', async () => {
      await mo();
      await chonTrangThai('locked');
      host.value.set({ status: 'active' });
      await onDinh();
      expect(oTrangThai().value).toBe('active');
      expect(nutApDung().disabled).toBeTrue();
    });

    it('fields đổi khi đang mở → ô mới xuất hiện, phần đang gõ ở ô cũ được giữ', async () => {
      await mo();
      await chonTrangThai('locked');
      host.fields.set([...TRUONG, { key: 'q', label: 'Từ khoá', kind: 'text' }]);
      await onDinh();
      expect(oTrangThai().value).toBe('locked');
      expect(trong<HTMLInputElement>('input#bang-loc-truong-q')).not.toBeNull();
    });

    it('disabled bật rồi tắt khi đang mở → ô khoá rồi mở lại', async () => {
      await mo();
      host.disabled.set(true);
      await onDinh();
      expect(oTrangThai().disabled).toBeTrue();
      host.disabled.set(false);
      await onDinh();
      expect(oTrangThai().disabled).toBeFalse();
    });

    it('trường kind chưa dựng: điều kiện đang áp đi qua nguyên vẹn khi áp dụng', async () => {
      host.fields.set([...TRUONG, { key: 'tu', label: 'Từ ngày', kind: 'date' }]);
      host.value.set({ tu: 5 });
      await onDinh();
      await mo();
      await chonTrangThai('active');
      nutApDung().click();
      await onDinh();
      expect(host.daAp).toEqual([{ roleId: null, status: 'active', tu: 5 }]);
    });
  });

  it('disabled: mọi trường khoá thật và nút Áp dụng khoá', async () => {
    host.disabled.set(true);
    await onDinh();
    await mo();
    expect(oTrangThai().disabled).toBeTrue();
    expect(oVaiTro().disabled).toBeTrue();
    expect(nutApDung().disabled).toBeTrue();
  });

  it('kind chưa dựng (date) → không vẽ trường, không làm hỏng các trường còn lại', async () => {
    host.fields.set([...TRUONG, { key: 'tu', label: 'Từ ngày', kind: 'date' }]);
    await onDinh();
    await mo();
    const nhan = Array.from(document.querySelectorAll('#bang-loc label')).map((l) =>
      l.textContent?.trim(),
    );
    expect(nhan).toEqual(['Vai trò', 'Trạng thái']);
  });

  it('trường text: gõ rồi Áp dụng → applied mang chuỗi', async () => {
    host.fields.set([{ key: 'q', label: 'Từ khoá', kind: 'text' }]);
    await onDinh();
    await mo();
    const o = trong<HTMLInputElement>('input')!;
    o.value = 'abc';
    o.dispatchEvent(new Event('input'));
    await onDinh();
    nutApDung().click();
    await onDinh();
    expect(host.daAp).toEqual([{ q: 'abc' }]);
  });
});
