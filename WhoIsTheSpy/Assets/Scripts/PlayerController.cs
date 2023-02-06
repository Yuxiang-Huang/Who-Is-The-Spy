using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks
{
    PhotonView PV;

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

        createList();
        generatePhrase();
    }

    void createList()
    {
        allPhrases.Add("Sandwich");
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

        PV.RPC("updatePhrase", RpcTarget.AllBuffered, PhotonNetwork.NickName, phrase);
    }

    [PunRPC]
    void updatePhrase(string name, string phrase)
    {
        playerName.text = name;
        displayPhrase.text = phrase;

        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);

        if (PV.IsMine)
        {
            //votingBtn.gameObject.SetActive(true);
            displayPhrase.gameObject.SetActive(true);
        }
        else
        {
            //votingBtn.gameObject.SetActive(false);
            displayPhrase.gameObject.SetActive(false);
        }
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

