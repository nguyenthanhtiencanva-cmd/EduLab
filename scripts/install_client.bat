@echo off
chcp 65001 >nul
:: Kiểm tra quyền Administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [LỖI] Vui lòng nhấp chuột phải vào file này và chọn "Run as administrator"!
    pause
    exit /b
)

echo ========================================================
echo        CÀI ĐẶT TỰ ĐỘNG EDULAB CLIENT (MÁY HỌC SINH)
echo ========================================================

set TARGET_DIR=C:\Program Files\EduLabClient

echo 1. Đang tạo thư mục cài đặt tại: %TARGET_DIR%...
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

echo 2. Đang sao chép các tệp tin ứng dụng...
xcopy /E /Y /I "%~dp0*" "%TARGET_DIR%\" >nul

echo 3. Đang cấu hình Tường lửa Windows (Firewall)...
netsh advfirewall firewall delete rule name="EduLab Client" >nul 2>&1
netsh advfirewall firewall add rule name="EduLab Client" dir=in action=allow program="%TARGET_DIR%\EduLab.Client.exe" enable=yes >nul
netsh advfirewall firewall add rule name="EduLab UDP Discovery" dir=in action=allow protocol=UDP localport=8887 enable=yes >nul

echo 4. Đang thiết lập tự động khởi động cùng Windows (Startup)...
reg add "HKLM\Software\Microsoft\Windows\CurrentVersion\Run" /v "EduLabClient" /t REG_SZ /d "\"%TARGET_DIR%\EduLab.Client.exe\"" /f >nul

echo 5. Khởi động ứng dụng ngay bây giờ...
start "" "%TARGET_DIR%\EduLab.Client.exe"

echo.
echo ========================================================
echo   CÀI ĐẶT THÀNH CÔNG TRÊN MÁY HỌC SINH NÀY!
echo ========================================================
timeout /t 3 >nul
exit /b
