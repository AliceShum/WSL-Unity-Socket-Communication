using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading;

public class ServerManager : UnitySingleton<ServerManager>
{
    Socket listenfd;
    public Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>(); //clients and their ClientState 
    public List<Socket> checkRead = new List<Socket>();

    public int pingInterval = 30;
    public bool isUsePing = false;

    Thread t;

    Queue<CmdMsg> clientRequestQueue = new Queue<CmdMsg>(); //queue for clients' request

    [HideInInspector] public bool server_started = false; //if server starts?

    private void Start()
    {
        CommonParams.Instance.IPAddr = GetLocalIPAddress();
        //开启socket服务器线程
        t = new Thread(new ThreadStart(StartLoop));
        t.Start();
    }

    #region Socket Start

    /// <summary>
    /// start server
    /// </summary>
    /// <param name="port"></param>
    public void StartLoop()
    {
        listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ip = IPAddress.Parse(CommonParams.Instance.IPAddr); // "127.0.0.1" --> WSL cannot connect to this address;
        IPEndPoint endPoint = new IPEndPoint(ip, CommonParams.Instance.port);
        listenfd.Bind(endPoint);
        listenfd.Listen(0);
        Debug.Log("Server starts:" + DateTime.Now.ToString());
        AfterServerStarted();
        while (true)
        {
            ResetCheckRead();
            Socket.Select(checkRead, null, null, 1000);
            for (int i = checkRead.Count - 1; i >= 0; i--)
            {
                Socket item = checkRead[i];
                if (item == listenfd)
                {
                    //The server receives a request for connection
                    ReadListenfd(listenfd);
                }
                else
                {
                    //Message comes from the client
                    ReadClientfd(item);
                }
            }
            CheckConnection();
        }

    }

    public void ResetCheckRead()
    {
        checkRead.Clear();
        checkRead.Add(listenfd);
        foreach (ClientState item in clients.Values)
        {
            checkRead.Add(item.Socket);
        }
    }

    public void ReadListenfd(Socket listenfd)
    {
        try
        {
            Socket clientfd = listenfd.Accept();
            Debug.Log("A new client connects:" + clientfd.RemoteEndPoint.ToString());
            ClientState state = new ClientState();
            state.Socket = clientfd;
            state.lastPingTime = CommonParams.Instance.GetTimeSpan();
            clients.Add(clientfd, state);
        }
        catch (Exception ex)
        {
            Debug.Log("Accept Fail:" + ex.ToString());
        }
    }

    public void ReadClientfd(Socket clientfd)
    {
        ClientState state = clients[clientfd];
        try
        {
            state.ThreadRecvWorker(clientfd);
        }
        catch (SocketException ex)
        {
            Debug.LogError(ex.ToString());
            Close(state);
        }
    }

    void AfterServerStarted()
    {
        server_started = true;
        EventManager.Instance.DispatchEvent("Server Started", null);
    }

    /// <summary>
    /// check if client cuts connetion
    /// </summary>
    private void CheckConnection()
    {
        if (!isUsePing)
        {
            return;
        }
        long nowts = CommonParams.Instance.GetTimeSpan();
        foreach (ClientState item in clients.Values)
        {
            if (nowts - item.lastPingTime > pingInterval * 4)
            {
                Debug.Log("Connection Cut:" + item.Socket.RemoteEndPoint.ToString());
                //Close(item); 
                return;
            }
        }
    }

    /// <summary>
    /// close a client connection
    /// </summary>
    public void Close(ClientState state)
    {
        state.Close();
        clients.Remove(state.Socket);
    }

    #endregion

    #region Send Message To Client

    /// <summary>
    /// send message to client
    /// </summary>
    /// <param name="state"></param>
    /// <param name="msg"></param>
    public void SendMsg(ClientState state, byte[] tcpPkg)
    {
        state.AddToSendMsgQueue(tcpPkg);
    }

    public byte[] GetJsonPackage(int stype, int ctype, string jsonBody)
    {
        byte[] cmdData = CmdPacker.PackJsonCmd(stype, ctype, jsonBody);
        if (cmdData == null)
        {
            return null;
        }

        byte[] tcpPkg = TcpPacker.PackUint(cmdData);
        return tcpPkg;
    }

    public byte[] GetBufferPackage(int stype, int ctype, byte[] body)
    {
        byte[] cmdData = CmdPacker.PackBufferCmd(stype, ctype, body);
        if (cmdData == null)
        {
            return null;
        }
        byte[] tcpPkg = TcpPacker.PackUint(cmdData);
        return tcpPkg;
    }

    private void OnSendCallBack(IAsyncResult asyncResult)
    {
        Socket socket = asyncResult.AsyncState as Socket;
        ClientState state = clients[socket];
        try
        {
            int count = socket.EndSend(asyncResult);
            byte[] ba = state.sendQueue.Peek();
            if (count == ba.Length)
            {
                lock (state.sendQueue)
                {
                    state.sendQueue.Dequeue();
                    ba = state.sendQueue.Count > 0 ? state.sendQueue.Peek() : null;
                }
            }
            if (ba != null)
            {
                socket.BeginSend(ba, 0, ba.Length, 0, OnSendCallBack, socket);
            }
            else
            {
                state.isSending = false;
            }
        }
        catch (SocketException ex)
        {
            Debug.Log("OnSendCallBack Fail:" + ex.ToString());
        }

    }

    #endregion

    #region Server Closing

    private void OnDestroy()
    {
        if (t != null)
        {
            if (listenfd != null && listenfd.Connected)
            {
                listenfd.Shutdown(SocketShutdown.Both);
                listenfd.Dispose();
                listenfd.Close();
            }
            t.Abort();
        }

        if (CommonParams.Instance.isCloseWSLOnDestroy)
            CloseWSL();
    }

    //call cmd to close WSL, or it runs int the background
    void CloseWSL()
    {
        string cmd = "wsl --shutdown";
        cmd = cmd.Trim().TrimEnd('&') + "&exit";

        using (System.Diagnostics.Process p = new System.Diagnostics.Process())
        {
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = false;
            //p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;  
            p.Start();


            p.StandardInput.WriteLine(cmd);
            p.StandardInput.AutoFlush = true;

            p.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            p.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

            print(p.StandardOutput.CurrentEncoding);


            System.IO.StreamReader reader = p.StandardOutput;


            string line = reader.ReadLine();


            print(line + "\n");

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                //print(line + "\n");
            }

            p.WaitForExit();
            p.Close();

        }

    }

    #endregion

    //add client's request to queue
    public void AddToClientRequestQueue(CmdMsg msg)
    {
        lock (clientRequestQueue)
        {
            clientRequestQueue.Enqueue(msg);
        }
    }

    //handle client's request in the main thread
    private void Update()
    {
        if (clientRequestQueue.Count <= 0)
            return;

        lock (clientRequestQueue)
        {
            CmdMsg msg = clientRequestQueue.Dequeue();
            Debug.Log("Client's end point：" + msg.endPoint + ". Stype：" + msg.stype + ". Ctype:" + msg.ctype);
            EventManager.Instance.Emit(msg);
        }
    }

    //get this ip address automatically
    public string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Debug.Log(ip.ToString());
                return ip.ToString();
            }
        }

        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }

}

