using System.Collections.Generic;

public class EventManager : Singleton<EventManager>
{

    public delegate void event_handler(string event_name = null, object udata = null);

    private Dictionary<string, event_handler> eventDic = new Dictionary<string, event_handler>();

    public void AddListener(string event_name, event_handler h)
    {
        if (this.eventDic.ContainsKey(event_name))
        {
            this.eventDic[event_name] += h;
        }
        else
        {
            this.eventDic.Add(event_name, h);
        }
    }

    public void RemoveListener(string event_name, event_handler h)
    {
        if (!this.eventDic.ContainsKey(event_name))
        {
            return;
        }

        this.eventDic[event_name] -= h;

        if (this.eventDic[event_name] == null)
        {
            this.eventDic.Remove(event_name);
        }
    }

    public void DispatchEvent(string event_name, object udata)
    {
        if (!this.eventDic.ContainsKey(event_name))
        {
            return;
        }

        this.eventDic[event_name](event_name, udata);
    }









    public delegate void net_message_handler(CmdMsg msg);

    private Dictionary<int, net_message_handler> dic = new Dictionary<int, net_message_handler>();

    public void AddListener(int stype, net_message_handler h)
    {
        if (this.dic.ContainsKey(stype))
        {
            this.dic[stype] += h;
        }
        else
        {
            this.dic.Add(stype, h);
        }
    }

    public void RemoveListener(int stype, net_message_handler h)
    {
        if (!this.dic.ContainsKey(stype))
        {
            return;
        }

        this.dic[stype] -= h;

        if (this.dic[stype] == null)
        {
            this.dic.Remove(stype);
        }
    }

    public void Emit(CmdMsg msg)
    {
        if (!this.dic.ContainsKey(msg.stype))
        {
            return;
        }

        this.dic[msg.stype](msg);
    }



}
