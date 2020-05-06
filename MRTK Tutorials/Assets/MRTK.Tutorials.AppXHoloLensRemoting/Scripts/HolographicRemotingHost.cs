using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA;

public class HolographicRemotingHost: MonoBehaviour
{
    [SerializeField]
    private string IP;

    private bool connected = false;

    private void Start()
    {
        StartCoroutine(LoadingWindowsMrWrapper());
    }

    private IEnumerator LoadingWindowsMrWrapper()
    {
        yield return new WaitForSeconds(1);
        StartCoroutine(LoadDevice("WindowsMR"));
    }

    public void Connect()
    {
        if (HolographicRemoting.ConnectionState != HolographicStreamerConnectionState.Connected)
        {
            HolographicRemoting.Connect(IP); //Uncomment this line for HoloLens2
            //HolographicRemoting.Connect(IP,99999,RemoteDeviceVersion.V2); //Uncomment this line for HoloLens2
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

   private static IEnumerator LoadDevice(string newDevice)
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