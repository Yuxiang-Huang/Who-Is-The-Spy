using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class NetworkManager: MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    [SerializeField] TMP_InputField roomNameInput;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListPrefab;

    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListPrefab;

    [SerializeField] GameObject startGameButton;

    private static Dictionary<string, RoomInfo> fullRoomList = new Dictionary<string, RoomInfo>();

    void Awake()
    {
        Instance = this;

        roomNameInput.text = "Room " + Random.Range(0, 1000).ToString("0000");
    }

    public void Start()
    {
        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Joined Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        ScreenManager.Instance.DisplayScreen("Main");
        Debug.Log("Joined Lobby");
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
            return;

        PhotonNetwork.CreateRoom(roomNameInput.text);
        ScreenManager.Instance.DisplayScreen("Loading");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed: " + errorText;
        ScreenManager.Instance.DisplayScreen("Error");
    }

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        ScreenManager.Instance.DisplayScreen("Loading");
    }

    public override void OnJoinedRoom()
    {
        ScreenManager.Instance.DisplayScreen("Room");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        //clear player list
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        //create players
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(playerListPrefab, playerListContent)
            .GetComponent<PlayerListItem>().SetUp(players[i]);
        }

        //Start Game Button only visible for the host
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        //Updates Game Button only visible for the host
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        ScreenManager.Instance.DisplayScreen("Loading");
    }

    public override void OnLeftRoom()
    {
        ScreenManager.Instance.DisplayScreen("Main");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        //clear list
        foreach (Transform transform in roomListContent)
        {
            Destroy(transform.gameObject);
        }

        //make room
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];
            Debug.Log(info.Name + ": " + info.RemovedFromList);

            if (info.RemovedFromList)
            {
                fullRoomList.Remove(info.Name);
            }
            else
            {
                fullRoomList[info.Name] = info;
            }
        }
        foreach (KeyValuePair<string, RoomInfo> entry in fullRoomList)
        {
            Instantiate(roomListPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(fullRoomList[entry.Key]);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListPrefab, playerListContent)
            .GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }
}
