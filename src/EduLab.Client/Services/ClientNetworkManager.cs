using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EduLab.Shared.Enums;
using EduLab.Shared.Models;
using EduLab.Shared.Network;
using EduLab.Shared.Utils;

namespace EduLab.Client.Services
{
    public class ClientNetworkManager
    {
        public static ClientNetworkManager Instance { get; } = new ClientNetworkManager();

        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public event Action? OnLocked;
        public event Action? OnUnlocked;
        public event Action<string>? OnBroadcastReceived;
        public event Action<string>? OnStatusChanged;
        public event Action<string, string>? OnFileReceived;

        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;
        public string ServerIp { get; private set; } = string.Empty;
        public string MachineName { get; } = Environment.MachineName;

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ConnectionLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            CloseConnection();
        }

        private async Task ConnectionLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    string targetIp = GetConfiguredServerIp();

                    // 1. Nếu có IP được lưu thủ công, ưu tiên kết nối trực tiếp
                    if (!string.IsNullOrWhiteSpace(targetIp))
                    {
                        OnStatusChanged?.Invoke($"Đang kết nối tới Máy Giáo Viên ({targetIp})...");
                        try
                        {
                            _tcpClient = new TcpClient();
                            var connectTask = _tcpClient.ConnectAsync(targetIp, NetworkConstants.COMMAND_PORT, ct).AsTask();
                            var completed = await Task.WhenAny(connectTask, Task.Delay(3000, ct));
                            if (completed == connectTask && _tcpClient.Connected)
                            {
                                ServerIp = targetIp;
                                _stream = _tcpClient.GetStream();
                                OnStatusChanged?.Invoke($"Đã kết nối Máy Giáo Viên ({ServerIp})");

                                await RegisterWithServerAsync(ct);

                                var receiveTask = ReceiveCommandsLoopAsync(ct);
                                var thumbnailTask = SendThumbnailLoopAsync(ct);
                                var heartbeatTask = HeartbeatLoopAsync(ct);

                                await Task.WhenAny(receiveTask, thumbnailTask, heartbeatTask);
                                continue;
                            }
                        }
                        catch
                        {
                            CloseConnection();
                        }
                    }

                    // 2. Dò tìm tự động máy chủ giáo viên qua UDP Broadcast
                    OnStatusChanged?.Invoke("Đang dò tìm Máy Giáo Viên trên Wi-Fi...");
                    var discovery = await DiscoveryBeacon.DiscoverServerAsync(3000, ct);
                    if (discovery == null)
                    {
                        OnStatusChanged?.Invoke("Chưa tìm thấy Máy GV (Bấm 'Cài đặt IP' nếu dùng Wi-Fi chặn Broadcast)");
                        await Task.Delay(3000, ct);
                        continue;
                    }

                    ServerIp = discovery.ServerIp;
                    OnStatusChanged?.Invoke($"Đã thấy máy chủ ({ServerIp}). Đang kết nối...");

                    // 3. Kết nối TCP tới máy giáo viên
                    _tcpClient = new TcpClient();
                    await _tcpClient.ConnectAsync(ServerIp, discovery.CommandPort, ct);
                    _stream = _tcpClient.GetStream();

                    OnStatusChanged?.Invoke($"Đã kết nối Máy Giáo Viên ({ServerIp})");

                    // 4. Gửi đăng ký định danh máy trạm
                    await RegisterWithServerAsync(ct);

                    // 5. Chạy các tiến trình song song: Nhận lệnh, Gửi Thumbnail, Heartbeat
                    var recTask = ReceiveCommandsLoopAsync(ct);
                    var thumbTask = SendThumbnailLoopAsync(ct);
                    var hbTask = HeartbeatLoopAsync(ct);

                    await Task.WhenAny(recTask, thumbTask, hbTask);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnStatusChanged?.Invoke($"Mất kết nối: {ex.Message}. Đang thử lại...");
                }
                finally
                {
                    CloseConnection();
                }

                await Task.Delay(3000, ct);
            }
        }

        public void SetManualServerIp(string ip)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_ip.txt");
                File.WriteAllText(configPath, ip.Trim());
                CloseConnection();
            }
            catch { }
        }

        public string GetConfiguredServerIp()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_ip.txt");
                if (File.Exists(configPath))
                {
                    string ip = File.ReadAllText(configPath).Trim();
                    if (!string.IsNullOrWhiteSpace(ip)) return ip;
                }
            }
            catch { }
            return string.Empty;
        }

        private async Task RegisterWithServerAsync(CancellationToken ct)
        {
            if (_stream == null) return;

            var clientInfo = new ClientInfo
            {
                MachineId = Environment.MachineName,
                MachineName = Environment.MachineName,
                IpAddress = GetLocalIpAddress(),
                MacAddress = GetMacAddress(),
                OsVersion = "Windows 11",
                Status = ClientStatus.Locked
            };

            var packet = NetworkPacket.FromObject(CommandType.RegisterClient, clientInfo);
            await SendPacketSafeAsync(packet, ct);
        }

        private async Task HeartbeatLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _stream != null)
            {
                await Task.Delay(10000, ct);
                try
                {
                    var ping = new NetworkPacket(CommandType.Ping, Array.Empty<byte>());
                    await SendPacketSafeAsync(ping, ct);
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task ReceiveCommandsLoopAsync(CancellationToken ct)
        {
            FileStream? incomingFileStream = null;
            string? currentReceivingFile = null;

            while (!ct.IsCancellationRequested && _stream != null)
            {
                var packet = await PacketFraming.ReadPacketAsync(_stream, ct);
                if (packet == null) break;

                switch (packet.Command)
                {
                    case CommandType.LockScreen:
                        OnLocked?.Invoke();
                        break;

                    case CommandType.UnlockScreen:
                        OnUnlocked?.Invoke();
                        break;

                    case CommandType.PowerShutdown:
                        PowerHelper.Shutdown(delaySeconds: 3);
                        break;

                    case CommandType.PowerRestart:
                        PowerHelper.Restart(delaySeconds: 3);
                        break;

                    case CommandType.PowerLogoff:
                        PowerHelper.Logoff();
                        break;

                    case CommandType.BroadcastMessage:
                        string msg = packet.GetPayloadAsString();
                        OnBroadcastReceived?.Invoke(msg);
                        break;

                    case CommandType.RequestThumbnail:
                        await SendSingleThumbnailAsync(ct);
                        break;

                    case CommandType.SendFileStart:
                        var meta = packet.GetPayloadAs<FileTransferMetadata>();
                        if (meta != null)
                        {
                            string targetDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                "BaiTap_Tu_GiaoVien"
                            );
                            Directory.CreateDirectory(targetDir);
                            string savePath = Path.Combine(targetDir, meta.FileName);
                            incomingFileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
                            currentReceivingFile = savePath;
                        }
                        break;

                    case CommandType.SendFileChunk:
                        var chunk = packet.GetPayloadAs<FileChunkPacket>();
                        if (chunk != null && incomingFileStream != null)
                        {
                            await incomingFileStream.WriteAsync(chunk.Data.AsMemory(0, chunk.Data.Length), ct);
                            if (chunk.IsLastChunk)
                            {
                                incomingFileStream.Flush();
                                incomingFileStream.Dispose();
                                incomingFileStream = null;
                                if (currentReceivingFile != null)
                                {
                                    OnFileReceived?.Invoke(Path.GetFileName(currentReceivingFile), currentReceivingFile);
                                }
                            }
                        }
                        break;
                }
            }

            incomingFileStream?.Dispose();
        }

        private async Task SendThumbnailLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _stream != null)
            {
                await SendSingleThumbnailAsync(ct);
                // Giới hạn 2 giây một ảnh để mượt và siêu nhẹ băng thông Wi-Fi
                await Task.Delay(2000, ct);
            }
        }

        private async Task SendSingleThumbnailAsync(CancellationToken ct)
        {
            if (_stream == null) return;
            try
            {
                byte[] thumb = ScreenHelper.CaptureThumbnail(360, 202, 40);
                if (thumb.Length > 0)
                {
                    var packet = new NetworkPacket(CommandType.ThumbnailData, thumb);
                    await SendPacketSafeAsync(packet, ct);
                }
            }
            catch { }
        }

        public async Task SubmitFileAsync(string filePath, string studentName, string studentId, string className)
        {
            if (_stream == null || !File.Exists(filePath)) return;

            var fileInfo = new FileInfo(filePath);
            var meta = new FileTransferMetadata
            {
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                SenderMachineName = MachineName,
                StudentName = studentName,
                StudentId = studentId,
                ClassName = className
            };

            var startPacket = NetworkPacket.FromObject(CommandType.SubmitFileStart, meta);
            await SendPacketSafeAsync(startPacket);

            using var fs = File.OpenRead(filePath);
            byte[] buffer = new byte[64 * 1024];
            int totalChunks = (int)Math.Ceiling((double)fileInfo.Length / buffer.Length);
            int chunkIndex = 0;
            int bytesRead;

            while ((bytesRead = await fs.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                byte[] chunkData = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, chunkData, 0, bytesRead);

                var chunkPacket = new FileChunkPacket
                {
                    TransferId = meta.TransferId,
                    ChunkIndex = chunkIndex++,
                    TotalChunks = totalChunks,
                    Data = chunkData
                };

                var p = NetworkPacket.FromObject(CommandType.SubmitFileChunk, chunkPacket);
                await SendPacketSafeAsync(p);
            }
        }

        public async Task CheckInAsync(string studentName, string studentId, string className)
        {
            if (_stream == null || _tcpClient == null || !_tcpClient.Connected)
            {
                throw new InvalidOperationException("Máy trạm CHƯA KẾT NỐI tới Máy Giáo Viên!\nVui lòng bấm '⚙️ Cài đặt IP' ở góc dưới màn hình để nhập IP máy giáo viên.");
            }

            var info = new ClientInfo
            {
                MachineName = MachineName,
                StudentName = studentName,
                StudentId = studentId,
                ClassName = className,
                IpAddress = GetLocalIpAddress(),
                Status = ClientStatus.Locked
            };

            var packet = NetworkPacket.FromObject(CommandType.StudentCheckIn, info);
            await SendPacketSafeAsync(packet);
        }

        private async Task SendPacketSafeAsync(NetworkPacket packet, CancellationToken ct = default)
        {
            if (_stream == null) return;
            await _sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await PacketFraming.SendPacketAsync(_stream, packet, ct).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private void CloseConnection()
        {
            try
            {
                _stream?.Dispose();
                _tcpClient?.Close();
            }
            catch { }
            _stream = null;
            _tcpClient = null;
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private static string GetMacAddress()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        return nic.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch { }
            return "000000000000";
        }
    }
}