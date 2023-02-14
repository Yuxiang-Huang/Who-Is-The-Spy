using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System;
using Random = UnityEngine.Random;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    PhotonView PV;

    public static GameManager Instance;

    public TextMeshProUGUI messageText;

    public List<PlayerController> allPlayers = new List<PlayerController>();

    List<string> allPhrases;

    public int numPlayerReady = 0;

    public int agreeVotes;
    public int disagreeVotes;

    [SerializeField] Photon.Realtime.Player spy;
    public Dictionary<Photon.Realtime.Player, int> spyVotes;

    public Coroutine curCoroutine;

    public Photon.Realtime.Player observer;

    void Awake()
    {
        Instance = this;
        PV = GetComponent<PhotonView>();
        createList();
        observer = PhotonNetwork.MasterClient;
    }

    public void forceRestart()
    {
        //clear things from last game
        PV.RPC(nameof(restart), RpcTarget.AllBuffered);

        PV.RPC(nameof(clearVote1), RpcTarget.AllBuffered);

        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(cur.clearList), RpcTarget.AllBuffered);
        }

        PV.RPC(nameof(message), RpcTarget.AllBuffered, "", false);

        //pick spy
        int spyNum = Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount);
        spy = PhotonNetwork.PlayerList[spyNum];

        //assign phrase
        string phrase = allPhrases[Random.Range(0, allPhrases.Count)];

        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.PV.RPC("assignPhrase", cur.PV.Owner, phrase, spy);
        }
    }

    [PunRPC]
    void restart()
    {
        numPlayerReady = 0;
    }

    public void checkReady()
    {
        //everyone is ready, start game
        if (numPlayerReady == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            //clear things from last game
            PV.RPC(nameof(restart), RpcTarget.AllBuffered);

            foreach (PlayerController cur in GameManager.Instance.allPlayers)
            {
                cur.PV.RPC(nameof(cur.clearList), RpcTarget.AllBuffered);

                //choose mode
                cur.PV.RPC(nameof(cur.chooseMode), RpcTarget.AllBuffered);
            }

            PV.RPC(nameof(message), RpcTarget.AllBuffered, "Choose Mode!", false);
        }
    }

    public void startGame()
    {
        //pick spy
        int spyNum = Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount);
        PV.RPC(nameof(assignSpy), RpcTarget.AllBuffered, spyNum);

        //assign phrase
        string phrase = allPhrases[Random.Range(0, allPhrases.Count)];

        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(cur.assignPhrase), cur.PV.Owner, phrase, spy);
        }
    }

    [PunRPC]
    public void assignSpy(int spyNum)
    {
        spy = PhotonNetwork.PlayerList[spyNum];
    }

    public void checkVotes1()
    {
        //if everyone voted
        if (agreeVotes + disagreeVotes == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            //count votes, display message, and clear in both cases
            if (agreeVotes > PhotonNetwork.CurrentRoom.PlayerCount / 2)
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Voting!", true);

                //ask every owner
                foreach (PlayerController cur in GameManager.Instance.allPlayers)
                {
                    cur.PV.RPC(nameof(cur.startVotingSpy), cur.PV.Owner);
                    cur.StartCoroutine(nameof(cur.delayVoteClear));
                }
            }
            else
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Voting Failed!", true);

                foreach (PlayerController cur in GameManager.Instance.allPlayers)
                {
                    cur.StartCoroutine(nameof(cur.delayNoVoteClear), RpcTarget.AllBuffered);
                }
            }

            //reset votes
            PV.RPC(nameof(clearVote1), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void clearVote1()
    {
        agreeVotes = 0;
        disagreeVotes = 0;
    }

    public void checkVoteSpy()
    {
        //check vote
        int totalVote = 0;

        int maxVote = 0;
        Photon.Realtime.Player voted = null;

        foreach (var (key, value) in GameManager.Instance.spyVotes)
        {
            totalVote += value;
            if (value > maxVote)
            {
                maxVote = value;
                voted = key;
            }
        }

        //reveal spy
        if (totalVote == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            foreach (PlayerController cur in GameManager.Instance.allPlayers)
            {
                cur.PV.RPC("revealPhrase", RpcTarget.AllBuffered);
            }

            //find winner
            string result = voted.NickName + " is voted as spy. Spy ";

            if (spy == voted)
            {
                result += "lose!";
            }
            else
            {
                result += "won!";
            }

            result += " --- Restart?";

            PV.RPC(nameof(message), RpcTarget.AllBuffered, result, false);

            //restart?
            foreach (PlayerController cur in GameManager.Instance.allPlayers)
            {
                cur.PV.RPC(nameof(cur.restart), RpcTarget.AllBuffered);
            }
        }
    }

    IEnumerator CountDown()
    {
        yield return new WaitForSeconds(2f);

        messageText.text = "";
    }

    [PunRPC]
    void message(string text, bool countDown)
    {
        //set text
        messageText.text = text;

        //stop last corountine and start a new one if timed disappear text is needed
        if (curCoroutine != null)
        {
            StopCoroutine(curCoroutine);
        }

        if (countDown)
        {
            curCoroutine = StartCoroutine("CountDown");
        }
    }

    //just in case
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        observer = newMasterClient;
    }

    void createList()
    {
        allPhrases = new List<string>();
        allPhrases.Add("Airplane");
        allPhrases.Add("Bank");
        allPhrases.Add("Beach");
        allPhrases.Add("Broadway Theater");
        allPhrases.Add("Casino");
        allPhrases.Add("Cathedral");
        allPhrases.Add("Circus Tent");
        allPhrases.Add("Corporate Party");
        allPhrases.Add("Crusader Army");
        allPhrases.Add("Day Spa");
        allPhrases.Add("Embassy");
        allPhrases.Add("Hospital");
        allPhrases.Add("Hotel");
        allPhrases.Add("Military Base");
        allPhrases.Add("Movie Studio");
        allPhrases.Add("Ocean Liner");
        allPhrases.Add("Passenger Train");
        allPhrases.Add("Pirate Ship");
        allPhrases.Add("Polar Station");
        allPhrases.Add("Police Station");
        allPhrases.Add("Restaurant");
        allPhrases.Add("School");
        allPhrases.Add("Service Station");
        allPhrases.Add("Space Station");
        allPhrases.Add("Submarine");
        allPhrases.Add("Supermarket");
        allPhrases.Add("University");
    }
}
