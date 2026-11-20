# Nhịp Ngày · Đếm ngược ngày lễ

Nhịp Ngày là widget nổi cho Windows. Bản portable có giao diện riêng cho tám dịp ở cả hai bố cục đồng hồ, **Dọc** (200 × 250 px) và **Ngang** (360 × 84 px). Đồng hồ và hiệu ứng chạy offline. Hiệu ứng trang trí chạy tối đa 16 khung hình mỗi giây.

## Mở trên Windows

1. Bấm đúp `NhipNgay.exe` trong thư mục này. Bản portable dùng WPF và .NET Framework có sẵn trên máy kiểm tra. Bạn có thể chạy từ Downloads.
2. Kéo mặt đồng hồ để di chuyển. Rê chuột lên widget và kéo **↘** ở góc dưới bên phải để đổi kích thước theo tỉ lệ của bố cục hiện tại. Ở bố cục dọc từ **160 × 200 px** trở xuống, widget ẩn dòng ngày và múi giờ rồi tăng cỡ tên năm và số ngày. Khi tay nắm nhận tiêu điểm, các phím mũi tên chỉnh kích thước từng bước. **F2** mở cài đặt và **Esc** quay về đồng hồ.
3. **Cài đặt** mở trong cửa sổ riêng có thể kéo giãn. Sáu ngày nghỉ lễ được xếp thành hai hàng, mỗi hàng ba thẻ. Trung thu và Giáng sinh nằm ở nhóm bên dưới. Khi dùng lần đầu, **Tết Nguyên đán** được tích sẵn. Bộ đếm chọn dịp gần nhất trong các thẻ bạn tích. Trong **Tùy chọn → Hiển thị**, có thể đổi bố cục **Dọc** hoặc **Ngang**, **Luôn nổi**, **Chuyển động** và **Độ đậm kính**.
4. Nút × ẩn cửa sổ. Bấm biểu tượng hoa ở khay hệ thống, xem thêm trong dấu mũi tên ^ nếu cần, hoặc mở lại `NhipNgay.exe` để hiện widget. Bấm phải vào biểu tượng và chọn **Thoát Nhịp Ngày** để dừng ứng dụng.
5. Để Nhịp Ngày tự mở lúc đăng nhập, bật **Mở cùng Windows** ngay trong **Cài đặt**. Công tắc mặc định tắt trên máy mới và chỉ áp dụng cho tài khoản Windows đang dùng. Khi nâng cấp từ Hẹn Tết, thoát phiên bản cũ rồi mở `NhipNgay.exe` từ cùng thư mục. Mục khởi động do bản cũ tạo trong thư mục đó sẽ trỏ sang tệp mới. Nếu chuyển ứng dụng sang thư mục khác, mở tệp ở vị trí mới rồi bật công tắc lại.

Ứng dụng tiếp tục dùng `%LOCALAPPDATA%\HenTet\settings.json` sau khi đổi tên. Tệp này lưu vị trí, kích thước, bố cục, các dịp đã chọn, chế độ luôn nổi, hiệu ứng và độ đậm kính. Nếu tệp chưa có, ứng dụng vẫn đọc tùy chọn từ `%LOCALAPPDATA%\Nocturne\settings.json` để giữ vị trí và kích thước của bản cũ hơn.

Dùng công tắc **Đào / Mai** trên thẻ **Tết Nguyên đán** trong Cài đặt để đổi giữa **Cành đào và bánh chưng** và **Cành mai và bánh tét**. Lựa chọn được lưu và áp dụng cho cả màn đếm ngược lẫn lời chúc năm mới.

## Phát nhạc

Đặt tệp MP3, WAV hoặc M4A trực tiếp vào thư mục `nhac` cạnh `NhipNgay.exe`. Vào **Cài đặt → Tùy chọn → Nhạc**, chọn bài rồi bấm **Bắt đầu phát**. Bấm nút này khi chưa chọn bài sẽ phát từ đầu danh sách. Khi đang nghe, chọn bài khác sẽ dừng bài hiện tại và chờ bạn bấm **Bắt đầu phát**. Danh sách sắp theo tên tệp và tự cập nhật khi mở mục Nhạc hoặc khi chuyển bài. Sau khi bắt đầu phát, ứng dụng phát lần lượt và quay về bài đầu khi hết danh sách. Nhạc tiếp tục khi quay về đồng hồ và dừng khi thoát ứng dụng.

Bản Tauri dùng thư mục `nhac` ở gốc dự án khi chạy chế độ phát triển hoặc cạnh tệp thực thi khi build. Các tệp nhạc riêng nằm ngoài Git và gói phát hành.

## Lịch đếm ngược

Bản portable đếm tới 00:00 giờ Việt Nam, giữ giao diện của dịp đang diễn ra và tự chuyển sau khi hết ngày hoặc kỳ nghỉ. **Tết Nguyên đán**, **Trung thu** và **Giáng sinh** hiện lời chúc khi bộ đếm về 0.

Các bảng âm lịch bao phủ **2026–2036** cho [Tết Nguyên đán](https://www.qppstudio.net/global-holidays-observances/tet-nguyen-dan-vietnamese-new-year.htm), [Giỗ Tổ Hùng Vương](https://time.so/holidays/vietnam/hung-kings-commemoration-day/) và [Trung thu](https://vietnam-calendar.com/holidays/mid-autumn-festival).

Bộ đếm **Tết Nguyên đán** luôn về 0 lúc 00:00 mùng 1 Tết. Năm Đinh Mùi 2027, mốc là **06/02/2027**. Lời chúc Tết hiển thị từ mùng 1 đến hết ngày **10/02/2027**. Giao diện **Quốc khánh 2027** giữ từ **02/09** đến hết **05/09** theo lịch nghỉ khối hành chính. Các năm ngoài lịch nghỉ đã nhập dùng ngày chính lễ. [Thông báo của Bộ Nội vụ](https://baochinhphu.vn/nghi-tet-am-lich-2027-keo-dai-7-ngay-nghi-quoc-khanh-4-ngay-102261010095648278.htm)

## Phát triển bản Tauri 2 + React + TypeScript

Máy kiểm tra có Windows ARM64, WebView2 Runtime và Node v24 do Codex cung cấp. Để build Tauri, cài [Node.js 24 LTS hoặc mới hơn có npm](https://nodejs.org/en/download), [Rust qua rustup](https://rustup.rs/), và [Microsoft C++ Build Tools theo hướng dẫn Tauri](https://v2.tauri.app/start/prerequisites/). Trong Visual Studio Installer chọn **Desktop development with C++**, Windows SDK và bộ công cụ C++ ARM64 nếu build cho ARM64. Mở terminal mới sau khi cài.

```powershell
node --version
npm.cmd --version
rustup show
npm.cmd install
npm.cmd test
npm.cmd run tauri dev
```

Build bộ cài NSIS cho ARM64:

```powershell
rustup target add aarch64-pc-windows-msvc
npm.cmd run tauri build -- --target aarch64-pc-windows-msvc
```

Trên máy Windows x64, thay target bằng `x86_64-pc-windows-msvc`. Tauri ghi bộ cài vào `src-tauri/target/<target>/release/bundle/nsis/`. Dự án cấu hình kèm bộ cài offline WebView2 để cài được trên máy chưa có runtime. [Đóng gói Tauri trên Windows](https://v2.tauri.app/distribute/windows-installer/).

Giao diện Tauri dùng tên Nhịp Ngày. Cấu hình đóng gói giữ tên bộ cài Hẹn Tết, tên binary Rust và định danh ứng dụng cũ để duy trì đường dẫn cài đặt và vùng dữ liệu của phiên bản trước.

Chỉnh giao diện React trong `src/style.css`, cành đào và hạt mưa trong `src/Weather.tsx`, kích thước cửa sổ và cấu hình đóng gói trong `src-tauri/tauri.conf.json`. Nếu đổi kích thước, cập nhật cả CSS và cấu hình Tauri. `src/calendar.ts` và `src/settings.ts` xử lý lịch và tùy chọn. Bản Tauri có vùng lưu riêng trong WebView2 và plugin lưu vị trí.

## Phát triển bản portable WPF

Điểm vào và widget ở `portable/NhipNgay.cs`. Hai bản dùng chung bảng ngày Tết và bản vẽ cành đào trong `data/`. Chỉnh cách vẽ và chuyển động của WPF trong `portable/PeachScene.cs`.

Chạy `.\portable\Build.ps1` từ thư mục dự án để tạo `NhipNgay.exe` bằng trình biên dịch .NET Framework có sẵn trên máy. Tham số `-OutputPath` cho phép chọn đường dẫn khác.

## Trạng thái kiểm tra

- Bản portable có các lệnh `--self-test`, `--smoke-test`, `--music-test` và `--startup-test`. Chạy mỗi lệnh với `NhipNgay.exe` và kiểm tra mã thoát bằng 0.
- Chạy `node --test tests/*.test.mjs` để kiểm tra lịch, cài đặt, kích thước và danh sách phát của bản Tauri.
- Bản Tauri chưa được biên dịch trên máy này vì thiếu npm, Rust MSVC và C++ Build Tools. Chạy cài phụ thuộc và build để tạo lockfile và bộ cài NSIS trước khi phát hành.
