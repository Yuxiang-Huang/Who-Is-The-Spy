using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks
{
    PhotonView PV;

    public Button revealBtn;
    public Button winCard;
    public Button loseCard;

    List<string> allPhrases = new List<string>();

    int revealTextcountDown = 0;

    //need to sync
    public int cardNum;
    public string phrase;

    public TextMeshProUGUI playerName;
    public TextMeshProUGUI displayCard;
    public TextMeshProUGUI displayPhrase;

    void Awake()
    {
        PV = GetComponent<PhotonView>();

        transform.SetParent(GameObject.Find("PlayerCanvas").transform);

        transform.localScale = new Vector3(1, 1, 1);

        cardNum = 5;

        createList();
        generatePhrase();
    }

    void createList()
    {
        //not do things
        allPhrases.Add("Not speak for 10 seconds");
        allPhrases.Add("Not move for 10 seconds");
        allPhrases.Add("Talk for 5 seconds");

        //look
        allPhrases.Add("Look Up");
        allPhrases.Add("Look Down");

        //do
        allPhrases.Add("Laugh");
        allPhrases.Add("Help");
        allPhrases.Add("Hesitate");
        allPhrases.Add("Recall");
        allPhrases.Add("Any two body parts touching");
        allPhrases.Add("Hold anything");
        allPhrases.Add("Make any gesture");
        allPhrases.Add("Apologize");

        //say
        allPhrases.Add("Ask");
        allPhrases.Add("Shout");

        allPhrases.Add("Agree");
        allPhrases.Add("Disagree");

        allPhrases.Add("Reject to answer");

        allPhrases.Add("Repeat");

        allPhrases.Add("Compare");

        allPhrases.Add("Guess the phrase");
        allPhrases.Add("Praise");

        //speak
        allPhrases.Add("Talk about time");
        allPhrases.Add("Talk about food");
        allPhrases.Add("Talk about school");
        allPhrases.Add("Talk about weather");
        allPhrases.Add("Talk about countries");
        allPhrases.Add("Talk about location");
        allPhrases.Add("Talk about sport");
        allPhrases.Add("Talk about money");
        allPhrases.Add("Talk about science");
        allPhrases.Add("Talk about history");
        allPhrases.Add("Talk about mathematics");
        allPhrases.Add("Talk about music or art");

        allPhrases.Add("Say any word not in English");
        allPhrases.Add("Say any number");
        allPhrases.Add("Say any name");
        allPhrases.Add("Say any food");
        allPhrases.Add("Say any animal");
        allPhrases.Add("Say any emotion");
        allPhrases.Add("Say any characteristic");
        allPhrases.Add("Say any occupation");
        allPhrases.Add("Say any letter");

        allPhrases.Add("Say 'If'");
        allPhrases.Add("Say 'No' or 'don’t'");
        allPhrases.Add("Say ‘Why’ or ‘How’");
        allPhrases.Add("Say ‘Where’ or ‘When’");
        allPhrases.Add("Say ‘Who’ or ‘What’ or ‘Which’");

        allPhrases.Add("Use more than one pronoun in one sentence");
    }

    public void reveal()
    {
        if (!PV.IsMine) return;

        winCard.gameObject.SetActive(true);
        loseCard.gameObject.SetActive(true);
        displayPhrase.gameObject.SetActive(true);
        revealBtn.gameObject.SetActive(false);

        PV.RPC("displayRevealMessage", RpcTarget.AllBuffered, PhotonNetwork.NickName + " revealed the card. "
            + cardNum + " cards.");
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

        winCard.gameObject.SetActive(false);
        loseCard.gameObject.SetActive(false);

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

