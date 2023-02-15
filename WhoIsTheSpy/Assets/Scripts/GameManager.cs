using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System;
using Random = UnityEngine.Random;
using Photon.Realtime;
using System.Runtime.ConstrainedExecution;

public class GameManager : MonoBehaviourPunCallbacks
{
    PhotonView PV;

    public static GameManager Instance;

    public TextMeshProUGUI messageText;

    public List<PlayerController> allPlayers = new List<PlayerController>();

    List<string> allPhrases;
    List<string> superNouns;

    public int numPlayerReady = 0;

    public int agreeVotes;
    public int disagreeVotes;

    [SerializeField] Photon.Realtime.Player spy;
    public Dictionary<Photon.Realtime.Player, int> spyVotes;

    public Coroutine curCoroutine;

    public Photon.Realtime.Player modeChooser;
    public Photon.Realtime.Player observer;

    //modes
    public bool superNoun;

    void Awake()
    {
        Instance = this;
        PV = GetComponent<PhotonView>();
        createList();
        modeChooser = PhotonNetwork.MasterClient;
    }

    #region restart

    public void forceRestart()
    {
        //clear things from last game
        observer = null;

        PV.RPC(nameof(restart), RpcTarget.AllBuffered);

        PV.RPC(nameof(clearVote1), RpcTarget.AllBuffered);

        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            cur.PV.RPC(nameof(cur.clearList), RpcTarget.AllBuffered);

            //clear ready
            cur.PV.RPC(nameof(cur.assignPhrase), cur.PV.Owner, "", "", spy);

            //choose mode
            cur.PV.RPC(nameof(cur.chooseMode), RpcTarget.AllBuffered);
        }

        PV.RPC(nameof(message), RpcTarget.AllBuffered, "Choose Mode!", true);
    }

    [PunRPC]
    void restart()
    {
        numPlayerReady = 0;
    }

    #endregion

    public void checkReady()
    {
        //everyone is ready, start game
        if (numPlayerReady == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            observer = null;

            //clear things from last game
            PV.RPC(nameof(restart), RpcTarget.AllBuffered);

            foreach (PlayerController cur in GameManager.Instance.allPlayers)
            {
                cur.PV.RPC(nameof(cur.clearList), RpcTarget.AllBuffered);

                //clear ready
                cur.PV.RPC(nameof(cur.assignPhrase), cur.PV.Owner, "", "", spy);

                //choose mode
                cur.PV.RPC(nameof(cur.chooseMode), RpcTarget.AllBuffered);
            }

            PV.RPC(nameof(message), RpcTarget.AllBuffered, "Choose Mode!", true);
        }
    }

    #region mode

    [PunRPC]
    public void updateSuperNoun()
    {
        superNoun = !superNoun;
    }

    public void startGame(int mode)
    {
        //choose wordbank
        List<String> wordBank = allPhrases;

        if (superNoun)
        {
            wordBank = superNouns;
        }

        //pick spy
        Photon.Realtime.Player spy;
        do {
            spy = PhotonNetwork.PlayerList[Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount)];
        } while (spy == observer);

        PV.RPC(nameof(assignSpy), RpcTarget.AllBuffered, spy);

        //phrase
        string normalPhrase = wordBank[Random.Range(0, wordBank.Count)];
        string spyPhrase = "";

        //choose spyPhrase base on mode
        if (mode == 1)
        {
            spyPhrase = "???";
        }
        else
        {
            spyPhrase = wordBank[Random.Range(0, wordBank.Count)];

            //not same phrase
            while (spyPhrase == normalPhrase)
            {
                spyPhrase = wordBank[Random.Range(0, wordBank.Count)];
            }
        }

        //assign phrase
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            //exclude observer
            if (cur.PV.Owner != observer)
            {
                cur.PV.RPC(nameof(cur.assignPhrase), cur.PV.Owner, normalPhrase, spyPhrase, spy);
                cur.PV.RPC(nameof(cur.RevealVotingBtn), cur.PV.Owner);
            }
        }
    }

    //custom input
    public void startGame(int mode, string normalPhrase, string spyPhrase)
    {
        //pick spy
        Photon.Realtime.Player spy;
        do
        {
            spy = PhotonNetwork.PlayerList[Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount)];
        } while (spy == observer);

        PV.RPC(nameof(assignSpy), RpcTarget.AllBuffered, spy);

        //choose spyPhrase base on mode
        if (mode == 1)
        {
            spyPhrase = "???";
        }

        //assign phrase
        foreach (PlayerController cur in GameManager.Instance.allPlayers)
        {
            //exclude observer
            if (cur.PV.Owner != observer)
            {
                cur.PV.RPC(nameof(cur.assignPhrase), cur.PV.Owner, normalPhrase, spyPhrase, spy);
                cur.PV.RPC(nameof(cur.RevealVotingBtn), cur.PV.Owner);
            }
        }
    }

    [PunRPC]
    public void assignSpy(Photon.Realtime.Player newSpy)
    {
        spy = newSpy;
    }

    #endregion

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

    #region networking

    //just in case
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        modeChooser = newMasterClient;
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        Destroy(RoomManager.Instance.gameObject);
        PhotonNetwork.LoadLevel(0);
    }

    #endregion

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

        superNouns = new List<string>();
        superNouns.Add("people");
        superNouns.Add("history");
        superNouns.Add("way");
        superNouns.Add("art");
        superNouns.Add("world");
        superNouns.Add("information");
        superNouns.Add("map");
        superNouns.Add("two");
        superNouns.Add("family");
        superNouns.Add("government");
        superNouns.Add("health");
        superNouns.Add("system");
        superNouns.Add("computer");
        superNouns.Add("meat");
        superNouns.Add("year");
        superNouns.Add("thanks");
        superNouns.Add("music");
        superNouns.Add("person");
        superNouns.Add("reading");
        superNouns.Add("method");
        superNouns.Add("data");
        superNouns.Add("food");
        superNouns.Add("understanding");
        superNouns.Add("theory");
        superNouns.Add("law");
        superNouns.Add("bird");
        superNouns.Add("literature");
        superNouns.Add("problem");
        superNouns.Add("software");
        superNouns.Add("control");
        superNouns.Add("knowledge");
        superNouns.Add("power");
        superNouns.Add("ability");
        superNouns.Add("economics");
        superNouns.Add("love");
        superNouns.Add("internet");
        superNouns.Add("television");
        superNouns.Add("science");
        superNouns.Add("library");
        superNouns.Add("nature");
        superNouns.Add("fact");
        superNouns.Add("product");
        superNouns.Add("idea");
        superNouns.Add("temperature");
        superNouns.Add("investment");
        superNouns.Add("area");
        superNouns.Add("society");
        superNouns.Add("activity");
        superNouns.Add("story");
        superNouns.Add("industry");
        superNouns.Add("media");
        superNouns.Add("thing");
        superNouns.Add("oven");
        superNouns.Add("community");
        superNouns.Add("definition");
        superNouns.Add("safety");
        superNouns.Add("quality");
        superNouns.Add("development");
        superNouns.Add("language");
        superNouns.Add("management");
        superNouns.Add("player");
        superNouns.Add("variety");
        superNouns.Add("video");
        superNouns.Add("week");
        superNouns.Add("security");
        superNouns.Add("country");
        superNouns.Add("exam");
        superNouns.Add("movie");
        superNouns.Add("organization");
        superNouns.Add("equipment");
        superNouns.Add("physics");
        superNouns.Add("analysis");
        superNouns.Add("policy");
        superNouns.Add("series");
        superNouns.Add("thought");
        superNouns.Add("basis");
        superNouns.Add("boyfriend");
        superNouns.Add("direction");
        superNouns.Add("strategy");
        superNouns.Add("technology");
        superNouns.Add("army");
        superNouns.Add("camera");
        superNouns.Add("freedom");
        superNouns.Add("paper");
        superNouns.Add("environment");
        superNouns.Add("child");
        superNouns.Add("instance");
        superNouns.Add("month");
        superNouns.Add("truth");
        superNouns.Add("marketing");
        superNouns.Add("university");
        superNouns.Add("writing");
        superNouns.Add("article");
        superNouns.Add("department");
        superNouns.Add("difference");
        superNouns.Add("goal");
        superNouns.Add("news");
        superNouns.Add("audience");
        superNouns.Add("fishing");
        superNouns.Add("growth");
        superNouns.Add("income");
        superNouns.Add("marriage");
        superNouns.Add("user");
        superNouns.Add("combination");
        superNouns.Add("failure");
        superNouns.Add("meaning");
        superNouns.Add("medicine");
        superNouns.Add("philosophy");
        superNouns.Add("teacher");
        superNouns.Add("communication");
        superNouns.Add("night");
        superNouns.Add("chemistry");
        superNouns.Add("disease");
        superNouns.Add("disk");
        superNouns.Add("energy");
        superNouns.Add("nation");
        superNouns.Add("road");
        superNouns.Add("role");
        superNouns.Add("soup");
        superNouns.Add("advertising");
        superNouns.Add("location");
        superNouns.Add("success");
        superNouns.Add("addition");
        superNouns.Add("apartment");
        superNouns.Add("education");
        superNouns.Add("math");
        superNouns.Add("moment");
        superNouns.Add("painting");
        superNouns.Add("politics");
        superNouns.Add("attention");
        superNouns.Add("decision");
        superNouns.Add("event");
        superNouns.Add("property");
        superNouns.Add("shopping");
        superNouns.Add("student");
        superNouns.Add("wood");
        superNouns.Add("competition");
        superNouns.Add("distribution");
        superNouns.Add("entertainment");
        superNouns.Add("office");
        superNouns.Add("population");
        superNouns.Add("president");
        superNouns.Add("unit");
        superNouns.Add("category");
        superNouns.Add("cigarette");
        superNouns.Add("context");
        superNouns.Add("introduction");
        superNouns.Add("opportunity");
        superNouns.Add("performance");
        superNouns.Add("driver");
        superNouns.Add("flight");
        superNouns.Add("length");
        superNouns.Add("magazine");
        superNouns.Add("newspaper");
        superNouns.Add("relationship");
        superNouns.Add("teaching");
        superNouns.Add("cell");
        superNouns.Add("dealer");
        superNouns.Add("finding");
        superNouns.Add("lake");
        superNouns.Add("member");
        superNouns.Add("message");
        superNouns.Add("phone");
        superNouns.Add("scene");
        superNouns.Add("appearance");
        superNouns.Add("association");
        superNouns.Add("concept");
        superNouns.Add("customer");
        superNouns.Add("death");
        superNouns.Add("discussion");
        superNouns.Add("housing");
        superNouns.Add("inflation");
        superNouns.Add("insurance");
        superNouns.Add("mood");
        superNouns.Add("woman");
        superNouns.Add("advice");
        superNouns.Add("blood");
        superNouns.Add("effort");
        superNouns.Add("expression");
        superNouns.Add("importance");
        superNouns.Add("opinion");
        superNouns.Add("payment");
        superNouns.Add("reality");
        superNouns.Add("responsibility");
        superNouns.Add("situation");
        superNouns.Add("skill");
        superNouns.Add("statement");
        superNouns.Add("wealth");
        superNouns.Add("application");
        superNouns.Add("city");
        superNouns.Add("county");
        superNouns.Add("depth");
        superNouns.Add("estate");
        superNouns.Add("foundation");
        superNouns.Add("grandmother");
        superNouns.Add("heart");
        superNouns.Add("perspective");
        superNouns.Add("photo");
        superNouns.Add("recipe");
        superNouns.Add("studio");
        superNouns.Add("topic");
        superNouns.Add("collection");
        superNouns.Add("depression");
        superNouns.Add("imagination");
        superNouns.Add("passion");
        superNouns.Add("percentage");
        superNouns.Add("resource");
        superNouns.Add("setting");
        superNouns.Add("ad");
        superNouns.Add("agency");
        superNouns.Add("college");
        superNouns.Add("connection");
        superNouns.Add("criticism");
        superNouns.Add("debt");
        superNouns.Add("description");
        superNouns.Add("memory");
        superNouns.Add("patience");
        superNouns.Add("secretary");
        superNouns.Add("solution");
        superNouns.Add("administration");
        superNouns.Add("aspect");
        superNouns.Add("attitude");
        superNouns.Add("director");
        superNouns.Add("personality");
        superNouns.Add("psychology");
        superNouns.Add("recommendation");
        superNouns.Add("response");
        superNouns.Add("selection");
        superNouns.Add("storage");
        superNouns.Add("version");
        superNouns.Add("alcohol");
        superNouns.Add("argument");
        superNouns.Add("complaint");
        superNouns.Add("contract");
        superNouns.Add("emphasis");
        superNouns.Add("highway");
        superNouns.Add("loss");
        superNouns.Add("membership");
        superNouns.Add("possession");
        superNouns.Add("preparation");
        superNouns.Add("steak");
        superNouns.Add("union");
        superNouns.Add("agreement");
        superNouns.Add("cancer");
        superNouns.Add("currency");
        superNouns.Add("employment");
        superNouns.Add("engineering");
        superNouns.Add("entry");
        superNouns.Add("interaction");
        superNouns.Add("mixture");
        superNouns.Add("preference");
        superNouns.Add("region");
        superNouns.Add("republic");
        superNouns.Add("tradition");
        superNouns.Add("virus");
        superNouns.Add("actor");
        superNouns.Add("classroom");
        superNouns.Add("delivery");
        superNouns.Add("device");
        superNouns.Add("difficulty");
        superNouns.Add("drama");
        superNouns.Add("election");
        superNouns.Add("engine");
        superNouns.Add("football");
        superNouns.Add("guidance");
        superNouns.Add("hotel");
        superNouns.Add("owner");
        superNouns.Add("priority");
        superNouns.Add("protection");
        superNouns.Add("suggestion");
        superNouns.Add("tension");
        superNouns.Add("variation");
        superNouns.Add("anxiety");
        superNouns.Add("atmosphere");
        superNouns.Add("awareness");
        superNouns.Add("bath");
        superNouns.Add("bread");
        superNouns.Add("candidate");
        superNouns.Add("climate");
        superNouns.Add("comparison");
        superNouns.Add("confusion");
        superNouns.Add("construction");
        superNouns.Add("elevator");
        superNouns.Add("emotion");
        superNouns.Add("employee");
        superNouns.Add("employer");
        superNouns.Add("guest");
        superNouns.Add("height");
        superNouns.Add("leadership");
        superNouns.Add("mall");
        superNouns.Add("manager");
        superNouns.Add("operation");
        superNouns.Add("recording");
        superNouns.Add("sample");
        superNouns.Add("transportation");
        superNouns.Add("charity");
        superNouns.Add("cousin");
        superNouns.Add("disaster");
        superNouns.Add("editor");
        superNouns.Add("efficiency");
        superNouns.Add("excitement");
        superNouns.Add("extent");
        superNouns.Add("feedback");
        superNouns.Add("guitar");
        superNouns.Add("homework");
        superNouns.Add("leader");
        superNouns.Add("mom");
        superNouns.Add("outcome");
        superNouns.Add("permission");
        superNouns.Add("presentation");
        superNouns.Add("promotion");
        superNouns.Add("reflection");
        superNouns.Add("refrigerator");
        superNouns.Add("resolution");
        superNouns.Add("revenue");
        superNouns.Add("session");
        superNouns.Add("singer");
        superNouns.Add("tennis");
        superNouns.Add("basket");
        superNouns.Add("bonus");
        superNouns.Add("cabinet");
        superNouns.Add("childhood");
        superNouns.Add("church");
        superNouns.Add("clothes");
        superNouns.Add("coffee");
        superNouns.Add("dinner");
        superNouns.Add("drawing");
        superNouns.Add("hair");
        superNouns.Add("hearing");
        superNouns.Add("initiative");
        superNouns.Add("judgment");
        superNouns.Add("lab");
        superNouns.Add("measurement");
        superNouns.Add("mode");
        superNouns.Add("mud");
        superNouns.Add("orange");
        superNouns.Add("poetry");
        superNouns.Add("police");
        superNouns.Add("possibility");
        superNouns.Add("procedure");
        superNouns.Add("queen");
        superNouns.Add("ratio");
        superNouns.Add("relation");
        superNouns.Add("restaurant");
        superNouns.Add("satisfaction");
        superNouns.Add("sector");
        superNouns.Add("signature");
        superNouns.Add("significance");
        superNouns.Add("song");
        superNouns.Add("tooth");
        superNouns.Add("town");
        superNouns.Add("vehicle");
        superNouns.Add("volume");
        superNouns.Add("wife");
        superNouns.Add("accident");
        superNouns.Add("airport");
        superNouns.Add("appointment");
        superNouns.Add("arrival");
        superNouns.Add("assumption");
        superNouns.Add("baseball");
        superNouns.Add("chapter");
        superNouns.Add("committee");
        superNouns.Add("conversation");
        superNouns.Add("database");
        superNouns.Add("enthusiasm");
        superNouns.Add("error");
        superNouns.Add("explanation");
        superNouns.Add("farmer");
        superNouns.Add("gate");
        superNouns.Add("girl");
        superNouns.Add("hall");
        superNouns.Add("historian");
        superNouns.Add("hospital");
        superNouns.Add("injury");
        superNouns.Add("instruction");
        superNouns.Add("maintenance");
        superNouns.Add("manufacturer");
        superNouns.Add("meal");
        superNouns.Add("perception");
        superNouns.Add("pie");
        superNouns.Add("poem");
        superNouns.Add("presence");
        superNouns.Add("proposal");
        superNouns.Add("reception");
        superNouns.Add("replacement");
        superNouns.Add("revolution");
        superNouns.Add("river");
        superNouns.Add("son");
        superNouns.Add("speech");
        superNouns.Add("tea");
        superNouns.Add("village");
        superNouns.Add("warning");
        superNouns.Add("winner");
        superNouns.Add("worker");
        superNouns.Add("writer");
        superNouns.Add("assistance");
        superNouns.Add("breath");
        superNouns.Add("buyer");
        superNouns.Add("chest");
        superNouns.Add("chocolate");
        superNouns.Add("conclusion");
        superNouns.Add("contribution");
        superNouns.Add("cookie");
        superNouns.Add("courage");
        superNouns.Add("dad");
        superNouns.Add("desk");
        superNouns.Add("drawer");
        superNouns.Add("establishment");
        superNouns.Add("examination");
        superNouns.Add("garbage");
        superNouns.Add("grocery");
        superNouns.Add("honey");
        superNouns.Add("impression");
        superNouns.Add("improvement");
        superNouns.Add("independence");
        superNouns.Add("insect");
        superNouns.Add("inspection");
        superNouns.Add("inspector");
        superNouns.Add("king");
        superNouns.Add("ladder");
        superNouns.Add("menu");
        superNouns.Add("penalty");
        superNouns.Add("piano");
        superNouns.Add("potato");
        superNouns.Add("profession");
        superNouns.Add("professor");
        superNouns.Add("quantity");
        superNouns.Add("reaction");
        superNouns.Add("requirement");
        superNouns.Add("salad");
        superNouns.Add("sister");
        superNouns.Add("supermarket");
        superNouns.Add("tongue");
        superNouns.Add("weakness");
        superNouns.Add("wedding");
        superNouns.Add("affair");
        superNouns.Add("ambition");
        superNouns.Add("analyst");
        superNouns.Add("apple");
        superNouns.Add("assignment");
        superNouns.Add("assistant");
        superNouns.Add("bathroom");
        superNouns.Add("bedroom");
        superNouns.Add("beer");
        superNouns.Add("birthday");
        superNouns.Add("celebration");
        superNouns.Add("championship");
        superNouns.Add("cheek");
        superNouns.Add("client");
        superNouns.Add("consequence");
        superNouns.Add("departure");
        superNouns.Add("diamond");
        superNouns.Add("dirt");
        superNouns.Add("ear");
        superNouns.Add("fortune");
        superNouns.Add("friendship");
        superNouns.Add("funeral");
        superNouns.Add("gene");
        superNouns.Add("girlfriend");
        superNouns.Add("hat");
        superNouns.Add("indication");
        superNouns.Add("intention");
        superNouns.Add("lady");
        superNouns.Add("midnight");
        superNouns.Add("negotiation");
        superNouns.Add("obligation");
        superNouns.Add("passenger");
        superNouns.Add("pizza");
        superNouns.Add("platform");
        superNouns.Add("poet");
        superNouns.Add("pollution");
        superNouns.Add("recognition");
        superNouns.Add("reputation");
        superNouns.Add("shirt");
        superNouns.Add("sir");
        superNouns.Add("speaker");
        superNouns.Add("stranger");
        superNouns.Add("surgery");
        superNouns.Add("sympathy");
        superNouns.Add("tale");
        superNouns.Add("throat");
        superNouns.Add("trainer");
        superNouns.Add("uncle");
        superNouns.Add("youth");
        superNouns.Add("time");
        superNouns.Add("work");
        superNouns.Add("film");
        superNouns.Add("water");
        superNouns.Add("money");
        superNouns.Add("example");
        superNouns.Add("while");
        superNouns.Add("business");
        superNouns.Add("study");
        superNouns.Add("game");
        superNouns.Add("life");
        superNouns.Add("form");
        superNouns.Add("air");
        superNouns.Add("day");
        superNouns.Add("place");
        superNouns.Add("number");
        superNouns.Add("part");
        superNouns.Add("field");
        superNouns.Add("fish");
        superNouns.Add("back");
        superNouns.Add("process");
        superNouns.Add("heat");
        superNouns.Add("hand");
        superNouns.Add("experience");
        superNouns.Add("job");
        superNouns.Add("book");
        superNouns.Add("end");
        superNouns.Add("point");
        superNouns.Add("type");
        superNouns.Add("home");
        superNouns.Add("economy");
        superNouns.Add("value");
        superNouns.Add("body");
        superNouns.Add("market");
        superNouns.Add("guide");
        superNouns.Add("interest");
        superNouns.Add("state");
        superNouns.Add("radio");
        superNouns.Add("course");
        superNouns.Add("company");
        superNouns.Add("price");
        superNouns.Add("size");
        superNouns.Add("card");
        superNouns.Add("list");
        superNouns.Add("mind");
        superNouns.Add("trade");
        superNouns.Add("line");
        superNouns.Add("care");
        superNouns.Add("group");
        superNouns.Add("risk");
        superNouns.Add("word");
        superNouns.Add("fat");
        superNouns.Add("force");
        superNouns.Add("key");
        superNouns.Add("light");
        superNouns.Add("training");
        superNouns.Add("name");
        superNouns.Add("school");
        superNouns.Add("top");
        superNouns.Add("amount");
        superNouns.Add("level");
        superNouns.Add("order");
        superNouns.Add("practice");
        superNouns.Add("research");
        superNouns.Add("sense");
        superNouns.Add("service");
        superNouns.Add("piece");
        superNouns.Add("web");
        superNouns.Add("boss");
        superNouns.Add("sport");
        superNouns.Add("fun");
        superNouns.Add("house");
        superNouns.Add("page");
        superNouns.Add("term");
        superNouns.Add("test");
        superNouns.Add("answer");
        superNouns.Add("sound");
        superNouns.Add("focus");
        superNouns.Add("matter");
        superNouns.Add("kind");
        superNouns.Add("soil");
        superNouns.Add("board");
        superNouns.Add("oil");
        superNouns.Add("picture");
        superNouns.Add("access");
        superNouns.Add("garden");
        superNouns.Add("range");
        superNouns.Add("rate");
        superNouns.Add("reason");
        superNouns.Add("future");
        superNouns.Add("site");
        superNouns.Add("demand");
        superNouns.Add("exercise");
        superNouns.Add("image");
        superNouns.Add("case");
        superNouns.Add("cause");
        superNouns.Add("coast");
        superNouns.Add("action");
        superNouns.Add("age");
        superNouns.Add("bad");
        superNouns.Add("boat");
        superNouns.Add("record");
        superNouns.Add("result");
        superNouns.Add("section");
        superNouns.Add("building");
        superNouns.Add("mouse");
        superNouns.Add("cash");
        superNouns.Add("class");
        superNouns.Add("nothing");
        superNouns.Add("period");
        superNouns.Add("plan");
        superNouns.Add("store");
        superNouns.Add("tax");
        superNouns.Add("side");
        superNouns.Add("subject");
        superNouns.Add("space");
        superNouns.Add("rule");
        superNouns.Add("stock");
        superNouns.Add("weather");
        superNouns.Add("chance");
        superNouns.Add("figure");
        superNouns.Add("man");
        superNouns.Add("model");
        superNouns.Add("source");
        superNouns.Add("beginning");
        superNouns.Add("earth");
        superNouns.Add("program");
        superNouns.Add("chicken");
        superNouns.Add("design");
        superNouns.Add("feature");
        superNouns.Add("head");
        superNouns.Add("material");
        superNouns.Add("purpose");
        superNouns.Add("question");
        superNouns.Add("rock");
        superNouns.Add("salt");
        superNouns.Add("act");
        superNouns.Add("birth");
        superNouns.Add("car");
        superNouns.Add("dog");
        superNouns.Add("object");
        superNouns.Add("scale");
        superNouns.Add("sun");
        superNouns.Add("note");
        superNouns.Add("profit");
        superNouns.Add("rent");
        superNouns.Add("speed");
        superNouns.Add("style");
        superNouns.Add("war");
        superNouns.Add("bank");
        superNouns.Add("craft");
        superNouns.Add("half");
        superNouns.Add("inside");
        superNouns.Add("outside");
        superNouns.Add("standard");
        superNouns.Add("bus");
        superNouns.Add("exchange");
        superNouns.Add("eye");
        superNouns.Add("fire");
        superNouns.Add("position");
        superNouns.Add("pressure");
        superNouns.Add("stress");
        superNouns.Add("advantage");
        superNouns.Add("benefit");
        superNouns.Add("box");
        superNouns.Add("frame");
        superNouns.Add("issue");
        superNouns.Add("step");
        superNouns.Add("cycle");
        superNouns.Add("face");
        superNouns.Add("item");
        superNouns.Add("metal");
        superNouns.Add("paint");
        superNouns.Add("review");
        superNouns.Add("room");
        superNouns.Add("screen");
        superNouns.Add("structure");
        superNouns.Add("view");
        superNouns.Add("account");
        superNouns.Add("ball");
        superNouns.Add("discipline");
        superNouns.Add("medium");
        superNouns.Add("share");
        superNouns.Add("balance");
        superNouns.Add("bit");
        superNouns.Add("black");
        superNouns.Add("bottom");
        superNouns.Add("choice");
        superNouns.Add("gift");
        superNouns.Add("impact");
        superNouns.Add("machine");
        superNouns.Add("shape");
        superNouns.Add("tool");
        superNouns.Add("wind");
        superNouns.Add("address");
        superNouns.Add("average");
        superNouns.Add("career");
        superNouns.Add("culture");
        superNouns.Add("morning");
        superNouns.Add("pot");
        superNouns.Add("sign");
        superNouns.Add("table");
        superNouns.Add("task");
        superNouns.Add("condition");
        superNouns.Add("contact");
        superNouns.Add("credit");
        superNouns.Add("egg");
        superNouns.Add("hope");
        superNouns.Add("ice");
        superNouns.Add("network");
        superNouns.Add("north");
        superNouns.Add("square");
        superNouns.Add("attempt");
        superNouns.Add("date");
        superNouns.Add("effect");
        superNouns.Add("link");
        superNouns.Add("post");
        superNouns.Add("star");
        superNouns.Add("voice");
        superNouns.Add("capital");
        superNouns.Add("challenge");
        superNouns.Add("friend");
        superNouns.Add("self");
        superNouns.Add("shot");
        superNouns.Add("brush");
        superNouns.Add("couple");
        superNouns.Add("debate");
        superNouns.Add("exit");
        superNouns.Add("front");
        superNouns.Add("function");
        superNouns.Add("lack");
        superNouns.Add("living");
        superNouns.Add("plant");
        superNouns.Add("plastic");
        superNouns.Add("spot");
        superNouns.Add("summer");
        superNouns.Add("taste");
        superNouns.Add("theme");
        superNouns.Add("track");
        superNouns.Add("wing");
        superNouns.Add("brain");
        superNouns.Add("button");
        superNouns.Add("click");
        superNouns.Add("desire");
        superNouns.Add("foot");
        superNouns.Add("gas");
        superNouns.Add("influence");
        superNouns.Add("notice");
        superNouns.Add("rain");
        superNouns.Add("wall");
        superNouns.Add("base");
        superNouns.Add("damage");
        superNouns.Add("distance");
        superNouns.Add("feeling");
        superNouns.Add("pair");
        superNouns.Add("savings");
        superNouns.Add("staff");
        superNouns.Add("sugar");
        superNouns.Add("target");
        superNouns.Add("text");
        superNouns.Add("animal");
        superNouns.Add("author");
        superNouns.Add("budget");
        superNouns.Add("discount");
        superNouns.Add("file");
        superNouns.Add("ground");
        superNouns.Add("lesson");
        superNouns.Add("minute");
        superNouns.Add("officer");
        superNouns.Add("phase");
        superNouns.Add("reference");
        superNouns.Add("register");
        superNouns.Add("sky");
        superNouns.Add("stage");
        superNouns.Add("stick");
        superNouns.Add("title");
        superNouns.Add("trouble");
        superNouns.Add("bowl");
        superNouns.Add("bridge");
        superNouns.Add("campaign");
        superNouns.Add("character");
        superNouns.Add("club");
        superNouns.Add("edge");
        superNouns.Add("evidence");
        superNouns.Add("fan");
        superNouns.Add("letter");
        superNouns.Add("lock");
        superNouns.Add("maximum");
        superNouns.Add("novel");
        superNouns.Add("option");
        superNouns.Add("pack");
        superNouns.Add("park");
        superNouns.Add("plenty");
        superNouns.Add("quarter");
        superNouns.Add("skin");
        superNouns.Add("sort");
        superNouns.Add("weight");
        superNouns.Add("baby");
        superNouns.Add("background");
        superNouns.Add("carry");
        superNouns.Add("dish");
        superNouns.Add("factor");
        superNouns.Add("fruit");
        superNouns.Add("glass");
        superNouns.Add("joint");
        superNouns.Add("master");
        superNouns.Add("muscle");
        superNouns.Add("red");
        superNouns.Add("strength");
        superNouns.Add("traffic");
        superNouns.Add("trip");
        superNouns.Add("vegetable");
        superNouns.Add("appeal");
        superNouns.Add("chart");
        superNouns.Add("gear");
        superNouns.Add("ideal");
        superNouns.Add("kitchen");
        superNouns.Add("land");
        superNouns.Add("log");
        superNouns.Add("mother");
        superNouns.Add("net");
        superNouns.Add("party");
        superNouns.Add("principle");
        superNouns.Add("relative");
        superNouns.Add("sale");
        superNouns.Add("season");
        superNouns.Add("signal");
        superNouns.Add("spirit");
        superNouns.Add("street");
        superNouns.Add("tree");
        superNouns.Add("wave");
        superNouns.Add("belt");
        superNouns.Add("bench");
        superNouns.Add("commission");
        superNouns.Add("copy");
        superNouns.Add("drop");
        superNouns.Add("minimum");
        superNouns.Add("path");
        superNouns.Add("progress");
        superNouns.Add("project");
        superNouns.Add("sea");
        superNouns.Add("south");
        superNouns.Add("status");
        superNouns.Add("stuff");
        superNouns.Add("ticket");
        superNouns.Add("tour");
        superNouns.Add("angle");
        superNouns.Add("blue");
        superNouns.Add("breakfast");
        superNouns.Add("confidence");
        superNouns.Add("daughter");
        superNouns.Add("degree");
        superNouns.Add("doctor");
        superNouns.Add("dot");
        superNouns.Add("dream");
        superNouns.Add("duty");
        superNouns.Add("essay");
        superNouns.Add("father");
        superNouns.Add("fee");
        superNouns.Add("finance");
        superNouns.Add("hour");
        superNouns.Add("juice");
        superNouns.Add("limit");
        superNouns.Add("luck");
        superNouns.Add("milk");
        superNouns.Add("mouth");
        superNouns.Add("peace");
        superNouns.Add("pipe");
        superNouns.Add("seat");
        superNouns.Add("stable");
        superNouns.Add("storm");
        superNouns.Add("substance");
        superNouns.Add("team");
        superNouns.Add("trick");
        superNouns.Add("afternoon");
        superNouns.Add("bat");
        superNouns.Add("beach");
        superNouns.Add("blank");
        superNouns.Add("catch");
        superNouns.Add("chain");
        superNouns.Add("consideration");
        superNouns.Add("cream");
        superNouns.Add("crew");
        superNouns.Add("detail");
        superNouns.Add("gold");
        superNouns.Add("interview");
        superNouns.Add("kid");
        superNouns.Add("mark");
        superNouns.Add("match");
        superNouns.Add("mission");
        superNouns.Add("pain");
        superNouns.Add("pleasure");
        superNouns.Add("score");
        superNouns.Add("screw");
        superNouns.Add("sex");
        superNouns.Add("shop");
        superNouns.Add("shower");
        superNouns.Add("suit");
        superNouns.Add("tone");
        superNouns.Add("window");
        superNouns.Add("agent");
        superNouns.Add("band");
        superNouns.Add("block");
        superNouns.Add("bone");
        superNouns.Add("calendar");
        superNouns.Add("cap");
        superNouns.Add("coat");
        superNouns.Add("contest");
        superNouns.Add("corner");
        superNouns.Add("court");
        superNouns.Add("cup");
        superNouns.Add("district");
        superNouns.Add("door");
        superNouns.Add("east");
        superNouns.Add("finger");
        superNouns.Add("garage");
        superNouns.Add("guarantee");
        superNouns.Add("hole");
        superNouns.Add("hook");
        superNouns.Add("implement");
        superNouns.Add("layer");
        superNouns.Add("lecture");
        superNouns.Add("lie");
        superNouns.Add("manner");
        superNouns.Add("meeting");
        superNouns.Add("nose");
        superNouns.Add("parking");
        superNouns.Add("partner");
        superNouns.Add("profile");
        superNouns.Add("respect");
        superNouns.Add("rice");
        superNouns.Add("routine");
        superNouns.Add("schedule");
        superNouns.Add("swimming");
        superNouns.Add("telephone");
        superNouns.Add("tip");
        superNouns.Add("winter");
        superNouns.Add("airline");
        superNouns.Add("bag");
        superNouns.Add("battle");
        superNouns.Add("bed");
        superNouns.Add("bill");
        superNouns.Add("bother");
        superNouns.Add("cake");
        superNouns.Add("code");
        superNouns.Add("curve");
        superNouns.Add("designer");
        superNouns.Add("dimension");
        superNouns.Add("dress");
        superNouns.Add("ease");
        superNouns.Add("emergency");
        superNouns.Add("evening");
        superNouns.Add("extension");
        superNouns.Add("farm");
        superNouns.Add("fight");
        superNouns.Add("gap");
        superNouns.Add("grade");
        superNouns.Add("holiday");
        superNouns.Add("horror");
        superNouns.Add("horse");
        superNouns.Add("host");
        superNouns.Add("husband");
        superNouns.Add("loan");
        superNouns.Add("mistake");
        superNouns.Add("mountain");
        superNouns.Add("nail");
        superNouns.Add("noise");
        superNouns.Add("occasion");
        superNouns.Add("package");
        superNouns.Add("patient");
        superNouns.Add("pause");
        superNouns.Add("phrase");
        superNouns.Add("proof");
        superNouns.Add("race");
        superNouns.Add("relief");
        superNouns.Add("sand");
        superNouns.Add("sentence");
        superNouns.Add("shoulder");
        superNouns.Add("smoke");
        superNouns.Add("stomach");
        superNouns.Add("string");
        superNouns.Add("tourist");
        superNouns.Add("towel");
        superNouns.Add("vacation");
        superNouns.Add("west");
        superNouns.Add("wheel");
        superNouns.Add("wine");
        superNouns.Add("arm");
        superNouns.Add("aside");
        superNouns.Add("associate");
        superNouns.Add("bet");
        superNouns.Add("blow");
        superNouns.Add("border");
        superNouns.Add("branch");
        superNouns.Add("breast");
        superNouns.Add("brother");
        superNouns.Add("buddy");
        superNouns.Add("bunch");
        superNouns.Add("chip");
        superNouns.Add("coach");
        superNouns.Add("cross");
        superNouns.Add("document");
        superNouns.Add("draft");
        superNouns.Add("dust");
        superNouns.Add("expert");
        superNouns.Add("floor");
        superNouns.Add("god");
        superNouns.Add("golf");
        superNouns.Add("habit");
        superNouns.Add("iron");
        superNouns.Add("judge");
        superNouns.Add("knife");
        superNouns.Add("landscape");
        superNouns.Add("league");
        superNouns.Add("mail");
        superNouns.Add("mess");
        superNouns.Add("native");
        superNouns.Add("opening");
        superNouns.Add("parent");
        superNouns.Add("pattern");
        superNouns.Add("pin");
        superNouns.Add("pool");
        superNouns.Add("pound");
        superNouns.Add("request");
        superNouns.Add("salary");
        superNouns.Add("shame");
        superNouns.Add("shelter");
        superNouns.Add("shoe");
        superNouns.Add("silver");
        superNouns.Add("tackle");
        superNouns.Add("tank");
        superNouns.Add("trust");
        superNouns.Add("assist");
        superNouns.Add("bake");
        superNouns.Add("bar");
        superNouns.Add("bell");
        superNouns.Add("bike");
        superNouns.Add("blame");
        superNouns.Add("boy");
        superNouns.Add("brick");
        superNouns.Add("chair");
        superNouns.Add("closet");
        superNouns.Add("clue");
        superNouns.Add("collar");
        superNouns.Add("comment");
        superNouns.Add("conference");
        superNouns.Add("devil");
        superNouns.Add("diet");
        superNouns.Add("fear");
        superNouns.Add("fuel");
        superNouns.Add("glove");
        superNouns.Add("jacket");
        superNouns.Add("lunch");
        superNouns.Add("monitor");
        superNouns.Add("mortgage");
        superNouns.Add("nurse");
        superNouns.Add("pace");
        superNouns.Add("panic");
        superNouns.Add("peak");
        superNouns.Add("plane");
        superNouns.Add("reward");
        superNouns.Add("row");
        superNouns.Add("sandwich");
        superNouns.Add("shock");
        superNouns.Add("spite");
        superNouns.Add("spray");
        superNouns.Add("surprise");
        superNouns.Add("till");
        superNouns.Add("transition");
        superNouns.Add("weekend");
        superNouns.Add("welcome");
        superNouns.Add("yard");
        superNouns.Add("alarm");
        superNouns.Add("bend");
        superNouns.Add("bicycle");
        superNouns.Add("bite");
        superNouns.Add("blind");
        superNouns.Add("bottle");
        superNouns.Add("cable");
        superNouns.Add("candle");
        superNouns.Add("clerk");
        superNouns.Add("cloud");
        superNouns.Add("concert");
        superNouns.Add("counter");
        superNouns.Add("flower");
        superNouns.Add("grandfather");
        superNouns.Add("harm");
        superNouns.Add("knee");
        superNouns.Add("lawyer");
        superNouns.Add("leather");
        superNouns.Add("load");
        superNouns.Add("mirror");
        superNouns.Add("neck");
        superNouns.Add("pension");
        superNouns.Add("plate");
        superNouns.Add("purple");
        superNouns.Add("ruin");
        superNouns.Add("ship");
        superNouns.Add("skirt");
        superNouns.Add("slice");
        superNouns.Add("snow");
        superNouns.Add("specialist");
        superNouns.Add("stroke");
        superNouns.Add("switch");
        superNouns.Add("trash");
        superNouns.Add("tune");
        superNouns.Add("zone");
        superNouns.Add("anger");
        superNouns.Add("award");
        superNouns.Add("bid");
        superNouns.Add("bitter");
        superNouns.Add("boot");
        superNouns.Add("bug");
        superNouns.Add("camp");
        superNouns.Add("candy");
        superNouns.Add("carpet");
        superNouns.Add("cat");
        superNouns.Add("champion");
        superNouns.Add("channel");
        superNouns.Add("clock");
        superNouns.Add("comfort");
        superNouns.Add("cow");
        superNouns.Add("crack");
        superNouns.Add("engineer");
        superNouns.Add("entrance");
        superNouns.Add("fault");
        superNouns.Add("grass");
        superNouns.Add("guy");
        superNouns.Add("hell");
        superNouns.Add("highlight");
        superNouns.Add("incident");
        superNouns.Add("island");
        superNouns.Add("joke");
        superNouns.Add("jury");
        superNouns.Add("leg");
        superNouns.Add("lip");
        superNouns.Add("mate");
        superNouns.Add("motor");
        superNouns.Add("nerve");
        superNouns.Add("passage");
        superNouns.Add("pen");
        superNouns.Add("pride");
        superNouns.Add("priest");
        superNouns.Add("prize");
        superNouns.Add("promise");
        superNouns.Add("resident");
        superNouns.Add("resort");
        superNouns.Add("ring");
        superNouns.Add("roof");
        superNouns.Add("rope");
        superNouns.Add("sail");
        superNouns.Add("scheme");
        superNouns.Add("script");
        superNouns.Add("sock");
        superNouns.Add("station");
        superNouns.Add("toe");
        superNouns.Add("tower");
        superNouns.Add("truck");
        superNouns.Add("witness");
        superNouns.Add("a");
        superNouns.Add("you");
        superNouns.Add("it");
        superNouns.Add("can");
        superNouns.Add("will");
        superNouns.Add("if");
        superNouns.Add("one");
        superNouns.Add("many");
        superNouns.Add("most");
        superNouns.Add("other");
        superNouns.Add("use");
        superNouns.Add("make");
        superNouns.Add("good");
        superNouns.Add("look");
        superNouns.Add("help");
        superNouns.Add("go");
        superNouns.Add("great");
        superNouns.Add("being");
        superNouns.Add("few");
        superNouns.Add("might");
        superNouns.Add("still");
        superNouns.Add("public");
        superNouns.Add("read");
        superNouns.Add("keep");
        superNouns.Add("start");
        superNouns.Add("give");
        superNouns.Add("human");
        superNouns.Add("local");
        superNouns.Add("general");
        superNouns.Add("she");
        superNouns.Add("specific");
        superNouns.Add("long");
        superNouns.Add("play");
        superNouns.Add("feel");
        superNouns.Add("high");
        superNouns.Add("tonight");
        superNouns.Add("put");
        superNouns.Add("common");
        superNouns.Add("set");
        superNouns.Add("change");
        superNouns.Add("simple");
        superNouns.Add("past");
        superNouns.Add("big");
        superNouns.Add("possible");
        superNouns.Add("particular");
        superNouns.Add("today");
        superNouns.Add("major");
        superNouns.Add("personal");
        superNouns.Add("current");
        superNouns.Add("national");
        superNouns.Add("cut");
        superNouns.Add("natural");
        superNouns.Add("physical");
        superNouns.Add("show");
        superNouns.Add("try");
        superNouns.Add("check");
        superNouns.Add("second");
        superNouns.Add("call");
        superNouns.Add("move");
        superNouns.Add("pay");
        superNouns.Add("let");
        superNouns.Add("increase");
        superNouns.Add("single");
        superNouns.Add("individual");
        superNouns.Add("turn");
        superNouns.Add("ask");
        superNouns.Add("buy");
        superNouns.Add("guard");
        superNouns.Add("hold");
        superNouns.Add("main");
        superNouns.Add("offer");
        superNouns.Add("potential");
        superNouns.Add("professional");
        superNouns.Add("international");
        superNouns.Add("travel");
        superNouns.Add("cook");
        superNouns.Add("alternative");
        superNouns.Add("following");
        superNouns.Add("special");
        superNouns.Add("working");
        superNouns.Add("whole");
        superNouns.Add("dance");
        superNouns.Add("excuse");
        superNouns.Add("cold");
        superNouns.Add("commercial");
        superNouns.Add("low");
        superNouns.Add("purchase");
        superNouns.Add("deal");
        superNouns.Add("primary");
        superNouns.Add("worth");
        superNouns.Add("fall");
        superNouns.Add("necessary");
        superNouns.Add("positive");
        superNouns.Add("produce");
        superNouns.Add("search");
        superNouns.Add("present");
        superNouns.Add("spend");
        superNouns.Add("talk");
        superNouns.Add("creative");
        superNouns.Add("tell");
        superNouns.Add("cost");
        superNouns.Add("drive");
        superNouns.Add("green");
        superNouns.Add("support");
        superNouns.Add("glad");
        superNouns.Add("remove");
        superNouns.Add("return");
        superNouns.Add("run");
        superNouns.Add("complex");
        superNouns.Add("due");
        superNouns.Add("effective");
        superNouns.Add("middle");
        superNouns.Add("regular");
        superNouns.Add("reserve");
        superNouns.Add("independent");
        superNouns.Add("leave");
        superNouns.Add("original");
        superNouns.Add("reach");
        superNouns.Add("rest");
        superNouns.Add("serve");
        superNouns.Add("watch");
        superNouns.Add("beautiful");
        superNouns.Add("charge");
        superNouns.Add("active");
        superNouns.Add("break");
        superNouns.Add("negative");
        superNouns.Add("safe");
        superNouns.Add("stay");
        superNouns.Add("visit");
        superNouns.Add("visual");
        superNouns.Add("affect");
        superNouns.Add("cover");
        superNouns.Add("report");
        superNouns.Add("rise");
        superNouns.Add("walk");
        superNouns.Add("white");
        superNouns.Add("beyond");
        superNouns.Add("junior");
        superNouns.Add("pick");
        superNouns.Add("unique");
        superNouns.Add("anything");
        superNouns.Add("classic");
        superNouns.Add("final");
        superNouns.Add("lift");
        superNouns.Add("mix");
        superNouns.Add("private");
        superNouns.Add("stop");
        superNouns.Add("teach");
        superNouns.Add("western");
        superNouns.Add("concern");
        superNouns.Add("familiar");
        superNouns.Add("fly");
        superNouns.Add("official");
        superNouns.Add("broad");
        superNouns.Add("comfortable");
        superNouns.Add("gain");
        superNouns.Add("maybe");
        superNouns.Add("rich");
        superNouns.Add("save");
        superNouns.Add("stand");
        superNouns.Add("young");
        superNouns.Add("fail");
        superNouns.Add("heavy");
        superNouns.Add("hello");
        superNouns.Add("lead");
        superNouns.Add("listen");
        superNouns.Add("valuable");
        superNouns.Add("worry");
        superNouns.Add("handle");
        superNouns.Add("leading");
        superNouns.Add("meet");
        superNouns.Add("release");
        superNouns.Add("sell");
        superNouns.Add("finish");
        superNouns.Add("normal");
        superNouns.Add("press");
        superNouns.Add("ride");
        superNouns.Add("secret");
        superNouns.Add("spread");
        superNouns.Add("spring");
        superNouns.Add("tough");
        superNouns.Add("wait");
        superNouns.Add("brown");
        superNouns.Add("deep");
        superNouns.Add("display");
        superNouns.Add("flow");
        superNouns.Add("hit");
        superNouns.Add("objective");
        superNouns.Add("shoot");
        superNouns.Add("touch");
        superNouns.Add("cancel");
        superNouns.Add("chemical");
        superNouns.Add("cry");
        superNouns.Add("dump");
        superNouns.Add("extreme");
        superNouns.Add("push");
        superNouns.Add("conflict");
        superNouns.Add("eat");
        superNouns.Add("fill");
        superNouns.Add("formal");
        superNouns.Add("jump");
        superNouns.Add("kick");
        superNouns.Add("opposite");
        superNouns.Add("pass");
        superNouns.Add("pitch");
        superNouns.Add("remote");
        superNouns.Add("total");
        superNouns.Add("treat");
        superNouns.Add("vast");
        superNouns.Add("abuse");
        superNouns.Add("beat");
        superNouns.Add("burn");
        superNouns.Add("deposit");
        superNouns.Add("print");
        superNouns.Add("raise");
        superNouns.Add("sleep");
        superNouns.Add("somewhere");
        superNouns.Add("advance");
        superNouns.Add("anywhere");
        superNouns.Add("consist");
        superNouns.Add("dark");
        superNouns.Add("double");
        superNouns.Add("draw");
        superNouns.Add("equal");
        superNouns.Add("fix");
        superNouns.Add("hire");
        superNouns.Add("internal");
        superNouns.Add("join");
        superNouns.Add("kill");
        superNouns.Add("sensitive");
        superNouns.Add("tap");
        superNouns.Add("win");
        superNouns.Add("attack");
        superNouns.Add("claim");
        superNouns.Add("constant");
        superNouns.Add("drag");
        superNouns.Add("drink");
        superNouns.Add("guess");
        superNouns.Add("minor");
        superNouns.Add("pull");
        superNouns.Add("raw");
        superNouns.Add("soft");
        superNouns.Add("solid");
        superNouns.Add("wear");
        superNouns.Add("weird");
        superNouns.Add("wonder");
        superNouns.Add("annual");
        superNouns.Add("count");
        superNouns.Add("dead");
        superNouns.Add("doubt");
        superNouns.Add("feed");
        superNouns.Add("forever");
        superNouns.Add("impress");
        superNouns.Add("nobody");
        superNouns.Add("repeat");
        superNouns.Add("round");
        superNouns.Add("sing");
        superNouns.Add("slide");
        superNouns.Add("strip");
        superNouns.Add("whereas");
        superNouns.Add("wish");
        superNouns.Add("combine");
        superNouns.Add("command");
        superNouns.Add("dig");
        superNouns.Add("divide");
        superNouns.Add("equivalent");
        superNouns.Add("hang");
        superNouns.Add("hunt");
        superNouns.Add("initial");
        superNouns.Add("march");
        superNouns.Add("mention");
        superNouns.Add("smell");
        superNouns.Add("spiritual");
        superNouns.Add("survey");
        superNouns.Add("tie");
        superNouns.Add("adult");
        superNouns.Add("brief");
        superNouns.Add("crazy");
        superNouns.Add("escape");
        superNouns.Add("gather");
        superNouns.Add("hate");
        superNouns.Add("prior");
        superNouns.Add("repair");
        superNouns.Add("rough");
        superNouns.Add("sad");
        superNouns.Add("scratch");
        superNouns.Add("sick");
        superNouns.Add("strike");
        superNouns.Add("employ");
        superNouns.Add("external");
        superNouns.Add("hurt");
        superNouns.Add("illegal");
        superNouns.Add("laugh");
        superNouns.Add("lay");
        superNouns.Add("mobile");
        superNouns.Add("nasty");
        superNouns.Add("ordinary");
        superNouns.Add("respond");
        superNouns.Add("royal");
        superNouns.Add("senior");
        superNouns.Add("split");
        superNouns.Add("strain");
        superNouns.Add("struggle");
        superNouns.Add("swim");
        superNouns.Add("train");
        superNouns.Add("upper");
        superNouns.Add("wash");
        superNouns.Add("yellow");
        superNouns.Add("convert");
        superNouns.Add("crash");
        superNouns.Add("dependent");
        superNouns.Add("fold");
        superNouns.Add("funny");
        superNouns.Add("grab");
        superNouns.Add("hide");
        superNouns.Add("miss");
        superNouns.Add("permit");
        superNouns.Add("quote");
        superNouns.Add("recover");
        superNouns.Add("resolve");
        superNouns.Add("roll");
        superNouns.Add("sink");
        superNouns.Add("slip");
        superNouns.Add("spare");
        superNouns.Add("suspect");
        superNouns.Add("sweet");
        superNouns.Add("swing");
        superNouns.Add("twist");
        superNouns.Add("upstairs");
        superNouns.Add("usual");
        superNouns.Add("abroad");
        superNouns.Add("brave");
        superNouns.Add("calm");
        superNouns.Add("concentrate");
        superNouns.Add("estimate");
        superNouns.Add("grand");
        superNouns.Add("male");
        superNouns.Add("mine");
        superNouns.Add("prompt");
        superNouns.Add("quiet");
        superNouns.Add("refuse");
        superNouns.Add("regret");
        superNouns.Add("reveal");
        superNouns.Add("rush");
        superNouns.Add("shake");
        superNouns.Add("shift");
        superNouns.Add("shine");
        superNouns.Add("steal");
        superNouns.Add("suck");
        superNouns.Add("surround");
        superNouns.Add("anybody");
        superNouns.Add("bear");
        superNouns.Add("brilliant");
        superNouns.Add("dare");
        superNouns.Add("dear");
        superNouns.Add("delay");
        superNouns.Add("drunk");
        superNouns.Add("female");
        superNouns.Add("hurry");
        superNouns.Add("inevitable");
        superNouns.Add("invite");
        superNouns.Add("kiss");
        superNouns.Add("neat");
        superNouns.Add("pop");
        superNouns.Add("punch");
        superNouns.Add("quit");
        superNouns.Add("reply");
        superNouns.Add("representative");
        superNouns.Add("resist");
        superNouns.Add("rip");
        superNouns.Add("rub");
        superNouns.Add("silly");
        superNouns.Add("smile");
        superNouns.Add("spell");
        superNouns.Add("stretch");
        superNouns.Add("stupid");
        superNouns.Add("tear");
        superNouns.Add("temporary");
        superNouns.Add("tomorrow");
        superNouns.Add("wake");
        superNouns.Add("wrap");
        superNouns.Add("yesterday");
    }
}
