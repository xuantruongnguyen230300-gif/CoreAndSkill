import { HttpHeaders, HttpResponse } from '@angular/common/http';

import { kichHoatTai, luuTepXuat, tenTepTuContentDisposition, tenTepTuPhanHoi } from './tai-tep';

/** Phản hồi blob giả lập — chỉ cần header và thân, không cần HttpClient thật. */
function phanHoi(contentDisposition: string | null, than: Blob | null = new Blob(['x'])) {
  const headers =
    contentDisposition === null
      ? new HttpHeaders()
      : new HttpHeaders({ 'Content-Disposition': contentDisposition });
  return new HttpResponse<Blob>({ body: than, headers, status: 200, statusText: 'OK' });
}

describe('tenTepTuContentDisposition', () => {
  it('đọc filename* trước, và giải mã percent-encoding ra tên có dấu tiếng Việt', () => {
    // BE đặt tên tệp theo tên màn: "Danh sách người dùng.xlsx" → filename* mang bản UTF-8 đã mã hoá,
    // filename mang bản ASCII bỏ dấu cho client cũ (fe-api-client.md §6.4 luật 3).
    const header =
      'attachment; filename="Danh sach nguoi dung.xlsx"; ' +
      "filename*=UTF-8''Danh%20s%C3%A1ch%20ng%C6%B0%E1%BB%9Di%20d%C3%B9ng.xlsx";

    expect(tenTepTuContentDisposition(header)).toBe('Danh sách người dùng.xlsx');
  });

  it('filename* đứng TRƯỚC filename trong header vẫn thắng', () => {
    const header = 'attachment; filename*=UTF-8\'\'b%C3%A1o%20c%C3%A1o.csv; filename="bao cao.csv"';

    expect(tenTepTuContentDisposition(header)).toBe('báo cáo.csv');
  });

  it('không có filename* thì đọc filename trong nháy kép', () => {
    expect(tenTepTuContentDisposition('attachment; filename="users.csv"')).toBe('users.csv');
  });

  it('filename không nháy vẫn đọc được, bỏ khoảng trắng thừa', () => {
    expect(tenTepTuContentDisposition('attachment; filename=users.csv ')).toBe('users.csv');
  });

  it('header không mang filename nào → null', () => {
    expect(tenTepTuContentDisposition('attachment')).toBeNull();
  });
});

describe('tenTepTuPhanHoi', () => {
  it('thiếu hẳn header Content-Disposition → NÉM LỖI, không đặt tên dự phòng', () => {
    // Header đọc ra null nghĩa là BE chưa khai nó vào Access-Control-Expose-Headers
    // (be-api-controller.md §7.3). Đặt tên dự phòng là giấu đúng lỗi đó (fe-api-client.md §6.4 luật 3).
    expect(() => tenTepTuPhanHoi(phanHoi(null))).toThrowError(/Access-Control-Expose-Headers/);
  });

  it('có header nhưng không đọc được tên tệp → NÉM LỖI', () => {
    expect(() => tenTepTuPhanHoi(phanHoi('attachment'))).toThrowError(/Content-Disposition/);
  });

  it('header hợp lệ → trả đúng tên tệp', () => {
    expect(tenTepTuPhanHoi(phanHoi('attachment; filename="users.csv"'))).toBe('users.csv');
  });
});

describe('kichHoatTai', () => {
  const URL_GIA = 'blob:http://localhost/9f3a-tep-xuat';

  it('dựng object URL, click thẻ <a download> đang nằm trong DOM, rồi thu hồi URL', () => {
    let tenLucClick = '';
    let hrefLucClick = '';
    let trongDomLucClick = false;
    const clickSpy = spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (
      this: HTMLAnchorElement,
    ) {
      tenLucClick = this.download;
      hrefLucClick = this.href;
      trongDomLucClick = document.body.contains(this);
    });
    const taoSpy = spyOn(URL, 'createObjectURL').and.returnValue(URL_GIA);
    let clickTruocRevoke = false;
    const thuHoiSpy = spyOn(URL, 'revokeObjectURL').and.callFake(() => {
      clickTruocRevoke = clickSpy.calls.count() === 1;
    });
    const blob = new Blob(['ma,ten\n1,a\n'], { type: 'text/csv' });

    kichHoatTai(blob, 'người dùng.csv');

    expect(taoSpy).toHaveBeenCalledOnceWith(blob);
    expect(tenLucClick).toBe('người dùng.csv');
    expect(hrefLucClick).toBe(URL_GIA);
    expect(trongDomLucClick)
      .withContext('thẻ <a> phải nằm trong DOM lúc click — Firefox bỏ qua click của thẻ rời')
      .toBeTrue();
    expect(clickTruocRevoke).withContext('revokeObjectURL phải chạy SAU click').toBeTrue();
    expect(thuHoiSpy).toHaveBeenCalledOnceWith(URL_GIA);
  });

  it('không để lại thẻ <a> nào trong DOM sau khi tải', () => {
    spyOn(HTMLAnchorElement.prototype, 'click');
    spyOn(URL, 'createObjectURL').and.returnValue(URL_GIA);
    spyOn(URL, 'revokeObjectURL');

    kichHoatTai(new Blob(['x']), 'a.csv');

    expect(document.body.querySelector('a[download]')).toBeNull();
  });

  it('click ném lỗi thì URL vẫn được thu hồi — không rò bộ nhớ đúng bằng cỡ tệp', () => {
    spyOn(HTMLAnchorElement.prototype, 'click').and.throwError('click hỏng');
    spyOn(URL, 'createObjectURL').and.returnValue(URL_GIA);
    const thuHoiSpy = spyOn(URL, 'revokeObjectURL');

    expect(() => kichHoatTai(new Blob(['x']), 'a.csv')).toThrow();

    expect(thuHoiSpy).toHaveBeenCalledOnceWith(URL_GIA);
    expect(document.body.querySelector('a[download]')).toBeNull();
  });
});

describe('luuTepXuat', () => {
  it('lấy tên từ Content-Disposition rồi kích hoạt tải', () => {
    let tenLucClick = '';
    spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (this: HTMLAnchorElement) {
      tenLucClick = this.download;
    });
    spyOn(URL, 'createObjectURL').and.returnValue('blob:http://localhost/x');
    spyOn(URL, 'revokeObjectURL');

    luuTepXuat(phanHoi("attachment; filename*=UTF-8''b%C3%A1o%20c%C3%A1o.csv"));

    expect(tenLucClick).toBe('báo cáo.csv');
  });

  it('thân phản hồi rỗng → NÉM LỖI, không tải tệp 0 byte', () => {
    const taoSpy = spyOn(URL, 'createObjectURL');

    expect(() => luuTepXuat(phanHoi('attachment; filename="a.csv"', null))).toThrowError(
      /thân phản hồi/,
    );
    expect(taoSpy).not.toHaveBeenCalled();
  });
});
