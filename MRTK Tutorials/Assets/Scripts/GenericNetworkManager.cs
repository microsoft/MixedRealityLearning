using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GenericNetworkManager : MonoBehaviour
{
    public static GenericNetworkManager instance;

    public static event Action OnReadyToStartNetwork;
   // public static event Action OnNetworkStarted_CreatePlayer; 

    private bool isConnected;

    public PhotonView localUser;
    public string AzureAnchorID = "";

    void Awake()
    {
        if (GenericNetworkManager.instance == null)
        {
            GenericNetworkManager.instance = this;
        }
        else
        {
            if (GenericNetworkManager.instance != this)
            {
                Destroy(GenericNetworkManager.instance.gameObject);
                GenericNetworkManager.instance = this;
            }
        }
        Debug.Log("GNM Created");
        DontDestroyOnLoad(this.gameObject);

    }

    // Start is called before the first frame update
    void Start()
    {
        ConnectToNetwork();
    }

    //For non Photon Networking solutions
    void StartNetwork(string ipaddress, string port)
    {

    }

    void ConnectToNetwork()
    {
        OnReadyToStartNetwork?.Invoke();
        
    }

    void StopNetwork()
    {
        //Unnecessary for this
    }

   
}
