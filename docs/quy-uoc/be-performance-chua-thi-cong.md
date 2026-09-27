---
kind: luat
scope: core
verified: chua-doi-chieu
---

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

# Phần chưa thi công của `be-performance.md` — Cache

> Nội dung dưới đây đã **chốt xong thiết kế** (chữ ký interface, lý do chưa cần cache) nhưng `src/BE`
> **chưa xây**. Tách khỏi [`be-performance.md`](be-performance.md) vì không lượt đọc quy ước nào hôm
> nay cần tới nó — chỉ mở file này khi thật sự bắt đầu xây cache. Luật đang có hiệu lực **ngay hôm
> nay** (chỉ đi qua `ICacheStore`, ba điều kiện bắt buộc trước khi thêm cache, và cái được cache mà không cần ba điều kiện đó)
> vẫn ở nguyên trong [`be-performance.md`](be-performance.md) §8.2–§8.4.
>
> 📖 Lý do, bẫy, ví dụ mở rộng: [`../wiki-core/be/ly-do/be-performance.md`](../wiki-core/be/ly-do/be-performance.md) §8.1, §8.2.

---

## 1. Trạng thái và lý do

> 📐 **Redis và `CachingBehavior` KHÔNG có ở v1.** Không phải bỏ sót — chưa đo được vấn đề nào để cache
> giải quyết. Quyết định về số lượng behavior ở v1:
> [`../adr/0006-pipeline-behavior.md`](../adr/0006-pipeline-behavior.md).

## 2. Interface khai trước — để đổi implementation không sửa call site

> 📐 **Chưa thi công.** Đây là chữ ký đã chốt; `src/BE/Core` chưa khai interface này. "Khai trước"
> nghĩa là khai **trước bản cài**, không phải đã có trong code.

```csharp
// Core.Application/Common/Interfaces/ICacheStore.cs
public interface ICacheStore
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);

    ValueTask SetAsync<T>(string key, T value, TimeSpan ttl,
                          IReadOnlyCollection<string>? tags = null, CancellationToken ct = default);

    ValueTask RemoveAsync(string key, CancellationToken ct = default);

    ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default);
}
```

Bốn method, không hơn. Luật dùng interface này — không chạm thẳng cache, chỉ đăng ký bản no-op khi cần trước — đang có hiệu lực ở [`be-performance.md`](be-performance.md) §8.2.
