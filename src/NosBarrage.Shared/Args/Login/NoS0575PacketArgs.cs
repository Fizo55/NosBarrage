namespace NosBarrage.Shared.Args.Login
{
    public class NoS0575PacketArgs
    {
        public NoS0575PacketArgs(string sessionId, string name)
        {
            SessionId = sessionId;
            Name = name;
        }

        public string SessionId { get; set; }

        public string Name { get; set; }

        // todo : other arguments, just a test
    }
}
