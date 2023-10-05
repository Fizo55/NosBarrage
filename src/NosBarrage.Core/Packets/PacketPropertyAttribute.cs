namespace NosBarrage.Core.Packets;

[AttributeUsage(AttributeTargets.Property)]
public class PacketPropertyAttribute : Attribute
{
    public int Order { get; }

    public PacketPropertyAttribute(int order)
    {
        Order = order;
    }
}
