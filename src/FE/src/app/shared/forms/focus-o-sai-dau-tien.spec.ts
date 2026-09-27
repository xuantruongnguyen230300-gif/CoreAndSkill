import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { focusOSaiDauTien, focusOSaiKhiGuiSai } from './focus-o-sai-dau-tien';

describe('focus ô sai đầu tiên (09-forms-validation.md §5)', () => {
  let goc: HTMLElement;

  beforeEach(() => {
    goc = document.createElement('form');
    goc.innerHTML = `
      <input id="a" />
      <input id="b" aria-invalid="true" />
      <input id="c" aria-invalid="true" />`;
    document.body.appendChild(goc);
  });

  afterEach(() => goc.remove());

  describe('focusOSaiDauTien()', () => {
    it('đưa focus về ô sai ĐẦU TIÊN theo thứ tự trên trang, trả true', () => {
      expect(focusOSaiDauTien(goc)).toBeTrue();
      expect(document.activeElement?.id).toBe('b');
    });

    it('không có ô nào sai → không đổi focus, trả false', () => {
      goc.querySelectorAll('[aria-invalid]').forEach((e) => e.removeAttribute('aria-invalid'));
      (goc.querySelector('#a') as HTMLElement).focus();

      expect(focusOSaiDauTien(goc)).toBeFalse();
      expect(document.activeElement?.id).toBe('a');
    });

    it('gốc null/undefined (chưa dựng xong) → false, không ném lỗi', () => {
      expect(focusOSaiDauTien(null)).toBeFalse();
      expect(focusOSaiDauTien(undefined)).toBeFalse();
    });
  });

  describe('focusOSaiKhiGuiSai()', () => {
    beforeEach(() => {
      TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    });

    it('mỗi lần bộ đếm tăng → focus về ô sai đầu tiên; lúc khởi tạo (0) thì KHÔNG đổi focus', () => {
      const lan = signal(0);
      (goc.querySelector('#a') as HTMLElement).focus();
      TestBed.runInInjectionContext(() => focusOSaiKhiGuiSai(lan, () => goc));

      TestBed.tick();
      expect(document.activeElement?.id).toBe('a');

      lan.set(1);
      TestBed.tick();
      expect(document.activeElement?.id).toBe('b');

      // Người dùng sửa ô b, ô c vẫn sai — bấm gửi lần nữa phải đưa focus lại về ô sai đầu tiên còn lại.
      (goc.querySelector('#b') as HTMLElement).removeAttribute('aria-invalid');
      lan.set(2);
      TestBed.tick();
      expect(document.activeElement?.id).toBe('c');
    });
  });
});
