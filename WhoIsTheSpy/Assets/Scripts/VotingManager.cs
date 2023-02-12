using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VotingManager : MonoBehaviour
{
    public static VotingManager Instance;

    public TextMeshProUGUI messageText;

    public List<PlayerController> allPlayers = new List<PlayerController>();

    public int agreeVotes;
    public int disagreeVotes;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }
}
