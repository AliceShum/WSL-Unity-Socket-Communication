using System;

public class DataViewer
{
    public static void WriteUshortLe(byte[] buf, int offset, ushort value)
    {
        // value ---> byte[];
        byte[] byteValue = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(byteValue);
        }

        Array.Copy(byteValue, 0, buf, offset, byteValue.Length);
    }

    public static void WriteUintLe(byte[] buf, int offset, uint value)
    {
        // value ---> byte[];
        byte[] byteValue = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(byteValue);
        }

        Array.Copy(byteValue, 0, buf, offset, byteValue.Length);
    }

    public static void WriteBytes(byte[] dst, int offset, byte[] value)
    {
        Array.Copy(value, 0, dst, offset, value.Length);
    }

    public static ushort ReadUshortLe(byte[] data, int offset)
    {
        int ret = (data[offset] | (data[offset + 1] << 8));

        return (ushort)ret;
    }
}
