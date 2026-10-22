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
echo     CẤU HÌNH TƯỜNG LỬA CHO MÁY GIÁO VIÊN (EDULAB SERVER)
echo ========================================================

echo Đang mở cổng giao tiếp mạng cho EduLab Server...
netsh advfirewall firewall delete rule name="EduLab Server Discovery UDP" >nul 2>&1
netsh advfirewall firewall delete rule name="EduLab Server Command TCP" >nul 2>&1
netsh advfirewall firewall delete rule name="EduLab Server Screen TCP" >nul 2>&1
netsh advfirewall firewall delete rule name="EduLab Server File TCP" >nul 2>&1

netsh advfirewall firewall add rule name="EduLab Server Discovery UDP" dir=in action=allow protocol=UDP localport=8887 enable=yes >nul
netsh advfirewall firewall add rule name="EduLab Server Command TCP" dir=in action=allow protocol=TCP localport=8888 enable=yes >nul
netsh advfirewall firewall add rule name="EduLab Server Screen TCP" dir=in action=allow protocol=TCP localport=8889 enable=yes >nul
netsh advfirewall firewall add rule name="EduLab Server File TCP" dir=in action=allow protocol=TCP localport=8890 enable=yes >nul

echo.
echo ========================================================
echo   ĐÃ MỞ CỔNG TƯỜNG LỬA THÀNH CÔNG CHO MÁY GIÁO VIÊN!
echo ========================================================
timeout /t 3 >nul
exit /b
