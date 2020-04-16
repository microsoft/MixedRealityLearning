using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PhotonUser : MonoBehaviour
{
    private PhotonView PV;
    private string username;

    void Start()
    {
        PV = GetComponent<PhotonView>();

        if (PV.IsMine)
        {
            username = "User" + PhotonNetwork.NickName;
            PV.RPC("RPC_SetNickName", RpcTarget.AllBuffered, username);
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
        Debug.Log("\nPhotonRoom.RPC_SetSharedAnchorID()");
        Debug.Log("GenericNetworkManager.AzureAnchorID: " + GenericNetworkManager.instance.AzureAnchorID);
    }

    public void PVShareAnchorNetwork()
    {
        DebugWindowMessaging.Clear();
        Debug.Log("\nPhotonRoom.PVShareAnchorNetwork()");
        Debug.Log("GenericNetworkManager.AzureAnchorID: " + GenericNetworkManager.instance.AzureAnchorID);
        if (PV != null)
        {
            PV.RPC("RPC_SetSharedAnchorID", RpcTarget.AllBuffered, GenericNetworkManager.instance.AzureAnchorID);
            Debug.Log("Azure Anchor ID shared by user: " + PV.Controller.UserId);
        }
        else
        {
            Debug.LogError("PV is null");
        }
    }
}
