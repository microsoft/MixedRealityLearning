using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugWindowMessaging : MonoBehaviour
{
    private static DebugWindowMessaging debugWindow;

    public TextMeshPro debugText;

    public bool _debugWindowEnabled = false;
    private int lineCount = 0;

    private bool parentWindow;

    void Awake()
    {
        if (debugWindow == null)
        {
            debugWindow = this;
        }
        else
        {
            //Destroy(this.gameObject);
        }

        Application.logMessageReceived += HandleLog;

    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Write(string message)
    {
        if (!_debugWindowEnabled)
        {
            return;
        }

        if (lineCount >= 20)
        {
            debugText.text = "";
            lineCount = 0;
        }

        debugText.text += message + " \n";
        lineCount++;
    }


    void HandleLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Error)
        {
            debugWindow.debugText.GetComponent<Renderer>().material.color = Color.red;
        }
        debugWindow.Write(message);
        debugWindow.debugText.GetComponent<Renderer>().material.color = Color.white;
    }

    public static void Clear()
    {
        debugWindow.debugText.text = "";
    }

    //public static void AddDebugMessage(string message)
    //{
    //    // nothing in the scene is receiving debug messages
    //    if (debugWindow == null)
    //        return;

    //    debugWindow.Write(message);

    //}
}
