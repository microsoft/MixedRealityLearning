using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA;

public class HolographicRemoteConnect : MonoBehaviour
{

    [SerializeField]
    private string IP;

    private bool connected = false;

    public void Connect()
    {
        if (HolographicRemoting.ConnectionState != HolographicStreamerConnectionState.Connected)
        {
            //HolographicRemoting.Connect(IP); //For HL1
            HolographicRemoting.Connect(IP, 99999, RemoteDeviceVersion.V2);

        }
    }

    void Update()
    {
        if (!connected && HolographicRemoting.ConnectionState == HolographicStreamerConnectionState.Connected)
        {
            connected = true;

            StartCoroutine(LoadDevice("WindowsMR"));
        }
    }

    IEnumerator LoadDevice(string newDevice)
    {
        XRSettings.LoadDeviceByName(newDevice);
        yield return null;
        XRSettings.enabled = true;
    }

    private void OnGUI()
    {
        IP = GUI.TextField(new Rect(10, 10, 200, 30), IP, 25);

        string button = (connected ? "Disconnect" : "Connect");

        if (GUI.Button(new Rect(220, 10, 100, 30), button))
        {
            if (connected)
            {
                HolographicRemoting.Disconnect();
                connected = false;
            }
            else
                Connect();
            Debug.Log(button);

        }

    }
}