using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Runtime.ConstrainedExecution;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public PhotonView PV;

    public bool isSpy;

    public Button restartBtn;

    //start voting buttons
    public Button votingBtn;
    public Button agreeBtn;
    public Button disagreeBtn;
    public RawImage agreeImage;
    public RawImage disagreeImage;

    public GameObject votingList;
    public Button voteMeBtn;
    public TextMeshProUGUI voteMeBtnText;

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
        foreach (PlayerController cur in VotingManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(voteBtnSetupOwnerCall), cur.PV.Owner);
        }

        pickSpy();
        generatePhrase();
    }

    [PunRPC]
    void voteBtnSetupOwnerCall()
    {
        PV.RPC(nameof(voteBtnSetup), RpcTarget.AllBuffered, PhotonNetwork.NickName, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    void voteBtnSetup(string name, Photon.Realtime.Player player)
    {
        votingList.GetComponent<Vote>().player = player;
        voteMeBtnText.text = name;
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

    [PunRPC]
    void revealPhrase()
    {
        displayPhrase.gameObject.SetActive(true);
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

    public void vote1()
    {
        foreach (PlayerController cur in VotingManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(votingButtons1), RpcTarget.AllBuffered);
        }

        PV.RPC(nameof(message), RpcTarget.AllBuffered, PhotonNetwork.NickName + " want to start voting!", true);
    }

    [PunRPC]
    void votingButtons1()
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
        PV.RPC(nameof(castVote1), RpcTarget.AllBuffered, true, false);

        checkVotes1();
    }

    public void disagree()
    {
        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        PV.RPC(nameof(castVote1), RpcTarget.AllBuffered, false, true);

        checkVotes1();
    }

    [PunRPC]
    void castVote1(bool agreeBool, bool disagreeBool)
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

    public void checkVotes1()
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

                foreach (PlayerController cur in VotingManager.Instance.allPlayers)
                {
                    cur.PV.RPC(nameof(startVotingSpy), cur.PV.Owner);
                    StartCoroutine(nameof(delayClear));
                }
            }
            else
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Voting Failed!", true);

                foreach (PlayerController cur in VotingManager.Instance.allPlayers)
                {
                    cur.PV.RPC(nameof(noVoteClear), RpcTarget.AllBuffered);
                }
            }
        }
    }

    [PunRPC]
    public void startVotingSpy()
    {
        VotingManager.Instance.spyVotes = new Dictionary<Photon.Realtime.Player, int>();

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            VotingManager.Instance.spyVotes[player] = 0;
        }

        PV.RPC(nameof(votingButtonsSpy), RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void votingButtonsSpy()
    {
        //able to vote other people
        if (PV.IsMine) return;
        voteMeBtn.gameObject.SetActive(true);
    }

    #region clear
    IEnumerator delayClear()
    {
        yield return new WaitForSeconds(2.0f);

        //ask all players to clear
        foreach (PlayerController cur in VotingManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(voteClear), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void voteClear()
    {
        //hide the images for start voting
        agreeImage.gameObject.SetActive(false);
        disagreeImage.gameObject.SetActive(false);
        displayPhrase.gameObject.SetActive(false);

        //reset
        VotingManager.Instance.agreeVotes = 0;
        VotingManager.Instance.disagreeVotes = 0;
    }

    [PunRPC]
    void noVoteClear()
    {
        //hide the images for start voting
        agreeImage.gameObject.SetActive(false);
        disagreeImage.gameObject.SetActive(false);

        //reset
        VotingManager.Instance.agreeVotes = 0;
        VotingManager.Instance.disagreeVotes = 0;

        if (!PV.IsMine) return;

        votingBtn.gameObject.SetActive(true);
    }
    #endregion

    #endregion

    [PunRPC]
    void message(string text, bool countDown)
    {
        VotingManager.Instance.messageText.text = text;

        if (VotingManager.Instance.curCoroutine != null)
        {
            VotingManager.Instance.StopCoroutine(VotingManager.Instance.curCoroutine);
        }

        if (countDown)
        {
            VotingManager.Instance.curCoroutine = VotingManager.Instance.StartCoroutine("CountDown");
        }
    }
}

