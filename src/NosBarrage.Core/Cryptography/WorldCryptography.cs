namespace NosBarrage.Core.Cryptography;

public static class WorldCryptography
{
    private static readonly byte[] _encryptionTable = new byte[] { 0x00, 0x20, 0x2D, 0x2E, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0xFF, 0x00 };
    private static readonly byte[] _decryptionTable = new byte[] { 0x00, 0x20, 0x2D, 0x2E, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x0A, 0x00 };

    private static bool GetMaskPart(byte ch, byte[] charset)
    {
        if (ch == 0)
            return false;

        return Array.Exists(charset, element => element == ch);
    }

    private static List<bool> GetMask(byte[] packet, byte[] charset)
    {
        var output = new List<bool>();

        foreach (var ch in packet)
        {
            if (ch == 0x0)
                break;

            output.Add(GetMaskPart(ch, charset));
        }

        return output;
    }

    private static int CalcLenOfMask(int start, List<bool> mask, bool value)
    {
        int currentLen = 0;
        for (int i = start; i < mask.Count; i++)
        {
            if (mask[i] == value)
                currentLen++;
            else
                break;
        }

        return currentLen;
    }

    private static byte[] Pack(byte[] packet, byte[] charsToPack)
    {
        var output = new List<byte>();
        var mask = GetMask(packet, charsToPack);
        int pos = 0;

        while (mask.Count > pos)
        {
            int currentChunkLen = CalcLenOfMask(pos, mask, false);

            for (int i = 0; i < currentChunkLen; i++)
            {
                if (pos > mask.Count)
                    break;

                if (i % 0x7E == 0)
                    output.Add((byte)Math.Min(currentChunkLen - i, 0x7E));

                output.Add((byte)(packet[pos] ^ 0xFF));
                pos++;
            }

            currentChunkLen = CalcLenOfMask(pos, mask, true);

            for (int i = 0; i < currentChunkLen; i++)
            {
                if (pos > mask.Count)
                    break;

                if (i % 0x7E == 0)
                    output.Add((byte)(Math.Min(currentChunkLen - i, 0x7E) | 0x80));

                int currentValue = Array.IndexOf(charsToPack, packet[pos]);

                if (i % 2 == 0)
                    output.Add((byte)(currentValue << 4));
                else
                    output[^1] |= (byte)currentValue;

                pos++;
            }
        }

        output.Add(0xFF);
        return output.ToArray();
    }

    private static byte[] Unpack(byte[] packet, byte[] charsToUnpack)
    {
        var output = new List<byte>();
        int pos = 0;

        while (packet.Length > pos)
        {
            if (packet[pos] == 0xFF)
            {
                break;
            }

            int currentChunkLen = packet[pos] & 0x7F;
            bool isPacked = (packet[pos] & 0x80) != 0;
            pos++;

            if (isPacked)
            {
                for (int i = 0; i < Math.Ceiling(currentChunkLen / 2.0); i++)
                {
                    if (pos >= packet.Length)
                        break;

                    byte twoChars = packet[pos];
                    pos++;

                    byte leftChar = (byte)(twoChars >> 4);
                    output.Add(charsToUnpack[leftChar]);

                    byte rightChar = (byte)(twoChars & 0xF);
                    if (rightChar == 0)
                        break;

                    output.Add(charsToUnpack[rightChar]);
                }
            }
            else
            {
                for (int i = 0; i < currentChunkLen; i++)
                {
                    if (pos >= packet.Length)
                        break;

                    output.Add((byte)(packet[pos] ^ 0xFF));
                    pos++;
                }
            }
        }

        return output.ToArray();
    }

    public static byte[] WorldEncrypt(byte[] packet, int session, bool isFirstPacket = false)
    {
        byte[] packed = Pack(packet, _encryptionTable);
        return WorldXor(packed, session, isFirstPacket);
    }

    public static byte[] WorldDecrypt(byte[] packet)
    {
        return Unpack(packet, _decryptionTable);
    }

    private static byte[] WorldXor(byte[] packet, int session, bool isFirstPacket = false)
    {
        byte[] output = new byte[packet.Length];

        int stype = !isFirstPacket ? (session >> 6) & 3 : -1;
        int key = session;

        for (int i = 0; i < packet.Length; i++)
        {
            if (stype == 0)
                output[i] = (byte)((packet[i] + key + 0x40) & 0xFF);
            else if (stype == 1)
                output[i] = (byte)((packet[i] - key - 0x40) & 0xFF);
            else if (stype == 2)
                output[i] = (byte)(((packet[i] ^ 0xC3) + key + 0x40) & 0xFF);
            else if (stype == 3)
                output[i] = (byte)(((packet[i] ^ 0xC3) - key - 0x40) & 0xFF);
            else
                output[i] = (byte)((packet[i] + 0xF) & 0xFF);
        }

        return output;
    }
}