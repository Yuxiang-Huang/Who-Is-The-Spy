using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Vote : MonoBehaviour
{
    public PhotonView PV;
    public Photon.Realtime.Player player;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
        player = PhotonNetwork.LocalPlayer;
    }

    public void voteMe()
    {
        PV.RPC(nameof(castVoteSpy), RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void castVoteSpy()
    {
        //Debug.Log(player);
        //Debug.Log(VotingManager.Instance.spyVotes);

        VotingManager.Instance.spyVotes[player]++;
        string str = "";

        foreach (var (key, value) in VotingManager.Instance.spyVotes)
        {
            str += value + " : ";
        }

        VotingManager.Instance.messageText.text = str;
    }
}
