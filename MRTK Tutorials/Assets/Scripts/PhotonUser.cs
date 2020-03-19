using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PhotonUser : MonoBehaviour
{
    private PhotonView PV;

    private string username;
    // Start is called before the first frame update
    void Start()
    {
        PV = GetComponent<PhotonView>();

        if (PV.IsMine)
        {
           username = "User" + PhotonNetwork.NickName;
           PV.RPC("RPC_SetNickName",RpcTarget.AllBuffered, username);
        }
    }

    [PunRPC]
    void RPC_SetNickName(string nName)
    {
        gameObject.name = nName;
    }

    [PunRPC]
    void RPC_SetSharedAnchorID(string anchorID)
    {
        GenericNetworkManager.instance.AzureAnchorID = anchorID;
        Debug.Log("RPC_SetSharedAnchorID RPC - AzureAnchorID" + GenericNetworkManager.instance.AzureAnchorID);
    }

    public void PVShareAnchorNetwork()
    {
        DebugWindowMessaging.Clear();
        Debug.Log("ShareAnchorNetwork RPC - AzureAnchorID" + GenericNetworkManager.instance.AzureAnchorID);
        if (PV != null)
        {
            PV.RPC("RPC_SetSharedAnchorID", RpcTarget.AllBuffered, GenericNetworkManager.instance.AzureAnchorID);
            Debug.Log("AzureAnchorID user " + " " + PV.Controller.UserId);
        }
        else
        {
            Debug.Log("PV is null");
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
