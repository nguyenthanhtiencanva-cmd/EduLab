@echo off
chcp 65001 >nul
echo ========================================================
echo       XUẤT BẢN PHẦN MỀM EDULAB (SELF-CONTAINED WIN-X64)
echo   (Tạo file chạy độc lập - KHÔNG CẦN CÀI .NET RUNTIME)
echo ========================================================

echo.
echo [1/2] Đang đóng gói EduLab.Server (Máy Giáo Viên)...
dotnet publish src/EduLab.Server/EduLab.Server.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/EduLab_Server

echo.
echo [2/2] Đang đóng gói EduLab.Client (Máy Trạm Học Sinh)...
dotnet publish src/EduLab.Client/EduLab.Client.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/EduLab_Client

echo.
echo ========================================================
echo HOÀN THÀNH!
echo • Thư mục máy giáo viên: dist\EduLab_Server
echo • Thư mục 40 máy học sinh: dist\EduLab_Client
echo ========================================================
pause
