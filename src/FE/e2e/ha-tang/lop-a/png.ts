// Đọc MỘT điểm ảnh từ PNG 1×1 do `page.screenshot({ clip })` trả về. Chỉ hỗ trợ 8 bit, RGB hoặc RGBA —
// đúng thứ Chromium sinh. Ảnh 1×1 nên bộ lọc dòng (Sub/Up/Avg/Paeth) đều cho đúng giá trị thô: mọi điểm lân
// cận bằng 0. Không thêm thư viện: đây là thang đo một điểm ảnh, không phải xử lý ảnh.
import { inflateSync } from 'node:zlib';

export function docDiemAnh(png: Buffer): string {
  if (png.readUInt32BE(0) !== 0x89504e47) throw new Error('Không phải PNG');
  let vt = 8;
  let rong = 0;
  let cao = 0;
  let sauBit = 0;
  let loaiMau = 0;
  const idat: Buffer[] = [];
  while (vt < png.length) {
    const dai = png.readUInt32BE(vt);
    const loai = png.toString('ascii', vt + 4, vt + 8);
    const du = png.subarray(vt + 8, vt + 8 + dai);
    if (loai === 'IHDR') {
      rong = du.readUInt32BE(0);
      cao = du.readUInt32BE(4);
      sauBit = du[8]!;
      loaiMau = du[9]!;
    } else if (loai === 'IDAT') idat.push(du);
    vt += 12 + dai;
  }
  if (rong !== 1 || cao !== 1) throw new Error(`Ảnh phải 1×1, nhận ${rong}×${cao}`);
  if (sauBit !== 8 || (loaiMau !== 2 && loaiMau !== 6))
    throw new Error(`PNG chưa hỗ trợ: ${sauBit} bit, loại màu ${loaiMau}`);
  const tho = inflateSync(Buffer.concat(idat)); // [bộ lọc, R, G, B(, A)]
  const [r, g, b] = [tho[1]!, tho[2]!, tho[3]!];
  return `rgb(${r}, ${g}, ${b})`;
}
