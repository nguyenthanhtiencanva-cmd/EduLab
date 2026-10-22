using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EduLab.Shared.Utils
{
    public static class WakeOnLanHelper
    {
        /// <summary>
        /// Gửi Magic Packet (Wake-on-LAN) để bật máy từ xa qua địa chỉ MAC
        /// </summary>
        public static async Task SendWakeOnLanAsync(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return;

            // Chuẩn hóa địa chỉ MAC (loại bỏ :, -, space)
            string cleanMac = Regex.Replace(macAddress, "[: -]", "");
            if (cleanMac.Length != 12) return;

            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = byte.Parse(cleanMac.Substring(i * 2, 2), NumberStyles.HexNumber);
            }

            // Gói tin Magic Packet: 6 bytes 0xFF theo sau là 16 lần địa chỉ MAC
            byte[] packet = new byte[6 + 16 * 6];
            for (int i = 0; i < 6; i++)
            {
                packet[i] = 0xFF;
            }

            for (int i = 0; i < 16; i++)
            {
                Buffer.BlockCopy(macBytes, 0, packet, 6 + i * 6, 6);
            }

            using var client = new UdpClient();
            client.EnableBroadcast = true;
            var endpoint = new IPEndPoint(IPAddress.Broadcast, 9);
            await client.SendAsync(packet, packet.Length, endpoint);
        }
    }
}
