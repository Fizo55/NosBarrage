namespace NosBarrage.Core.Packets;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PacketHandlerAttribute : Attribute
{
    public string PacketName { get; }
    public Type PacketType { get; }

    public PacketHandlerAttribute(string packetName, Type packetType)
    {
        PacketName = packetName;
        PacketType = packetType;
    }
}