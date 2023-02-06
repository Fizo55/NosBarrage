namespace NosBarrage.Shared.Args.Login
{
    public class NoS0575PacketArgs
    {
        public NoS0575PacketArgs(int sessionId, string? name, string? password, string? clientId, string? unknown, string? regionType, string? clientVersion, string? md5String)
        {
            SessionId = sessionId;
            Name = name;
            Password = password;
            ClientId = clientId;
            Unknown = unknown;
            RegionType = regionType;
            ClientVersion = clientVersion;
            Md5String = md5String;
        }

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
