using System.Collections.Generic;
using System;

public class CommonParams : Singleton<CommonParams>
{
    public string IPAddr = "192.168.110.191";
    public readonly int port = 10086;

    public readonly bool isCloseWSLOnDestroy = true;

    public readonly Dictionary<int, Type> dic = new Dictionary<int, Type>()
    {
        { 1, typeof(ServerManager)},
        { 2, typeof(WSLManager)},
        { 3, typeof(UIManager)},
    };
    public long GetTimeSpan()
    {
        TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
        return Convert.ToInt64(timeSpan.TotalSeconds);
    }

}
