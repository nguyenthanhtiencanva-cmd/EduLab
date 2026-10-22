using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EduLab.Shared.Network
{
    public class ServerDiscoveryResult
    {
        public string ServerIp { get; set; } = string.Empty;
        public int CommandPort { get; set; } = NetworkConstants.COMMAND_PORT;
        public string ServerName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Module tự động dò tìm máy chủ giáo viên trên mạng Wi-Fi (Auto-Discovery)
    /// </summary>
    public static class DiscoveryBeacon
    {
        /// <summary>
        /// Phía Máy Chủ: Chạy dịch vụ phát tín hiệu và phản hồi cho các máy trạm dò tìm
        /// </summary>
        public static async Task RunServerBeaconAsync(string serverName, int commandPort, CancellationToken ct)
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            try
            {
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, NetworkConstants.DISCOVERY_PORT));
            }
            catch (SocketException)
            {
                // Có thể cổng đang được sử dụng hoặc đang chạy nền
            }

            // Task 1: Định kỳ phát broadcast ra mạng Wi-Fi (3 giây một lần)
            var broadcastTask = Task.Run(async () =>
            {
                var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, NetworkConstants.DISCOVERY_PORT);
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        string msg = $"{NetworkConstants.DISCOVERY_MAGIC_RESP}|{commandPort}|{serverName}";
                        byte[] bytes = Encoding.UTF8.GetBytes(msg);
                        await udp.SendAsync(bytes, bytes.Length, broadcastEndpoint);
                    }
                    catch
                    {
                        // Bỏ qua lỗi gửi tạm thời của Wi-Fi
                    }
                    await Task.Delay(3000, ct);
                }
            }, ct);

            // Task 2: Lắng nghe yêu cầu tìm kiếm trực tiếp từ các máy trạm
            var listenTask = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var result = await udp.ReceiveAsync(ct);
                        string received = Encoding.UTF8.GetString(result.Buffer);
                        if (received.StartsWith(NetworkConstants.DISCOVERY_MAGIC_REQ))
                        {
                            // Trả lời trực tiếp về máy trạm vừa hỏi
                            string resp = $"{NetworkConstants.DISCOVERY_MAGIC_RESP}|{commandPort}|{serverName}";
                            byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                            await udp.SendAsync(respBytes, respBytes.Length, result.RemoteEndPoint);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Thử lại nếu có ngoại lệ mạng
                        await Task.Delay(500, ct);
                    }
                }
            }, ct);

            await Task.WhenAny(broadcastTask, listenTask);
        }

        /// <summary>
        /// Phía Máy Trạm: Gửi yêu cầu tìm kiếm và chờ phản hồi từ Máy Giáo Viên
        /// </summary>
        public static async Task<ServerDiscoveryResult?> DiscoverServerAsync(int timeoutMs = 4000, CancellationToken ct = default)
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            try
            {
                // Gửi broadcast yêu cầu tìm máy chủ
                var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, NetworkConstants.DISCOVERY_PORT);
                byte[] reqBytes = Encoding.UTF8.GetBytes(NetworkConstants.DISCOVERY_MAGIC_REQ);
                await udp.SendAsync(reqBytes, reqBytes.Length, broadcastEndpoint);

                // Chờ phản hồi với timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeoutMs);

                while (!cts.IsCancellationRequested)
                {
                    var receiveTask = udp.ReceiveAsync(cts.Token).AsTask();
                    var completed = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs, cts.Token));

                    if (completed == receiveTask)
                    {
                        var result = await receiveTask;
                        string msg = Encoding.UTF8.GetString(result.Buffer);
                        if (msg.StartsWith(NetworkConstants.DISCOVERY_MAGIC_RESP))
                        {
                            var parts = msg.Split('|');
                            int port = NetworkConstants.COMMAND_PORT;
                            string name = "EduLab Server";
                            if (parts.Length > 1 && int.TryParse(parts[1], out int p)) port = p;
                            if (parts.Length > 2) name = parts[2];

                            return new ServerDiscoveryResult
                            {
                                ServerIp = result.RemoteEndPoint.Address.ToString(),
                                CommandPort = port,
                                ServerName = name
                            };
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch
            {
                // Timeout hoặc lỗi socket
            }

            return null;
        }
    }
}
