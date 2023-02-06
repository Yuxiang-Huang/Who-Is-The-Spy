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

        createList();
        generatePhrase();
    }

    void createList()
    {
        allPhrases.Add("Sandwich");
    }

    public void vote()
    {
        if (!PV.IsMine) return;

        agreeBtn.gameObject.SetActive(true);
        disagreeBtn.gameObject.SetActive(true);

        PV.RPC("startVotingMessage", RpcTarget.AllBuffered, PhotonNetwork.NickName + " want to start voting!");
    }

    public void win()
    {
        if (!PV.IsMine) return;

        cardNum++;

        generatePhrase();
    }

    public void lose()
    {
        if (!PV.IsMine) return;

        cardNum--;

        generatePhrase();
    }

    public void generatePhrase()
    {
        if (!PV.IsMine) return;

        phrase = allPhrases[Random.Range(0, allPhrases.Count)];

        PV.RPC("updatePhrase", RpcTarget.AllBuffered, PhotonNetwork.NickName, cardNum, phrase);
    }

    [PunRPC]
    void updatePhrase(string name, int number, string phrase)
    {
        playerName.text = name;
        displayCard.text = "Cards Left: " + number;
        displayPhrase.text = phrase;

        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);

        if (PV.IsMine)
        {
            revealBtn.gameObject.SetActive(true);
            displayPhrase.gameObject.SetActive(false);
        }
        else
        {
            revealBtn.gameObject.SetActive(false);
            displayPhrase.gameObject.SetActive(true);
        }
    }

    [PunRPC]
    void displayRevealMessage(string text)
    {
        RevealText.Instance.text.text = text;
        StartCoroutine("CountDownReveal");
    }

    IEnumerator CountDownReveal()
    {
        RevealText.Instance.gameObject.SetActive(true);
        revealTextcountDown++;

        yield return new WaitForSeconds(3);

        revealTextcountDown--;

        if (revealTextcountDown == 0)
        {
            RevealText.Instance.gameObject.SetActive(false);
        }
        else
        {
            RevealText.Instance.gameObject.SetActive(true);
        }
    }
}

