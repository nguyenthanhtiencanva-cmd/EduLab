# EduLab - Phần Mềm Quản Lý Phòng Tin Học (40 Máy Windows 11 qua Wi-Fi)

Hệ thống quản lý phòng máy trường học kết hợp mô hình kiểm soát phiên (kiểu Gcafe) và công cụ hỗ trợ giảng dạy (kiểu NetOp School / Veyon), được thiết kế tối ưu riêng cho môi trường **40 máy trạm Windows 11 kết nối hoàn toàn qua sóng Wi-Fi**.

---

## 🎯 Các tính năng nổi bật

1. **Auto-Discovery qua Wi-Fi (Zero-Config):**
   * Các máy trạm tự động tìm và kết nối tới máy giáo viên qua UDP Broadcast. Không cần cài đặt IP tĩnh.
2. **Sơ đồ phòng máy 40 trạm trực quan:**
   * Ma trận 8 cột x 5 hàng hiển thị đầy đủ thông tin: Trạng thái (Online / Đang khóa / Đang học / Offline), Tên học sinh, Lớp, IP, xem trước màn hình.
3. **Màn hình khóa an toàn trên Windows 11:**
   * Chặn hoàn toàn `Alt + Tab`, phím `Windows`, `Alt + F4`, `Ctrl + Esc`, và vô hiệu hóa `Task Manager` qua Registry khi khóa máy.
4. **Giám sát trực tiếp 40 màn hình (Matrix View):**
   * Hiển thị thumbnail trực tiếp của 40 học sinh cùng lúc. Thuật toán nén JPEG siêu nhẹ (~10-15 KB/s/máy) không làm nghẽn sóng Wi-Fi.
   * Nhấp đúp chuột để phóng to toàn màn hình máy bất kỳ.
5. **Phát đề bài & Thu bài tập tự động 1-Click:**
   * Phát đề bài từ máy giáo viên tới thẳng Desktop của 40 máy học sinh.
   * Học sinh nộp bài bằng cách kéo thả file vào thanh công cụ nổi; máy chủ tự động gom và phân loại theo Thư mục Lớp $\rightarrow$ Họ tên học sinh.
6. **Điều khiển nguồn từ xa:**
   * Khóa/Mở hàng loạt hoặc từng máy.
   * Tắt nguồn (Shutdown) hoặc Khởi động lại (Restart) toàn bộ 40 máy chỉ với một click.
7. **Thông báo toàn phòng máy:**
   * Gửi thông điệp nhắc nhở, dặn dò bài tập hiển thị ngay trên màn hình học sinh.

---

## 📂 Cấu trúc mã nguồn

* [`EduLab.sln`](file:///Users/macbookpro13/Documents/Phần mềm quản lý phòng tin/EduLab.sln): File giải pháp Visual Studio 2022 (.NET 8.0).
* [`src/EduLab.Shared`](file:///Users/macbookpro13/Documents/Phần mềm quản lý phòng tin/src/EduLab.Shared): Thư viện dùng chung (Đóng gói giao thức TCP/UDP framing, tự động dò tìm máy chủ Wi-Fi, nén ảnh màn hình, điều khiển nguồn).
* [`src/EduLab.Server`](file:///Users/macbookpro13/Documents/Phần mềm quản lý phòng tin/src/EduLab.Server): Ứng dụng Máy Giáo Viên (WPF, sơ đồ 40 máy, ma trận giám sát, phát/thu bài).
* [`src/EduLab.Client`](file:///Users/macbookpro13/Documents/Phần mềm quản lý phòng tin/src/EduLab.Client): Ứng dụng Máy Trạm Học Sinh (WPF Native, màn hình khóa chặn phím hệ thống, thanh công cụ nộp bài kéo-thả).
* [`docs/HUONG_DAN_CAI_DAT_VA_SU_DUNG.md`](file:///Users/macbookpro13/Documents/Phần mềm quản lý phòng tin/docs/HUONG_DAN_CAI_DAT_VA_SU_DUNG.md): Hướng dẫn chi tiết cài đặt, cấu hình router Wi-Fi và sử dụng.
