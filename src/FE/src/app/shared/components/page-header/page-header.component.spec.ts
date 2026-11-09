import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { PageHeaderComponent } from './page-header.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

@Component({
  standalone: true,
  imports: [PageHeaderComponent],
  template: `<app-page-header title="Tên" backRoute=".." [loading]="true" />`,
})
class HostComponent {}

describe('PageHeaderComponent — loading', () => {
  it('skeleton tiêu đề phải thấy được (bề rộng > 0), không co về 0px trong hàng flex', () => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    });
    const fixture = TestBed.createComponent(HostComponent);
    document.body.appendChild(fixture.nativeElement as HTMLElement);
    fixture.detectChanges();
    const sk = (fixture.nativeElement as HTMLElement).querySelector(
      '.skeleton-loader',
    ) as HTMLElement;
    expect(sk.getBoundingClientRect().width).toBeGreaterThan(0);
    expect(sk.getBoundingClientRect().height).toBeGreaterThan(0);
    (fixture.nativeElement as HTMLElement).remove();
  });
});

@Component({
  standalone: true,
  imports: [PageHeaderComponent],
  template: `<div style="width: 200px">
    <app-page-header
      title="Một tiêu đề rất rất dài để ép hàng tiêu đề co lại trên màn hẹp"
      backRoute=".."
      backLabel="Quay lại"
    >
      <button type="button">Hành động</button>
    </app-page-header>
  </div>`,
})
class HostQuayLaiComponent {}

describe('PageHeaderComponent — vùng chạm nút quay lại (WCAG 2.2 SC 2.5.8)', () => {
  it('rộng cao >= 24px và không bị nhóm hành động đè khi khung hẹp', () => {
    TestBed.configureTestingModule({
      imports: [HostQuayLaiComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    });
    const fixture = TestBed.createComponent(HostQuayLaiComponent);
    // `elementFromPoint` chỉ trả phần tử TRONG khung nhìn (ngoài khung là `null`). Nối thẳng vào <body>
    // thì vị trí của nút phụ thuộc phần đứng trước nó: bộ báo cáo HTML của Jasmine thêm một chấm cho
    // mỗi ca đã chạy nên càng chạy muộn, nút càng bị đẩy xuống dưới khung nhìn của khung karma — ca
    // đỏ chập chờn theo thứ tự ngẫu nhiên. Ghim host vào góc khung nhìn để phép đo không đổi theo thứ tự.
    const host = fixture.nativeElement as HTMLElement;
    host.style.position = 'fixed';
    host.style.top = '0';
    host.style.left = '0';
    document.body.appendChild(host);
    fixture.detectChanges();
    const a = (fixture.nativeElement as HTMLElement).querySelector(
      '.page-header__quay-lai',
    ) as HTMLElement;
    const r = a.getBoundingClientRect();
    expect(r.width).toBeGreaterThanOrEqual(24);
    expect(r.height).toBeGreaterThanOrEqual(24);
    for (const x of [r.left + 1, r.right - 1]) {
      const tren = document.elementFromPoint(x, r.top + r.height / 2);
      expect(tren === a || a.contains(tren))
        .withContext(`bị che tại x=${x}`)
        .toBeTrue();
    }
    (fixture.nativeElement as HTMLElement).remove();
  });
});
