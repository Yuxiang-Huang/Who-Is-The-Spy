using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks
{
    PhotonView PV;

    public bool isSpy;

    public Button restartBtn;

    public Button votingBtn;
    public Button agreeBtn;
    public Button disagreeBtn;
    public RawImage agreeImage;
    public RawImage disagreeImage;

    List<string> allPhrases = new List<string>();

    public string phrase;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI displayPhrase;

    void Awake()
    {
        PV = GetComponent<PhotonView>();

        //join the horizontal layout group
        transform.SetParent(GameObject.Find("PlayerCanvas").transform);
        transform.localScale = new Vector3(1, 1, 1);

        //update name
        if (PV.IsMine)
        {
            PV.RPC(nameof(updatePhrase), RpcTarget.AllBuffered, PhotonNetwork.NickName, "", isSpy);
        }

        AllPlayers.Instance.allPlayers.Add(this); //keep track of all players

        //master client responsible for picking spy and starting game
        if (PhotonNetwork.IsMasterClient) 
        {
            createList();
            pickSpy();

            if (PV.IsMine)
            {
                restartBtn.gameObject.SetActive(true);
            }
            else
            {
                restartBtn.gameObject.SetActive(false);
            }
        }
        else
        {
            restartBtn.gameObject.SetActive(false);
        }
    }

    void createList()
    {
        for (int i = 0; i < 26; i++)
        {
            allPhrases.Add("" + (char) (i + 'a'));
        }
    }

    public void restart()
    {
        pickSpy();
        generatePhrase();
    }

    public void generatePhrase()
    {
        if (!PV.IsMine) return;

        //pick a phrase
        phrase = allPhrases[Random.Range(0, allPhrases.Count)];

        //ask the owner of each player
        foreach (PlayerController cur in AllPlayers.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(update), cur.PV.Owner, phrase);
        }
    }

    //update phrase if this gameobject belong to this player
    [PunRPC]
    void update(string phrase)
    {
        PV.RPC(nameof(updatePhrase), RpcTarget.AllBuffered, PhotonNetwork.NickName, phrase, isSpy);
    }

    [PunRPC]
    void updatePhrase(string name, string phrase, bool isSpy)
    {
        playerName.text = name;

        if (isSpy)
        {
            displayPhrase.text = "???";
        }
        else
        {
            displayPhrase.text = phrase;
        }

        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        agreeImage.gameObject.SetActive(false);
        disagreeImage.gameObject.SetActive(false);

        if (PV.IsMine)
        {
            votingBtn.gameObject.SetActive(true);
            displayPhrase.gameObject.SetActive(true);
        }
        else
        {
            votingBtn.gameObject.SetActive(false);
            displayPhrase.gameObject.SetActive(false);
        }
    }

    #region Spy
    void pickSpy()
    {
        int spyNum = Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount);
        PV.RPC(nameof(assignSpy), RpcTarget.AllBuffered, spyNum);
    }

    [PunRPC]
    void assignSpy(int spyNum)
    {
        if (PhotonNetwork.LocalPlayer == PhotonNetwork.PlayerList[spyNum])
        {
            foreach (PlayerController cur in AllPlayers.Instance.allPlayers)
            {
                cur.isSpy = true;
            }
        }
        else
        {
            foreach (PlayerController cur in AllPlayers.Instance.allPlayers)
            {
                cur.isSpy = false;
            }
        }
    }
    #endregion

    #region Voting

    public void vote()
    {
        foreach (PlayerController cur in AllPlayers.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(votingButtons), RpcTarget.AllBuffered);
        }

        PV.RPC(nameof(message), RpcTarget.AllBuffered, PhotonNetwork.NickName + " want to start voting!");
    }

    [PunRPC]
    void votingButtons()
    {
        if (!PV.IsMine) return;

        //display the buttons for agree and disagree to vote
        agreeBtn.gameObject.SetActive(true);
        disagreeBtn.gameObject.SetActive(true);

        //hide the button for start voting
        votingBtn.gameObject.SetActive(false);
    }

    public void agree()
    {
        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        PV.RPC(nameof(updateVotingButtons), RpcTarget.AllBuffered, true, false);

        checkVotes();
    }

    public void disagree()
    {
        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        PV.RPC(nameof(updateVotingButtons), RpcTarget.AllBuffered, false, true);

        checkVotes();
    }

    public void checkVotes()
    {
        int agreeVotes = VotingManager.Instance.agreeVotes;
        int disagreeVotes = VotingManager.Instance.disagreeVotes;

        PV.RPC(nameof(message), RpcTarget.AllBuffered, agreeVotes + " " + disagreeVotes);
        Debug.Log(PhotonNetwork.CountOfPlayers);

        if (agreeVotes + disagreeVotes == AllPlayers.Instance.allPlayers.Count)
        {
            if (agreeVotes > AllPlayers.Instance.allPlayers.Count / 2)
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Begin Vote!");
            }
            else
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Vote Failed!");
            }
        }
    }

    [PunRPC]
    void updateVotingButtons(bool agreeBool, bool disagreeBool)
    {
        agreeImage.gameObject.SetActive(agreeBool);
        disagreeImage.gameObject.SetActive(disagreeBool);

        if (agreeBool)
        {
            VotingManager.Instance.agreeVotes ++;
        }
        if (disagreeBool)
        {
            VotingManager.Instance.disagreeVotes++;
        }
    }

    #endregion

    [PunRPC]
    void message(string text)
    {
        VotingManager.Instance.messageText.text = text;
    }
}

