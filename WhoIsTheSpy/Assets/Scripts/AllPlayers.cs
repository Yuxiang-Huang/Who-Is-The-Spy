using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllPlayers : MonoBehaviour
{
    public static AllPlayers Instance;

    public List<PlayerController> allPlayers = new List<PlayerController>();

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
