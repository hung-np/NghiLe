---
version: alpha
name: "Nghỉ Lễ"
description: "Lịch để bàn thu nhỏ cho widget đếm ngược ngày lễ trên Windows."
colors:
  canvas: "#1C151A"
  panel: "#2B2028"
  ink: "#FAEEE2"
  muted: "#CEB9B3"
  line: "#94776C"
  divider: "#4D3941"
  hover: "#3A2932"
  selected: "#4A333E"
  gold: "#E6BE8A"
  on-gold: "#2A1A1E"
  error: "#FFC0BA"
  date-paper: "#EED5B3"
typography:
  display:
    fontFamily: "Georgia, Segoe UI"
  body:
    fontFamily: "Segoe UI"
  counter:
    fontFamily: "Bahnschrift, Segoe UI"
rounded:
  panel: "12px"
  control: "5px"
spacing:
  side-inset: "20px"
  holiday-row-min: "40px"
components:
  date-stamp: {}
  countdown-face: {}
  settings-tabs: {}
  holiday-row: {}
---

# Thiết kế Nghỉ Lễ

## Overview

Widget nổi dành cho người dùng Windows muốn biết còn bao lâu tới một dịp lễ ở Việt Nam. Bề mặt gợi màu sơn mài, với hai lớp viền mảnh và một dấu hình thoi phân cách số ngày với giờ, phút, giây. Hình minh họa của từng dịp đứng cạnh hoặc phía trên số đếm. Ô ngày màu giấy làm nổi bật mốc dương lịch trong Cài đặt.

`portable/PanelTheme.xaml` là nguồn màu và điều khiển dùng chung trong Cài đặt. Màu trong tài liệu phản chiếu các khóa `PanelCanvas`, `PanelField`, `PanelInk`, `PanelMuted`, `PanelLine`, `PanelDivider`, `PanelHot`, `PanelSelected`, `PanelGold`, `PanelOnGold` cùng `PanelError` và `PanelDatePaper`. `portable/NghiLe.cs` sở hữu bảng màu theo dịp và phông `Widget.CounterFont` dùng cho giờ, phút, giây. `src/style.css` áp dụng cùng hướng thị giác cho bản Tauri, với các biến CSS riêng cho WebView. Mục Năm Tết dùng hộp chọn hệ thống và giữ hành vi bàn phím của WebView. Kích thước trong tài liệu tương ứng với DIP ở WPF.

## Colors

Trong Cài đặt, nền mận sẫm và chữ ngà tạo tương phản. Ánh đồng đánh dấu tiêu điểm, thao tác chính và tab đang chọn. Ô ngày dùng nền giấy ngà và chữ nâu sẫm. Viền mờ phân nhóm nội dung, còn viền sáng hơn xác định điều khiển. Trên widget, nền, viền, số và nhãn lấy màu từ bảng riêng của từng dịp. Mặt kính đậm dần từ khu vực minh họa đến vùng chữ để bộ đếm dễ đọc trên nền sáng.

## Typography

Georgia dùng cho tên dịp, số ngày lớn, mốc ngày và lời chúc. Segoe UI dùng cho nhãn và thao tác. Bahnschrift dùng cho giờ, phút, giây và ngày tháng trên từng dòng, với Segoe UI dự phòng. Dùng số thẳng, cùng bề rộng khi phông chữ hỗ trợ để bộ đếm giữ bố cục ổn định khi giây thay đổi.

## Layout

Bố cục dọc 200 × 250 DIP đặt hình minh họa và tên dịp trên số đếm, sau đó là ba cột thời gian và mốc theo giờ Việt Nam. Ở cỡ gọn, mặt đồng hồ dành chỗ cho số đếm lớn hơn. Bố cục ngang 360 × 84 DIP đưa hình minh họa sang trái. Tên dịp dùng một dòng có chiều cao cố định. Chữ tự thu theo bề rộng, dùng dấu ba chấm ở cỡ tối thiểu và giữ tên đầy đủ trong chú giải. Sáu tab Cài đặt luôn nằm trên một hàng ngang ở đầu cửa sổ. Nút Xong nằm ở chân cửa sổ và nội dung cuộn theo từng tab.

## Elevation & Depth

Dùng viền ngoài ánh đồng và viền chìm để tạo chiều sâu mà không che hình minh họa. Trên mặt đồng hồ, một dấu hình thoi trên đường kẻ mờ phân tách các nhóm số. Trong Cài đặt, mỗi nhóm ngày lễ nằm trong cùng một khung.

## Shapes

Các ô lớn bo nhẹ theo dáng tờ lịch. Điều khiển dùng góc nhỏ hơn. Công tắc Đào / Mai giữ hình viên thuốc để biểu thị rõ hai vị trí.

## Components

- **Mặt đồng hồ** dùng bốn vị trí số cố định. Tên dịp, đơn vị và ngày tháng đi kèm số đếm. Nút cài đặt và ẩn hiện khi con trỏ hoặc tiêu điểm bàn phím tới các điều khiển. Kéo cạnh hoặc góc để đổi kích thước theo tỉ lệ. Người dùng bàn phím chỉnh bằng thanh Kích thước widget trong Tùy chọn.
- **Ô ngày tháng** lấy mốc dương lịch từ đúng bộ đếm. Tiêu đề cho biết dịp đang đếm ngược hay đang diễn ra. Khi thiếu mốc, khu vực này hiển thị cách tiếp tục.
- **Tab và hàng ngày lễ** dùng điều khiển WPF. Sáu tab dùng cùng một khung và điều khiển bàn phím WPF. Tab đang chọn có nền mận sáng hơn và đường ánh đồng. Hàng được chọn có dấu tích và nền riêng, còn viền vàng chỉ vị trí bàn phím. Ngày âm lịch có nhãn riêng để phân biệt với mốc dương lịch dùng trong ô ngày tháng.
- **Dịp ghim** có tối đa ba ô trên cùng một hàng. Mỗi ô có tên dịp, số ngày và chú giải đầy đủ. Chọn ô để chuyển bộ đếm chính. Bố cục dọc đặt hàng dưới ngày đích. Bố cục ngang đặt hàng dưới các số, cạnh hình minh họa.
- **Sắp tới** dùng nét gáy lịch cho các dòng và màu vàng cho dịp đang diễn ra. Tên dịp có thể xuống dòng. Ngày dương lịch và số ngày còn lại nằm bên dưới, cạnh ô chọn mốc nhắc dùng mẫu thả xuống chung.
- **Lịch tháng** trong tab Sắp tới dùng bảy cột từ thứ Hai đến Chủ nhật. Số dương lịch lớn hơn số âm lịch. Dấu chấm đánh dấu dịp theo dõi, gạch dưới đánh dấu hôm nay và nền chọn đánh dấu ngày đang xem. Các dịp trùng ngày dùng cùng mẫu hàng và ô chọn nhắc với danh sách.
- **Thêm ngày** dùng danh sách thả xuống riêng cho ngày và tháng để chọn mốc dương lịch hằng năm. Khung xem trước dùng bảng màu và cảnh như trên widget. Mặt sinh nhật dùng nền tím mận với dây cờ, bánh kem, bóng bay và quà. Số đếm nằm dưới cảnh ở bố cục dọc và bên cạnh cảnh ở bố cục ngang.
- **Tùy chọn và nhạc** dùng chung kiểu nút, ô nhập và thanh cuộn trong `PanelTheme.xaml`. Danh sách thả xuống dùng mẫu WPF hiện có và bám bề rộng nút mở. Tùy chọn Chuyển động điều khiển hiệu ứng trên mặt đồng hồ.
- **Thú cưng** dùng ô tùy chọn chung và hình xem trước theo con vật và tỉ lệ đã chọn. Mèo xám xanh có vằn và cuộn tròn khi nghỉ. Chó nâu vàng có tai cụp, vòng cổ và các động tác ngắn khi bật chuyển động. Hắc Long dùng bộ 15 khung đi bộ và lật hướng khi tới mỗi đầu thanh tác vụ. Cửa sổ thú cưng đậu bên ngoài thanh tác vụ, giữ nguyên tiêu điểm ở ứng dụng đang dùng và có thể hiện cùng đồng hồ. Bấm nhẹ vào thú cưng để hiện tên trong nhãn nhỏ phía trên và phát một phản ứng ngắn. Giữ chuột rồi kéo sẽ nhấc thú cưng lên và đổi hướng mặt theo chiều kéo. Khi thanh tác vụ ẩn, mép màn hình thay cho chỗ đậu và có khoảng hở dành cho thao tác gọi thanh tác vụ lên.
- **Widget bổ sung** nằm đầu tab Tùy chọn, với nút **Thêm widget** dùng kiểu nút chính màu vàng. Các mục Đếm ngược, Hiển thị, Tiết kiệm pin, Khởi động và Sao lưu và khôi phục nằm bên dưới. Widget bổ sung dùng cùng mặt đồng hồ, cảnh và thao tác kéo viền. Mỗi cửa sổ chọn một dịp và có vị trí, kích thước, bố cục riêng. Khay hệ thống quản lý việc ẩn và mở lại từng cửa sổ. Lịch, tùy chọn hiệu ứng và thông báo nhắc dùng chung.
- **Tiết kiệm pin** dừng chuyển động theo trạng thái nguồn của Windows khi bật tùy chọn. Lớp giảm sáng theo giờ phủ bên trong khung kính, giữ con trỏ và bàn phím thao tác với widget.
- **Sao lưu và khôi phục** đặt cạnh các tùy chọn khác. Hộp thoại chọn tệp dùng giao diện Windows. Khôi phục hiển thị số ngày cá nhân, lời nhắc và dịp ghim trước khi xác nhận thay thế, giữ vị trí cửa sổ theo máy đang dùng.
- **Chuyển động** tập trung vào hình minh họa của từng dịp. `HolidayScene` điều khiển mọi cảnh, kể cả hoa đào, ở tối đa 12 khung hình mỗi giây. Số đếm đổi trực tiếp theo từng giây. Khi tắt Chuyển động hoặc ẩn widget, cảnh dừng.

## Do's and Don'ts

- Ưu tiên số đếm dễ đọc ở kích thước mặc định và kích thước nhỏ nhất.
- Dành hình minh họa cho từng dịp và giữ các tab quản lý nhất quán trong cùng một hệ màu.
- Duy trì dấu tích, nhãn và viền tiêu điểm khi điều chỉnh màu trạng thái.

## Cảnh 3D trong widget portable

Mỗi dịp có sẵn dùng một tiểu cảnh WPF 3D nhìn từ góc ba phần tư. Khay sơn mài có cạnh vát, hai đường chỉ màu đồng và bóng tiếp xúc để tạo chiều sâu. Ánh sáng ấm lướt nhẹ trên vật liệu khi bật chuyển động. Bố cục dọc đặt cảnh trên tiêu đề và bộ đếm. Bố cục ngang đặt cảnh bên trái, phía trên tiêu đề. Màn lời chúc dọc dành thêm chỗ cho cảnh. Trong **Tùy chọn → Hiển thị**, **Mô hình 3D** đổi giữa mô hình và hình 2D. **Chuyển động** điều khiển hiệu ứng của chế độ đang dùng.

| Dịp | Cảnh 3D | Chuyển động |
|---|---|---|
| Tết Dương lịch | Pháo hoa trên dãy nhà đêm. | Các chùm pháo nở lần lượt. |
| Tết Nguyên đán | Tờ lịch giấy ngà cùng cành đào và bánh chưng, hoặc cành mai và bánh tét theo tùy chọn **Đào / Mai**. | Cành hoa đung đưa, cánh hoa rơi nhẹ. |
| Giỗ Tổ Hùng Vương | Trống đồng nghiêng cạnh cổng đền. | Mặt trống xoay nhẹ để lộ hoa văn dưới ánh đèn. |
| Giải phóng miền Nam | Mặt tiền Dinh Độc Lập, sân cỏ và cờ trên mái. | Lá cờ gợn sóng. |
| Quốc tế Lao động | Bánh răng và mũ bảo hộ lao động. | Bánh răng quay. |
| Quốc khánh | Cờ đỏ sao vàng và Lăng ở Quảng trường Ba Đình. | Vải cờ chuyển động quanh điểm treo trên cột. |
| Ngày Văn hóa Việt Nam | Nhà hát Lớn Hà Nội với mái, hàng cột và cửa vòm. | Ánh đèn quét qua mặt tiền. |
| Trung thu | Đèn lồng, trăng tròn và bánh Trung thu. | Đèn lồng đung đưa, mặt trăng dịch nhẹ. |
| Giáng sinh | Cây thông, quả châu và những hộp quà. | Cây thông xoay chậm, ánh sao trên ngọn thay đổi. |

`portable/HolidayDiorama.cs` chọn cảnh và giữ ảnh tĩnh của từng mô hình. Bước build dựng trước các tư thế và góc nhìn, sau đó nhúng khung hình nén vào tệp thực thi. `portable/HolidayDioramaMotion.cs` giải nén các khung của cảnh đang chạy vào cùng một ảnh hiển thị. Widget giải phóng bộ đệm khi đổi cảnh, tắt chuyển động hoặc ẩn cửa sổ. Nếu cảnh 3D không đọc được, widget hiển thị hình minh họa 2D tương ứng.

### Cảnh đề xuất cho các màn hình riêng của bản portable

Các hàng sau là thiết kế cho bản WPF, với cảnh mở từ đồng hồ, khay hệ thống hoặc tab Cài đặt hiện có. Các thao tác chính vẫn dùng điều khiển WPF và luôn sẵn ở chế độ hình tĩnh.

| Màn hình | Cảnh và máy quay | Thao tác và phản hồi |
|---|---|---|
| Ngày lễ · Chọn dịp theo dõi | Tủ mẫu nông với biểu tượng dịp đang xem, như trống đồng hoặc đèn Trung thu. Góc trước cố định. | Ô chọn đổi trạng thái theo dõi. Nút Đào / Mai đổi tiểu cảnh Tết. |
| Sắp tới · Xem ngày gần nhất và đặt nhắc | Xấp giấy lịch thấp, tờ trên cùng lấy ngày từ dịp đang xem. | Chọn mốc nhắc trên từng dòng. **Xem lịch tháng** mở lịch âm – dương. |
| Lịch tháng · Xem ngày âm và các dịp cùng ngày | Tờ lịch nghệ thuật nhìn từ trên xuống, bên cạnh lưới ngày WPF. | Chuyển tháng, về **Hôm nay**, chọn ngày để xem dịp và đặt nhắc. **Xem danh sách** quay lại Sắp tới. |
| Thêm ngày · Quản lý dịp cá nhân | Bánh và nến hoặc thiệp gấp, xem gần ở góc ba phần tư. | Loại sự kiện đổi hình xem trước. Biểu mẫu WPF tiếp tục xử lý ngày, tên, lỗi và lưu. |
| Tùy chọn · Chỉnh widget | Mẫu widget ở góc thấp, giữ đủ cả bố cục ngang lẫn dọc. | Bố cục và độ đậm kính đổi mẫu trực tiếp. Công tắc chuyển động giữ cảnh tĩnh khi tắt. |
| Thú cưng · Chọn và đặt vị trí | Một đoạn thanh tác vụ nhìn ngang tầm mắt, với Mèo, Chó hoặc Hắc Long. | Điều khiển hiện có đổi mẫu, vị trí và tỉ lệ. Chế độ **Ở yên** giữ tư thế tĩnh. |
| Nhạc · Nghe nhạc trong thư mục cục bộ | Máy đĩa than chiếu nghiêng trên khay sơn mài. | Chọn và phát nhạc qua danh sách WPF. Đĩa quay theo trạng thái phát khi bật chuyển động. |
