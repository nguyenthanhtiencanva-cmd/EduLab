namespace EduLab.Shared.Network
{
    public static class NetworkConstants
    {
        // Cổng UDP phát hiện máy chủ tự động trên Wi-Fi
        public const int DISCOVERY_PORT = 8887;
        
        // Cổng TCP nhận lệnh điều khiển, khóa/mở máy, thông báo
        public const int COMMAND_PORT = 8888;

        // Cổng TCP/UDP truyền nhận dữ liệu màn hình (Thumbnail / Stream)
        public const int SCREEN_PORT = 8889;

        // Cổng TCP truyền tải tệp tin (Phát đề & Thu bài)
        public const int FILE_PORT = 8890;

        // Cổng UDP tiêu chuẩn cho Wake-on-LAN
        public const int WOL_PORT = 9;

        // Thông điệp định danh gói tin Discovery
        public const string DISCOVERY_MAGIC_REQ = "EDULAB_DISCOVER_SERVER_REQ";
        public const string DISCOVERY_MAGIC_RESP = "EDULAB_DISCOVER_SERVER_RESP";
    }
}
