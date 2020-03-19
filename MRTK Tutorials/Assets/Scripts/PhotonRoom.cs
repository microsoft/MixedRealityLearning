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
    public PhotonView PV;
    public bool isRoomLoaded;
    public bool isRoomFull;


    private Player[] photonPlayers;
    public int playersInRoom;
    public int myNumberInRoom;


    public static event Action OnJoinedRoomEvent;

    // public GameObject Tableprefab;
    // public GameObject ModulePrefab;

    private GameObject table;
    private GameObject module;

    private string prefabName = "Rocket Launcher_Complete Variant";
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
        DontDestroyOnLoad(this.gameObject);
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
        GameObject player = PhotonNetwork.Instantiate(Path.Combine("Prefabs", "PhotonUser"), Vector3.zero, Quaternion.identity);
        player.transform.parent = Camera.main.transform;
    }


    void StartGame()
    {
        CreatPlayer();

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        // CreateInteractableObjects();
    }

    void CreateInteractableObjects()
    {
        GameObject go = PhotonNetwork.Instantiate(Path.Combine("Prefabs", prefabName), Vector3.zero, Quaternion.identity);
        go.transform.parent = TableAnchor.instance.transform;
        go.transform.localPosition = moduleLocation;
    }

    private void CreateMainLunarModule()
    {
        module = PhotonNetwork.Instantiate(Path.Combine("Prefabs", prefabName), Vector3.zero, Quaternion.identity);
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
