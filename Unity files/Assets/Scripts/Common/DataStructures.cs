using System;

[Serializable]
public class CmdMsg
{
    public int stype;
    public int ctype;
    public byte[] body; // the main content
    public string endPoint; //ip of client that sends this message from
}

namespace MessageFormat
{
    [Serializable]
    public class ClientInfo
    {
        public ClientPlatform platform;
        public string ipAddr; //client ip address
        public int port; //client ip port 

        public override string ToString()
        {
            return "Client platform:" + platform.ToString() + ".IP:" + ipAddr + ":" + port;
        }
    }

    [Serializable]
    public enum ClientPlatform
    {
        Unidentified = 0,
        WSL = 1,
        Android = 2,
    }

    [Serializable]
    public class WslResult
    {
        public string result;
    }

    [Serializable]
    //send the command to WSL
    public class CallWslCommand
    {
        public string command;
    }

}
