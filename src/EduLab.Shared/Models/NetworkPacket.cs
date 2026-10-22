using System;
using System.IO;
using System.Text;
using System.Text.Json;
using EduLab.Shared.Enums;

namespace EduLab.Shared.Models
{
    public class NetworkPacket
    {
        public static readonly ushort MAGIC_HEADER = 0xED01; // "ED" v1

        public CommandType Command { get; set; }
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        public NetworkPacket() { }

        public NetworkPacket(CommandType command, byte[] payload)
        {
            Command = command;
            Payload = payload;
        }

        public NetworkPacket(CommandType command, string jsonString)
        {
            Command = command;
            Payload = Encoding.UTF8.GetBytes(jsonString);
        }

        public static NetworkPacket FromObject<T>(CommandType command, T obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return new NetworkPacket(command, json);
        }

        public T? GetPayloadAs<T>()
        {
            if (Payload == null || Payload.Length == 0) return default;
            var json = Encoding.UTF8.GetString(Payload);
            return JsonSerializer.Deserialize<T>(json);
        }

        public string GetPayloadAsString()
        {
            if (Payload == null || Payload.Length == 0) return string.Empty;
            return Encoding.UTF8.GetString(Payload);
        }

        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(MAGIC_HEADER);
            writer.Write((ushort)Command);
            writer.Write(Payload.Length);
            if (Payload.Length > 0)
            {
                writer.Write(Payload);
            }

            return ms.ToArray();
        }

        public static NetworkPacket? Deserialize(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            
            // Check Magic Header
            if (stream.Length - stream.Position < 8) return null;
            ushort magic = reader.ReadUInt16();
            if (magic != MAGIC_HEADER)
            {
                throw new InvalidDataException("Invalid EduLab Magic Header");
            }

            ushort cmd = reader.ReadUInt16();
            int length = reader.ReadInt32();

            if (length < 0 || length > 100 * 1024 * 1024) // 100MB max limit
            {
                throw new InvalidDataException($"Unreasonable packet length: {length}");
            }

            byte[] payload = reader.ReadBytes(length);
            if (payload.Length != length)
            {
                throw new EndOfStreamException("Incomplete packet payload received");
            }

            return new NetworkPacket((CommandType)cmd, payload);
        }
    }
}
