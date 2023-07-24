using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Singleton<T> where T : new()
{
    private static T _instance;
    private static object mutex = new object();
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }
}



public class UnitySingleton<T> : MonoBehaviour
where T : Component
{
    private static T _instance = null;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType(typeof(T)) as T;
                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    _instance = (T)obj.AddComponent(typeof(T));
                    obj.hideFlags = HideFlags.DontSave;
                    // obj.hideFlags = HideFlags.HideAndDontSave;
                    obj.name = typeof(T).Name;
                }
            }
            return _instance;
        }
    }

    public virtual void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        if (_instance == null)
        {
            _instance = this as T;
        }
        else
        {
            GameObject.Destroy(this.gameObject);
        }
        InitStypeListeners();
    }

    #region Stype And Ctype Handler
    public int stype;
    public delegate void ctype_handler(CmdMsg msg);
    protected Dictionary<int, ctype_handler> ctype_listeners;

    protected void InitStypeListeners()
    {
        string className = GetType().ToString();
        foreach (KeyValuePair<int, System.Type> pair in CommonParams.Instance.dic)
        {
            if (pair.Value.Name.Equals(className))
            {
                stype = pair.Key;
                EventManager.Instance.AddListener(pair.Key, NetMessageHandler);
                break;
            }
        }
        AddStypeListeners();
    }

    //handle client's different request
    protected void NetMessageHandler(CmdMsg msg)
    {
        if (stype != msg.stype)
            return;
        if (ctype_listeners == null)
            return;
        if (!ctype_listeners.ContainsKey(msg.ctype))
            return;
        ctype_listeners[msg.ctype](msg);
    }

    protected virtual void AddStypeListeners() { }
    #endregion
}
