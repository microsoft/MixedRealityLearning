using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GenericNetSync : MonoBehaviourPun, IPunObservable
{
    public bool isUser;

    public Vector3 startingLocalPosition;
    public Quaternion startingLocalRotation;
    public Vector3 startingScale;

    private Vector3 networkLocalPosition;
    private Quaternion networkLocalRotation;
    private Vector3 networkLocalScale;

    private PhotonView PV;
    private Camera mainCamera;

    void Start()
    {
        PV = GetComponent<PhotonView>();
        mainCamera = Camera.main;

        if (isUser)
        {
            if (TableAnchor.instance != null)
            {
                transform.parent = FindObjectOfType<TableAnchor>().transform;
            }

            if (PV.IsMine)
            {
                GenericNetworkManager.instance.localUser = PV;
            }
        }

        startingLocalPosition = transform.localPosition;
        startingLocalRotation = transform.localRotation;
        startingScale = transform.localScale;

        networkLocalPosition = startingLocalPosition;
        networkLocalRotation = startingLocalRotation;
        networkLocalScale = startingScale;
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.localPosition);
            stream.SendNext(transform.localRotation);
        }
        else
        {
            networkLocalPosition = (Vector3)stream.ReceiveNext();
            networkLocalRotation = (Quaternion)stream.ReceiveNext();
        }
    }

    void FixedUpdate()
    {
        if (!PV.IsMine)
        {
            transform.localPosition = networkLocalPosition;
            transform.localRotation = networkLocalRotation;
        }

        if (PV.IsMine && isUser)
        {
            transform.position = mainCamera.transform.position;
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}
