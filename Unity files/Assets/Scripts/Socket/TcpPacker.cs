using System;

public class TcpPacker
{
    private const int headerSize = 2;

    //add overall package size before the package contents (unsigned short ranges from 0 to 65535)
    public static byte[] Pack(byte[] cmdData)
    {
        int len = cmdData.Length;
        if (len > 65535 - 2)
        {
            return null;
        }

        int cmdLen = len + headerSize;
        byte[] cmd = new byte[cmdLen];
        DataViewer.WriteUshortLe(cmd, 0, (ushort)cmdLen);
        DataViewer.WriteBytes(cmd, headerSize, cmdData);

        return cmd;
    }

    //add overall package size before the package contents(uint)
    public static byte[] PackUint(byte[] cmdData)
    {
        int headerSize = 4; // uint: 4 bytes

        int len = cmdData.Length;

        int cmdLen = len + headerSize;
        byte[] cmd = new byte[cmdLen];
        DataViewer.WriteUintLe(cmd, 0, (uint)cmdLen);
        DataViewer.WriteBytes(cmd, headerSize, cmdData);

        return cmd;
    }

    //read head size of a package（ushort）
    public static bool ReadHeader(byte[] data, int dataLen, out int pkgSize, out int headSize)
    {
        pkgSize = 0;
        headSize = 0;

        if (dataLen < 2)
        {
            return false;
        }

        headSize = 2;
        pkgSize = (data[0] | (data[1] << 8));

        return true;
    }

    /// <summary>
    /// read head size of a package（uint）
    /// </summary>
    /// <param name="data">the bytes received from socket</param>
    /// <param name="dataLen">the length of the bytes received</param>
    /// <param name="pkgSize"></param>
    /// <param name="headSize"></param>
    /// <returns></returns>
    public static bool ReadHeaderUint(byte[] data, int dataLen, out int pkgSize, out int headSize)
    {
        pkgSize = 0; //the real content size in the bytes received
        headSize = 0; //header size   uint: 4bytes

        if (dataLen < 4)
        {
            return false; //something is wrong
        }

        headSize = 4;

        byte[] pkgSizeBuffer = new byte[4];
        Array.Copy(data, 0, pkgSizeBuffer, 0, 4);
        pkgSize = BitConverter.ToInt32(pkgSizeBuffer, 0);

        /*if (pkg_size < data_len)
        {
            // 粘包
        }

        if (data_len != pkg_size)
            return false;    //不完整的包，分包*/

        return true;
    }
}
