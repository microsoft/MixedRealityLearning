using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Random = System.Random;

public class PhotonLobby : MonoBehaviourPunCallbacks
{
    public static PhotonLobby Lobby;
    private int roomNumber = 1;
    private int userIDCount = 0;

    void Awake()
    {
        if (PhotonLobby.Lobby == null)
        {
            PhotonLobby.Lobby = this;
        }
        else
        {
            if (PhotonLobby.Lobby != this)
            {
                Destroy(PhotonLobby.Lobby.gameObject);
                PhotonLobby.Lobby = this;
            }
        }
        DontDestroyOnLoad(this.gameObject);

        GenericNetworkManager.OnReadyToStartNetwork += StartNetwork;
    }

    public void StartNetwork()
    {
        PhotonNetwork.ConnectUsingSettings();
        Lobby = this;
    }

    public override void OnConnectedToMaster()
    {
        int randomuserID = UnityEngine.Random.Range(0, 999999);
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.AuthValues = new AuthenticationValues();
        PhotonNetwork.AuthValues.UserId = randomuserID.ToString();
        userIDCount++;
        PhotonNetwork.NickName = PhotonNetwork.AuthValues.UserId;
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        Debug.Log("\nPhotonLobby.OnJoinedRoom()");
        Debug.Log("Current room name: " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("Other players in room: " + PhotonNetwork.CountOfPlayersInRooms);
        Debug.Log("Total players in room: " + (PhotonNetwork.CountOfPlayersInRooms + 1));
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("\nPhotonLobby.OnCreateRoomFailed()");
        Debug.LogError("Creating Room Failed");
        CreateRoom();
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        roomNumber++;
    }

    public void OnCancelButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = 10 };
        PhotonNetwork.CreateRoom("Room" + UnityEngine.Random.Range(1, 3000), roomOptions);
    }
}
