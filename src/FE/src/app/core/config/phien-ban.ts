import { MA_COMMIT } from './build-info';

/** 7 ký tự đầu — nơi gọi `Footer` cắt, `Footer` không cắt. Rỗng/`null` → `null` (ẩn phiên bản). */
export function maCommitNgan(ma: string | null = MA_COMMIT): string | null {
  const gon = ma?.trim().slice(0, 7);
  return gon ? gon : null;
}
