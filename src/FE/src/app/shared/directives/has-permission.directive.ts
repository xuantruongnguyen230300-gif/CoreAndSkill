import {
  Directive,
  TemplateRef,
  ViewContainerRef,
  computed,
  effect,
  inject,
  input,
} from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';

/**
 * Ẩn/hiện một phần tử theo PERMISSION, không theo role (adr/0005-permission-based.md).
 * Guard bảo vệ MÀN HÌNH; directive này ẩn PHẦN TỬ — cả hai đọc chung `AuthService.coQuyen()`
 * (fe-routing-guard.md §3.4).
 *
 * 🛑 Chỉ là trải nghiệm, KHÔNG phải bảo mật — BE tự kiểm quyền độc lập ở mọi endpoint (RULES.md S2).
 */
@Directive({
  selector: '[appHasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);

  readonly appHasPermission = input.required<string | string[]>();

  /** `computed` so bằng `===`: làm mới phiên với cùng kết luận không đánh thức effect bên dưới. */
  private readonly duocPhep = computed(() => {
    const canCo = this.appHasPermission();
    const ds = Array.isArray(canCo) ? canCo : [canCo];
    return ds.every((q) => this.auth.coQuyen(q));
  });

  constructor() {
    // Chỉ dựng/gỡ view khi kết luận chuyển giữa true và false — dựng lại mất focus và trạng thái con.
    effect(() => {
      if (this.duocPhep()) {
        if (this.vcr.length === 0) this.vcr.createEmbeddedView(this.tpl);
      } else {
        this.vcr.clear();
      }
    });
  }
}
