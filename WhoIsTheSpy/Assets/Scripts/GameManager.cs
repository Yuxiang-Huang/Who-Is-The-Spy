using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEditor.VersionControl;

public class GameManager : MonoBehaviour
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

    void Awake()
    {
        Instance = this;
        PV = GetComponent<PhotonView>();
        createList();
    }

    void createList()
    {
        allPhrases = new List<string>();

        for (int i = 0; i < 26; i++)
        {
            allPhrases.Add("" + (char)(i + 'a'));
        }
    }

    public void checkReady()
    {
        //everyone is ready, start game
        if (numPlayerReady == PhotonNetwork.CurrentRoom.PlayerCount)
        {
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
    }

    public void checkVotes1()
    {
        //if everyone voted
        if (agreeVotes + disagreeVotes == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            //count votes
            if (agreeVotes > PhotonNetwork.CurrentRoom.PlayerCount / 2)
            {
                PV.RPC(nameof(message), RpcTarget.AllBuffered, "Start Voting!", true);

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
                    cur.PV.RPC(nameof(cur.delayNoVoteClear), RpcTarget.AllBuffered);
                }
            }
        }
    }

    IEnumerator CountDown()
    {
        yield return new WaitForSeconds(3f);

        messageText.text = "";
    }

    [PunRPC]
    void message(string text, bool countDown)
    {
        messageText.text = text;

        if (curCoroutine != null)
        {
            StopCoroutine(curCoroutine);
        }

        if (countDown)
        {
            curCoroutine = StartCoroutine("CountDown");
        }
    }
}
