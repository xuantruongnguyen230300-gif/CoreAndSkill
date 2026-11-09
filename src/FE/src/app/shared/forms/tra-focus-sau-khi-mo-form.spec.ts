import { phanTuDangFocus, traFocus } from './tra-focus-sau-khi-mo-form';

describe('traFocus — focus sau khi gửi hỏng (AuthCard.md §Accessibility)', () => {
  let goc: HTMLElement;
  let o1: HTMLInputElement;
  let o2: HTMLInputElement;

  beforeEach(() => {
    goc = document.createElement('form');
    o1 = document.createElement('input');
    o2 = document.createElement('input');
    goc.append(o1, o2);
    document.body.append(goc);
  });
  afterEach(() => goc.remove());

  it('nhánh 1: phần tử cũ còn nhận focus → focus về đúng nó, không gọi phương án', () => {
    o2.focus();
    const truoc = phanTuDangFocus(goc);
    expect(truoc).toBe(o2);
    o2.disabled = true; // lúc khoá
    o2.disabled = false; // sau khi mở
    const phuongAn = jasmine.createSpy('phuongAn');
    traFocus(truoc, phuongAn);
    expect(document.activeElement).toBe(o2);
    expect(phuongAn).not.toHaveBeenCalled();
  });

  it('nhánh 2: phần tử cũ không còn trong DOM → phương án (ô lỗi đầu / ô đầu form)', () => {
    o2.focus();
    const truoc = phanTuDangFocus(goc);
    o2.remove();
    traFocus(truoc, () => o1.focus());
    expect(document.activeElement).toBe(o1);
  });

  it('nhánh 3: phần tử cũ còn disabled → phương án', () => {
    o2.focus();
    const truoc = phanTuDangFocus(goc);
    o2.disabled = true;
    const phuongAn = jasmine.createSpy('phuongAn').and.callFake(() => o1.focus());
    traFocus(truoc, phuongAn);
    expect(phuongAn).toHaveBeenCalledTimes(1);
    expect(document.activeElement).toBe(o1);
  });

  it('không có phần tử nào giữ focus lúc khoá (null) → phương án', () => {
    (document.activeElement as HTMLElement | null)?.blur();
    expect(phanTuDangFocus(goc)).toBeNull();
    const phuongAn = jasmine.createSpy('phuongAn');
    traFocus(null, phuongAn);
    expect(phuongAn).toHaveBeenCalledTimes(1);
  });

  it('focus nằm ngoài vùng bọc → phanTuDangFocus trả null', () => {
    const ngoai = document.createElement('input');
    document.body.append(ngoai);
    ngoai.focus();
    expect(phanTuDangFocus(goc)).toBeNull();
    ngoai.remove();
  });
});
