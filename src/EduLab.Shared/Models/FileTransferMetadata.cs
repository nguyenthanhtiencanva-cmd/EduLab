using System;

namespace EduLab.Shared.Models
{
    public class FileTransferMetadata
    {
        public string TransferId { get; set; } = Guid.NewGuid().ToString("N");
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string SenderMachineName { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string? StudentId { get; set; }
        public string? ClassName { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class FileChunkPacket
    {
        public string TransferId { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public bool IsLastChunk => ChunkIndex + 1 >= TotalChunks;
    }
}
