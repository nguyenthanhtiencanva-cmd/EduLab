using System;
using EduLab.Shared.Enums;

namespace EduLab.Shared.Models
{
    public class ClientInfo
    {
        public string MachineId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 8888;
        public ClientStatus Status { get; set; } = ClientStatus.Offline;

        // Thông tin học sinh đang ngồi
        public string? StudentName { get; set; }
        public string? StudentId { get; set; }
        public string? ClassName { get; set; }

        // Vị trí trên sơ đồ (Dãy, Cột)
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }

        // Màn hình thu nhỏ (thumbnail)
        public byte[]? LatestThumbnail { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        // Trạng thái hệ thống máy trạm
        public string OsVersion { get; set; } = "Windows 11";
        public bool IsScreenLocked { get; set; } = true;
        public bool IsInputLocked { get; set; } = false;

        public bool IsActive => (DateTime.UtcNow - LastSeen).TotalSeconds < 15;
    }
}
