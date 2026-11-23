---
version: alpha
name: "Nghỉ Lễ"
description: "Lịch để bàn thu nhỏ cho widget đếm ngược ngày lễ trên Windows."
colors:
  canvas: "#111C28"
  panel: "#1C2D3D"
  ink: "#F8F2E7"
  muted: "#BAC9D4"
  line: "#7990A3"
  divider: "#324658"
  hover: "#294054"
  selected: "#31485D"
  gold: "#F8D48F"
  on-gold: "#2F2318"
  error: "#FFB5AA"
typography:
  display:
    fontFamily: "Georgia, Segoe UI"
  body:
    fontFamily: "Segoe UI"
  counter:
    fontFamily: "Bahnschrift, Segoe UI"
rounded:
  panel: "10px"
  control: "5px"
spacing:
  side-inset: "20px"
  holiday-row-min: "38px"
components:
  date-stamp: {}
  countdown-face: {}
  settings-tabs: {}
  holiday-row: {}
---

# Thiết kế Nghỉ Lễ

## Overview

Widget nổi dành cho người dùng Windows muốn biết còn bao lâu tới một dịp lễ ở Việt Nam. Giao diện v2 dùng nét dọc như gáy lịch để đánh dấu tên dịp trên mặt đồng hồ và thông tin đếm ngược trong Cài đặt. Đường đục lỗ ngăn số ngày với giờ, phút, giây. Hình minh họa riêng của từng dịp nằm ở đầu mặt đồng hồ.

`portable/PanelTheme.xaml` là nguồn màu và điều khiển dùng chung trong Cài đặt. Màu trong tài liệu phản chiếu các khóa `PanelCanvas`, `PanelField`, `PanelInk`, `PanelMuted`, `PanelLine`, `PanelDivider`, `PanelHot`, `PanelSelected`, `PanelGold`, `PanelOnGold` và `PanelError` theo thứ tự khai báo trong tài liệu. `portable/NghiLe.cs` sở hữu bảng màu ngày lễ và phông `Widget.CounterFont` dùng cho bộ đếm và ô ngày tháng. Kích thước trong tài liệu tương ứng với DIP ở WPF.

## Colors

Trong Cài đặt, nền xanh mực và các lớp xanh xám giữ chữ tiếng Việt dễ đọc. Vàng nhạt đánh dấu tiêu điểm, thao tác chính, tab đang chọn và mốc ngày lễ. Viền mờ phân nhóm nội dung, còn viền sáng hơn xác định điều khiển. Trên widget, nền, viền, số và nhãn lấy màu từ bảng riêng của từng dịp. Mặt kính đậm dần từ khu vực minh họa đến vùng chữ để bộ đếm dễ đọc trên nền sáng.

## Typography

Georgia xuất hiện ở tiêu đề dịp đang theo dõi và lời chúc. Segoe UI dùng cho nhãn và thao tác. Bahnschrift dùng cho số trên mặt đồng hồ và ô ngày tháng, với Segoe UI dự phòng. Dùng số thẳng, cùng bề rộng khi phông chữ hỗ trợ để bộ đếm giữ bố cục ổn định khi giây thay đổi.

## Layout

Bố cục dọc 200 × 250 DIP đặt hình minh họa và tên dịp trên số đếm, sau đó là ba cột thời gian và mốc theo giờ Việt Nam. Ở cỡ gọn, mặt đồng hồ dành chỗ cho số đếm lớn hơn. Bố cục ngang 360 × 84 DIP đưa hình minh họa sang trái. Tên dịp dùng một dòng có chiều cao cố định. Chữ tự thu theo bề rộng, dùng dấu ba chấm ở cỡ tối thiểu và giữ tên đầy đủ trong chú giải. Cửa sổ Cài đặt giữ các tab ở trên, nút Xong ở dưới và để nội dung cuộn theo từng tab.

## Elevation & Depth

Tạo lớp bằng sắc độ và đường viền mảnh. Nét vàng bên trái đánh dấu mốc ngày lễ trong Cài đặt. Trên mặt đồng hồ, khoảng trống và đường đục lỗ phân tách các nhóm số.

## Shapes

Các ô lớn bo nhẹ theo dáng tờ lịch. Điều khiển dùng góc nhỏ hơn. Công tắc Đào / Mai giữ hình viên thuốc để biểu thị rõ hai vị trí.

## Components

Trong tab **Thú cưng**, Hắc Long có thêm ô **Xem thử động tác** dùng cùng kiểu ô chọn với **Loại thú cưng**. Ô xem thử phát động tác đã chọn ngay trong hình minh họa và dừng khi rời tab. Hình trên thanh tác vụ phát theo nhịp riêng của từng động tác.

- **Mặt đồng hồ** dùng bốn vị trí số cố định. Tên dịp, đơn vị và ngày tháng đi kèm số đếm. Nút cài đặt, ẩn và đổi kích thước hiện khi con trỏ hoặc tiêu điểm bàn phím tới các điều khiển.
- **Ô ngày tháng** lấy mốc dương lịch từ đúng bộ đếm. Tiêu đề cho biết dịp đang đếm ngược hay đang diễn ra. Khi thiếu mốc, khu vực này hiển thị cách tiếp tục.
- **Tab và hàng ngày lễ** dùng điều khiển WPF. Sáu tab chia đều bề ngang. Tab đang chọn có nền xanh xám và gạch vàng. Hàng được chọn có dấu tích và nền riêng, còn viền vàng chỉ vị trí bàn phím. Ngày âm lịch có nhãn riêng để phân biệt với mốc dương lịch dùng trong ô ngày tháng.
- **Sắp tới** dùng nét gáy lịch cho các dòng và màu vàng cho dịp đang diễn ra. Tên dịp có thể xuống dòng. Ngày dương lịch và số ngày còn lại nằm bên dưới, cạnh ô chọn mốc nhắc dùng mẫu thả xuống chung.
- **Thêm ngày** dùng danh sách thả xuống riêng cho ngày và tháng để chọn mốc dương lịch hằng năm. Khung xem trước dùng bảng màu và cảnh như trên widget. Mặt sinh nhật dùng nền tím mận với dây cờ, bánh kem, bóng bay và quà. Số đếm nằm dưới cảnh ở bố cục dọc và bên cạnh cảnh ở bố cục ngang.
- **Tùy chọn và nhạc** dùng chung kiểu nút, ô nhập và thanh cuộn trong `PanelTheme.xaml`. Danh sách thả xuống dùng mẫu WPF hiện có và bám bề rộng nút mở. Tùy chọn Chuyển động điều khiển hiệu ứng trên mặt đồng hồ.
- **Thú cưng** dùng ô tùy chọn chung và hình xem trước theo con vật và tỉ lệ đã chọn. Mèo xám xanh có vằn và cuộn tròn khi nghỉ. Chó nâu vàng có tai cụp, vòng cổ và các động tác ngắn khi bật chuyển động. Cửa sổ thú cưng đậu bên ngoài thanh tác vụ, giữ nguyên tiêu điểm ở ứng dụng đang dùng và có thể hiện cùng đồng hồ. Khi được nhấc lên, thú cưng có tư thế riêng và quay mặt theo hướng kéo ngang. Khi thanh tác vụ ẩn, mép màn hình thay cho chỗ đậu và có khoảng hở dành cho thao tác gọi thanh tác vụ lên.
- **Chuyển động** tập trung vào hình minh họa của từng dịp. `HolidayScene` điều khiển mọi cảnh, kể cả hoa đào, ở tối đa 12 khung hình mỗi giây. Số đếm đổi trực tiếp theo từng giây. Khi tắt Chuyển động hoặc ẩn widget, cảnh dừng.

## Do's and Don'ts

- Ưu tiên số đếm dễ đọc ở kích thước mặc định và kích thước nhỏ nhất.
- Dành hình minh họa cho từng dịp và giữ các tab quản lý nhất quán trong cùng một hệ màu.
- Duy trì dấu tích, nhãn và viền tiêu điểm khi điều chỉnh màu trạng thái.
