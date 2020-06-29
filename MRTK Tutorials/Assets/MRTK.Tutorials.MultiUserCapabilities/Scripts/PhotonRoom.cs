using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonRoom : MonoBehaviourPunCallbacks, IInRoomCallbacks
{
    public static PhotonRoom room;

    PhotonView PV;
    Player[] photonPlayers;
    int playersInRoom;
    int myNumberInRoom;

    [SerializeField]
    GameObject photonUserPrefab = default;
    [SerializeField]
    GameObject roverExplorerPrefab = default;
    [SerializeField]
    Transform roverExplorerLocation = default;

    private GameObject module;
    private Vector3 moduleLocation = Vector3.zero;

    void Awake()
    {
        if (PhotonRoom.room == null)
        {
            PhotonRoom.room = this;
        }
        else
        {
            if (PhotonRoom.room != this)
            {
                Destroy(PhotonRoom.room.gameObject);
                PhotonRoom.room = this;
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void Start()
    {
        PV = GetComponent<PhotonView>();

        // Allow prefabs not in a Resources folder
        if (PhotonNetwork.PrefabPool is DefaultPool pool)
        {
            if (photonUserPrefab != null)
            {
                pool.ResourceCache.Add(photonUserPrefab.name, photonUserPrefab);
            }
            if (roverExplorerPrefab != null)
            {
                pool.ResourceCache.Add(roverExplorerPrefab.name, roverExplorerPrefab);
            }
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        photonPlayers = PhotonNetwork.PlayerList;
        playersInRoom = photonPlayers.Length;
        myNumberInRoom = playersInRoom;
        PhotonNetwork.NickName = myNumberInRoom.ToString();

        StartGame();
    }


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        photonPlayers = PhotonNetwork.PlayerList;
        playersInRoom++;
        //CreatPlayer();
    }


    void CreatPlayer()
    {
        GameObject player = PhotonNetwork.Instantiate(photonUserPrefab.name, Vector3.zero, Quaternion.identity);
    }


    void StartGame()
    {
        CreatPlayer();

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (TableAnchor.instance != null)
        {
            CreateInteractableObjects();
        }
    }

    void CreateInteractableObjects()
    {
        var position = roverExplorerLocation.position;
        var positionOnTopOfSurface = new Vector3(position.x, position.y + (roverExplorerLocation.localScale.y / 2), position.z);
        
        GameObject go = PhotonNetwork.Instantiate(roverExplorerPrefab.name, positionOnTopOfSurface, roverExplorerLocation.rotation);
    }

    private void CreateMainLunarModule()
    {
        module = PhotonNetwork.Instantiate(roverExplorerPrefab.name, Vector3.zero, Quaternion.identity);
        PV.RPC("Rpc_SetModuleParent", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void Rpc_SetModuleParent()
    {
        Debug.Log("Rpc_SetModuleParent- RPC Called");
        module.transform.parent = TableAnchor.instance.transform;
        module.transform.localPosition = moduleLocation;
    }
}
