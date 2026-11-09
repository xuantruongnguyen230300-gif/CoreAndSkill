import {
  GIOI_HAN_BAO_CAO,
  NguCanhBaoCao,
  catQueryTrongUrl,
  dauVanTay,
  dungBaoCao,
} from './client-error-report';

const NGU_CANH: NguCanhBaoCao = {
  duongDan: '/phieu/danh-sach',
  traceId: 'c1807b11710f43c1964ec2988f532e56',
  phienBanApp: 'a1b2c3d',
};

describe('dungBaoCao — hình dạng khớp hợp đồng client-errors.md §1', () => {
  it('Error thường → đủ sáu field của hợp đồng', () => {
    const loi = new TypeError("Cannot read properties of undefined (reading 'ten')");

    const baoCao = dungBaoCao(loi, NGU_CANH);

    expect(baoCao.kind).toBe('TypeError');
    expect(baoCao.message).toBe("Cannot read properties of undefined (reading 'ten')");
    expect(baoCao.stack).toContain('TypeError');
    expect(baoCao.duongDan).toBe('/phieu/danh-sach');
    expect(baoCao.traceId).toBe('c1807b11710f43c1964ec2988f532e56');
    expect(baoCao.phienBanApp).toBe('a1b2c3d');
    expect(Object.keys(baoCao).sort()).toEqual(
      ['duongDan', 'kind', 'message', 'phienBanApp', 'stack', 'traceId'].sort(),
    );
  });

  it('không có traceId hay stack → hai field đó vắng hẳn (hợp đồng: không bắt buộc)', () => {
    const loi = new Error('x');
    loi.stack = undefined;

    const baoCao = dungBaoCao(loi, { ...NGU_CANH, traceId: null });

    expect('stack' in baoCao).toBeFalse();
    expect('traceId' in baoCao).toBeFalse();
  });
});

describe('dungBaoCao — mọi field trong giới hạn BE (BE TỪ CHỐI khi vượt, không cắt hộ)', () => {
  it('cắt từng field dài quá giới hạn', () => {
    class LoiTenDai extends Error {
      override name = 'K'.repeat(300);
    }
    const loi = new LoiTenDai('m'.repeat(2000));
    loi.stack = 's'.repeat(9000);

    const baoCao = dungBaoCao(loi, {
      duongDan: '/' + 'd'.repeat(5000),
      traceId: 't'.repeat(300),
      phienBanApp: 'v'.repeat(300),
    });

    expect(baoCao.kind.length).toBe(GIOI_HAN_BAO_CAO.kind);
    expect(baoCao.message.length).toBe(GIOI_HAN_BAO_CAO.message);
    expect(baoCao.stack?.length).toBe(GIOI_HAN_BAO_CAO.stack);
    expect(baoCao.duongDan.length).toBe(GIOI_HAN_BAO_CAO.duongDan);
    expect(baoCao.traceId?.length).toBe(GIOI_HAN_BAO_CAO.traceId);
    expect(baoCao.phienBanApp.length).toBe(GIOI_HAN_BAO_CAO.phienBanApp);
  });

  it('field BE bắt buộc không bao giờ rỗng, dù đầu vào rỗng', () => {
    const loi = new Error('');
    loi.name = '';

    const baoCao = dungBaoCao(loi, { duongDan: '', traceId: '', phienBanApp: '  ' });

    expect(baoCao.kind).toBe('Error');
    expect(baoCao.message.trim()).not.toBe('');
    expect(baoCao.duongDan).toBe('/');
    expect(baoCao.phienBanApp).not.toBe('');
    expect('traceId' in baoCao).toBeFalse();
  });
});

describe('dungBaoCao — giá trị lỗi không phải Error', () => {
  it('chuỗi bị ném → kind NonError, message là chính chuỗi, không stack', () => {
    const baoCao = dungBaoCao('Promise bị từ chối bằng chuỗi', NGU_CANH);

    expect(baoCao.kind).toBe('NonError');
    expect(baoCao.message).toBe('Promise bị từ chối bằng chuỗi');
    expect('stack' in baoCao).toBeFalse();
  });

  it('object trần có trường message kiểu chuỗi → lấy đúng trường đó', () => {
    expect(dungBaoCao({ message: 'từ object' }, NGU_CANH).message).toBe('từ object');
  });

  it('undefined / null / số → vẫn ra báo cáo hợp lệ', () => {
    for (const giaTri of [undefined, null, 42]) {
      const baoCao = dungBaoCao(giaTri, NGU_CANH);
      expect(baoCao.kind).toBe('NonError');
      expect(baoCao.message.trim()).not.toBe('');
    }
  });
});

describe('dungBaoCao — không lộ dữ liệu nhạy cảm (10-observability.md §4.1 ràng buộc 1)', () => {
  it('object bị ném mang nội dung form → KHÔNG xuất hiện ở bất kỳ field nào', () => {
    const noiDungForm = { tenDangNhap: 'nguyenvana', matKhau: 'Abc@12345' };

    const baoCao = dungBaoCao(noiDungForm, NGU_CANH);

    expect(JSON.stringify(baoCao)).not.toContain('Abc@12345');
    expect(JSON.stringify(baoCao)).not.toContain('nguyenvana');
  });

  it('Error kèm cause và thuộc tính phụ mang nội dung form → không field nào mang theo', () => {
    const loi = Object.assign(new Error('Lưu thất bại'), {
      cause: { matKhau: 'Abc@12345' },
      giaTriForm: { soCccd: '012345678901' },
    });

    const json = JSON.stringify(dungBaoCao(loi, NGU_CANH));

    expect(json).not.toContain('Abc@12345');
    expect(json).not.toContain('012345678901');
  });

  it('duongDan không kèm query string và fragment', () => {
    const baoCao = dungBaoCao(new Error('x'), {
      ...NGU_CANH,
      duongDan: '/tim-kiem?hoTen=Nguyen+Van+A&cccd=012345678901#ket-qua',
    });

    expect(baoCao.duongDan).toBe('/tim-kiem');
  });

  it('URL có query trong message bị bỏ query', () => {
    const loi = new Error(
      'Không gọi được https://api.test/nguoi-dung?hoTen=Nguyen+Van+A&token=xyz sau 3 lần',
    );

    const baoCao = dungBaoCao(loi, NGU_CANH);

    expect(baoCao.message).toBe('Không gọi được https://api.test/nguoi-dung sau 3 lần');
  });

  it('URL có query trong stack bị bỏ query nhưng GIỮ :dòng:cột', () => {
    const loi = new Error('x');
    loi.stack = 'Error: x\n    at hienThi (http://localhost:4200/main.js?v=ab12&u=hoTen:1:2481)';

    const baoCao = dungBaoCao(loi, NGU_CANH);

    expect(baoCao.stack).toContain('http://localhost:4200/main.js:1:2481');
    expect(baoCao.stack).not.toContain('ab12');
  });
});

describe('catQueryTrongUrl', () => {
  it('không đụng tới dấu ? không thuộc URL', () => {
    expect(catQueryTrongUrl('Bạn chắc chứ? Thử lại?')).toBe('Bạn chắc chứ? Thử lại?');
  });

  it('bỏ cả fragment', () => {
    expect(catQueryTrongUrl('xem https://a.test/x#phan-2 nhé')).toBe('xem https://a.test/x nhé');
  });
});

describe('dauVanTay', () => {
  it('cùng loại, câu và đầu stack → cùng dấu; khác câu → khác dấu', () => {
    const a = dungBaoCao(new Error('lỗi A'), NGU_CANH);
    const aLai = { ...a, traceId: 'khac', duongDan: '/man-khac' };
    const b = dungBaoCao(new Error('lỗi B'), NGU_CANH);

    expect(dauVanTay(aLai)).toBe(dauVanTay(a));
    expect(dauVanTay(b)).not.toBe(dauVanTay(a));
  });
});
