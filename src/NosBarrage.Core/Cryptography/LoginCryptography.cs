namespace NosBarrage.Core.Cryptography;

public static class LoginCryptography
{
    private const byte EncryptionKey = 0xC3;
    private const byte EncryptionOffset = 0xF;

    public static byte[] LoginEncrypt(byte[] packet)
    {
        List<byte> output = new();
        const byte EncryptionOffset = 0xF;

        if (packet[^1] != 0xA)
        {
            List<byte> packetList = packet.ToList();
            packetList.Add((byte)'\n');
            packet = packetList.ToArray();
        }

        for (int i = 0; i < packet.Length; i++)
        {
            byte b = packet[i];
            byte result = (byte)((b + EncryptionOffset) & 0xFF);
            output.Add(result);
        }

        return output.ToArray();
    }

    public static byte[] LoginDecrypt(ReadOnlySpan<byte> packet)
    {
        byte[] output = new byte[packet.Length];
        for (int i = 0; i < packet.Length; i++)
        {
            byte b = packet[i];
            output[i] = (byte)(((b - EncryptionOffset) ^ EncryptionKey) & 0xFF);
        }

        return output;
    }
}