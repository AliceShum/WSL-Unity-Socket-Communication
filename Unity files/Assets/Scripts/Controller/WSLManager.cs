using System.Collections.Generic;
using System.Diagnostics;

public class WSLManager : UnitySingleton<WSLManager>
{
    protected Process currentProcess;

    private void Start()
    {
        EventManager.Instance.AddListener("Server Started", OpenWsl);
    }

    public void OpenWsl(string event_name = null, object udata = null)
    {
        currentProcess = new Process();
        currentProcess.StartInfo.FileName = "wsl.exe";
        currentProcess.StartInfo.Arguments = "expect -f /usr/bin/open_python.sh";
        currentProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
        currentProcess.Start();
    }

    private void OnDestroy()
    {
        if (currentProcess != null && !currentProcess.HasExited)
        {
            currentProcess.Kill();
            currentProcess.Dispose();
            currentProcess.Close();
            currentProcess = null;
        }
    }

    protected override void AddStypeListeners()
    {
        ctype_listeners = new Dictionary<int, ctype_handler>
        {
            {1, OnReceiveClientInfo},
        };
    }

    void OnReceiveClientInfo(CmdMsg msg)
    {
        EventManager.Instance.DispatchEvent("Save Client Platform Info", msg.body);
    }

}
