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
   
	// Use this for initialization
    
    void Awake()
    {
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
        Debug.Log("OnConnectedToMaster - Successful");
        PhotonNetwork.AuthValues = new AuthenticationValues();
        PhotonNetwork.AuthValues.UserId = randomuserID.ToString();
        userIDCount++;
        PhotonNetwork.NickName = PhotonNetwork.AuthValues.UserId;
        Debug.Log("Connected To Master");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("RoomName :" + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("Players in room :" + PhotonNetwork.CountOfPlayersInRooms);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {

        Debug.Log("Random Room Join Failed no available room");
        Debug.Log("Trying to Create a New Room");
        
       CreateRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {

        Debug.Log("Creating Room Failed");
        CreateRoom();    
    }
    
    public override void OnCreatedRoom()
    {
        Debug.Log("Room Created");
       base.OnCreatedRoom();
        roomNumber++;
    }

    public void OnCancelButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }


    void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = 2 };
        PhotonNetwork.CreateRoom("Room" + UnityEngine.Random.Range(1,3000), roomOptions);
    }
}
