using System;
using System.Text;

public class CmdPacker
{
    private const int headerSize = 8; // 2 stype, 2 ctype, 4utag, msg--> body;

    public static byte[] PackJsonCmd(int stype, int ctype, string jsonMsg)
    {
        int cmdLen = headerSize;
        byte[] cmdBody = null;

        if (jsonMsg.Length > 0)
        { // utf8
            cmdBody = Encoding.UTF8.GetBytes(jsonMsg);
            cmdLen += cmdBody.Length;
        }

        byte[] cmd = new byte[cmdLen];
        // stype, ctype, utag(4 for space reservation), cmd_body
        DataViewer.WriteUshortLe(cmd, 0, (ushort)stype);
        DataViewer.WriteUshortLe(cmd, 2, (ushort)ctype);
        if (cmdBody != null)
        {
            DataViewer.WriteBytes(cmd, headerSize, cmdBody);
        }
        return cmd;
    }

    public static byte[] PackBufferCmd(int stype, int ctype, byte[] cmdBody)
    {
        int cmdLen = headerSize;

        cmdLen += cmdBody.Length;

        byte[] cmd = new byte[cmdLen];
        // stype, ctype, utag(4 for space reservation), cmd_body
        DataViewer.WriteUshortLe(cmd, 0, (ushort)stype);
        DataViewer.WriteUshortLe(cmd, 2, (ushort)ctype);
        if (cmdBody != null)
        {
            DataViewer.WriteBytes(cmd, headerSize, cmdBody);
        }
        return cmd;
    }

    public static bool UnpackCmdMsg(byte[] data, int start, int cmdLen, out CmdMsg msg)
    {
        msg = new CmdMsg();
        msg.stype = DataViewer.ReadUshortLe(data, start);
        msg.ctype = DataViewer.ReadUshortLe(data, start + 2);

        int bodyLen = cmdLen - headerSize;
        msg.body = new byte[bodyLen];
        Array.Copy(data, start + headerSize, msg.body, 0, bodyLen);

        return true;
    }

}
