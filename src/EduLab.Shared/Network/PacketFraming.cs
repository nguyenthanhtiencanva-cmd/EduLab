using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EduLab.Shared.Enums;
using EduLab.Shared.Models;

namespace EduLab.Shared.Network
{
    public static class PacketFraming
    {
        /// <summary>
        /// Gửi một gói tin hoàn chỉnh qua Stream với cấu trúc:
        /// [2 bytes Magic: 0xED01] + [2 bytes Command] + [4 bytes Length] + [Payload bytes]
        /// </summary>
        public static async Task SendPacketAsync(Stream stream, NetworkPacket packet, CancellationToken ct = default)
        {
            byte[] serialized = packet.Serialize();
            await stream.WriteAsync(serialized.AsMemory(0, serialized.Length), ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Đọc một gói tin hoàn chỉnh từ Stream một cách an toàn chống phân mảnh dữ liệu mạng Wi-Fi
        /// </summary>
        public static async Task<NetworkPacket?> ReadPacketAsync(Stream stream, CancellationToken ct = default)
        {
            byte[] headerBuffer = new byte[8];
            if (!await ReadExactBytesAsync(stream, headerBuffer, 0, 8, ct).ConfigureAwait(false))
            {
                return null; // Stream đóng hoặc ngắt kết nối
            }

            ushort magic = BitConverter.ToUInt16(headerBuffer, 0);
            if (magic != NetworkPacket.MAGIC_HEADER)
            {
                throw new InvalidDataException($"Gói tin không hợp lệ, Magic nhận được: 0x{magic:X4}");
            }

            ushort commandValue = BitConverter.ToUInt16(headerBuffer, 2);
            int payloadLength = BitConverter.ToInt32(headerBuffer, 4);

            if (payloadLength < 0 || payloadLength > 50 * 1024 * 1024) // Giới hạn an toàn 50MB
            {
                throw new InvalidDataException($"Độ dài gói tin bất thường: {payloadLength} bytes");
            }

            byte[] payload = new byte[payloadLength];
            if (payloadLength > 0)
            {
                if (!await ReadExactBytesAsync(stream, payload, 0, payloadLength, ct).ConfigureAwait(false))
                {
                    return null;
                }
            }

            return new NetworkPacket((CommandType)commandValue, payload);
        }

        private static async Task<bool> ReadExactBytesAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead), ct).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    return false; // Connection closed
                }
                totalBytesRead += bytesRead;
            }
            return true;
        }
    }
}
