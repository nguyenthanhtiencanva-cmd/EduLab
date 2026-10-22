using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EduLab.Shared.Models;
using EduLab.Shared.Network;

namespace EduLab.Server.Services
{
    public class ConnectedClient : IDisposable
    {
        public TcpClient Socket { get; }
        public NetworkStream Stream { get; }
        public ClientInfo Info { get; set; }
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private bool _isDisposed = false;

        public ConnectedClient(TcpClient socket, ClientInfo info)
        {
            Socket = socket;
            Stream = socket.GetStream();
            Info = info;
        }

        public async Task SendPacketAsync(NetworkPacket packet, CancellationToken ct = default)
        {
            if (_isDisposed || !Socket.Connected) return;

            await _sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await PacketFraming.SendPacketAsync(Stream, packet, ct).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            try
            {
                Stream.Dispose();
                Socket.Close();
                _sendLock.Dispose();
            }
            catch { }
        }
    }
}
