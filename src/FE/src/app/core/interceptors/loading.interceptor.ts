import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { LoadingService } from '../http/loading.service';

/** Đếm request đang chạy — `finalize` chứ không `tap`, để chạy cả khi request bị huỷ. */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject(LoadingService);
  loading.batDau();
  return next(req).pipe(finalize(() => loading.ketThuc()));
};
