using NosBarrage.Core.Packets;

namespace NosBarrage.PacketHandlers.Args.Login
{
    public class NoS0575PacketArgs
    {
        [PacketProperty(1)]
        public int SessionId { get; set; }

        [PacketProperty(2)]
        public string? Name { get; set; }

        [PacketProperty(3)]
        public string? Password { get; set; }

        [PacketProperty(4)]
        public string? ClientId { get; set; }

        [PacketProperty(5)]
        public string? Unknown { get; set; }

        [PacketProperty(6)]
        public string? RegionType { get; set; }

        [PacketProperty(7)]
        public string? ClientVersion { get; set; }

        [PacketProperty(8)]
        public string? Md5String { get; set; }
    }
}
