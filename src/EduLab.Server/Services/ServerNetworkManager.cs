using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EduLab.Shared.Enums;
using EduLab.Shared.Models;
using EduLab.Shared.Network;
using EduLab.Shared.Utils;

namespace EduLab.Server.Services
{
    public class ServerNetworkManager
    {
        public static ServerNetworkManager Instance { get; } = new ServerNetworkManager();

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly ConcurrentDictionary<string, ConnectedClient> _clients = new(StringComparer.OrdinalIgnoreCase);

        // Lưu trữ các file đang nhận từ học sinh nộp về
        private readonly ConcurrentDictionary<string, FileStream> _incomingSubmissions = new();
        private readonly ConcurrentDictionary<string, FileTransferMetadata> _submissionMeta = new();

        public string SubmissionsDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "EduLab_ThuBai_HocSinh"
        );

        public event Action<ClientInfo>? OnClientConnected;
        public event Action<string>? OnClientDisconnected;
        public event Action<string, byte[]>? OnThumbnailReceived;
        public event Action<ClientInfo>? OnClientUpdated;
        public event Action<FileTransferMetadata, string>? OnSubmissionCompleted;

        public IReadOnlyCollection<ConnectedClient> ConnectedClients => _clients.Values.ToList();

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Directory.CreateDirectory(SubmissionsDirectory);

            // 1. Chạy dịch vụ phát hiện máy chủ Wi-Fi (UDP Beacon)
            Task.Run(() => DiscoveryBeacon.RunServerBeaconAsync("EduLab Server", NetworkConstants.COMMAND_PORT, _cts.Token));

            // 2. Chạy dịch vụ lắng nghe TCP các máy trạm kết nối
            Task.Run(() => ListenClientsLoopAsync(_cts.Token));

            // 3. Chạy dịch vụ kiểm tra ngắt kết nối định kỳ
            Task.Run(() => HealthCheckLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            try
            {
                _listener?.Stop();
            }
            catch { }

            foreach (var client in _clients.Values)
            {
                client.Dispose();
            }
            _clients.Clear();
        }

        private async Task ListenClientsLoopAsync(CancellationToken ct)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, NetworkConstants.COMMAND_PORT);
                _listener.Start(100);

                while (!ct.IsCancellationRequested)
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync(ct);
                    _ = HandleClientConnectionAsync(tcpClient, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi TCP Listener: {ex.Message}");
            }
        }

        private async Task HandleClientConnectionAsync(TcpClient socket, CancellationToken ct)
        {
            string? machineName = null;
            ConnectedClient? clientWrapper = null;

            try
            {
                var stream = socket.GetStream();

                // Chờ gói tin đăng ký ban đầu từ máy trạm
                var firstPacket = await PacketFraming.ReadPacketAsync(stream, ct);
                if (firstPacket == null || firstPacket.Command != CommandType.RegisterClient)
                {
                    socket.Close();
                    return;
                }

                var clientInfo = firstPacket.GetPayloadAs<ClientInfo>() ?? new ClientInfo();
                machineName = string.IsNullOrWhiteSpace(clientInfo.MachineName) ? $"MAY-{Guid.NewGuid().ToString().Substring(0, 4)}" : clientInfo.MachineName;
                clientInfo.MachineName = machineName;
                clientInfo.Status = ClientStatus.Locked;
                clientInfo.LastSeen = DateTime.UtcNow;

                // Nếu máy này đã kết nối trước đó (reconnect), dọn dẹp kết nối cũ
                if (_clients.TryRemove(machineName, out var oldClient))
                {
                    oldClient.Dispose();
                }

                clientWrapper = new ConnectedClient(socket, clientInfo);
                _clients[machineName] = clientWrapper;

                OnClientConnected?.Invoke(clientInfo);

                // Lặp đọc dữ liệu từ máy trạm
                while (!ct.IsCancellationRequested && socket.Connected)
                {
                    var packet = await PacketFraming.ReadPacketAsync(stream, ct);
                    if (packet == null) break;

                    clientInfo.LastSeen = DateTime.UtcNow;

                    switch (packet.Command)
                    {
                        case CommandType.ThumbnailData:
                            clientInfo.LatestThumbnail = packet.Payload;
                            OnThumbnailReceived?.Invoke(machineName, packet.Payload);
                            break;

                        case CommandType.StudentCheckIn:
                            var checkIn = packet.GetPayloadAs<ClientInfo>();
                            if (checkIn != null)
                            {
                                clientInfo.StudentName = checkIn.StudentName;
                                clientInfo.StudentId = checkIn.StudentId;
                                clientInfo.ClassName = checkIn.ClassName;
                                clientInfo.Status = ClientStatus.InUse;
                                OnClientUpdated?.Invoke(clientInfo);
                            }
                            break;

                        case CommandType.SubmitFileStart:
                            var meta = packet.GetPayloadAs<FileTransferMetadata>();
                            if (meta != null)
                            {
                                _submissionMeta[meta.TransferId] = meta;
                                string classFolder = string.IsNullOrWhiteSpace(meta.ClassName) ? "Chung" : meta.ClassName;
                                string classPath = Path.Combine(SubmissionsDirectory, classFolder);
                                Directory.CreateDirectory(classPath);

                                string safeStudent = string.IsNullOrWhiteSpace(meta.StudentName) ? machineName : meta.StudentName;
                                string fileName = $"{safeStudent}_{meta.FileName}";
                                string fullPath = Path.Combine(classPath, fileName);

                                _incomingSubmissions[meta.TransferId] = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                            }
                            break;

                        case CommandType.SubmitFileChunk:
                            var chunk = packet.GetPayloadAs<FileChunkPacket>();
                            if (chunk != null && _incomingSubmissions.TryGetValue(chunk.TransferId, out var fs))
                            {
                                await fs.WriteAsync(chunk.Data.AsMemory(0, chunk.Data.Length), ct);
                                if (chunk.IsLastChunk)
                                {
                                    fs.Flush();
                                    string savedPath = fs.Name;
                                    fs.Dispose();
                                    _incomingSubmissions.TryRemove(chunk.TransferId, out _);

                                    if (_submissionMeta.TryRemove(chunk.TransferId, out var metadata))
                                    {
                                        OnSubmissionCompleted?.Invoke(metadata, savedPath);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            catch { }
            finally
            {
                if (machineName != null)
                {
                    _clients.TryRemove(machineName, out _);
                    OnClientDisconnected?.Invoke(machineName);
                }
                clientWrapper?.Dispose();
            }
        }

        private async Task HealthCheckLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                foreach (var kvp in _clients)
                {
                    if ((now - kvp.Value.Info.LastSeen).TotalSeconds > 15)
                    {
                        kvp.Value.Info.Status = ClientStatus.Offline;
                        OnClientUpdated?.Invoke(kvp.Value.Info);
                    }
                }
                await Task.Delay(5000, ct);
            }
        }

        public async Task LockAllAsync()
        {
            var packet = new NetworkPacket(CommandType.LockScreen, Array.Empty<byte>());
            await BroadcastPacketAsync(packet);
        }

        public async Task UnlockAllAsync()
        {
            var packet = new NetworkPacket(CommandType.UnlockScreen, Array.Empty<byte>());
            await BroadcastPacketAsync(packet);
        }

        public async Task LockMachineAsync(string machineName)
        {
            if (_clients.TryGetValue(machineName, out var client))
            {
                client.Info.Status = ClientStatus.Locked;
                client.Info.IsScreenLocked = true;
                await client.SendPacketAsync(new NetworkPacket(CommandType.LockScreen, Array.Empty<byte>()));
                OnClientUpdated?.Invoke(client.Info);
            }
        }

        public async Task UnlockMachineAsync(string machineName)
        {
            if (_clients.TryGetValue(machineName, out var client))
            {
                client.Info.Status = ClientStatus.InUse;
                client.Info.IsScreenLocked = false;
                await client.SendPacketAsync(new NetworkPacket(CommandType.UnlockScreen, Array.Empty<byte>()));
                OnClientUpdated?.Invoke(client.Info);
            }
        }

        public async Task ShutdownAllAsync()
        {
            var packet = new NetworkPacket(CommandType.PowerShutdown, Array.Empty<byte>());
            await BroadcastPacketAsync(packet);
        }

        public async Task RestartAllAsync()
        {
            var packet = new NetworkPacket(CommandType.PowerRestart, Array.Empty<byte>());
            await BroadcastPacketAsync(packet);
        }

        public async Task ShutdownMachineAsync(string machineName)
        {
            if (_clients.TryGetValue(machineName, out var client))
            {
                await client.SendPacketAsync(new NetworkPacket(CommandType.PowerShutdown, Array.Empty<byte>()));
            }
        }

        public async Task RestartMachineAsync(string machineName)
        {
            if (_clients.TryGetValue(machineName, out var client))
            {
                await client.SendPacketAsync(new NetworkPacket(CommandType.PowerRestart, Array.Empty<byte>()));
            }
        }

        public async Task BroadcastMessageAsync(string message)
        {
            var packet = new NetworkPacket(CommandType.BroadcastMessage, message);
            await BroadcastPacketAsync(packet);
        }

        public async Task DistributeFileAsync(string filePath, IEnumerable<string>? targetMachines = null)
        {
            if (!File.Exists(filePath)) return;

            var fileInfo = new FileInfo(filePath);
            var targets = targetMachines != null && targetMachines.Any()
                ? _clients.Where(c => targetMachines.Contains(c.Key, StringComparer.OrdinalIgnoreCase)).Select(c => c.Value).ToList()
                : _clients.Values.ToList();

            var meta = new FileTransferMetadata
            {
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                SenderMachineName = "GIAO_VIEN"
            };

            var startPacket = NetworkPacket.FromObject(CommandType.SendFileStart, meta);
            foreach (var client in targets)
            {
                await client.SendPacketAsync(startPacket);
            }

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

                var chunkNetPacket = NetworkPacket.FromObject(CommandType.SendFileChunk, chunkPacket);
                foreach (var client in targets)
                {
                    await client.SendPacketAsync(chunkNetPacket);
                }
            }
        }

        private async Task BroadcastPacketAsync(NetworkPacket packet)
        {
            var tasks = _clients.Values.Select(c => c.SendPacketAsync(packet));
            await Task.WhenAll(tasks);
        }
    }
}
