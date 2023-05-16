namespace NosBarrage.Core.Cryptography;

public static class LoginCryptography
{
    private const byte EncryptionKey = 0xC3;
    private const byte EncryptionOffset = 0xF;

    public static byte[] LoginEncrypt(ReadOnlySpan<byte> packet)
    {
        if (packet[^1] != 0xA)
            packet = packet.ToArray().Append((byte)'\n').ToArray();

        byte[] output = new byte[packet.Length];
        for (int i = 0; i < packet.Length; i++)
        {
            byte b = packet[i];
            output[i] = (byte)(((b ^ EncryptionKey) + EncryptionOffset) & 0xFF);
        }

        return output;
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