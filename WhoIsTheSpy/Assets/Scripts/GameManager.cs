using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class GameManager : MonoBehaviour
{
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

    IEnumerator CountDown()
    {
        yield return new WaitForSeconds(3f);

        messageText.text = "";
    }
}
