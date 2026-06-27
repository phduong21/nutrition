# Code Review Report

## Findings (ưu tiên theo mức độ)

### High
- **Unhandled exception có thể trả về 500 thay vì 502**  
  Trong `NutritionAgent/Infrastructure/FoodFetcher.cs`, các lệnh gọi `HttpClient.GetAsync(...)` và `JsonSerializer.DeserializeAsync(...)` chưa được bao trong xử lý lỗi cho `HttpRequestException`, `TaskCanceledException`, hoặc `JsonException`.  
  Khi upstream lỗi mạng/timeout/JSON hỏng, luồng có thể ném exception trực tiếp và rơi vào 500 thay vì đi qua `Result<T>` để map về `ErrorKind.UpstreamFailure` -> HTTP `502`.

### Medium
- **`barcode` được nội suy trực tiếp vào URL path**  
  Trong `NutritionAgent/Infrastructure/FoodFetcher.cs`, `barcode` chỉ được kiểm tra rỗng rồi ghép thẳng vào đường dẫn `api/v2/product/{barcode}.json`.  
  Input có ký tự đặc biệt (vd `/`, `?`, `%`) có thể làm thay đổi semantics của route và gây lỗi khó truy vết. Nên encode hoặc validate chặt hơn format barcode trước khi gọi upstream.

### Low
- **Insight so sánh category bị sai khi bằng nhau**  
  Trong `NutritionAgent/Domain/NutritionScoringEngine.cs`, logic:
  - `productValue > categoryAverage` -> `above`
  - còn lại -> `below`  
  Trường hợp `productValue == categoryAverage` hiện bị gán thành `below`, khiến nội dung insight sai ngữ nghĩa.

## Missing Tests / Coverage Gaps

- Chưa có test cho các nhánh lỗi hạ tầng:
  - upstream ném `HttpRequestException`
  - deserialize ném `JsonException`
  - timeout/cancel khi gọi OFF
- Vì thiếu các test này, issue mức **High** hiện chưa bị phát hiện bởi test suite.

## Verification Snapshot

- Đã chạy: `dotnet test`
- Kết quả: **Passed 24/24**

## Open Questions / Assumptions

- Giả định mong muốn hệ thống là mọi lỗi transport/parse từ Open Food Facts đều được map thống nhất về `UpstreamFailure` và trả `502`.
- Giả định input barcode nên được coi là untrusted path data và cần encode/validate trước khi ghép URL.
