using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class OwnershipHandler : MonoBehaviourPun, IPunOwnershipCallbacks, IMixedRealityInputHandler
{
    private PhotonView PV;

    void Start()
    {
        PV = GetComponent<PhotonView>();
    }

    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        targetView.TransferOwnership(requestingPlayer);
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
    }

    void TransferControl(Player idPlayer)
    {
        if (PV.IsMine)
        {
            PV.TransferOwnership(idPlayer);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (PV != null)
        {
            this.PV.RequestOwnership();
        }
    }

    private void OnTriggerExit(Collider other)
    {
    }

    public void OnInputUp(InputEventData eventData)
    {
    }

    public void OnInputDown(InputEventData eventData)
    {
        this.PV.RequestOwnership();
    }

    public void RequestOwnership()
    {
        this.PV.RequestOwnership();
    }
}
