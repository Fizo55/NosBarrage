namespace NosBarrage.Shared.Args.Login
{
    public class NoS0575PacketArgs
    {
        public int SessionId { get; set; }

        public string? Name { get; set; }

        public string? Password { get; set; }

        public string? ClientId { get; set; }

        public string? Unknown { get; set; }

        public string? RegionType { get; set; }

        public string? ClientVersion { get; set; }

        public string? Md5String { get; set; }
    }
}
