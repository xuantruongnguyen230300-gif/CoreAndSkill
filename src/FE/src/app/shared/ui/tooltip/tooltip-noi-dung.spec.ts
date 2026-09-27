import {
  ATTR_MO_TA,
  canhBaoChuaNoiMoTa,
  canhBaoNoiDungSai,
  daNoiMoTa,
  doNoiDungChieuVao,
  laComponent,
} from './tooltip-noi-dung';

/**
 * Luật F26: lớp cộng tác thuần tách khỏi component có `.spec.ts` RIÊNG — test tích hợp của
 * `TooltipComponent` không được tính là đủ. Ở đây không có `TestBed`, không có Angular: mọi ca
 * dựng bằng DOM tay, đúng như lớp này được viết để chạy.
 */
function dungHost(html: string): HTMLElement {
  const host = document.createElement('div');
  host.innerHTML = html;
  return host;
}

describe('doNoiDungChieuVao — §Hình dạng, ràng buộc 1', () => {
  it('đúng MỘT phần tử con → hợp lệ, và phần tử đó là neo', () => {
    const host = dungHost('<button>Lưu</button>');

    const { neo, hopLe } = doNoiDungChieuVao(host);

    expect(hopLe).toBeTrue();
    expect(neo).toBe(host.querySelector('button'));
  });

  it('BỎ QUA phần tử mô tả do chính lớp bọc vẽ — nó là con của host nhưng không phải nội dung chiếu vào', () => {
    // Ca dễ hỏng nhất: template vẽ <span ...> mô tả ngay cạnh <ng-content />, nên khi tooltip đang
    // hiện thì host CÓ hai phần tử con. Đếm cả nó thì mọi tooltip đang hiện tự khai là không hợp lệ.
    const host = dungHost(`<button>Lưu</button><span ${ATTR_MO_TA}>Lưu hồ sơ</span>`);

    const { neo, hopLe } = doNoiDungChieuVao(host);

    expect(hopLe).toBeTrue();
    expect(neo).toBe(host.querySelector('button'));
  });

  it('hai phần tử anh em → không hợp lệ, không neo', () => {
    const host = dungHost('<button>Lưu</button><button>Huỷ</button>');

    expect(doNoiDungChieuVao(host)).toEqual({ neo: null, hopLe: false });
  });

  it('chữ trần cạnh một phần tử → không hợp lệ', () => {
    const host = dungHost('Ghi chú <button>Lưu</button>');

    expect(doNoiDungChieuVao(host)).toEqual({ neo: null, hopLe: false });
  });

  it('chỉ có chữ trần → không hợp lệ', () => {
    const host = dungHost('Chỉ là chữ');

    expect(doNoiDungChieuVao(host)).toEqual({ neo: null, hopLe: false });
  });

  it('host rỗng → không hợp lệ', () => {
    expect(doNoiDungChieuVao(dungHost(''))).toEqual({ neo: null, hopLe: false });
  });

  it('khoảng trắng và xuống dòng KHÔNG tính là chữ trần — prettier bẻ dòng template là chuyện thường', () => {
    const host = dungHost('\n  <button>Lưu</button>\n');

    expect(doNoiDungChieuVao(host).hopLe).toBeTrue();
  });
});

describe('laComponent — dấu hiệu "neo tự dựng control bên trong"', () => {
  it('thẻ HTML gốc → false', () => {
    expect(laComponent(dungHost('<button></button>').firstElementChild as HTMLElement)).toBeFalse();
    expect(
      laComponent(dungHost('<span tabindex="0"></span>').firstElementChild as HTMLElement),
    ).toBeFalse();
  });

  it('selector component (có gạch ngang) → true', () => {
    expect(
      laComponent(dungHost('<app-check></app-check>').firstElementChild as HTMLElement),
    ).toBeTrue();
  });
});

describe('daNoiMoTa', () => {
  it('id nằm trên phần tử BÊN TRONG → đã nối', () => {
    const neo = dungHost('<app-check><input aria-describedby="tip-1" /></app-check>')
      .firstElementChild as HTMLElement;

    expect(daNoiMoTa(neo, 'tip-1')).toBeTrue();
  });

  it('`aria-describedby` mang NHIỀU id → vẫn nhận ra (so theo `~=`, không so cả chuỗi)', () => {
    const neo = dungHost('<app-check><input aria-describedby="loi-1 tip-1" /></app-check>')
      .firstElementChild as HTMLElement;

    expect(daNoiMoTa(neo, 'tip-1')).toBeTrue();
  });

  it('id chỉ nằm trên CHÍNH neo, không nằm bên trong → chưa nối', () => {
    // Đây là ca lớp bọc phải kêu lên: thuộc tính rơi lên host trong khi focus nằm ở <input>.
    const neo = dungHost('<app-check aria-describedby="tip-1"><input /></app-check>')
      .firstElementChild as HTMLElement;

    expect(daNoiMoTa(neo, 'tip-1')).toBeFalse();
  });

  it('id khác → chưa nối', () => {
    const neo = dungHost('<app-check><input aria-describedby="tip-9" /></app-check>')
      .firstElementChild as HTMLElement;

    expect(daNoiMoTa(neo, 'tip-1')).toBeFalse();
  });
});

describe('cảnh báo lúc phát triển — im lặng là cách lời chú biến mất mà không ai biết', () => {
  it('canhBaoNoiDungSai kêu lên và kèm chính host để lần ra chỗ sai', () => {
    const loi = spyOn(console, 'error');
    const host = dungHost('<b>a</b><b>b</b>');

    canhBaoNoiDungSai(host);

    expect(loi).toHaveBeenCalledTimes(1);
    expect(loi.calls.mostRecent().args[0]).toContain('ĐÚNG MỘT phần tử chiếu vào');
    expect(loi.calls.mostRecent().args[1]).toBe(host);
  });

  it('canhBaoChuaNoiMoTa IM LẶNG khi nơi gọi đã nối đúng', () => {
    const canh = spyOn(console, 'warn');
    const neo = dungHost('<app-check><input aria-describedby="tip-1" /></app-check>')
      .firstElementChild as HTMLElement;

    canhBaoChuaNoiMoTa(neo, 'tip-1');

    expect(canh).not.toHaveBeenCalled();
  });

  it('canhBaoChuaNoiMoTa kêu lên khi chưa nối, và nêu ĐÚNG tên thẻ để sửa được ngay', () => {
    const canh = spyOn(console, 'warn');
    const neo = dungHost('<app-check><input /></app-check>').firstElementChild as HTMLElement;

    canhBaoChuaNoiMoTa(neo, 'tip-1');

    expect(canh).toHaveBeenCalledTimes(1);
    const cau = canh.calls.mostRecent().args[0] as string;
    expect(cau).toContain('app-check');
    expect(cau).toContain('describedBy');
    expect(canh.calls.mostRecent().args[1]).toBe(neo);
  });
});
