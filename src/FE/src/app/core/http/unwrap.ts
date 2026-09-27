import { ApiFailureError, ApiResult } from './api-result.model';

/** Một cửa duy nhất để lấy `data` ra khỏi envelope — không đọc `res.data` trực tiếp ở nơi khác (luật F18). */
export function unwrapData<T>(res: ApiResult<T>): T {
  if (!res.success) {
    throw new ApiFailureError(res);
  }
  return res.data;
}
