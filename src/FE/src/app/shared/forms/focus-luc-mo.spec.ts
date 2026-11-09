import { FormControl } from '@angular/forms';

import { datFocusLucMo, roiOLaCham } from './focus-luc-mo';

describe('focus-luc-mo (fe-ui-conventions.md §6.5)', () => {
  let o: HTMLInputElement;

  beforeEach(() => {
    o = document.createElement('input');
    document.body.append(o);
  });
  afterEach(() => o.remove());

  describe('roiOLaCham', () => {
    it('có cờ, control chưa sửa → false', () => {
      o.setAttribute('data-focus-luc-mo', '');
      expect(roiOLaCham(o, new FormControl(''))).toBeFalse();
    });

    it('có cờ, control đã sửa (dirty) → true', () => {
      o.setAttribute('data-focus-luc-mo', '');
      const c = new FormControl('');
      c.markAsDirty();
      expect(roiOLaCham(o, c)).toBeTrue();
    });

    it('không cờ → true, dù control chưa sửa', () => {
      expect(roiOLaCham(o, new FormControl(''))).toBeTrue();
    });

    it('không có control (ô không gắn control) → theo cờ', () => {
      expect(roiOLaCham(o, null)).toBeTrue();
      o.setAttribute('data-focus-luc-mo', '');
      expect(roiOLaCham(o, null)).toBeFalse();
    });
  });

  describe('datFocusLucMo', () => {
    it('gắn cờ TRƯỚC khi phần tử nhận focus', () => {
      let coKhiNhanFocus: boolean | null = null;
      o.addEventListener('focus', () => {
        coKhiNhanFocus = o.hasAttribute('data-focus-luc-mo');
      });
      datFocusLucMo(o);
      expect(coKhiNhanFocus).toBeTrue();
      expect(document.activeElement).toBe(o);
    });

    it('chuyển tuỳ chọn cho focus()', () => {
      const spy = spyOn(o, 'focus').and.callThrough();
      datFocusLucMo(o, { preventScroll: true });
      expect(spy).toHaveBeenCalledOnceWith({ preventScroll: true });
    });
  });
});
