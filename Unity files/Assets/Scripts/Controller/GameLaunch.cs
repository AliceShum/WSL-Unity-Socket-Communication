using System;
using System.Collections.Generic;
using UnityEngine;

public class GameLaunch : MonoBehaviour
{

    void Start()
    {
        GameObject newObj = new GameObject("StypeManagers");
        foreach (KeyValuePair<int, Type> pair in CommonParams.Instance.dic)
        {
            newObj.AddComponent(pair.Value);
        }
    }

}
