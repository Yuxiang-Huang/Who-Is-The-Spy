using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AllMessage : MonoBehaviour
{
    public TextMeshProUGUI text;

    public static AllMessage Instance;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }
}
