namespace NosBarrage.Core.Packets;

[AttributeUsage(AttributeTargets.Class)]
public class PacketHandlerAttribute : Attribute
{
    public PacketHandlerAttribute(string packetName)
    {
        PacketName = packetName;
    }

    public string PacketName { get; set; }
}
