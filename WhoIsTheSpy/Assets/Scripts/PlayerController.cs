using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Runtime.ConstrainedExecution;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("Ready Phase")]
    public PhotonView PV;
    bool ready;
    public Button readyBtn;
    public TextMeshProUGUI readyText;
    public TextMeshProUGUI readyTextAll;

    [Header("Choose Mode Phase")]
    public GameObject modeScreen;

    public GameObject passModeChooserBtn;
    public GameObject passModeChooser;
    public TextMeshProUGUI passModeChooserText;

    public GameObject screenCustomOrRandom;

    public GameObject normalWord;
    [SerializeField] TMP_InputField normalWordInput;
    public GameObject spyWord;
    [SerializeField] TMP_InputField spyWordInput;
    public GameObject screenCustomInput;

    public GameObject screenInOutGame;
    public TextMeshProUGUI superNounText;
    [SerializeField] int mode;

    [Header("First Voting Phase")]
    public Button votingBtn;
    public Button agreeBtn;
    public Button disagreeBtn;
    public RawImage agreeImage;
    public RawImage disagreeImage;

    [Header("Voting Spy Phase")]
    public Button voteMeBtn;
    public TextMeshProUGUI voteMeBtnText;
    [SerializeField] Transform votingList;
    [SerializeField] GameObject votingItem;

    [Header("PlayerUI")]
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI displayPhrase;
    public Button restartBtn;

    void Awake()
    {
        PV = GetComponent<PhotonView>();

        //join the horizontal layout group
        transform.SetParent(GameObject.Find("PlayerCanvas").transform);
        transform.localScale = new Vector3(1, 1, 1);

        //update name
        if (PV.IsMine)
        {
            PV.RPC(nameof(updateName), RpcTarget.AllBuffered, PhotonNetwork.NickName);
        }
        //everything off first
        votingBtn.gameObject.SetActive(false);
        displayPhrase.gameObject.SetActive(false);
        readyTextAll.gameObject.SetActive(false);

        modeScreen.SetActive(false);
        screenCustomOrRandom.SetActive(false);
        screenCustomInput.SetActive(false);
        normalWord.SetActive(false);
        spyWord.SetActive(false);
        screenInOutGame.SetActive(false);
        passModeChooser.SetActive(false);
        passModeChooserBtn.SetActive(false);

        GameManager.Instance.allPlayers.Add(this); //keep track of all players

        //ready button visible if owner and readyTextAll only shown if not owner
        if (PV.IsMine)
        {
            readyBtn.gameObject.SetActive(true);
        }
        else
        {
            readyBtn.gameObject.SetActive(false);
            readyTextAll.gameObject.SetActive(true);
        }

        //restart button only visible to masterclient
        if (PhotonNetwork.IsMasterClient && PV.IsMine)
        {
            restartBtn.gameObject.SetActive(true);
        }
        else
        {
            restartBtn.gameObject.SetActive(false);
        }
    }

    #region readyPhase

    [PunRPC]
    void updateName(string name)
    {
        playerName.text = name;
        voteMeBtnText.text = name;
        passModeChooserText.text = name;
    }

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
            readyTextAll.text = "Ready";
        }
        else
        {
            GameManager.Instance.numPlayerReady--;
            readyText.text = "Not Ready";
            readyTextAll.text = "Not Ready";
        }

        if (PV.IsMine)
        {
            GameManager.Instance.checkReady();
        }
    }
    #endregion

    #region Choose Mode Phase

    [PunRPC]
    public void chooseMode()
    {
        //modeChooser responsible for choosing mode
        if (PhotonNetwork.LocalPlayer == GameManager.Instance.modeChooser && PV.IsMine)
        {
            modeScreen.SetActive(true);
            passModeChooserBtn.SetActive(true);
            if (GameManager.Instance.superNoun)
            {
                superNounText.text = "Super Noun: On";
            }
            else
            {
                superNounText.text = "Super Noun: Off";
            }
        }
    }

    public void wantToPass()
    {
        passModeChooserBtn.SetActive(false);

        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            Debug.Log(cur);

            //exclude self
            if (cur != this)
            {
                cur.passModeChooser.SetActive(true);
            }
        }
    }

    //pass mode chooser
    public void passModeChooserClick()
    {
        PV.RPC(nameof(message), RpcTarget.AllBuffered, PV.Owner.NickName + " is choosing mode!", true);

        PV.RPC(nameof(updateModeChooser), RpcTarget.AllBuffered, PV.Owner);
        PV.RPC(nameof(chooseMode), PV.Owner);

        //clear
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.modeScreen.SetActive(false);
            cur.passModeChooser.SetActive(false);
        }
    }

    [PunRPC]
    void updateModeChooser(Photon.Realtime.Player newModeChooser)
    {
        GameManager.Instance.modeChooser = newModeChooser;
    }

    public void updateSuperNoun()
    {
        if (GameManager.Instance.superNoun)
        {
            superNounText.text = "Super Noun: Off";
        }
        else
        {
            superNounText.text = "Super Noun: On";
        }

        GameManager.Instance.updateSuperNoun();
    }

    //only differ in mode number
    public void startGameOneWord()
    {
        mode = 1;
        modeScreen.SetActive(false);
        screenCustomOrRandom.SetActive(true);
        passModeOff();
    }

    public void startGameTwoWords()
    {
        mode = 2;
        modeScreen.SetActive(false);
        screenCustomOrRandom.SetActive(true);
        passModeOff();
    }

    //screen changes
    public void randomWord()
    {
        screenCustomOrRandom.SetActive(false);
        screenInOutGame.SetActive(true);
    }

    void passModeOff()
    {
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.passModeChooser.SetActive(false);
        }

        passModeChooserBtn.SetActive(false);
    }

    public void customWord()
    {
        screenCustomOrRandom.SetActive(false);
        screenCustomInput.SetActive(true);
        normalWord.SetActive(true);
        if (mode == 2)
        {
            spyWord.SetActive(true);
        }

        //automatically observer
        PV.RPC(nameof(updateObserver), RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer);
    }

    public void startGameCustomWord()
    {
        screenCustomInput.SetActive(false);
        normalWord.SetActive(false);
        spyWord.SetActive(false);
        GameManager.Instance.startGame(mode, normalWordInput.text, spyWordInput.text);
        normalWordInput.text = "";
        spyWordInput.text = "";
    }

    //Third choice - Very Random
    public void startGameRandom()
    {
        mode = Random.Range(1, 3);
        modeScreen.SetActive(false);
        screenInOutGame.SetActive(true);

        passModeOff();
    }

    //in game or out game option
    public void startInGame()
    {
        screenInOutGame.SetActive(false);

        GameManager.Instance.startGame(mode);
    }

    public void startOutGame()
    {
        PV.RPC(nameof(updateObserver), RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer);

        screenInOutGame.SetActive(false);

        GameManager.Instance.startGame(mode);
    }

    [PunRPC]
    void updateObserver(Photon.Realtime.Player player)
    {
        GameManager.Instance.observer = player;
    }

    #endregion

    #region Phrase

    [PunRPC]
    public void assignPhrase(string normalPhrase, string spyPhrase, Photon.Realtime.Player spy)
    {
        PV.RPC(nameof(updatePhrase), RpcTarget.AllBuffered, normalPhrase, spyPhrase, PhotonNetwork.LocalPlayer == spy);
    }

    [PunRPC]
    void updatePhrase(string normalPhrase, string spyPhrase, bool isSpy)
    {
        //everything not needed off
        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        agreeImage.gameObject.SetActive(false);
        disagreeImage.gameObject.SetActive(false);

        voteMeBtn.gameObject.SetActive(false);
        readyBtn.gameObject.SetActive(false);

        votingBtn.gameObject.SetActive(false);
        displayPhrase.gameObject.SetActive(false);

        passModeChooser.SetActive(false);
        passModeChooserBtn.SetActive(false);

        readyTextAll.gameObject.SetActive(false);

        modeScreen.SetActive(false);
        screenCustomOrRandom.SetActive(false);
        screenCustomInput.SetActive(false);
        normalWord.SetActive(false);
        spyWord.SetActive(false);
        screenInOutGame.SetActive(false);

        //don't assign to observer
        if (PV.Owner == GameManager.Instance.observer) return;

        //different phrase for roles
        if (isSpy)
        {
            displayPhrase.text = spyPhrase;
        }
        else
        {
            displayPhrase.text = normalPhrase;
        }

        //observer can see all words
        if (PhotonNetwork.LocalPlayer == GameManager.Instance.observer)
        {
            displayPhrase.gameObject.SetActive(true);
        }

        //restart button only visible to masterclient
        if (PhotonNetwork.IsMasterClient && PV.IsMine)
        {
            restartBtn.gameObject.SetActive(true);
        }
        else
        {
            restartBtn.gameObject.SetActive(false);
        }
    }

    [PunRPC]
    public void RevealVotingBtn()
    {
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
    public void revealPhrase()
    {
        displayPhrase.gameObject.SetActive(true);
    }

    #endregion

    #region First Voting Process

    public void vote1()
    {
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            //exclude observer from voting
            if (cur.PV.Owner != GameManager.Instance.observer)
            {
                cur.PV.RPC(nameof(votingButtons1), RpcTarget.AllBuffered);
            }
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

    //set off button, display image and message, and checkVotes
    public void agree()
    {
        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        PV.RPC(nameof(castVote1), RpcTarget.AllBuffered, true, false);
        PV.RPC(nameof(message), RpcTarget.AllBuffered, GameManager.Instance.agreeVotes + " : " + GameManager.Instance.disagreeVotes, false);
        GameManager.Instance.checkVotes1();
    }

    //set off button, display image and message, and checkVotes
    public void disagree()
    {
        agreeBtn.gameObject.SetActive(false);
        disagreeBtn.gameObject.SetActive(false);
        PV.RPC(nameof(castVote1), RpcTarget.AllBuffered, false, true);
        PV.RPC(nameof(message), RpcTarget.AllBuffered, GameManager.Instance.agreeVotes + " : " + GameManager.Instance.disagreeVotes, false);
        GameManager.Instance.checkVotes1();
    }

    //display the correct image and increase the corresponding vote
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
    #endregion

    #region clear
    public IEnumerator delayVoteClear()
    {
        displayPhrase.gameObject.SetActive(false);

        yield return new WaitForSeconds(1.5f);

        //ask all players to clear
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(voteClear), RpcTarget.AllBuffered);
        }
    }

    public IEnumerator delayNoVoteClear()
    {
        yield return new WaitForSeconds(1.5f);

        //ask all players to clear
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(noVoteClear), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void voteClear()
    {
        //hide the images for start voting
        agreeImage.gameObject.SetActive(false);
        disagreeImage.gameObject.SetActive(false);

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

        //voting button active if owner again
        votingBtn.gameObject.SetActive(true);
    }
    #endregion

    #region Vote Spy

    [PunRPC]
    public void startVotingSpy()
    {
        //set the spyVotes diccionaries
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
        //don't vote observer and observer can't vote
        if (PV.Owner == GameManager.Instance.observer ||
            PhotonNetwork.LocalPlayer == GameManager.Instance.observer) return;

        voteMeBtn.gameObject.SetActive(true);
    }

    public void voteMe()
    {
        //ask the owner of this button so the list can be created
        PV.RPC(nameof(castVoteSpy), RpcTarget.AllBuffered, PV.Owner, PhotonNetwork.NickName);

        //clear buttons
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.voteMeBtn.gameObject.SetActive(false);
        }

        GameManager.Instance.checkVoteSpy();
    }

    [PunRPC]
    void castVoteSpy(Photon.Realtime.Player votedPlayer, string voter)
    {
        //weird error
        if (PhotonNetwork.LocalPlayer == GameManager.Instance.observer) return;

        //vote
        GameManager.Instance.spyVotes[votedPlayer]++;

        //UI
        GameObject cur = Instantiate(votingItem, votingList);
        cur.GetComponent<TextMeshProUGUI>().text = voter;
    }
    #endregion

    #region restart

    [PunRPC]
    public void clearList()
    {
        foreach (Transform transform in votingList)
        {
            Destroy(transform.gameObject);
        }
    }

    [PunRPC]
    public void tiedVoteReset()
    {
        foreach (Transform transform in votingList)
        {
            Destroy(transform.gameObject);
        }

        if (PV.IsMine)
        {
            votingBtn.gameObject.SetActive(true);
            displayPhrase.gameObject.SetActive(true);
        }
    }

    [PunRPC]
    public void restart()
    {
        ready = false;
        readyText.text = "Not Ready";
        readyTextAll.text = "Not Ready";

        //ready button visible if owner and readyTextAll only shown if not owner
        if (PV.IsMine)
        {
            readyBtn.gameObject.SetActive(true);
        }
        else
        {
            readyBtn.gameObject.SetActive(false);
            readyTextAll.gameObject.SetActive(true);
        }
    }

    public void forceRestart()
    {
        GameManager.Instance.forceRestart();
    }

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
            GameManager.Instance.curCoroutine = GameManager.Instance.StartCoroutine(nameof(GameManager.Instance.CountDown));
        }
    }

    //just in case master leave
    [PunRPC]
    public void restartButton()
    {
        if (PhotonNetwork.IsMasterClient && PV.IsMine){
            restartBtn.gameObject.SetActive(true);
        }
    }
}

