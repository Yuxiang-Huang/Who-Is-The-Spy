using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RevealText : MonoBehaviour
{
    public TextMeshProUGUI text;

    public static RevealText Instance;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }
}
