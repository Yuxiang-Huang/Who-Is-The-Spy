using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using System.Runtime.ConstrainedExecution;

public class PlayerController : MonoBehaviourPunCallbacks
{
    PhotonView PV;

    public bool isSpy;

    public Button restartBtn;
    public Button votingBtn;
    public Button agreeBtn;
    public Button disagreeBtn;

    List<string> allPhrases = new List<string>();

    //need to sync
    public string phrase;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI displayPhrase;

    void Awake()
    {
        PV = GetComponent<PhotonView>();

        transform.SetParent(GameObject.Find("PlayerCanvas").transform);
        transform.localScale = new Vector3(1, 1, 1);

        votingBtn.gameObject.SetActive(false);

        if (PV.IsMine)
        {
            PV.RPC("updateName", RpcTarget.AllBuffered, PhotonNetwork.NickName, "");
        }

        AllPlayers.Instance.allPlayers.Add(this);

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

    void pickSpy()
    {
        int spyNum = Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount);
        PV.RPC("assignSpy", RpcTarget.AllBuffered, spyNum);
    }

    public void restart()
    {
        pickSpy();
        generatePhrase();
    }

    public void vote()
    {
        PV.RPC("votingButtons", RpcTarget.AllBuffered);

        PV.RPC("message", RpcTarget.AllBuffered, PhotonNetwork.NickName + " want to start voting!");
    }

    public void agree()
    {
        agreeBtn.gameObject.SetActive(true);

        if (!PV.IsMine) return;

        disagreeBtn.gameObject.SetActive(false);
    }

    public void disagree()
    {
        disagreeBtn.gameObject.SetActive(true);

        if (!PV.IsMine) return;

        agreeBtn.gameObject.SetActive(false);
    }

    public void generatePhrase()
    {
        if (!PV.IsMine) return;

        phrase = allPhrases[Random.Range(0, allPhrases.Count)];

        foreach (PlayerController cur in AllPlayers.Instance.allPlayers)
        {
            cur.PV.RPC("updatePhrase", RpcTarget.AllBuffered, phrase, cur.isSpy);
        }
    }

    [PunRPC]
    void assignSpy(int spyNum)
    {
        if (PhotonNetwork.LocalPlayer == PhotonNetwork.PlayerList[spyNum])
        {
            isSpy = true;
        }
        else
        {
            isSpy = false;
        }
    }

    [PunRPC]
    void updateName(string name, string phrase)
    {
        playerName.text = name;
        displayPhrase.text = phrase;

        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);

        //if (PV.IsMine)
        //{
        //    //votingBtn.gameObject.SetActive(true);
        //    displayPhrase.gameObject.SetActive(true);
        //}
        //else
        //{
        //    //votingBtn.gameObject.SetActive(false);
        //    displayPhrase.gameObject.SetActive(false);
        //}
    }

    [PunRPC]
    void updatePhrase(string phrase, bool isSpy)
    {
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
    }

    [PunRPC]
    void votingButtons()
    {
        if (!PV.IsMine) return;

        agreeBtn.gameObject.SetActive(true);
        disagreeBtn.gameObject.SetActive(true);
    }

    [PunRPC]
    void message(string text)
    {
        AllMessage.Instance.text.text = text;
    }
}

