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

    //start voting buttons
    public Button votingBtn;
    public Button agreeBtn;
    public Button disagreeBtn;
    public RawImage agreeImage;
    public RawImage disagreeImage;

    public Button voteMeBtn;

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

        VotingManager.Instance.allPlayers.Add(this); //keep track of all players
        voteMeBtn.GetComponent<Vote>().player = PhotonNetwork.LocalPlayer;
        voteMeBtn.GetComponent<Vote>().PV = PV;

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

    #region Phrase

    public void generatePhrase()
    {
        if (!PV.IsMine) return;

        //pick a phrase
        phrase = allPhrases[Random.Range(0, allPhrases.Count)];

        //ask the owner of each player
        foreach (PlayerController cur in VotingManager.Instance.allPlayers)
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
        voteMeBtn.gameObject.SetActive(false);

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

    #endregion

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
            foreach (PlayerController cur in VotingManager.Instance.allPlayers)
            {
                cur.isSpy = true;
            }
        }
        else
        {
            foreach (PlayerController cur in VotingManager.Instance.allPlayers)
            {
                cur.isSpy = false;
            }
        }
    }
    #endregion

    #region Voting

    public void vote()
    {
        foreach (PlayerController cur in VotingManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(votingButtons), RpcTarget.AllBuffered);
        }

        PV.RPC(nameof(message), RpcTarget.AllBuffered, PhotonNetwork.NickName + " want to start voting!", true);
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
        PV.RPC(nameof(castVote), RpcTarget.AllBuffered, true, false);

        checkVotes();
    }

    public void disagree()
    {
        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        PV.RPC(nameof(castVote), RpcTarget.AllBuffered, false, true);

        checkVotes();
    }

    [PunRPC]
    void castVote(bool agreeBool, bool disagreeBool)
    {
        agreeImage.gameObject.SetActive(agreeBool);
        disagreeImage.gameObject.SetActive(disagreeBool);

        if (agreeBool)
        {
            VotingManager.Instance.agreeVotes++;
        }
        if (disagreeBool)
        {
            VotingManager.Instance.disagreeVotes++;
        }
    }

    public void checkVotes()
    {
        int agreeVotes = VotingManager.Instance.agreeVotes;
        int disagreeVotes = VotingManager.Instance.disagreeVotes;

        PV.RPC(nameof(message), RpcTarget.AllBuffered, agreeVotes + " : " + disagreeVotes, false);

        //if everyone voted
        if (agreeVotes + disagreeVotes == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            //count votes
            if (agreeVotes > PhotonNetwork.CurrentRoom.PlayerCount / 2)
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Voting!", true);
            }
            else
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Voting Failed!", true);
            }

            //reset
            VotingManager.Instance.agreeVotes = 0;
            VotingManager.Instance.disagreeVotes = 0;

            foreach (PlayerController cur in VotingManager.Instance.allPlayers)
            {
                cur.PV.RPC(nameof(startVoting), cur.PV.Owner);
            }

            StartCoroutine(nameof(clear));
        }
    }

    [PunRPC]
    public void startVoting()
    {
        VotingManager.Instance.spyVotes = new Dictionary<Photon.Realtime.Player, int>();

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            VotingManager.Instance.spyVotes[player] = 0;
        }
    }

    #region clear
    IEnumerator clear()
    {
        yield return new WaitForSeconds(2.0f);

        //ask all players to clear
        foreach (PlayerController cur in VotingManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(clearAll), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void clearAll()
    {
        //hide the images for start voting
        agreeImage.gameObject.SetActive(false);
        disagreeImage.gameObject.SetActive(false);
        displayPhrase.gameObject.SetActive(false);
    }
    #endregion

    #endregion

    [PunRPC]
    void message(string text, bool countDown)
    {
        VotingManager.Instance.messageText.text = text;
        if (countDown)
        {
            VotingManager.Instance.StartCoroutine("CountDown");
        }
    }
}

