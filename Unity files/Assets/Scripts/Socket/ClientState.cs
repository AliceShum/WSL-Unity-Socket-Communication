using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MessageFormat;
using UnityEngine;

public class ClientState
{
    private Socket socket;

    public int capacity = 8192;
    public byte[] recvBuf; //bytes received from client, its length is no larger than capacity 
    public int recved = 0; //length of bytes received from client
    private byte[] longPkg = null; //the package for receiving large package
    private int longPkgSize = 0;

    public Queue<byte[]> sendQueue; //queue for message to be send to client
    public bool isSending = false; //whether it is sending message to client?
    public long lastPingTime; //tiem that client last ping server

    public ClientPlatform platform = ClientPlatform.Unidentified; //record client's platform

    public ClientState()
    {
        recvBuf = new byte[capacity];
        sendQueue = new Queue<byte[]>();
    }

    public Socket Socket
    {
        set
        {
            socket = value;
            AddListenEvents();
        }
        get { return socket; }
    }

    void AddListenEvents()
    {
        EventManager.Instance.AddListener("Save Client Platform Info", OnReceiveClientInfo);
        EventManager.Instance.AddListener("Send Msg To WSL", SendMsgToWsl);
    }

    #region Receive Msg From Client

    //receive bytes from client
    public void ThreadRecvWorker(Socket clientfd)
    {
        int recv_len = 0;
        if (this.recved < capacity)
        {
            recv_len = clientfd.Receive(this.recvBuf, this.recved, capacity - this.recved, SocketFlags.None);
        }
        else //put received bytes to long_pkg
        {
            if (this.longPkg == null)
            {
                int pkg_size;
                int head_size;
                TcpPacker.ReadHeaderUint(this.recvBuf, this.recved, out pkg_size, out head_size);
                this.longPkgSize = pkg_size;
                this.longPkg = new byte[pkg_size];
                Array.Copy(this.recvBuf, 0, this.longPkg, 0, this.recved);
            }
            recv_len = clientfd.Receive(this.longPkg, this.recved, this.longPkgSize - this.recved, SocketFlags.None);
        }

        if (recv_len > 0)
        {
            this.recved += recv_len;
            this.OnRecvTcpData();
        }
        else
        {

        }
    }

    //gets message from received bytes
    public void OnRecvTcpData()
    {
        byte[] pkgData = (this.longPkg != null) ? this.longPkg : this.recvBuf;
        while (this.recved > 0)
        {
            int pkgSize = 0;
            int headSize = 0;

            if (!TcpPacker.ReadHeaderUint(pkgData, this.recved, out pkgSize, out headSize)) //error:cannot read head size of a package
            {
                break;
            }

            if (this.recved < pkgSize) //package is not a complete one 
            {
                break;
            }

            int rawDataStart = headSize;
            int rawDataLen = pkgSize - headSize;

            OnRecvCmd(pkgData, rawDataStart, rawDataLen);

            if (this.recved > pkgSize)
            {
                this.recvBuf = new byte[capacity];
                Array.Copy(pkgData, pkgSize, this.recvBuf, 0, this.recved - pkgSize);
                pkgData = this.recvBuf;
            }

            this.recved -= pkgSize;

            if (this.recved == 0 && this.longPkg != null)
            {
                this.longPkg = null;
                this.longPkgSize = 0;
            }
        }
    }

    public void OnRecvCmd(byte[] data, int start, int data_len)
    {
        CmdMsg msg;
        CmdPacker.UnpackCmdMsg(data, start, data_len, out msg);
        if (msg != null && Socket.Connected)
        {
            msg.endPoint = Socket.RemoteEndPoint.ToString();
            ServerManager.Instance.AddToClientRequestQueue(msg);
        }
    }

    #endregion

    #region Send Msg To Client

    //send message to client
    public void AddToSendMsgQueue(byte[] tcpPkg)
    {
        lock (sendQueue)
        {
            sendQueue.Enqueue(tcpPkg);
        }
        SendMsg();
    }

    private void OnSendCallBack(IAsyncResult asyncResult)
    {
        try
        {
            int count = Socket.EndSend(asyncResult);
            byte[] ba = sendQueue.Peek();
            if (count == ba.Length)
            {
                lock (sendQueue)
                {
                    sendQueue.Dequeue();
                }
            }
            Debug.Log("length of bytes sent ：" + ba.Length);
            isSending = false;
            SendMsg();
        }
        catch (SocketException ex)
        {
            Debug.Log("OnSendCallBack Fail:" + ex.ToString());
        }

    }

    private void SendMsg()
    {
        if (sendQueue == null || sendQueue.Count == 0)
        {
            return;
        }

        byte[] data = sendQueue.Peek();
        if (data == null || data.Length <= 0)
        {
            return;
        }

        //check if is sending 
        if (isSending)
        {
            return;
        }
        isSending = true;

        try
        {
            Socket.BeginSend(data, 0, data.Length, 0, OnSendCallBack, Socket);
        }
        catch (SocketException ex)
        {
            Debug.Log("SendMsg Fail:" + ex.ToString());
        }
    }

    //send message to client
    public void OnSendCmd(CmdMsg msg)
    {
        byte[] buffer = ServerManager.Instance.GetBufferPackage(msg.stype, msg.ctype, msg.body);
        ServerManager.Instance.SendMsg(this, buffer);
        Debug.Log("message sent to WSL:" + System.Text.Encoding.UTF8.GetString(buffer));
    }

    public void SendMsgToWsl(string event_name = null, object udata = null)
    {
        if (platform != ClientPlatform.WSL)
            return;
        CmdMsg msg = new CmdMsg();
        msg.stype = 100;
        msg.ctype = 1;
        msg.body = (byte[])udata;
        OnSendCmd(msg);
    }

    #endregion

    //client socket closes
    public void Close()
    {
        if (Socket == null) return;
        if (Socket.RemoteEndPoint == null) return;
        Socket.Close();
        Socket.Dispose();
    }

    //save client's platform info
    public void OnReceiveClientInfo(string event_name = null, object udata = null)
    {
        byte[] buffer = (byte[])udata;
        string json = System.Text.Encoding.UTF8.GetString(buffer);
        ClientInfo info = JsonUtility.FromJson(json, typeof(ClientInfo)) as ClientInfo;
        platform = info.platform;
        UnityEngine.Debug.Log("Client's info :" + json);

        //byte[] buffer1 = ServerManager.Instance.GetBufferPackage(9, 1, System.Text.Encoding.UTF8.GetBytes("Received Client's Platform Info"));
        //ServerManager.Instance.SendMsg(this, buffer1);
    }

}


