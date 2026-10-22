# Hướng Dẫn Cài Đặt & Triển Khai Phần Mềm EduLab Cho Phòng Tin (40 Máy Windows 11 qua Wi-Fi)

Hệ thống **EduLab** được thiết kế chuyên biệt cho phòng tin học gồm **40 máy trạm Windows 11 kết nối qua mạng Wi-Fi**, kết hợp ưu điểm quản lý trạm của Gcafe và các tính năng giảng dạy của NetOp School/Veyon.

---

## I. Yêu Cầu Mạng Wi-Fi Phòng Tin (Quan Trọng)

Vì toàn bộ 40 máy kết nối qua Wi-Fi (không dùng dây LAN), thầy chỉ cần lưu ý 1 thao tác cấu hình router:

1. **Tắt tính năng AP Isolation (Client Isolation):**
   * Đăng nhập vào trang quản trị của Router/Modem Wi-Fi phòng máy (thường là `192.168.1.1` hoặc `192.168.0.1`).
   * Tìm mục **Wireless** / **Wi-Fi Settings** $\rightarrow$ Tìm tùy chọn **"AP Isolation"** (hoặc *"Client Isolation"*, *"Station Isolation"*).
   * Chuyển trạng thái sang **DISABLED (Tắt)** $\rightarrow$ Lưu lại.
   * *Mục đích*: Cho phép các máy học sinh và máy giáo viên kết nối truyền dữ liệu trực tiếp với nhau qua Wi-Fi.

2. **Cơ chế Tự Động Kết Nối (Zero-Config):**
   * Hệ thống đã tích hợp công nghệ **UDP Auto-Discovery**: Máy trạm tự tìm máy giáo viên qua sóng Wi-Fi trong vòng 1 giây.
   * **Thầy không cần phải cài đặt hay gán IP tĩnh thủ công** cho từng máy. Dù Router cấp IP nào thì hệ thống vẫn tự động nhận diện và kết nối chuẩn xác.

---

## II. Đặt Tên Cho 40 Máy Trạm (Khuyến Nghị)

Để sơ đồ 40 máy trên màn hình máy giáo viên hiển thị trực quan theo hàng/dãy:
* Đặt tên các máy trạm theo cú pháp: `MAY-01`, `MAY-02`, ..., `MAY-40`.
* **Cách đổi tên nhanh trên Windows 11:**
  1. Nhấn `Windows + X` $\rightarrow$ Chọn **Terminal (Admin)** hoặc **PowerShell (Admin)**.
  2. Gõ lệnh:
     ```powershell
     Rename-Computer -NewName "MAY-01" -Restart
     ```
  *(Thay `MAY-01` bằng số thứ tự máy tương ứng rồi nhấn Enter, máy sẽ tự khởi động lại với tên mới).*

---

## III. Hướng Dẫn Biên Dịch & Đóng Gói (Chỉ Cần Làm 1 Lần)

Để việc cài đặt trên 40 máy diễn ra nhanh nhất, em đã viết sẵn script xuất bản thành **file chạy độc lập (Self-Contained)**. Với cách này, **các máy học sinh KHÔNG CẦN phải cài thêm bất kỳ phần mềm .NET Runtime nào**, cắm vào là chạy ngay!

1. Trên máy có cài **.NET 8 SDK** (hoặc máy của thầy), nhấp đúp chạy file:  
   👉 [**`scripts/publish_windows.bat`**](file:///Users/macbookpro13/Documents/Phần mềm quản lý phòng tin/scripts/publish_windows.bat)
2. Sau khi chạy xong, thầy sẽ có thư mục `dist/` gồm 2 phần:
   * **`dist/EduLab_Server/`**: Dành cho Máy Giáo Viên.
   * **`dist/EduLab_Client/`**: Dành cho 40 Máy Học Sinh.

---

## IV. Hướng Dẫn Cài Đặt Chi Tiết

### 1. Cài đặt trên Máy Giáo Viên (Máy Chủ) - Mất 1 phút
1. Copy thư mục `dist/EduLab_Server/` ra màn hình Desktop của thầy.
2. Nhấp chuột phải vào file [**`scripts/setup_server_firewall.bat`**](file:///Users/macbookpro13/Documents/Phần mềm quản lý phòng tin/scripts/setup_server_firewall.bat) $\rightarrow$ Chọn **"Run as administrator"** (để mở tường lửa Windows cho phép các máy trạm kết nối về).
3. Nhấp đúp vào `EduLab.Server.exe` để mở giao diện quản trị phòng máy.

### 2. Cài đặt trên 40 Máy Học Sinh (Máy Trạm) - Chỉ mất 20 giây mỗi máy!
Em đã tích hợp sẵn script tự động hóa [**`scripts/install_client.bat`**](file:///Users/macbookpro13/Documents/Phần mềm quản lý phòng tin/scripts/install_client.bat). Thầy chỉ cần thao tác rất nhanh như sau:

1. Copy thư mục `dist/EduLab_Client/` (kèm file `install_client.bat`) vào một chiếc **USB**.
2. Cắm USB vào từng máy trạm:
   * Nhấp chuột phải vào **`install_client.bat`** $\rightarrow$ Chọn **"Run as administrator"**.
3. **Script sẽ tự động hoàn toàn:**
   * Tự copy file vào `C:\Program Files\EduLabClient\`.
   * Tự mở cổng Windows Firewall để kết nối Wi-Fi thông suốt.
   * Tự đăng ký khởi động cùng Windows (mỗi lần bật máy lên là phần mềm tự chạy và khóa màn hình ngay).
   * Tự bật ứng dụng chạy luôn lần đầu.
4. Rút USB sang máy tiếp theo. Cả 40 máy chỉ mất tầm 10-15 phút là hoàn tất toàn bộ phòng!

---

## IV. Hướng Dẫn Sử Dụng Các Tính Năng Trong Giờ Học

### 1. Mở máy & Khóa máy
* **Khóa toàn bộ:** Nhấn nút **[🔒 Khóa Tất Cả]** trên thanh công cụ máy giáo viên. Mọi máy học sinh sẽ hiện màn hình đen mờ, chặn toàn bộ `Alt+Tab`, phím `Windows`, và vô hiệu hóa `Task Manager`.
* **Mở toàn bộ:** Khi đến giờ thực hành, nhấn **[🔓 Mở Tất Cả]**. Màn hình máy học sinh sẽ được mở ra và xuất hiện thanh công cụ nộp bài ở góc trên màn hình.
* **Điều khiển từng máy:** Nhấp chuột phải vào từng ô máy trên sơ đồ để Khóa, Mở, Tắt hoặc Khởi động lại riêng máy đó.

### 2. Học sinh điểm danh / Báo danh
* Khi máy đang khóa, học sinh nhập: **Họ và tên**, **Mã học sinh**, **Lớp** rồi bấm *"Báo Danh Vào Tiết Học"*.
* Thông tin học sinh sẽ tự động hiện lên ô máy tương ứng trên màn hình của thầy.

### 3. Giám sát ma trận 40 màn hình (Live Screen Matrix)
* Bấm nút **[🖥️ Ma Trận Màn Hình (40 Máy)]**.
* Màn hình sẽ hiển thị trực tiếp 40 thumbnail thu nhỏ xem tất cả học sinh đang làm gì trong thời gian thực.
* Tần số thumbnail được tối ưu siêu nhẹ (1 FPS, nén JPEG thông minh), chỉ tốn ~10-15 KB/giây mỗi máy, đảm bảo 40 máy stream mượt mà không làm chậm sóng Wi-Fi.
* **Nhấp đúp chuột** vào bất kỳ ô màn hình nào để phóng to xem chi tiết ở kích thước lớn.

### 4. Phát tài liệu / Đề bài tập
* Thầy bấm nút **[📁 Phát Đề Bài]** $\rightarrow$ Chọn file bài tập (Word, PDF, file mã nguồn...).
* Hệ thống sẽ tự động gửi file qua Wi-Fi và lưu trực tiếp vào thư mục `Desktop\BaiTap_Tu_GiaoVien` trên máy của tất cả học sinh.

### 5. Học sinh nộp bài tập
* Học sinh chỉ cần **kéo thả file bài làm vào thanh công cụ nhỏ trên đỉnh màn hình** (hoặc bấm nút *"📁 Nộp Bài Tập"*).
* Bài nộp được tự động chuyển về máy giáo viên và phân loại ngăn nắp theo cấu trúc thư mục:
  ```
  Desktop/EduLab_ThuBai_HocSinh/
  └── [TenLop]/
      ├── NguyenVanA_BaiTap1.docx
      ├── TranThiB_BaiTap1.docx
      └── ...
  ```
* Thầy chỉ cần bấm nút **[📥 Thư Mục Thu Bài]** trên thanh công cụ để mở ngay thư mục chứa toàn bộ bài nộp của lớp.

### 6. Thông báo & Quản lý nguồn cuối giờ
* **Gửi thông báo:** Bấm nút **[📢 Gửi Thông Báo]** để phát thông điệp nhắc nhở toàn lớp (ví dụ: *"Còn 5 phút nữa hết giờ"*).
* **Tắt nguồn cuối buổi:** Sau khi cả lớp nộp bài xong, thầy chỉ cần bấm **[🔌 Tắt Toàn Bộ]**, toàn bộ 40 máy trong phòng sẽ tự động tắt nguồn an toàn mà thầy không cần phải đi từng máy.
