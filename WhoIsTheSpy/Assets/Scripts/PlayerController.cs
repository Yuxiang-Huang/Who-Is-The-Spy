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

    bool ready;
    public TextMeshProUGUI readyText;

    bool isSpy;

    public Button readyBtn;

    //first voting buttons
    public Button votingBtn;
    public Button agreeBtn;
    public Button disagreeBtn;
    public RawImage agreeImage;
    public RawImage disagreeImage;

    //voting spy
    public GameObject votingList;
    public Button voteMeBtn;
    public TextMeshProUGUI voteMeBtnText;

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
        //everything off first
        votingBtn.gameObject.SetActive(false);
        displayPhrase.gameObject.SetActive(false);

        GameManager.Instance.allPlayers.Add(this); //keep track of all players

        //ready button visible if owner
        if (PV.IsMine)
        {
            readyBtn.gameObject.SetActive(true);
        }
        else
        {
            readyBtn.gameObject.SetActive(false);
        }
    }

    #region readyPhase

    public void getReady()
    {
        PV.RPC(nameof(getReady_RPC), RpcTarget.AllBuffered);
    }

    [PunRPC]
    void getReady_RPC()
    {
        //update ready, show phrase, and check if can start game
        ready = !ready;
        if (ready)
        {
            GameManager.Instance.numPlayerReady++;
            readyText.text = "Ready";
        }
        else
        {
            GameManager.Instance.numPlayerReady--;
            readyText.text = "Not Ready";
        }
        GameManager.Instance.checkReady();
    }
    #endregion

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

    [PunRPC]
    void assignPhrase(string phrase, Photon.Realtime.Player spy)
    {
        PV.RPC(nameof(updatePhrase), RpcTarget.AllBuffered, PhotonNetwork.NickName, phrase,
            PhotonNetwork.LocalPlayer == spy);
    }

    [PunRPC]
    void updatePhrase(string name, string phrase, bool isSpy)
    {
        playerName.text = name;

        //spy can't see the phrase
        if (isSpy)
        {
            displayPhrase.text = "???";
        }
        else
        {
            displayPhrase.text = phrase;
        }

        //everything in voting process off 
        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        agreeImage.gameObject.SetActive(false);
        disagreeImage.gameObject.SetActive(false);

        voteMeBtn.gameObject.SetActive(false);
        readyBtn.gameObject.SetActive(false);

        votingBtn.gameObject.SetActive(false);
        displayPhrase.gameObject.SetActive(false);

        //voting button and phrase are only shown if owner
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

    #region Voting

    public void vote1()
    {
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
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
            GameManager.Instance.agreeVotes++;
        }
        if (disagreeBool)
        {
            GameManager.Instance.disagreeVotes++;
        }
    }

    public void checkVotes1()
    {
        int agreeVotes = GameManager.Instance.agreeVotes;
        int disagreeVotes = GameManager.Instance.disagreeVotes;

        PV.RPC(nameof(message), RpcTarget.AllBuffered, agreeVotes + " : " + disagreeVotes, false);

        //if everyone voted
        if (agreeVotes + disagreeVotes == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            //count votes
            if (agreeVotes > PhotonNetwork.CurrentRoom.PlayerCount / 2)
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Voting!", true);

                foreach (PlayerController cur in GameManager.Instance.allPlayers)
                {
                    cur.PV.RPC(nameof(startVotingSpy), cur.PV.Owner);
                    StartCoroutine(nameof(delayClear));
                }
            }
            else
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Voting Failed!", true);

                foreach (PlayerController cur in GameManager.Instance.allPlayers)
                {
                    cur.PV.RPC(nameof(noVoteClear), RpcTarget.AllBuffered);
                }
            }
        }
    }

    [PunRPC]
    public void startVotingSpy()
    {
        GameManager.Instance.spyVotes = new Dictionary<Photon.Realtime.Player, int>();

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            GameManager.Instance.spyVotes[player] = 0;
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
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
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
        GameManager.Instance.agreeVotes = 0;
        GameManager.Instance.disagreeVotes = 0;
    }

    [PunRPC]
    void noVoteClear()
    {
        //hide the images for start voting
        agreeImage.gameObject.SetActive(false);
        disagreeImage.gameObject.SetActive(false);

        //reset
        GameManager.Instance.agreeVotes = 0;
        GameManager.Instance.disagreeVotes = 0;

        if (!PV.IsMine) return;

        votingBtn.gameObject.SetActive(true);
    }
    #endregion

    #endregion

    [PunRPC]
    void message(string text, bool countDown)
    {
        GameManager.Instance.messageText.text = text;

        if (GameManager.Instance.curCoroutine != null)
        {
            GameManager.Instance.StopCoroutine(GameManager.Instance.curCoroutine);
        }

        if (countDown)
        {
            GameManager.Instance.curCoroutine = GameManager.Instance.StartCoroutine("CountDown");
        }
    }
}

