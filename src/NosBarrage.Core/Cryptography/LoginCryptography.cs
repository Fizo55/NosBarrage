namespace NosBarrage.Core.Cryptography;

public class LoginCryptography
{
    public static byte[] LoginEncrypt(byte[] packet)
    {
        byte[] output;

        if (packet[^1] != 0xA)
            packet = packet.Concat("\n"u8.ToArray()).ToArray();

        output = new byte[packet.Length];
        for (int i = 0; i < packet.Length; i++)
        {
            byte b = packet[i];
            byte v = (byte)(b + 0xF);
            output[i] = (byte)(v & 0xFF);
        }

        return output;
    }

    public static byte[] LoginDecrypt(byte[] packet)
    {
        byte[] output = new byte[packet.Length];

        for (int i = 0; i < packet.Length; i++)
        {
            byte b = packet[i];
            byte v = (byte)((b - 0xF) ^ 0xC3);
            output[i] = (byte)(v & 0xFF);
        }

        return output;
    }
}