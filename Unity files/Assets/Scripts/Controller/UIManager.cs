using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : UnitySingleton<UIManager>
{
    Text msgReceiveText;
    string toSendStr = ""; // the string that's about to send to the client

    private void Start()
    {
        Init();
    }

    void Init()
    {
        Transform canvas = FindObjectOfType<Canvas>().transform;
        if (canvas == null)
            return;
        InputField msgInputField = canvas.Find("inputField").GetComponent<InputField>();
        msgInputField.onEndEdit.AddListener(OnMsgInputFieldEndEdit);
        Button sendBtn = canvas.Find("sendBtn").GetComponent<Button>();
        sendBtn.onClick.AddListener(OnSendBtnClick);
        msgReceiveText = canvas.Find("receiveTxt").GetComponent<Text>();
        msgReceiveText.text = "";
    }

    void OnMsgInputFieldEndEdit(string value)
    {
        toSendStr = value;
    }

    //点击了发送按钮
    void OnSendBtnClick()
    {
        if (!string.IsNullOrEmpty(toSendStr))
        {
            EventManager.Instance.DispatchEvent("Send Msg To WSL", Encoding.UTF8.GetBytes(toSendStr));
        }
    }

    #region handle the request sent from client
    protected override void AddStypeListeners()
    {
        ctype_listeners = new Dictionary<int, ctype_handler>
        {
            {1,  OnReceiveServerTxt},
        };
    }

    //收到客户端发来的文字消息，直接显示
    public void OnReceiveServerTxt(CmdMsg msg)
    {
        string content = Encoding.UTF8.GetString(msg.body);
        msgReceiveText.text = "Receive from client: " + content;
    }

    #endregion

}
